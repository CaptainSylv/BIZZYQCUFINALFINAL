using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using MySql.Data.MySqlClient;

namespace BizzyQCU.Controllers
{
    public class CheckoutController : Controller
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

        // GET: Show the checkout page
        public ActionResult Checkout()
        {
            return View("CheckoutPage");
        }

        // Backward-compatible redirect for old links/bookmarks
        public ActionResult CheckoutPage()
        {
            return RedirectToAction("Checkout");
        }

        // POST: Place the order — called via AJAX from Checkout.js
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PlaceOrder( 
            int enterpriseId,
            decimal totalAmount,
            string paymentMethod,
            string deliveryOption,
            string customerLocation,
            string orderNote,
            decimal deliveryFee,
            string itemsJson)   // JSON array: [{ productId, quantity, unitPrice }, ...]
        {
            // Must be logged in as a student
            if (Session["UserId"] == null)
            {
                return Json(new { success = false, message = "Please log in to place an order." });
            }

            int userId = (int)Session["UserId"];
            string connStr = ConfigurationManager.ConnectionStrings["BizzyQCUConnection"].ConnectionString;

            try
            {
                // Parse items JSON — simple manual parse kept C#5-compatible
                var items = Newtonsoft.Json.JsonConvert.DeserializeObject<CartItem[]>(itemsJson);
                    if (items == null || items.Length == 0)
                    {
                        return Json(new { success = false, message = "Your cart is empty." });
                    }

                    foreach (var item in items)
                    {
                        if (item.ProductId <= 0 || item.Quantity <= 0)
                        {
                            return Json(new { success = false, message = "Invalid cart item found." });
                        }
                    }

                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    EnsureOrderStatusColumn(conn);

                    // Look up the student_id for this user
                    int studentId = 0;
                    string studentSql = "SELECT student_id FROM students WHERE user_id = @uid LIMIT 1";
                    using (var cmd = new MySqlCommand(studentSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);
                        var result = cmd.ExecuteScalar();
                        if (result == null)
                        {
                            return Json(new { success = false, message = "Student profile not found." });
                        }
                        studentId = Convert.ToInt32(result);
                    }

                    using (var tx = conn.BeginTransaction())
                    {
                        try
                        {
                            string validateItemSql = "SELECT COUNT(*) FROM products WHERE product_id = @productId AND enterprise_id = @enterpriseId";
                            foreach (var item in items)
                            {
                                using (var cmd = new MySqlCommand(validateItemSql, conn, tx))
                                {
                                    cmd.Parameters.AddWithValue("@productId", item.ProductId);
                                    cmd.Parameters.AddWithValue("@enterpriseId", enterpriseId);
                                    if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                                    {
                                        tx.Rollback();
                                        return Json(new { success = false, message = "Your cart contains an item from a different enterprise. Please refresh your cart and try again." });
                                    }
                                }
                            }

                            // Insert the order
                            string orderSql = @"
                                INSERT INTO orders
                                    (student_id, enterprise_id, total_amount, status,
                                     payment_method, delivery_option, customer_location,
                                     order_note, delivery_fee)
                                VALUES
                                    (@studentId, @enterpriseId, @totalAmount, 'pending',
                                     @paymentMethod, @deliveryOption, @customerLocation,
                                     @orderNote, @deliveryFee);
                                SELECT LAST_INSERT_ID();";

                            int newOrderId = 0;
                            using (var cmd = new MySqlCommand(orderSql, conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@studentId", studentId);
                                cmd.Parameters.AddWithValue("@enterpriseId", enterpriseId);
                                cmd.Parameters.AddWithValue("@totalAmount", totalAmount);
                                cmd.Parameters.AddWithValue("@paymentMethod", paymentMethod ?? "cash");
                                cmd.Parameters.AddWithValue("@deliveryOption", deliveryOption ?? "pickup");
                                cmd.Parameters.AddWithValue("@customerLocation", customerLocation ?? "");
                                cmd.Parameters.AddWithValue("@orderNote", orderNote ?? "");
                                cmd.Parameters.AddWithValue("@deliveryFee", deliveryFee);
                                newOrderId = Convert.ToInt32(cmd.ExecuteScalar());
                            }

                            // Insert order items
                            string itemSql = @"
                                INSERT INTO order_items (order_id, product_id, quantity, unit_price)
                                VALUES (@orderId, @productId, @qty, @unitPrice)";

                            foreach (var item in items)
                            {
                                using (var cmd = new MySqlCommand(itemSql, conn, tx))
                                {
                                    cmd.Parameters.AddWithValue("@orderId", newOrderId);
                                    cmd.Parameters.AddWithValue("@productId", item.ProductId);
                                    cmd.Parameters.AddWithValue("@qty", item.Quantity);
                                    cmd.Parameters.AddWithValue("@unitPrice", item.UnitPrice);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            tx.Commit();
                            return Json(new { success = true, orderId = newOrderId });
                        }
                        catch
                        {
                            tx.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Order failed: " + ex.Message });
            }
        }
        public ActionResult Reorder(int orderId)
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");

            int userId = (int)Session["UserId"];
            string connStr = ConfigurationManager.ConnectionStrings["BizzyQCUConnection"].ConnectionString;

            // This list will hold the items to pass to the View
            var reorderItems = new System.Collections.Generic.List<object>();

            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();
                // Fetch product details along with order items so the cart has names/prices
                string sql = @"
            SELECT oi.product_id, oi.quantity, oi.unit_price, 
                   p.product_name, e.enterprise_id, e.store_name, p.product_image
            FROM order_items oi
            JOIN products p ON oi.product_id = p.product_id
            JOIN enterprises e ON p.enterprise_id = e.enterprise_id
            WHERE oi.order_id = @oid";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@oid", orderId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            byte[] imgBytes = reader["product_image"] as byte[];
                            string base64 = imgBytes != null ? Convert.ToBase64String(imgBytes) : "";

                            reorderItems.Add(new
                            {
                                Id = DateTime.Now.Ticks, // Temporary ID for JS state
                                ProductId = reader["product_id"],
                                ProductName = reader["product_name"],
                                UnitPrice = reader["unit_price"],
                                Quantity = reader["quantity"],
                                EnterpriseId = reader["enterprise_id"],
                                EnterpriseName = reader["store_name"],
                                ImageBase64 = base64
                            });
                        }
                    }
                }
            }

            // Convert to JSON and pass it to the Checkout View via TempData
            TempData["ReorderCart"] = Newtonsoft.Json.JsonConvert.SerializeObject(reorderItems);

            return RedirectToAction("Checkout");
        }

