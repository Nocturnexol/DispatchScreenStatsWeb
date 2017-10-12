using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
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
        //
        // GET: /ScreenStats/ScreenRecStats/
        public ActionResult Index()
        {
            int count;
            var list = _rep.QueryByPage(0, PageSize, out count).Select(Map).ToList();
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
                filter.Any() ? Builders<ScreenRec>.Filter.And(filter) : null).Select(Map).ToList();

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