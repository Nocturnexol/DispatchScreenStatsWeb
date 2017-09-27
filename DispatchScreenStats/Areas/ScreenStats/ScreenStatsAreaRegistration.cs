using System.Web.Mvc;

namespace DispatchScreenStats.Areas.ScreenStats
{
    public class ScreenStatsAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "ScreenStats";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "ScreenStats_default",
                "ScreenStats/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}