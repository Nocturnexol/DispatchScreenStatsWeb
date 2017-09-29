using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using DispatchScreenStats.Controllers;
using DispatchScreenStats.IRepository;
using DispatchScreenStats.Models;
using DispatchScreenStats.Repository;
using FineUIMvc;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;

namespace DispatchScreenStats.Areas.ScreenStats.Controllers
{
    public class ScreenLogController : BaseController
    {
        private readonly IMongoRepository<ScreenLog> _rep = new MongoRepository<ScreenLog>();
        //
        // GET: /ScreenStats/ScreenLog/
        public ActionResult Index()
        {
            int count;
            var list = _rep.QueryByPage(0, PageSize, out count);
            ViewBag.RecordCount = count;
            ViewBag.PageSize = PageSize;
            return View(list);
        }
        private void UpdateGrid(NameValueCollection values)
        {
            JArray fields = JArray.Parse(values["Grid1_fields"]);
            int pageIndex = Convert.ToInt32(values["Grid1_pageIndex"] ?? "0");
            var line = values["tbxLineName"];
            var station = values["tbxStation"];

            var filter = new List<FilterDefinition<ScreenLog>>();
            if (!string.IsNullOrWhiteSpace(line))
            {
                filter.Add(Builders<ScreenLog>.Filter.Regex(t => t.LineName,
                     new BsonRegularExpression(new Regex(line.Trim()))));
            }
            if (!string.IsNullOrWhiteSpace(station))
            {
                filter.Add(Builders<ScreenLog>.Filter.Regex(t => t.InstallStation,
                    new BsonRegularExpression(new Regex(station.Trim()))));
            }


            int count;
            var list = _rep.QueryByPage(pageIndex, PageSize, out count,
                filter.Any() ? Builders<ScreenLog>.Filter.And(filter) : null);

            var grid1 = UIHelper.Grid("Grid1");
            grid1.RecordCount(count);
            grid1.DataSource(list, fields);
        }
        public ActionResult DoSearch(FormCollection values)
        {
            UpdateGrid(values);
            return UIHelper.Result();
        }

        public ActionResult Delete(JArray selectedRows, FormCollection values)
        {
            var ids = selectedRows.Select(Convert.ToInt32).ToList();
            _rep.Delete(t => ids.Contains(t._id));
            UpdateGrid(values);
            return UIHelper.Result();
        }
	}
}