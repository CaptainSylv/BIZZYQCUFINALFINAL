using System;
using System.IO;
using System.Web.Mvc;
using BizzyQCU.Models.Landingpage;

namespace BizzyQCU.Controllers
{
    public class EditProductController : Controller
    {
        private readonly SimpleDb db = new SimpleDb();

        public ActionResult EditProduct(int id)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Index", "Login");
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
                return RedirectToAction("Index", "Login");
            }

            // Get product details
            var product = db.GetProductById(id, enterprise.EnterpriseId);
            if (product == null)
            {
                return RedirectToAction("EditStore");
            }

            // Get category name
            string categoryName = db.GetCategoryNameById(product.CategoryId);

            ViewBag.Product = product;
            ViewBag.CategoryName = categoryName;
            ViewBag.EnterpriseName = enterprise.StoreName;

            return View();
        }

        [HttpPost]
        public JsonResult UpdateProduct(int productId, string name, decimal price, string category, string preparationTime, string description)
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

                // Handle image upload
                byte[] productImage = null;
                if (Request.Files.Count > 0 && Request.Files[0] != null && Request.Files[0].ContentLength > 0)
                {
                    var file = Request.Files[0];
                    using (var binaryReader = new BinaryReader(file.InputStream))
                    {
                        productImage = binaryReader.ReadBytes(file.ContentLength);
                    }
                }

                // Get or create category
                int? categoryId = null;
                if (!string.IsNullOrWhiteSpace(category))
                {
                    categoryId = db.GetOrCreateCategory(category);
                }

                // Parse preparation time
                int prepTime = 0;
                if (!string.IsNullOrWhiteSpace(preparationTime))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(preparationTime, @"\d+");
                    if (match.Success)
                    {
                        int.TryParse(match.Value, out prepTime);
                    }
                }

                bool result = db.UpdateProduct(productId, enterprise.EnterpriseId, name, description ?? "", price, categoryId, prepTime, productImage);

                if (result)
                {
                    return Json(new { success = true, message = "Product updated successfully!" });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to update product" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
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
    }
}