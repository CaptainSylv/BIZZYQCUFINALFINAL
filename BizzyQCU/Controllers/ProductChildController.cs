using System.Collections.Generic;
using System.Configuration;
using System.Web.Mvc;
using BizzyQCU.Models;
using MySql.Data.MySqlClient;

namespace BizzyQCU.Controllers
{
    public class ProductChildController : Controller
    {
        public ActionResult ProductChild(int id)
        {
            ProductDetailViewModel model = null;
            string connStr = ConfigurationManager.ConnectionStrings["BizzyQCUConnection"].ConnectionString;

            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();

                string sql = @"
                    SELECT p.product_id, p.product_name, p.description, p.price,
                           p.preparation_time, p.product_image,
                           e.enterprise_id, e.store_name, e.store_logo
                    FROM products p
                    INNER JOIN enterprises e ON p.enterprise_id = e.enterprise_id
                    WHERE p.product_id = @id
                      AND p.status = 'active'
                      AND p.is_approved = 1";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model = new ProductDetailViewModel
                            {
                                ProductId = reader.GetInt32("product_id"),
                                ProductName = reader.GetString("product_name"),
                                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? "" : reader.GetString("description"),
                                Price = reader.GetDecimal("price"),
                                PreparationTime = reader.GetInt32("preparation_time"),
                                ProductImage = reader.IsDBNull(reader.GetOrdinal("product_image")) ? null : (byte[])reader["product_image"],
                                EnterpriseId = reader.GetInt32("enterprise_id"),
                                EnterpriseName = reader.GetString("store_name"),
                                EnterpriseLogo = reader.IsDBNull(reader.GetOrdinal("store_logo")) ? null : reader.GetString("store_logo")
                            };
                        }
                    }
                }

                if (model == null) { return HttpNotFound(); }

                string deliverySql = @"
                    SELECT delivery_type
                    FROM delivery_options
                    WHERE enterprise_id = @eid AND is_active = 1";

                var options = new List<string>();
                using (var cmd2 = new MySqlCommand(deliverySql, conn))
                {
                    cmd2.Parameters.AddWithValue("@eid", model.EnterpriseId);
                    using (var reader2 = cmd2.ExecuteReader())
                    {
                        while (reader2.Read())
                        {
                            string raw = reader2.GetString("delivery_type");
                            if (raw == "pickup") { options.Add("Pickup"); }
                            else if (raw == "room_to_room") { options.Add("Room to Room"); }
                            else if (raw == "campus_delivery") { options.Add("Campus Delivery"); }
                        }
                    }
                }

                model.DeliveryOptions = options.Count > 0 ? string.Join(" or ", options) : "Pickup";
            }

            return View(model);
        }
    }
}