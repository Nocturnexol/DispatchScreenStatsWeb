using System.Collections.Concurrent;
using System.Configuration;
using System.IO;
using System.Web.Mvc;
using System.Web.Routing;
using DispatchScreenStats.Models;
using FineUIMvc;
using Newtonsoft.Json.Linq;

namespace DispatchScreenStats
{
    public class MvcApplication : System.Web.HttpApplication
    {
        private readonly string _transPath = ConfigurationManager.AppSettings["samplePath"];
        public static readonly ConcurrentDictionary<string, string> HashList = new ConcurrentDictionary<string, string>();
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            ModelBinders.Binders.Add(typeof(JArray), new JArrayModelBinder());
            ModelBinders.Binders.Add(typeof(JObject), new JObjectModelBinder());

            string[] files = Directory.GetFiles(_transPath, "*.jpg", SearchOption.AllDirectories);
            foreach (string item in files)
            {
                try
                {
                    //string Tag = Path.GetFileName(item).Substring(0, 1);
                    using (SimilarPhoto photo = new SimilarPhoto(item))
                    {
                        HashList.AddOrUpdate(photo.GetHash(), item, (k, v) => v);
                    }
                }
                catch
                {
                    // ignored
                }
            }
        }
    }
}
