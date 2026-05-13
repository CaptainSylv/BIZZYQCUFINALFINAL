using System;
using System.Web;
using System.Web.Mvc;
using BizzyQCU.Models.Landingpage;
using System.Web.Script.Serialization;
using System.IO;

namespace BizzyQCU.Controllers
{
    public class AccountController : Controller
    {
        private readonly SimpleDb db = new SimpleDb();

        [HttpPost]
        public JsonResult Login(string username, string password)
        {
            try
            {
                if (!db.TestConnection())
                {
                    return Json(new { success = false, message = "Cannot connect to database. Please check if XAMPP MySQL is running." });
                }

                var user = db.GetUserByUsernameOrEmail(username);

                if (user == null)
                {
                    return Json(new { success = false, message = "Invalid username/email or password." });
                }

                if (user.Password != password)
                {
                    return Json(new { success = false, message = "Invalid username/email or password." });
                }

                if (!user.IsApproved && (user.Role == "student" || user.Role == "enterprise"))
                {
                    return Json(new { success = false, message = "this account is currently disabled" });
                }

                if (!user.IsApproved)
                {
                    return Json(new { success = false, message = "Your account is pending approval. Please wait for admin confirmation." });
                }

                Session["UserId"] = user.UserId;
                Session["Username"] = user.Username;
                Session["Email"] = user.Email;
                Session["Role"] = user.Role;

                string redirectUrl = "";
                if (user.Role == "admin")
                {
                    redirectUrl = Url.Action("LandingAdmin", "AdminPanel");
                }
                else if (user.Role == "enterprise")
                {
                    redirectUrl = Url.Action("EnterpriseDashboard", "EnterpriseDashboard");
                }
                else
                {
                    redirectUrl = Url.Action("ProductList", "ProductList");
                }

                return Json(new
                {
                    success = true,
                    username = user.Username,
                    role = user.Role,
                    displayName = user.Username,
                    redirectUrl = redirectUrl
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Server error: " + ex.Message });
            }
        }

        [HttpGet]
        public JsonResult CheckLogin()
        {
            bool isLoggedIn = Session["UserId"] != null;
            return Json(new { isLoggedIn = isLoggedIn }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult RegisterStudent(
            string Firstname,
            string Lastname,
            string Username,
            string Email,
            string Password,
            string Birthdate,
            string StudentNumber,
            string Section,
            string ContactNumber,
            HttpPostedFileBase qcidFile)
        {
            try
            {
                string firstName = Firstname;
                string lastName = Lastname;
                string username = Username;
                string email = Email;
                string password = Password;
                string birthdate = Birthdate;
                string studentNumber = StudentNumber;
                string section = Section;
                string contactNumber = ContactNumber;

                // Compatibility fallback: accept JSON body only when it looks like JSON.
                if (string.IsNullOrWhiteSpace(firstName) &&
                    string.IsNullOrWhiteSpace(lastName) &&
                    string.IsNullOrWhiteSpace(username) &&
                    string.IsNullOrWhiteSpace(email) &&
                    Request.InputStream != null &&
                    Request.InputStream.Length > 0)
                {
                    Request.InputStream.Position = 0;
                    using (var reader = new StreamReader(Request.InputStream))
                    {
                        var body = reader.ReadToEnd();
                        if (!string.IsNullOrWhiteSpace(body))
                        {
                            body = body.Trim();
                            if (body.StartsWith("{") || body.StartsWith("["))
                            {
                                var serializer = new JavaScriptSerializer();
                                var data = serializer.Deserialize<dynamic>(body);
                                firstName = GetValue(data, "Firstname");
                                lastName = GetValue(data, "Lastname");
                                username = GetValue(data, "Username");
                                email = GetValue(data, "Email");
                                password = GetValue(data, "Password");
                                birthdate = GetValue(data, "Birthdate");
                                studentNumber = GetValue(data, "StudentNumber");
                                section = GetValue(data, "Section");
                                contactNumber = GetValue(data, "ContactNumber");
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName) ||
                    string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    return Json(new { success = false, message = "Please fill all required fields correctly." });
                }

                if (password.Length < 8)
                {
                    return Json(new { success = false, message = "Password must be at least 8 characters long." });
                }

                if (db.IsUsernameRequestExists(username, "student"))
                {
                    return Json(new { success = false, message = "Username already submitted for approval." });
                }

 
                if (db.IsEmailRequestExists(email, "student"))
                {
                    return Json(new { success = false, message = "Email already submitted for approval." });
                }

       
                byte[] qcuIdBytes = null;
                if (qcidFile != null && qcidFile.ContentLength > 0)
                {
                    using (var br = new BinaryReader(qcidFile.InputStream))
                    {
                        qcuIdBytes = br.ReadBytes(qcidFile.ContentLength);
                    }
                }

                bool result = db.SubmitStudentRequest(firstName, lastName, username, email, password, birthdate, studentNumber, section, contactNumber, qcuIdBytes);

                if (result)
                {
                    return Json(new { success = true, message = "Registration submitted! Please wait for admin approval." });
                }
                return Json(new { success = false, message = "Registration failed. Please try again." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult RegisterEnterprise(HttpPostedFileBase documents)
        {
            try
            {
                string storeName = Request.Form["StoreName"];
                string enterpriseType = Request.Form["EnterpriseType"];
                string username = Request.Form["Username"];
                string email = Request.Form["Email"];
                string password = Request.Form["Password"];
                string contactNumber = Request.Form["ContactNumber"];
                string gcashNumber = Request.Form["GcashNumber"];

                bool isJsonRequest = !string.IsNullOrWhiteSpace(Request.ContentType) &&
                                     Request.ContentType.IndexOf("application/json", StringComparison.OrdinalIgnoreCase) >= 0;

                if (isJsonRequest &&
                    string.IsNullOrWhiteSpace(storeName) &&
                    string.IsNullOrWhiteSpace(enterpriseType) &&
                    string.IsNullOrWhiteSpace(username) &&
                    Request.InputStream != null &&
                    Request.InputStream.Length > 0)
                {
                    Request.InputStream.Position = 0;
                    using (var reader = new StreamReader(Request.InputStream))
                    {
                        var jsonString = reader.ReadToEnd();
                        if (!string.IsNullOrWhiteSpace(jsonString))
                        {
                            var serializer = new JavaScriptSerializer();
                            var data = serializer.Deserialize<dynamic>(jsonString);
                            storeName = GetValue(data, "StoreName");
                            enterpriseType = GetValue(data, "EnterpriseType");
                            username = GetValue(data, "Username");
                            email = GetValue(data, "Email");
                            password = GetValue(data, "Password");
                            contactNumber = GetValue(data, "ContactNumber");
                            gcashNumber = GetValue(data, "GcashNumber");
                        }
                    }
                }

          
                if (string.IsNullOrEmpty(storeName) || string.IsNullOrEmpty(enterpriseType) ||
                    string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) ||
                    string.IsNullOrEmpty(password) || string.IsNullOrEmpty(contactNumber))
                {
                    return Json(new { success = false, message = "Please fill all required fields correctly." });
                }

              
                if (db.IsUsernameRequestExists(username, "enterprise"))
                {
                    return Json(new { success = false, message = "Username already submitted for approval." });
                }

               
                if (db.IsEmailRequestExists(email, "enterprise"))
                {
                    return Json(new { success = false, message = "Email already submitted for approval." });
                }

                byte[] uploadedDocumentBytes = null;
                var uploadedFile = documents ?? (Request.Files.Count > 0 ? Request.Files[0] : null);
                if (uploadedFile != null && uploadedFile.ContentLength > 0)
                {
                    using (var br = new BinaryReader(uploadedFile.InputStream))
                    {
                        uploadedDocumentBytes = br.ReadBytes(uploadedFile.ContentLength);
                    }
                }

                bool result = db.SubmitEnterpriseRequest(storeName, enterpriseType, username, email, password, contactNumber, gcashNumber, uploadedDocumentBytes);

                if (result)
                {
                    return Json(new { success = true, message = "Registration submitted! Please wait for admin approval." });
                }
                return Json(new { success = false, message = "Registration failed. Please try again." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        private string GetValue(dynamic data, string key)
        {
            try
            {
                var dict = data as System.Collections.Generic.Dictionary<string, object>;
                if (dict != null && dict.ContainsKey(key))
                {
                    return dict[key]?.ToString() ?? "";
                }
                return "";
            }
            catch
            {
                return "";
            }
        }

        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Login", "Home");
        }
    }
}
