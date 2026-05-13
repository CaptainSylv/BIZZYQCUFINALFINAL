using System.Web.Mvc;
using System.Web.Routing;

namespace BizzyQCU
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "EnterpriseProfile",
                url: "profile/enterpriseprofile",
                defaults: new { controller = "Profile", action = "EnterpriseProfile" }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Homepage", id = UrlParameter.Optional }
            );
        }
    }
}
