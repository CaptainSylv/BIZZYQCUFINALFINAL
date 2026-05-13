using BizzyQCU.Models.Landingpage;
using System;
using System.Linq;
using System.Web.Mvc;

namespace BizzyQCU.Controllers
{
    public class EditStoreController : Controller
    {
        private readonly SimpleDb db = new SimpleDb();

        public ActionResult EditStore(int page = 1)
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

            int userId = (int)Session["UserId"];
            var enterprise = db.GetEnterpriseByUserId(userId);

            if (enterprise == null)
            {
                return RedirectToAction("Login", "Home");
            }

            int pageSize = 6;
            int currentPage = page;

            // Get all products of this enterprise
            var allProducts = db.GetProductsByEnterpriseId(enterprise.EnterpriseId);

            // Pagination
            int totalProducts = allProducts.Count;
            int totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);
            var products = allProducts.Skip((currentPage - 1) * pageSize).Take(pageSize).ToList();
            var adProduct = allProducts.OrderBy(_ => Guid.NewGuid()).FirstOrDefault();

            ViewBag.EnterpriseName = enterprise.StoreName;
            ViewBag.StoreLogo = enterprise.StoreLogo;
            ViewBag.StoreDescription = enterprise.StoreDescription ?? "";
            ViewBag.ContactNumber = enterprise.ContactNumber ?? "";
            ViewBag.GcashNumber = enterprise.GcashNumber ?? "";
            ViewBag.Products = products;
            ViewBag.AdProduct = adProduct;
            ViewBag.EnterpriseId = enterprise.EnterpriseId;
            ViewBag.CurrentPage = currentPage;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalProducts = totalProducts;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult UpdateStore(string storeName, string storeDescription, string contactNumber, string gcashNumber)
        {
            if (Session["UserId"] == null)
            {
                return Json(new { success = false, message = "Please login first." });
            }

            int userId = (int)Session["UserId"];
            bool result = db.UpdateEnterpriseProfile(userId, storeName, storeDescription, contactNumber, gcashNumber);

            if (result)
            {
                return Json(new { success = true, message = "Store information updated successfully!" });
            }
            return Json(new { success = false, message = "Failed to update store information." });
        }

        // Get product image
        public ActionResult GetProductImage(int productId)
        {
            try
            {
                byte[] imageData = db.GetProductImage(productId);
                if (imageData != null && imageData.Length > 0)
                {
                    return File(imageData, "image/jpeg");
                }
                else
                {
                    byte[] emptyImage = Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7");
                    return File(emptyImage, "image/gif");
                }
            }
            catch (Exception)
            {
                byte[] emptyImage = Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7");
                return File(emptyImage, "image/gif");
            }
        }

        // Delete product
        [HttpPost]
        public JsonResult DeleteProduct(int productId)
        {
            try
            {
                if (Session["UserId"] == null)
                {
                    return Json(new { success = false, message = "Please login first" });
                }

                int userId = (int)Session["UserId"];
                var enterprise = db.GetEnterpriseByUserId(userId);

                if (enterprise == null)
                {
                    return Json(new { success = false, message = "Enterprise not found" });
                }

                bool result = db.DeleteProduct(productId, enterprise.EnterpriseId);
                if (result)
                {
                    return Json(new { success = true, message = "Product deleted successfully!" });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to delete product" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
