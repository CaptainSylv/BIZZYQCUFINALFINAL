using System.Web.Mvc;

namespace BizzyQCU.Controllers
{
    public class BaseController : Controller
    {
        protected bool IsLoggedIn()
        {
            return Session["UserId"] != null;
        }

        protected JsonResult UnauthorizedJson()
        {
            return Json(new { success = false, message = "Please login first to continue." });
        }
    }
}