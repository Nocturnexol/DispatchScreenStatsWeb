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
    public class ScreenRecIFrameController : BaseController
    {
        private readonly IMongoRepository<ScreenRec> _rep = new MongoRepository<ScreenRec>();
        private readonly Expression<Func<ScreenRec, bool>> _filter = t =>!t.IsLog;
        //
        // GET: /ScreenStats/ScreenRecIFrame/
        public ActionResult Index()
        {
            int count;
            var list = _rep.QueryByPage(0, PageSize, out count,_filter);
            ViewBag.RecordCount = count;
            ViewBag.PageSize = PageSize;
            return View(list);
        }

        private void UpdateGrid(NameValueCollection values)
        {
            var fields = JArray.Parse(values["Grid1_fields"]);
            var pageIndex = Convert.ToInt32(values["Grid1_pageIndex"] ?? "0");
            var pageSize = Convert.ToInt32(values["Grid1_pageSize"] ?? "0");

            var devNum = values["tbDevNum"];
            var line = values["tbLine"];
            var owner = values["ddlOwner"] != null && values["ddlOwner"] != "[]" ? JArray.Parse(values["ddlOwner"]) : null;
            var station = values["tbStation"];
            var filter = new List<FilterDefinition<ScreenRec>> { _filter };
            if (!string.IsNullOrWhiteSpace(devNum))
            {
                filter.Add(Builders<ScreenRec>.Filter.Regex(t =>t.DeviceNum,
                    new BsonRegularExpression(new Regex(devNum.Trim(),RegexOptions.IgnoreCase))));
            }
            if (owner != null)
            {
                filter.Add(Builders<ScreenRec>.Filter.In(t => t.Owner, owner));
            }
            if (!string.IsNullOrWhiteSpace(station))
            {
                filter.Add(Builders<ScreenRec>.Filter.Regex(t => t.InstallStation,
                    new BsonRegularExpression(new Regex(station.Trim()))));
            }
            if (!string.IsNullOrWhiteSpace(line))
            {
                filter.Add(Builders<ScreenRec>.Filter.Regex(t => t.LineName,
                    new BsonRegularExpression(new Regex(line.Trim()))));
            }
            int count;
            var list = _rep.QueryByPage(pageIndex, pageSize, out count, filter.Any() ? Builders<ScreenRec>.Filter.And(filter) : null);
            var grid1 = UIHelper.Grid("Grid1");
            grid1.RecordCount(count);
            grid1.PageSize(pageSize);
            grid1.DataSource(list, fields);
        }
        public ActionResult DoSearch(FormCollection values)
        {
            UpdateGrid(values);
            return UIHelper.Result();
        }
	}
}