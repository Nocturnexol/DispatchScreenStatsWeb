using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DispatchScreenStats.IRepository;
using DispatchScreenStats.Models;
using DispatchScreenStats.Repository;

namespace DispatchScreenStats.Areas.ScreenStats.Controllers
{
    public class DispatchScreenRecController : Controller
    {
        private readonly IMongoRepository<ScreenRec> _rep = new MongoRepository<ScreenRec>();

        //
        // GET: /ScreenStats/DispatchScreenRec/
        public ActionResult Index()
        {
            return View();
        }
	}
}