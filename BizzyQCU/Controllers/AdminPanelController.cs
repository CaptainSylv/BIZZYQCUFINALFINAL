using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Antlr.Runtime.Misc;
using System.Web.Services.Description;
using BizzyQCU.Models.Admin;
using BizzyQCU.Models.Landingpage;
using Google.Protobuf.WellKnownTypes;
using Mysqlx.Crud;
using Mysqlx.Prepare;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Crypto;
using static Mysqlx.Datatypes.Scalar.Types;

namespace BizzyQCU.Controllers
{
    public class AdminPanelController : Controller
    {
        private readonly AdminDb adminDb = new AdminDb();
        private readonly SimpleDb db = new SimpleDb();


        private bool IsAdmin()
        {
            return Session["UserId"] != null && Session["Role"]?.ToString() == "admin";
        }

        public ActionResult LandingAdmin()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            return View();
        }

        public ActionResult AdminlandingEntrep()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            return View();
        }

        public ActionResult AdminEditEntrep()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            return View();
        }

        public ActionResult AdminItemListing()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            return View();
        }

        public ActionResult AdminUsers()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            return View();
        }


        [HttpGet]
        public JsonResult GetAllUsers()
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" }, JsonRequestBehavior.AllowGet);

            var users = adminDb.GetAllUsers();
            return Json(users, JsonRequestBehavior.AllowGet);
        }


        [HttpGet]
        public JsonResult GetPendingStudents()
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" }, JsonRequestBehavior.AllowGet);

            var students = adminDb.GetPendingStudentRequests();
            return Json(students, JsonRequestBehavior.AllowGet);
        }


        [HttpGet]
        public JsonResult GetPendingEnterprises()
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" }, JsonRequestBehavior.AllowGet);

            var enterprises = adminDb.GetPendingEnterpriseRequests();
            return Json(enterprises, JsonRequestBehavior.AllowGet);
        }


        [HttpPost]
        public JsonResult UpdateUserApproval(int userId, bool isApproved)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

            bool result = adminDb.UpdateUserApproval(userId, isApproved);
            return Json(new { success = result });
        }

        [HttpPost]
        public JsonResult UpdateRequestAccountStatus(int requestId, bool isEnabled)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

            bool result = adminDb.UpdateRequestAccountStatus(requestId, isEnabled);
            return Json(new
            {
                success = result,
                message = result
                    ? (isEnabled ? "Account enabled." : "Account disabled.")
                    : "Failed to update account status."
            });
        }


        [HttpPost]
        public JsonResult DeleteUser(int userId)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

            bool result = adminDb.DeleteUser(userId);
            return Json(new { success = result });
        }


        [HttpPost]
        public JsonResult ApproveRequest(int requestId)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

            bool result = adminDb.ApproveRequest(requestId);
            return Json(new { success = result });
        }


        [HttpPost]
        public JsonResult RejectRequest(int requestId)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

            bool result = adminDb.RejectRequest(requestId);
            return Json(new { success = result });
        }


        [HttpGet]
        public JsonResult GetAllStudentRequests()
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" }, JsonRequestBehavior.AllowGet);

            var students = adminDb.GetAllStudentRequests();
            var json = Json(students, JsonRequestBehavior.AllowGet);
            json.MaxJsonLength = int.MaxValue;
            return json;
        }


        [HttpGet]
        public JsonResult GetAllEnterpriseRequests()
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" }, JsonRequestBehavior.AllowGet);

            var enterprises = adminDb.GetAllEnterpriseRequests();
            var json = Json(enterprises, JsonRequestBehavior.AllowGet);
            json.MaxJsonLength = int.MaxValue;
            return json;
        }

        [HttpGet]
        public JsonResult GetFeedbacks(int? rating = null)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" }, JsonRequestBehavior.AllowGet);

            var feedbacks = adminDb.GetFeedbacks(rating);
            return Json(feedbacks, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult UpdateStudentRequestDetails(int requestId, string username, string email, string firstname, string lastname, string studentNumber, string section, string contactNumber)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

            bool result = adminDb.UpdateStudentRequestDetails(requestId, username, email, firstname, lastname, studentNumber, section, contactNumber);
            return Json(new { success = result, message = result ? "Student details updated." : "Failed to update student details." });
        }

        [HttpPost]
        public JsonResult UpdateEnterpriseRequestDetails(int requestId, string username, string email, string storeName, string businessType, string contactNumber, string gcashNumber)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });
            if (!string.IsNullOrWhiteSpace(gcashNumber) && !System.Text.RegularExpressions.Regex.IsMatch(gcashNumber, @"^09\d{9}$"))
            {
                return Json(new { success = false, message = "GCash number must be 11 digits and start with 09." });
            }

            bool result = adminDb.UpdateEnterpriseRequestDetails(requestId, username, email, storeName, businessType, contactNumber, gcashNumber);
            return Json(new { success = result, message = result ? "Enterprise details updated." : "Failed to update enterprise details." });
        }

        [HttpPost]
        public JsonResult DeleteFeedback(int feedbackId)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

            bool result = adminDb.DeleteFeedback(feedbackId);
            return Json(new { success = result, message = result ? "Feedback deleted." : "Failed to delete feedback." });
        }


        //Enterprise FETCHING ITO SA SIMULA THEN PAPASA NIYA ID SA NEXT PAGEEE

        [HttpGet]
        public JsonResult GetAllApprovedEnterprises()
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" }, JsonRequestBehavior.AllowGet);

            var enterprises = adminDb.GetAllApprovedEnterprises();
            return Json(enterprises, JsonRequestBehavior.AllowGet);
        }


        //Enterprise NEXT PAGE DETAILS ITO 
        [HttpGet]
        public JsonResult GetEnterpriseDetails(int enterpriseId)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" }, JsonRequestBehavior.AllowGet);

            try
            {
                var enterprise = adminDb.GetEnterpriseDetails(enterpriseId);
                if (enterprise == null)
                {
                    return Json(new { success = false, message = "Enterprise not found" }, JsonRequestBehavior.AllowGet);
                }
                return Json(enterprise, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {

                return Json(new { success = false, message = ex.Message, stack = ex.StackTrace }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetEnterpriseDocument(int enterpriseId)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" }, JsonRequestBehavior.AllowGet);

            var bytes = adminDb.GetEnterpriseDocumentByEnterpriseId(enterpriseId);
            if (bytes == null || bytes.Length == 0)
            {
                return Json(new { success = false, message = "No document found." }, JsonRequestBehavior.AllowGet);
            }

            var base64 = Convert.ToBase64String(bytes);
            return Json(new { success = true, data = base64 }, JsonRequestBehavior.AllowGet);
        }


        //RECIPE NG SINIGANG LAGAY KO TALAGA RITO TAMO HAHAHAHAAHA. Kasama toh sa enterprise details
        [HttpGet]
        public JsonResult GetEnterpriseProducts(int enterpriseId)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" }, JsonRequestBehavior.AllowGet);

            var products = adminDb.GetProductsByEnterpriseId(enterpriseId);
            return Json(products, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult DeleteEnterprise(int enterpriseId)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

            bool result = adminDb.DeleteEnterprise(enterpriseId);
            return Json(new { success = result, message = result ? "Enterprise deleted successfully" : "Failed to delete enterprise" });
        }

      //FOR FUTURE PURPOSES ITO
        [HttpGet]
        public JsonResult GetSalesData(int enterpriseId, int days = 7)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" }, JsonRequestBehavior.AllowGet);

            var salesData = adminDb.GetSalesData(enterpriseId, days);
            return Json(salesData, JsonRequestBehavior.AllowGet);
        }

        //RATINGS
        [HttpGet]
        public JsonResult GetRatingsData(int enterpriseId, int days = 7)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" }, JsonRequestBehavior.AllowGet);

            var ratingsData = adminDb.GetRatingsData(enterpriseId, days);
            return Json(ratingsData, JsonRequestBehavior.AllowGet);
        }

        // ITO NA YUNG NEXT PAGEEEE
        [HttpGet]
        public JsonResult GetProductsForListing(int enterpriseId)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" }, JsonRequestBehavior.AllowGet);

            var products = adminDb.GetProductsForListing(enterpriseId);
            return Json(products, JsonRequestBehavior.AllowGet);
        }

        //APROVAL
        [HttpPost]
        public JsonResult ApproveProduct(int productId)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

            bool result = adminDb.ApproveProduct(productId);
            return Json(new { success = result, message = result ? "Product approved successfully" : "Failed to approve product" });
        }

        //REJECT KA NA TOL
        [HttpPost]
        public JsonResult RemoveProduct(int productId)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Unauthorized" });

            bool result = adminDb.RemoveProduct(productId);
            return Json(new { success = result, message = result ? "Product rejected successfully" : "Failed to reject product" });
        }
    }


}

