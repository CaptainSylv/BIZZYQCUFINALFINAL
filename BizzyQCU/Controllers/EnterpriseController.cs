using System.Collections.Generic;
using System.Configuration;
using System.Web.Mvc;
using BizzyQCU.Models;
using MySql.Data.MySqlClient;

namespace BizzyQCU.Controllers
{
    public class EnterpriseController : Controller
    {
        public ActionResult ViewEnterprise(int? id)
        {
            if (id == null) { return RedirectToAction("EnterpriseList", "EnterpriseList"); }

            EnterpriseDetailViewModel model = null;
            string connStr = ConfigurationManager.ConnectionStrings["BizzyQCUConnection"].ConnectionString;

            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();

                string sql = @"
                    SELECT e.enterprise_id, e.store_name, e.enterprise_type,
                           e.store_description, e.store_logo, e.contact_number,
                           e.gcash_number, e.status
                    FROM enterprises e
                    WHERE e.enterprise_id = @id
                      AND e.status = 'approved'";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id.Value);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model = new EnterpriseDetailViewModel
                            {
                                EnterpriseId = reader.GetInt32("enterprise_id"),
                                StoreName = reader.GetString("store_name"),
                                EnterpriseType = reader.IsDBNull(reader.GetOrdinal("enterprise_type")) ? "" : reader.GetString("enterprise_type"),
                                Description = reader.IsDBNull(reader.GetOrdinal("store_description")) ? "" : reader.GetString("store_description"),
                                StoreLogo = reader.IsDBNull(reader.GetOrdinal("store_logo")) ? null : reader.GetString("store_logo"),
                                ContactNumber = reader.IsDBNull(reader.GetOrdinal("contact_number")) ? "" : reader.GetString("contact_number"),
                                GcashNumber = reader.IsDBNull(reader.GetOrdinal("gcash_number")) ? "" : reader.GetString("gcash_number"),
                                Status = reader.IsDBNull(reader.GetOrdinal("status")) ? "pending" : reader.GetString("status"),
                                AvgRating = 0,
                                RatingCount = 0,
                                Products = new List<EnterpriseProductItem>()
                            };
                        }
                    }
                }

                if (model == null) { return HttpNotFound(); }

                string productSql = @"
                    SELECT p.product_id, p.product_name, p.description,
                           p.price, p.product_image, p.preparation_time,
                           COALESCE(pc.category_name, 'other') AS category_name
                    FROM products p
                    LEFT JOIN product_categories pc ON p.category_id = pc.category_id
                    WHERE p.enterprise_id = @eid
                      AND p.status = 'active'
                      AND p.is_approved = 1
                    ORDER BY p.created_at DESC";

                using (var cmd2 = new MySqlCommand(productSql, conn))
                {
                    cmd2.Parameters.AddWithValue("@eid", model.EnterpriseId);
                    using (var reader2 = cmd2.ExecuteReader())
                    {
                        while (reader2.Read())
                        {
                            model.Products.Add(new EnterpriseProductItem
                            {
                                ProductId = reader2.GetInt32("product_id"),
                                ProductName = reader2.GetString("product_name"),
                                Description = reader2.IsDBNull(reader2.GetOrdinal("description")) ? "" : reader2.GetString("description"),
                                Price = reader2.GetDecimal("price"),
                                ProductImage = reader2.IsDBNull(reader2.GetOrdinal("product_image")) ? null : (byte[])reader2["product_image"],
                                PreparationTime = reader2.GetInt32("preparation_time"),
                                CategoryName = reader2.GetString("category_name")
                            });
                        }
                    }
                }

                string deliverySql = @"
                    SELECT delivery_type FROM delivery_options
                    WHERE enterprise_id = @eid AND is_active = 1";

                var deliveryList = new List<string>();
                using (var cmd3 = new MySqlCommand(deliverySql, conn))
                {
                    cmd3.Parameters.AddWithValue("@eid", model.EnterpriseId);
                    using (var reader3 = cmd3.ExecuteReader())
                    {
                        while (reader3.Read())
                        {
                            string raw = reader3.GetString("delivery_type");
                            if (raw == "pickup") { deliveryList.Add("Pickup"); }
                            else if (raw == "room_to_room") { deliveryList.Add("Room to Room"); }
                            else if (raw == "campus_delivery") { deliveryList.Add("Campus Delivery"); }
                        }
                    }
                }
                model.DeliveryOptions = deliveryList.Count > 0 ? string.Join(", ", deliveryList) : "Pickup";
            }

            return View(model);
        }
    }
}