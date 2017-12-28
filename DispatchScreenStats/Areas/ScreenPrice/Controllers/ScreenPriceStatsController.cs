using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Web;
using System.Web.Mvc;
using DispatchScreenStats.IRepository;
using DispatchScreenStats.Models;
using DispatchScreenStats.Repository;
using MongoDB.Driver;

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
        [HttpPost]
        public JsonResult Search()
        {
            var list = _rep.Find(_f).ToList().OrderBy(t => t.DeviceNum).Select(Map).ToList();
            return Json(GetHtmlStr(list).Replace("border=\"1\"", ""));
        }
        private string GetHtmlStr(List<ScreenPriceStats> list)
        {
            var htmlStr = new StringBuilder(
              "<table class=\"x-table\" border=\"1\" style=\"left: 0; table-layout: fixed; width: 857px; position: absolute;text-align:center\" cellspacing=\"0\" cellpadding=\"0\"><colgroup><col style=\"width: 50px;\"><col style=\"width: 60px;\"><col style=\"width: 150px;\"><col style=\"width: 120px;\"><col style=\"width: 100px;\"></colgroup><tbody><tr style=\"height: 43px;\"><td class=\"title\" style=\"background-color: rgb(235, 247, 252); color: rgb(42, 135, 198); border-bottom-color: rgb(149, 211, 244); font-family: Microsoft YaHei UI;\" colspan=\"5\"> &nbsp; 整屏费用报表</td></tr><tr style=\"height: 10px;\"></tr><tr style=\"height: 29px;\"><td class=\"td colName\" style=\"text-align: center;\">序号</td><td class=\"td colName\" style=\"text-align: center;\">屏号</td><td class=\"td colName\" style=\"text-align: center;\">详细信息</td><td class=\"td colName\" style=\"text-align: center;\">金额</td><td class=\"td colName\" style=\"text-align: center;\">收费日期</td></tr>");
            if (list.Any())
            {
                var no = 1;
                foreach (var rec in list)
                {
                    htmlStr.AppendFormat(
                               "<tr class=\"tr\" style=\"text-align: center;\"><td class=\"td td-data\">{4}</td><td class=\"td td-data azure\">{0}</td><td class=\"td td-data azure\">{1}</td><td class=\"td td-data\">{2}</td><td class=\"td td-data\">{3}</td></tr>",
                               rec.DeviceNum, rec.Details, rec.Price, rec.ChargeTime, no++);
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
                ChargeTime = rec.ChargeTime
            };
        }

	}
}