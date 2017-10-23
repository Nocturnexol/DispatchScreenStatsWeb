using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using DispatchScreenStats.Controllers;
using DispatchScreenStats.Enums;
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
        private static SortTypeEnum _sortType = SortTypeEnum.Default;

        private static readonly SortDefinition<ScreenRec> Sort =
            new SortDefinitionBuilder<ScreenRec>().Ascending(t => t.DeviceNum)
                .Ascending(t => t.LineName)
                .Ascending(t => t.InstallStation).Ascending(t => t.InstallDate);

        //
        // GET: /ScreenStats/ScreenRecStats/
        public ActionResult Index()
        {
            int count;
            var list = _rep.QueryByPage(0, PageSize, out count, null, Sort).Select(Map).ToList();
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

            var list =
                _rep.QueryByPage(0, int.MaxValue, out count,
                    filter.Any() ? Builders<ScreenRec>.Filter.And(filter) : null, Sort).Select(Map).ToList();
            return Json(GetHtmlStr(list, _sortType).Replace("border=\"1\"", ""));
        }

        public ViewResult Search()
        {
            return View();
        }

        public FileResult Export()
        {
            int count;
            var list = _rep.QueryByPage(0, int.MaxValue, out count, null, Sort).Select(Map).ToList();
            var str = GetHtmlStr(list, _sortType).Replace("<tr style=\"height: 10px;\"></tr>", "");
            return File(Encoding.UTF8.GetBytes(str), "application/excel", "发车屏记录报表.xls");
        }

        public JsonResult Sort_changed(SortTypeEnum? ddlSort)
        {
            int count;
            var list = _rep.QueryByPage(0, int.MaxValue, out count, null, Sort).Select(Map).ToList();
            var str = string.Empty;
            switch (ddlSort)
            {
                case SortTypeEnum.Default:
                    str = GetHtmlStr(list);
                    _sortType = ddlSort.Value;
                    break;
                case SortTypeEnum.Line:
                    str = GetHtmlStr(list, SortTypeEnum.Line);
                    _sortType = ddlSort.Value;
                    break;
                case SortTypeEnum.Date:
                    str = GetHtmlStr(list, SortTypeEnum.Date);
                    _sortType = ddlSort.Value;
                    break;
                case null:
                    break;
                default:
                    throw new ArgumentOutOfRangeException("ddlSort", ddlSort, null);
            }
            return Json(str.Replace("border=\"1\"", ""));
        }

        private string GetHtmlStr(List<ScreenRecStats> list, SortTypeEnum type = SortTypeEnum.Default)
        {
            var htmlStr = new StringBuilder(
                "<table class=\"x-table\" border=\"1\" style=\"left: 0; table-layout: fixed; width: 857px; position: absolute;text-align:center\" cellspacing=\"0\" cellpadding=\"0\"><colgroup><col style=\"width: 50px;\"><col style=\"width: 60px;\"><col style=\"width: 150px;\"><col style=\"width: 120px;\"><col style=\"width: 100px;\"><col style=\"width: 220px;\"></colgroup><tbody><tr style=\"height: 43px;\"><td class=\"title\" style=\"background-color: rgb(235, 247, 252); color: rgb(42, 135, 198); border-bottom-color: rgb(149, 211, 244); font-family: Microsoft YaHei UI;\" colspan=\"6\"> &nbsp; 发车屏记录报表</td></tr><tr style=\"height: 10px;\"></tr><tr style=\"height: 29px;\"><td class=\"td colName\" style=\"text-align: center;\">序号</td><td class=\"td colName\" style=\"text-align: center;\">屏号</td><td class=\"td colName\" style=\"text-align: center;\">详细信息</td><td class=\"td colName\" style=\"text-align: center;\">施工类型</td><td class=\"td colName\" style=\"text-align: center;\">施工日期</td><td class=\"td colName\" style=\"text-align: center;\">备注</td></tr>");
            if (list.Any())
            {
                if (type != SortTypeEnum.Date)
                {
                    var devNumList = list.GroupBy(t => new {t.DeviceNum, t.Details}).Select(t => t.Key).ToList();
                    if (type == SortTypeEnum.Line)
                        devNumList = devNumList.OrderBy(t => t.Details).ToList();
                    var no = 0;
                    foreach (var devNum in devNumList)
                    {
                        no++;
                        var num = devNum.DeviceNum;
                        var recs = list.Where(t => t.DeviceNum == num && t.Details == devNum.Details).ToList();
                        var rec = recs.First();
                        var recCount = recs.Count;
                        if (recCount > 1)
                        {
                            htmlStr.AppendFormat(
                                "<tr class=\"tr\" style=\"text-align: center;\"><td class=\"td td-data\" rowspan=\"{5}\">{6}</td><td class=\"td td-data azure\" rowspan=\"{5}\">{0}</td><td class=\"td td-data azure\" rowspan=\"{5}\">{1}</td><td class=\"td td-data\">{2}</td><td class=\"td td-data\">{3:yyyy-MM-dd}</td><td class=\"td td-data\">{4}</td></tr>",
                                num, rec.Details, rec.ConstructionType, rec.Date, rec.Remark, recCount, no);
                            recs.RemoveAt(0);
                            foreach (var r in recs)
                            {
                                htmlStr.AppendFormat(
                                    "<tr class=\"tr\" style=\"text-align: center;\"><td class=\"td td-data\">{0}</td><td class=\"td td-data\">{1:yyyy-MM-dd}</td><td class=\"td td-data\">{2}</td></tr>",
                                    r.ConstructionType, r.Date, r.Remark);
                            }
                        }
                        else
                        {
                            htmlStr.AppendFormat(
                                "<tr class=\"tr\" style=\"text-align: center;\"><td class=\"td td-data\">{5}</td><td class=\"td td-data azure\">{0}</td><td class=\"td td-data azure\">{1}</td><td class=\"td td-data\">{2}</td><td class=\"td td-data\">{3:yyyy-MM-dd}</td><td class=\"td td-data\">{4}</td></tr>",
                                num, rec.Details, rec.ConstructionType, rec.Date, rec.Remark, no);
                        }
                    }
                }
                else
                {
                    var i = 0;
                    foreach (var rec in list.OrderBy(t => t.Date).ToList())
                    {
                        i++;
                        htmlStr.AppendFormat(
                            "<tr class=\"tr\" style=\"text-align: center;\"><td class=\"td td-data\">{5}</td><td class=\"td td-data azure\">{0}</td><td class=\"td td-data azure\">{1}</td><td class=\"td td-data\">{2}</td><td class=\"td td-data\">{3:yyyy-MM-dd}</td><td class=\"td td-data\">{4}</td></tr>",
                            rec.DeviceNum, rec.Details, rec.ConstructionType, rec.Date, rec.Remark, i);
                    }
                }
            }
            else
            {
                htmlStr.Append("<tr class=\"tr\"><td class=\"td td-data\" colspan=\"6\">没有数据</td></tr>");
            }
            htmlStr.Append("</tbody></table>");
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
                filter.Any() ? Builders<ScreenRec>.Filter.And(filter) : null, Sort).Select(Map).ToList();

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
                Details = string.Format("{0} - {1}", rec.LineName.Replace("/", "、"), rec.InstallStation),
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