using BizzyQCU.Models.Landingpage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace BizzyQCU.Controllers
{
    public class ManageOrdersController : Controller
    {
        private readonly SimpleDb db = new SimpleDb();

        public ActionResult ManageOrders()
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Home");
            }

            string role = Session["Role"]?.ToString();
            if (role != "enterprise")
            {
                return RedirectToAction("ProductList", "ProductList");
            }

            return View();
        }

        [HttpGet]
        public JsonResult GetPendingOrders()
        {
            try
            {
                if (Session["UserId"] == null)
                {
                    return Json(new { success = false, message = "Not logged in" }, JsonRequestBehavior.AllowGet);
                }

                int userId = (int)Session["UserId"];
                var enterprise = db.GetEnterpriseByUserId(userId);

                if (enterprise == null)
                {
                    return Json(new { success = false, message = "Enterprise not found" }, JsonRequestBehavior.AllowGet);
                }

                // DEBUG: I-print ang enterprise ID
                System.Diagnostics.Debug.WriteLine("=== DEBUG ===");
                System.Diagnostics.Debug.WriteLine("User ID: " + userId);
                System.Diagnostics.Debug.WriteLine("Enterprise ID: " + enterprise.EnterpriseId);
                System.Diagnostics.Debug.WriteLine("Store Name: " + enterprise.StoreName);

                var orders = db.GetPendingOrders(enterprise.EnterpriseId);

                System.Diagnostics.Debug.WriteLine("Orders count: " + orders.Count);

                return Json(new { success = true, orders = orders }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error: " + ex.Message);
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpPost]
        public JsonResult UpdateOrderStatus(int orderId, string status)
        {
            try
            {
                if (Session["UserId"] == null)
                {
                    return Json(new { success = false, message = "Not logged in" });
                }

                int userId = (int)Session["UserId"];
                var enterprise = db.GetEnterpriseByUserId(userId);

                if (enterprise == null)
                {
                    return Json(new { success = false, message = "Enterprise not found" });
                }

                var allowedStatuses = new[] { "preparing", "out_for_delivery", "completed", "cancelled" };
                status = string.IsNullOrWhiteSpace(status) ? string.Empty : status.Trim().ToLowerInvariant();
                if (!allowedStatuses.Contains(status))
                {
                    return Json(new { success = false, message = "Invalid order status" });
                }

                bool result = db.UpdateOrderStatus(orderId, status, enterprise.EnterpriseId);
                return Json(new { success = result, message = result ? "Status updated" : "Update failed" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetOrderDetails(int orderId)
        {
            try
            {
                if (Session["UserId"] == null)
                {
                    return Json(new { success = false, message = "Not logged in" }, JsonRequestBehavior.AllowGet);
                }

                int userId = (int)Session["UserId"];
                var enterprise = db.GetEnterpriseByUserId(userId);

                if (enterprise == null)
                {
                    return Json(new { success = false, message = "Enterprise not found" }, JsonRequestBehavior.AllowGet);
                }

                // TANGGALIN ANG TEST DATA! ITO ANG GAMITIN:
                var order = db.GetOrderDetails(orderId, enterprise.EnterpriseId);

                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found" }, JsonRequestBehavior.AllowGet);
                }

                return Json(new { success = true, order = order }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}
