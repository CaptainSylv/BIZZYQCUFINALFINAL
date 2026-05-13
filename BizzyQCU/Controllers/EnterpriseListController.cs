using System.Collections.Generic;
using System.Configuration;
using System.Web.Mvc;
using BizzyQCU.Models;
using MySql.Data.MySqlClient;

namespace BizzyQCU.Controllers
{
    public class EnterpriseListController : Controller
    {
        public ActionResult EnterpriseList()
        {
            if (Session["UserId"] != null && Session["Role"] != null && Session["Role"].ToString() == "enterprise")
            {
                return RedirectToAction("EnterpriseDashboard", "EnterpriseDashboard");
            }

            var enterprises = new List<EnterpriseListItem>();
            string connStr = ConfigurationManager.ConnectionStrings["BizzyQCUConnection"].ConnectionString;

            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();

                string sql = @"
                    SELECT e.enterprise_id, e.store_name, e.enterprise_type,
                           e.store_description, e.store_logo, e.status,
                           COUNT(DISTINCT p.product_id) AS product_count
                    FROM enterprises e
                    LEFT JOIN products p ON e.enterprise_id = p.enterprise_id
                        AND p.status = 'active' AND p.is_approved = 1
                    WHERE e.status = 'approved'
                    GROUP BY e.enterprise_id, e.store_name, e.enterprise_type,
                             e.store_description, e.store_logo, e.status
                    ORDER BY e.store_name ASC";

                using (var cmd = new MySqlCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        enterprises.Add(new EnterpriseListItem
                        {
                            EnterpriseId = reader.GetInt32("enterprise_id"),
                            StoreName = reader.GetString("store_name"),
                            EnterpriseType = reader.IsDBNull(reader.GetOrdinal("enterprise_type")) ? "" : reader.GetString("enterprise_type"),
                            Description = reader.IsDBNull(reader.GetOrdinal("store_description")) ? "" : reader.GetString("store_description"),
                            StoreLogo = reader.IsDBNull(reader.GetOrdinal("store_logo")) ? null : reader.GetString("store_logo"),
                            Status = reader.IsDBNull(reader.GetOrdinal("status")) ? "pending" : reader.GetString("status"),
                            ProductCount = reader.GetInt32("product_count"),
                            AvgRating = 0
                        });
                    }
                }
            }

            return View(enterprises);
        }
    }
}