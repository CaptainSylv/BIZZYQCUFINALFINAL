using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web.Mvc;
using MySql.Data.MySqlClient;

namespace BizzyQCU.Controllers
{
    public class ProductListController : Controller
    {
        public ActionResult ProductList()
        {
            if (Session["UserId"] != null && Session["Role"] != null && Session["Role"].ToString() == "enterprise")
            {
                return RedirectToAction("EnterpriseDashboard", "EnterpriseDashboard");
            }

            var products = new List<ProductListItem>();
            string connStr = ConfigurationManager.ConnectionStrings["BizzyQCUConnection"].ConnectionString;

            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();
                string sql = @"
                    SELECT p.product_id, p.product_name, p.description, p.price,
                           p.product_image, e.enterprise_id, e.store_name,
                           COALESCE(pc.category_name, 'other') AS category_name,
                           COUNT(oi.orderitem_id) AS order_count
                    FROM products p
                    INNER JOIN enterprises e ON p.enterprise_id = e.enterprise_id
                    LEFT JOIN product_categories pc ON p.category_id = pc.category_id
                    LEFT JOIN order_items oi ON p.product_id = oi.product_id
                    WHERE p.status = 'active'
                      AND p.is_approved = 1
                      AND e.status = 'approved'
                    GROUP BY p.product_id, p.product_name, p.description, p.price,
                             p.product_image, e.enterprise_id, e.store_name, pc.category_name, p.created_at
                    ORDER BY p.created_at DESC";

                using (var cmd = new MySqlCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        products.Add(new ProductListItem
                        {
                            ProductId = reader.GetInt32("product_id"),
                            ProductName = reader.GetString("product_name"),
                            Description = reader.IsDBNull(reader.GetOrdinal("description")) ? "" : reader.GetString("description"),
                            Price = reader.GetDecimal("price"),
                            EnterpriseId = reader.GetInt32("enterprise_id"),
                            StoreName = reader.GetString("store_name"),
                            CategoryName = reader.GetString("category_name"),
                            OrderCount = reader.GetInt32("order_count"),
                            ProductImage = reader.IsDBNull(reader.GetOrdinal("product_image")) ? null : (byte[])reader["product_image"]
                        });
                    }
                }
            }

            return View(products);
        }
    }

    // Defined here to avoid namespace conflict with StudentDashboard.ProductViewModel
    public class ProductListItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int EnterpriseId { get; set; }
        public string StoreName { get; set; }
        public string CategoryName { get; set; }
        public int OrderCount { get; set; }
        public byte[] ProductImage { get; set; }
    }
}