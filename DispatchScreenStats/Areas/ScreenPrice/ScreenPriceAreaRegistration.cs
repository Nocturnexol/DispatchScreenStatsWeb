using System.Web.Mvc;

namespace DispatchScreenStats.Areas.ScreenPrice
{
    public class ScreenPriceAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "ScreenPrice";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "ScreenPrice_default",
                "ScreenPrice/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}