        [HttpGet]
        public JsonResult GetEnterpriseQr(int enterpriseId)
        {
            if (enterpriseId <= 0)
            {
                return Json(new { success = false, message = "Invalid enterprise ID." }, JsonRequestBehavior.AllowGet);
            }

            string connStr = ConfigurationManager.ConnectionStrings["BizzyQCUConnection"].ConnectionString;

            try
            {
                int userId = 0;
                string storeName = "Enterprise";

                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string sql = "SELECT user_id, store_name FROM enterprises WHERE enterprise_id = @enterpriseId LIMIT 1";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@enterpriseId", enterpriseId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                return Json(new { success = false, message = "Enterprise not found." }, JsonRequestBehavior.AllowGet);
                            }

                            userId = reader.GetInt32("user_id");
                            storeName = reader.IsDBNull(reader.GetOrdinal("store_name")) ? "Enterprise" : reader.GetString("store_name");
                        }
                    }
                }

                var uploadsFolder = Server.MapPath("~/Content/Uploads");
                if (string.IsNullOrWhiteSpace(uploadsFolder) || !Directory.Exists(uploadsFolder))
                {
                    return Json(new { success = false, message = "QR code not available yet." }, JsonRequestBehavior.AllowGet);
                }

                var pattern = "enterprise_" + userId + "_qr.*";
                var files = Directory.GetFiles(uploadsFolder, pattern);
                if (files == null || files.Length == 0)
                {
                    return Json(new { success = false, message = "QR code not available yet." }, JsonRequestBehavior.AllowGet);
                }

                var latest = files
                    .Select(path => new FileInfo(path))
                    .OrderByDescending(info => info.LastWriteTimeUtc)
                    .FirstOrDefault();

                if (latest == null)
                {
                    return Json(new { success = false, message = "QR code not available yet." }, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    success = true,
                    enterpriseId = enterpriseId,
                    enterpriseName = storeName,
                    qrUrl = Url.Content("~/Content/Uploads/" + latest.Name)
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to fetch QR code: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // Helper class for deserializing cart items from the AJAX POST
        private class CartItem
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
        }
    }
}
