using BizzyQCU.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web.Mvc;

namespace BizzyQCU.Controllers
{
    public class TrackingController : Controller
    {
        private static bool orderStatusColumnChecked;

        private void EnsureOrderStatusColumn(MySqlConnection conn)
        {
            if (orderStatusColumnChecked)
            {
                return;
            }

            string sql = @"ALTER TABLE orders MODIFY status ENUM('pending','preparing','out_for_delivery','completed','cancelled') DEFAULT 'pending'";
            using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.ExecuteNonQuery();
            }

            orderStatusColumnChecked = true;
        }

        public ActionResult Tracking(int orderId)
        {
            if (Session["UserId"] == null) { return RedirectToAction("Login", "Home"); }

            OrderTrackingViewModel model = null;
            string connStr = ConfigurationManager.ConnectionStrings["BizzyQCUConnection"].ConnectionString;

            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();
                EnsureOrderStatusColumn(conn);

                string sql = @"
                    SELECT o.order_id, o.status, o.total_amount, o.delivery_fee,
                           o.delivery_option, o.customer_location, o.order_note,
                           o.order_date, TIME_FORMAT(o.estimated_time, '%h:%i %p') AS estimated_time_formatted, e.store_name
                    FROM orders o
                    INNER JOIN enterprises e ON o.enterprise_id = e.enterprise_id
                    INNER JOIN students s ON s.student_id = o.student_id
                    WHERE o.order_id = @orderId
                      AND s.user_id = @userId";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@orderId", orderId);
                    cmd.Parameters.AddWithValue("@userId", (int)Session["UserId"]);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model = new OrderTrackingViewModel
                            {
                                OrderId = reader.GetInt32("order_id"),
                                Status = reader.IsDBNull(reader.GetOrdinal("status")) ? "preparing" : reader.GetString("status"),
                                TotalAmount = reader.GetDecimal("total_amount"),
                                DeliveryFee = reader.GetDecimal("delivery_fee"),
                                DeliveryOption = reader.GetString("delivery_option"),
                                CustomerLocation = reader.IsDBNull(reader.GetOrdinal("customer_location")) ? "" : reader.GetString("customer_location"),
                                OrderNote = reader.IsDBNull(reader.GetOrdinal("order_note")) ? "" : reader.GetString("order_note"),
                                OrderDate = reader.GetDateTime("order_date"),
                                EstimatedTime = reader.IsDBNull(reader.GetOrdinal("estimated_time_formatted")) ? "TBD" : reader.GetString("estimated_time_formatted"),
                                StoreName = reader.GetString("store_name"),
                                Items = new List<OrderTrackingItem>()
                            };
                        }
                    }
                }

                if (model == null) { return HttpNotFound(); }

                string itemsSql = @"
                    SELECT p.product_name, oi.quantity, oi.unit_price
                    FROM order_items oi
                    INNER JOIN products p ON oi.product_id = p.product_id
                    WHERE oi.order_id = @orderId";

                using (var cmd2 = new MySqlCommand(itemsSql, conn))
                {
                    cmd2.Parameters.AddWithValue("@orderId", orderId);
                    using (var reader2 = cmd2.ExecuteReader())
                    {
                        while (reader2.Read())
                        {
                            model.Items.Add(new OrderTrackingItem
                            {
                                ProductName = reader2.GetString("product_name"),
                                Quantity = reader2.GetInt32("quantity"),
                                UnitPrice = reader2.GetDecimal("unit_price")
                            });
                        }
                    }
                }
            }

            return View("Tracking", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult CancelOrder(int orderId)
        {
            if (Session["UserId"] == null)
            {
                return Json(new { success = false, message = "Please log in first." });
            }

            int userId = (int)Session["UserId"];
            string role = Session["Role"]?.ToString();
            if (role != "student")
            {
                return Json(new { success = false, message = "Only students can cancel their orders." });
            }

            string connStr = ConfigurationManager.ConnectionStrings["BizzyQCUConnection"].ConnectionString;
            var cancellableStatuses = new[] { "pending", "preparing", "out_for_delivery" };

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    EnsureOrderStatusColumn(conn);

                    using (var tx = conn.BeginTransaction())
                    {
                        int enterpriseId;
                        decimal totalAmount;
                        string status;

                        string lookupSql = @"
                            SELECT o.enterprise_id, o.total_amount, o.status
                            FROM orders o
                            INNER JOIN students s ON s.student_id = o.student_id
                            WHERE o.order_id = @orderId
                              AND s.user_id = @userId
                            FOR UPDATE";

                        using (var cmd = new MySqlCommand(lookupSql, conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@orderId", orderId);
                            cmd.Parameters.AddWithValue("@userId", userId);

                            using (var reader = cmd.ExecuteReader())
                            {
                                if (!reader.Read())
                                {
                                    tx.Rollback();
                                    return Json(new { success = false, message = "Order not found." });
                                }

                                enterpriseId = reader.GetInt32("enterprise_id");
                                totalAmount = reader.GetDecimal("total_amount");
                                status = reader.IsDBNull(reader.GetOrdinal("status")) ? "" : reader.GetString("status").ToLowerInvariant();
                            }
                        }

                        if (!cancellableStatuses.Contains(status))
                        {
                            tx.Rollback();
                            return Json(new { success = false, message = "This order can no longer be cancelled." });
                        }

                        string updateSql = "UPDATE orders SET status = 'cancelled' WHERE order_id = @orderId";
                        using (var cmd = new MySqlCommand(updateSql, conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@orderId", orderId);
                            cmd.ExecuteNonQuery();
                        }

                        string historySql = @"
                            INSERT INTO transaction_history (enterprise_id, order_id, amount, transaction_type)
                            SELECT @enterpriseId, @orderId, @amount, 'refund'
                            FROM DUAL
                            WHERE NOT EXISTS (
                                SELECT 1 FROM transaction_history
                                WHERE order_id = @orderId AND transaction_type = 'refund'
                            )";

                        using (var cmd = new MySqlCommand(historySql, conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@enterpriseId", enterpriseId);
                            cmd.Parameters.AddWithValue("@orderId", orderId);
                            cmd.Parameters.AddWithValue("@amount", totalAmount);
                            cmd.ExecuteNonQuery();
                        }

                        tx.Commit();
                    }
                }

                return Json(new { success = true, message = "Order cancelled successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Cancel failed: " + ex.Message });
            }
        }

        public ActionResult History()
        {
            if (Session["UserId"] == null) { return RedirectToAction("Login", "Home"); }

            int userId = (int)Session["UserId"];
            var orders = new List<OrderHistoryItem>();
            string connStr = ConfigurationManager.ConnectionStrings["BizzyQCUConnection"].ConnectionString;

            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();
                EnsureOrderStatusColumn(conn);
                string sql = @"
                    SELECT o.order_id, o.status, o.total_amount, o.delivery_option,
                           o.order_date, e.store_name,
                           GROUP_CONCAT(CONCAT(oi.quantity, 'x ', p.product_name)
                               ORDER BY p.product_name SEPARATOR ', ') AS items_summary
                    FROM orders o
                    INNER JOIN enterprises e ON o.enterprise_id = e.enterprise_id
                    INNER JOIN students s ON o.student_id = s.student_id
                    LEFT JOIN order_items oi ON o.order_id = oi.order_id
                    LEFT JOIN products p ON oi.product_id = p.product_id
                    WHERE s.user_id = @userId
                    GROUP BY o.order_id, o.status, o.total_amount,
                             o.delivery_option, o.order_date, e.store_name
                    ORDER BY o.order_date DESC";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            orders.Add(new OrderHistoryItem
                            {
                                OrderId = reader.GetInt32("order_id"),
                                Status = reader.IsDBNull(reader.GetOrdinal("status")) ? "preparing" : reader.GetString("status"),
                                TotalAmount = reader.GetDecimal("total_amount"),
                                DeliveryOption = reader.GetString("delivery_option"),
                                OrderDate = reader.GetDateTime("order_date"),
                                StoreName = reader.GetString("store_name"),
                                ItemsSummary = reader.IsDBNull(reader.GetOrdinal("items_summary")) ? "" : reader.GetString("items_summary")
                            });
                        }
                    }
                }
            }

            return View(orders);
        }
    }

    public class OrderHistoryItem
    {
        public int OrderId { get; set; }
        public string Status { get; set; }
        public decimal TotalAmount { get; set; }
        public string DeliveryOption { get; set; }
        public DateTime OrderDate { get; set; }
        public string StoreName { get; set; }
        public string ItemsSummary { get; set; }
    }
}
