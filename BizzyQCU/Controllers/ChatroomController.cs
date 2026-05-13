using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web.Mvc;
using MySql.Data.MySqlClient;
using System.Diagnostics;

namespace BizzyQCU.Controllers
{
    public class ChatroomController : Controller
    {
        string connStr = ConfigurationManager.ConnectionStrings["BizzyQCUConnection"].ConnectionString;

        private class ChatOrderContext
        {
            public int OrderId { get; set; }
            public string Status { get; set; }
            public decimal TotalAmount { get; set; }
            public string StoreName { get; set; }
            public int EnterpriseId { get; set; }
            public int EnterpriseUserId { get; set; }
            public int StudentUserId { get; set; }
            public string StudentName { get; set; }
        }

        private bool IsChatOpen(string status)
        {
            return status != "completed" && status != "cancelled";
        }

        private ChatOrderContext GetAuthorizedChatOrder(MySqlConnection conn, int orderId, int userId, string role)
        {
            string roleFilter;
            if (role == "student")
            {
                roleFilter = "AND s.user_id = @uid";
            }
            else if (role == "enterprise")
            {
                roleFilter = "AND e.user_id = @uid";
            }
            else
            {
                return null;
            }

            string sql = @"
                SELECT
                    o.order_id,
                    o.status,
                    o.total_amount,
                    e.store_name,
                    e.enterprise_id,
                    e.user_id AS enterprise_user_id,
                    s.user_id AS student_user_id,
                    CONCAT(COALESCE(s.firstname, ''), ' ', COALESCE(s.lastname, '')) AS student_name
                FROM orders o
                INNER JOIN enterprises e ON e.enterprise_id = o.enterprise_id
                INNER JOIN students s ON s.student_id = o.student_id
                WHERE o.order_id = @oid " + roleFilter + @"
                LIMIT 1";

            using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@oid", orderId);
                cmd.Parameters.AddWithValue("@uid", userId);

                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    return new ChatOrderContext
                    {
                        OrderId = Convert.ToInt32(reader["order_id"]),
                        Status = reader["status"].ToString(),
                        TotalAmount = Convert.ToDecimal(reader["total_amount"]),
                        StoreName = reader["store_name"].ToString(),
                        EnterpriseId = Convert.ToInt32(reader["enterprise_id"]),
                        EnterpriseUserId = Convert.ToInt32(reader["enterprise_user_id"]),
                        StudentUserId = Convert.ToInt32(reader["student_user_id"]),
                        StudentName = reader["student_name"].ToString().Trim()
                    };
                }
            }
        }

        // GET: /Chatroom/Chatroom?orderId=123
        public ActionResult Chatroom(int orderId)
        {
            try
            {
                if (Session["UserId"] == null)
                    return RedirectToAction("Login", "Account");

                int userId = (int)Session["UserId"];
                string role = Session["Role"]?.ToString();

                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    var chatOrder = GetAuthorizedChatOrder(conn, orderId, userId, role);

                    if (chatOrder == null)
                    {
                        return RedirectToAction("AccessDenied", "Home");
                    }

                    ViewBag.OrderId = chatOrder.OrderId;
                    ViewBag.OrderStatus = chatOrder.Status;
                    ViewBag.StoreName = chatOrder.StoreName;
                    ViewBag.EnterpriseId = chatOrder.EnterpriseId;
                    ViewBag.TotalAmount = chatOrder.TotalAmount;
                    ViewBag.ChatOpen = IsChatOpen(chatOrder.Status);
                }

                return View();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Chatroom Error: " + ex.Message);
                Debug.WriteLine(ex.StackTrace);
                return RedirectToAction("Error", "Home");
            }
        }

        [HttpPost]
        public JsonResult SendMessage(string message, int orderId)
        {
            try
            {
                if (Session["UserId"] == null)
                    return Json(new { success = false, message = "Not logged in" });

                int senderId = (int)Session["UserId"];
                string role = Session["Role"]?.ToString();

                // Validate inputs
                if (string.IsNullOrWhiteSpace(message))
                {
                    return Json(new { success = false, message = "Message cannot be empty" });
                }

                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    var chatOrder = GetAuthorizedChatOrder(conn, orderId, senderId, role);

                    if (chatOrder == null)
                    {
                        return Json(new { success = false, message = "You do not have access to this chat" });
                    }

                    if (!IsChatOpen(chatOrder.Status))
                    {
                        return Json(new { success = false, message = "This chat is already closed for this order" });
                    }

                    int receiverId = role == "student" ? chatOrder.EnterpriseUserId : chatOrder.StudentUserId;

                    // Insert message
                    string insertSql = @"INSERT INTO chat_messages 
                                        (sender_id, receiver_id, order_id, message_text, is_courier_message, sent_at) 
                                        VALUES (@sid, @rid, @oid, @msg, @is_courier, NOW())";

                    using (var cmd = new MySqlCommand(insertSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@sid", senderId);
                        cmd.Parameters.AddWithValue("@rid", receiverId);
                        cmd.Parameters.AddWithValue("@oid", orderId);
                        cmd.Parameters.AddWithValue("@msg", message);
                        cmd.Parameters.AddWithValue("@is_courier", role == "enterprise");
                        cmd.ExecuteNonQuery();
                    }
                }
                return Json(new { success = true });
            }
            catch (MySqlException sqlEx)
            {
                Debug.WriteLine("SQL Error: " + sqlEx.Message);
                Debug.WriteLine("SQL Error Code: " + sqlEx.Number);
                return Json(new { success = false, message = "Database error: " + sqlEx.Message });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("General Error: " + ex.Message);
                Debug.WriteLine(ex.StackTrace);
                return Json(new { success = false, message = "Server error: " + ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetMessages(int orderId)
        {
            try
            {
                if (Session["UserId"] == null)
                    return Json(new { success = false, message = "Not logged in" }, JsonRequestBehavior.AllowGet);

                int myId = (int)Session["UserId"];
                string role = Session["Role"]?.ToString();

                var list = new List<object>();

                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    var chatOrder = GetAuthorizedChatOrder(conn, orderId, myId, role);
                    if (chatOrder == null)
                    {
                        return Json(new { success = false, message = "You do not have access to this chat" }, JsonRequestBehavior.AllowGet);
                    }

                    // Get messages for this specific order
                    string sql = @"SELECT message_id, sender_id, message_text, sent_at, is_courier_message 
                                  FROM chat_messages 
                                  WHERE order_id = @oid
                                  ORDER BY sent_at ASC, message_id ASC";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@oid", orderId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int senderId = Convert.ToInt32(reader["sender_id"]);
                                bool isCourier = Convert.ToBoolean(reader["is_courier_message"]);

                                // Determine message type for styling
                                string messageType = "incoming";
                                if (senderId == myId)
                                {
                                    messageType = "outgoing";
                                }
                                list.Add(new
                                {
                                    messageId = Convert.ToInt32(reader["message_id"]),
                                    senderId = senderId,
                                    text = reader["message_text"].ToString(),
                                    sentAt = Convert.ToDateTime(reader["sent_at"]).ToString("hh:mm tt"),
                                    messageType = messageType,
                                    isCourier = isCourier
                                });
                            }
                        }
                    }

                    string markReadSql = @"UPDATE chat_messages
                                          SET is_read = TRUE
                                          WHERE order_id = @oid AND receiver_id = @uid AND is_read = FALSE";
                    using (var cmd = new MySqlCommand(markReadSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@oid", orderId);
                        cmd.Parameters.AddWithValue("@uid", myId);
                        cmd.ExecuteNonQuery();
                    }
                }
                return Json(new { success = true, messages = list }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("GetMessages Error: " + ex.Message);
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // Mark messages as read for the current user
        [HttpPost]
        public JsonResult MarkAsRead(int orderId)
        {
            try
            {
                if (Session["UserId"] == null)
                    return Json(new { success = false });

                int myId = (int)Session["UserId"];

                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    var chatOrder = GetAuthorizedChatOrder(conn, orderId, myId, Session["Role"]?.ToString());
                    if (chatOrder == null)
                    {
                        return Json(new { success = false });
                    }

                    string sql = @"UPDATE chat_messages 
                                  SET is_read = TRUE 
                                  WHERE order_id = @oid AND receiver_id = @uid AND is_read = FALSE";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@oid", orderId);
                        cmd.Parameters.AddWithValue("@uid", myId);
                        int rowsAffected = cmd.ExecuteNonQuery();
                        Debug.WriteLine($"Marked {rowsAffected} messages as read");
                    }
                }
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("MarkAsRead Error: " + ex.Message);
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
