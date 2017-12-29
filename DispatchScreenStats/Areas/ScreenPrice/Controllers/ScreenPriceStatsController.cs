using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using DispatchScreenStats.Common;
using DispatchScreenStats.Enums;
using DispatchScreenStats.IRepository;
using DispatchScreenStats.Models;
using DispatchScreenStats.Repository;
using FineUIMvc;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;

namespace DispatchScreenStats.Areas.ScreenPrice.Controllers
{
    public class ScreenPriceStatsController : Controller
    {
        private readonly IMongoRepository<ScreenRec> _rep = new MongoRepository<ScreenRec>();
        private readonly IMongoRepository<ScreenRecDetail> _repDetail = new MongoRepository<ScreenRecDetail>();
        private readonly Expression<Func<ScreenRec, bool>> _f = t => !t.IsLog;
        //
        // GET: /ScreenPrice/ScreenPriceStats/
        public ActionResult Index()
        {
            return View();
        }
        public ViewResult Search()
        {
            ViewBag.ScreenTypes = CommonHelper.GetEnumSelectList(typeof(ScreenTypeEnum));
            ViewBag.Owners = _rep.Distinct(t => t.Owner).OrderBy(t => t).Select(t => new ListItem(t.ToString(), t.ToString())).ToArray();
            return View();
        }
        [HttpPost]
        public JsonResult Search(FormCollection values)
        {
            var devNum = values["tbDevNum"];
            var owner = values["ddlOwner[]"];
            var line = values["tbLine"];
            var isFuzzyLine = values["cbIsFuzzyLine"];
            var station = values["tbStation"];
            var isFuzzyStation = values["cbIsFuzzyStation"];
            var screenType = values["tbType"];
            var screenCount = values["nbCount"];
            var inStallDate = values["tbDate"];
            var filter = new List<FilterDefinition<ScreenRec>>
            {
                _f
            };
            if (!string.IsNullOrWhiteSpace(devNum))
            {
                filter.Add(Builders<ScreenRec>.Filter.Eq(t => t.DeviceNum, devNum));
            }
            if (owner != null)
            {
                var arr = owner.Split(',');
                if(arr.Any())
                    filter.Add(Builders<ScreenRec>.Filter.In(t => t.Owner, arr));
            }
            if (!string.IsNullOrWhiteSpace(line))
            {
                var isFuzzy = bool.Parse(isFuzzyLine);
                if (isFuzzy)
                {
                    filter.Add(Builders<ScreenRec>.Filter.Regex(t => t.LineName,
                        new BsonRegularExpression(new Regex(line.Trim()))));
                }
                else
                {
                    filter.Add(Builders<ScreenRec>.Filter.Eq(t => t.LineName, line.Trim()));
                }
            }
            if (!string.IsNullOrWhiteSpace(station))
            {
                var isFuzzy = bool.Parse(isFuzzyStation);
                if (isFuzzy)
                {
                    filter.Add(Builders<ScreenRec>.Filter.Regex(t => t.InstallStation,
                        new BsonRegularExpression(new Regex(station.Trim()))));
                }
                else
                {
                    filter.Add(Builders<ScreenRec>.Filter.Eq(t => t.InstallStation, station.Trim()));
                }
            }
            if (!string.IsNullOrWhiteSpace(screenType))
            {
                filter.Add(Builders<ScreenRec>.Filter.Eq(t => t.ScreenType,
                    Enum.Parse(typeof(ScreenTypeEnum), screenType)));
            }
            if (!string.IsNullOrWhiteSpace(screenCount))
            {
                int c;
                if (int.TryParse(screenCount, out c))
                {
                    filter.Add(Builders<ScreenRec>.Filter.Eq(t => t.ScreenCount, c));
                }
            }
            if (!string.IsNullOrWhiteSpace(inStallDate))
            {
                filter.Add(Builders<ScreenRec>.Filter.Eq(t => t.InstallDate, DateTime.Parse(inStallDate)));
            }
            var list = _rep.Find(filter.Any() ? Builders<ScreenRec>.Filter.And(filter) : null).ToList().OrderBy(t => t.DeviceNum).Select(Map).ToList();
            return Json(GetHtmlStr(list).Replace("border=\"1\"", ""));
        }
        private string GetHtmlStr(List<ScreenPriceStats> list)
        {
            var htmlStr = new StringBuilder(
              "<table class=\"x-table\" border=\"1\" style=\"left: 0; table-layout: fixed; width: 857px; position: absolute;text-align:center\" cellspacing=\"0\" cellpadding=\"0\"><colgroup><col style=\"width: 50px;\"><col style=\"width: 60px;\"><col style=\"width: 150px;\"><col style=\"width: 120px;\"><col style=\"width: 100px;\"><col style=\"width: 100px;\"><col style=\"width: 100px;\"><col style=\"width: 100px;\"></colgroup><tbody><tr style=\"height: 43px;\"><td class=\"title\" style=\"background-color: rgb(235, 247, 252); color: rgb(42, 135, 198); border-bottom-color: rgb(149, 211, 244); font-family: Microsoft YaHei UI;\" colspan=\"8\"> &nbsp; 整屏费用报表</td></tr><tr style=\"height: 10px;\"></tr><tr style=\"height: 29px;\"><td class=\"td colName\" style=\"text-align: center;\">序号</td><td class=\"td colName\" style=\"text-align: center;\">屏号</td><td class=\"td colName\" style=\"text-align: center;\">详细信息</td><td class=\"td colName\" style=\"text-align: center;\">屏幕类型</td><td class=\"td colName\" style=\"text-align: center;\">金额</td><td class=\"td colName\" style=\"text-align: center;\">付款状态</td><td class=\"td colName\" style=\"text-align: center;\">发生时间</td><td class=\"td colName\" style=\"text-align: center;\">收费时间</td></tr>");
            if (list.Any())
            {
                var no = 1;
                foreach (var rec in list)
                {
                    htmlStr.AppendFormat(
                               "<tr class=\"tr\" style=\"text-align: center;\"><td class=\"td td-data\">{0}</td><td class=\"td td-data azure\">{1}</td><td class=\"td td-data azure\">{2}</td><td class=\"td td-data\">{3}</td><td class=\"td td-data\">{4}</td><td class=\"td td-data\">{5}</td><td class=\"td td-data\">{6:yyyy-MM-dd}</td><td class=\"td td-data\">{7:yyyy-MM-dd}</td></tr>", no++, rec.DeviceNum, rec.Details, rec.ScreenType, rec.Price, rec.PaymentStatus, rec.OccurredTime, rec.ChargeTime);
                }
            }
            return htmlStr.ToString();
        }

        private static ScreenPriceStats Map(ScreenRec rec)
        {
            return new ScreenPriceStats
            {
                DeviceNum = rec.DeviceNum,
                Details = string.Format("{0} - {1}", rec.LineName.Replace("/", "、"), rec.InstallStation),
                Price = rec.Price.ToString(CultureInfo.InvariantCulture),
                ChargeTime = rec.ChargeTime,
                ScreenType = rec.ScreenType,
                PaymentStatus = rec.PaymentStatus,
                OccurredTime = rec.InstallDate
            };
        }

	}
}