//🛒 Ingredients
//Protein(choose one) : 1 lb(500g) Pork belly or spare ribs, Shrimp (prawns), or Fish steaks (like salmon or milkfish) .

//Souring Agent : 1 packet of Sinigang (Tamarind) soup mix OR 2 cups fresh tamarind pulp .

//Aromatics : 1 medium onion(quartered), 2 medium tomatoes(quartered), 2 - 3 slices of ginger.

//Vegetables : 1 medium radish(sliced), 1 bunch water spinach (kangkong), 1 cup string beans, 1 eggplant (sliced), and 2 - 3 green chilies(optional) .

//Seasoning: 2 tbsp Fish sauce (Patis), Salt, and pepper.

//🍳 Cooking Instructions (Sauté-First Method for Pork)
//Step 1: Prepare the Meat
//Cut the pork into bite-sized pieces. In a pot, boil the pork for 5-7 minutes to remove scum. Drain, rinse the meat, and discard the water .

//Step 2: Sauté Aromatics
//In the same clean pot, sauté the onion, tomatoes, and ginger until soft .

//Step 3: Simmer the Meat
//Add the clean pork back to the pot. Cover with about 6 cups of fresh water. Bring to a boil, then lower the heat and simmer for about 45 minutes to 1 hour, until the pork is tender .

//Step 4: Flavor the Broth
//Once the meat is tender, add the tamarind mix (or fresh tamarind juice). Stir in the fish sauce .

//Step 5: Add Vegetables
//Add the radish and cook for 3 minutes. Then add the eggplant, string beans, and green chilies. Cook for another 5 minutes .

//Step 6: Final Touches
//Season with salt and pepper if needed. Add the tender water spinach leaves last and turn off the heat. The residual heat will wilt them perfectly
