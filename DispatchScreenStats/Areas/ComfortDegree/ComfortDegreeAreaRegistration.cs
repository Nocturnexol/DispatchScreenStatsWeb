using System.Web.Mvc;

namespace DispatchScreenStats.Areas.ComfortDegree
{
    public class ComfortDegreeAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "ComfortDegree";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "ComfortDegree_default",
                "ComfortDegree/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}