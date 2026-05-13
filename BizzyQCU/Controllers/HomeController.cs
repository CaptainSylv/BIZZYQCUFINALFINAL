using System;
using System.Web.Mvc;
using BizzyQCU.Models.Landingpage;

namespace BizzyQCU.Controllers
{
    public class HomeController : Controller
    {
        private readonly SimpleDb db = new SimpleDb();

        public ActionResult Login()
        {
            return View();
        }

        public ActionResult RegisterStudent()
        {
            return View();
        }

        public ActionResult RegisterEnterprise()
        {
            return View();
        }

        public ActionResult Manage()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "About page.";
            return View();
        }

        public ActionResult Homepage()
        {
            if (Session["UserId"] != null)
            {
                string role = Session["Role"] != null ? Session["Role"].ToString() : "";
                if (role == "enterprise")
                {
                    return RedirectToAction("EnterpriseDashboard", "EnterpriseDashboard");
                }
                else if (role == "student")
                {
                    return RedirectToAction("ProductList", "ProductList");
                }
                else if (role == "admin")
                {
                    return RedirectToAction("LandingAdmin", "AdminPanel");
                }
            }

            ViewBag.Message = "Your Homepage.";
            return View();
        }

        public ActionResult Index()
        {
            if (Session["UserId"] != null)
            {
                string role = Session["Role"] != null ? Session["Role"].ToString() : "";
                if (role == "enterprise")
                {
                    return RedirectToAction("EnterpriseDashboard", "EnterpriseDashboard");
                }
            }
            return View("Homepage");
        }

        public ActionResult UserProfile()
        {
            return RedirectToAction("UserProfile", "Profile");
        }

        public new ActionResult Profile()
        {
            return RedirectToAction("EnterpriseProfile", "Profile");
        }

        [HttpPost]
        public JsonResult SubmitFeedback(string email, string contactNumber, string userType, string category, string message, int rating)
        {
            try
            {
                if (Session["UserId"] == null)
                {
                    return Json(new { success = false, message = "Please login first to submit feedback." });
                }

                int userId = (int)Session["UserId"];
                string userTypeValue = Session["Role"] != null && Session["Role"].ToString() == "enterprise" ? "entrepreneur" : "customer";

                bool result = db.SubmitFeedback(email, contactNumber, userTypeValue, category, message, rating, userId);

                if (result)
                {
                    return Json(new { success = true, message = "Feedback submitted successfully!" });
                }
                return Json(new { success = false, message = "Failed to submit feedback." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
    }
}