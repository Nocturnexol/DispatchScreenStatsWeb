using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
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
    public class ScreenRecStatsController : BaseController
    {
        private readonly IMongoRepository<ScreenRec> _rep = new MongoRepository<ScreenRec>();

        private readonly SortDefinition<ScreenRec> _sort =
            new SortDefinitionBuilder<ScreenRec>().Ascending(t => t.DeviceNum)
                .Ascending(t => t.LineName)
                .Ascending(t => t.InstallStation);
        //
        // GET: /ScreenStats/ScreenRecStats/
        public ActionResult Index()
        {
            int count;
            var list = _rep.QueryByPage(0, PageSize, out count, null, _sort).Select(Map).ToList();
            ViewBag.RecordCount = count;
            ViewBag.PageSize = PageSize;
            var nums = new List<ListItem>
            {
                new ListItem("全部设备", "")
            };
            nums.AddRange(_rep.Distinct(t => t.DeviceNum).Select(t => new ListItem(t, t)));
            ViewBag.DeviceNums = nums.ToArray();
            return View(list);
        }

        public ActionResult IndexNew()
        {
            return View();
        }
        [HttpPost]
        public JsonResult Search(FormCollection values)
        {
            int count;
            var devNum = values["tbDevNum"];
            var details = values["tbDetails"];
            var type = values["tbType"];
            var date = values["tbDate"];
            var remark = values["tbRemark"];

            var filter = new List<FilterDefinition<ScreenRec>>();
            if (!string.IsNullOrWhiteSpace(devNum))
            {
                filter.Add(Builders<ScreenRec>.Filter.Eq(t => t.DeviceNum, devNum));
            }
            if (!string.IsNullOrWhiteSpace(details))
            {
                filter.Add(
                    Builders<ScreenRec>.Filter.Or(
                        Builders<ScreenRec>.Filter.Regex(t => t.LineName,
                            new BsonRegularExpression(new Regex(details.Trim()))),
                        Builders<ScreenRec>.Filter.Regex(t => t.InstallStation,
                            new BsonRegularExpression(new Regex(details.Trim())))));
            }
            if (!string.IsNullOrWhiteSpace(type))
            {
                Expression<Func<ScreenRec, bool>> exp = t => string.IsNullOrEmpty(t.Materials.Remark);
                var typeFilter = type == "安装"
                    ? exp
                    : Builders<ScreenRec>.Filter.Regex(t => t.Materials.Remark,
                        new BsonRegularExpression(new Regex(type.Trim())));
                filter.Add(typeFilter);
            }
            if (!string.IsNullOrWhiteSpace(date))
            {
                DateTime dt;
                if (DateTime.TryParse(date, out dt))
                    filter.Add(Builders<ScreenRec>.Filter.Eq(t => t.InstallDate, dt));
            }
            if (!string.IsNullOrWhiteSpace(remark))
            {
                filter.Add(
                   Builders<ScreenRec>.Filter.Or(
                       Builders<ScreenRec>.Filter.Regex(t => t.Materials.Remark,
                           new BsonRegularExpression(new Regex(remark.Trim()))),
                       Builders<ScreenRec>.Filter.Regex(t => t.ExtraRemark,
                           new BsonRegularExpression(new Regex(remark.Trim())))));
            }

            var list = _rep.QueryByPage(0, int.MaxValue, out count, filter.Any() ? Builders<ScreenRec>.Filter.And(filter) : null, _sort).Select(Map).ToList();
            return Json(GetHtmlStr(list));
        }
        public ViewResult Search()
        {
            return View();
        }
        private string GetHtmlStr(List<ScreenRecStats> list)
        {
            var devNumList = list.GroupBy(t => new { t.DeviceNum, t.Details }).Select(t => t.Key).ToList();
            var htmlStr = new StringBuilder();
            foreach (var devNum in devNumList)
            {
                var num = devNum.DeviceNum;
                var recs = list.Where(t => t.DeviceNum == num && t.Details == devNum.Details).ToList();
                var rec = recs.First();
                var recCount = recs.Count;
                if (recCount > 1)
                {
                    htmlStr.AppendFormat(
                        "<tr class=\"tr\"><td class=\"td td-data azure\" rowspan=\"{5}\">{0}</td><td class=\"td td-data azure\" rowspan=\"{5}\">{1}</td><td class=\"td td-data\">{2}</td><td class=\"td td-data\">{3:yyyy-MM-dd}</td><td class=\"td td-data\">{4}</td></tr>",
                        num, rec.Details, rec.ConstructionType, rec.Date, rec.Remark, recCount);
                    recs.RemoveAt(0);
                    foreach (var r in recs)
                    {
                        htmlStr.AppendFormat(
                      "<tr class=\"tr\"><td class=\"td td-data\">{0}</td><td class=\"td td-data\">{1:yyyy-MM-dd}</td><td class=\"td td-data\">{2}</td></tr>", r.ConstructionType, r.Date, r.Remark);
                    }
                }
                else
                {
                    htmlStr.AppendFormat(
                        "<tr class=\"tr\"><td class=\"td td-data azure\">{0}</td><td class=\"td td-data azure\">{1}</td><td class=\"td td-data\">{2}</td><td class=\"td td-data\">{3:yyyy-MM-dd}</td><td class=\"td td-data\">{4}</td></tr>",
                        num, rec.Details, rec.ConstructionType, rec.Date, rec.Remark);
                }
            }
            return htmlStr.ToString().Replace("\r", "\\r").Replace("\n", "\\n");
        }
        private void UpdateGrid(NameValueCollection values)
        {
            JArray fields = JArray.Parse(values["Grid1_fields"]);
            int pageIndex = Convert.ToInt32(values["Grid1_pageIndex"] ?? "0");
            var num = values["ddlDeviceNum"];

            var filter = new List<FilterDefinition<ScreenRec>>();
            if (!string.IsNullOrWhiteSpace(num))
            {
                filter.Add(Builders<ScreenRec>.Filter.Regex(t => t.DeviceNum,
                    new BsonRegularExpression(new Regex(num))));
            }


            int count;
            var list = _rep.QueryByPage(pageIndex, PageSize, out count,
                filter.Any() ? Builders<ScreenRec>.Filter.And(filter) : null, _sort).Select(Map).ToList();

            var grid1 = UIHelper.Grid("Grid1");
            grid1.RecordCount(count);
            grid1.DataSource(list, fields);
        }
        public ActionResult DoSearch(FormCollection values)
        {
            UpdateGrid(values);
            return UIHelper.Result();
        }

        private static ScreenRecStats Map(ScreenRec rec)
        {
            return new ScreenRecStats
            {
                DeviceNum = rec.DeviceNum,
                Details = string.Format("{0} - {1}", rec.LineName.Replace("/","、"), rec.InstallStation),
                ConstructionType = string.IsNullOrEmpty(rec.Materials.Remark) ? "安装" : rec.Materials.Remark,
                Date = rec.InstallDate,
                Remark =
                    !string.IsNullOrEmpty(rec.Materials.Remark)
                        ? rec.Materials.Remark + "；" + rec.ExtraRemark
                        : rec.ExtraRemark
            };
        }
	}
}