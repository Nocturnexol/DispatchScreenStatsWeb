﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Web.Mvc;
using DispatchScreenStats.Controllers;
using DispatchScreenStats.IRepository;
using DispatchScreenStats.Models;
using DispatchScreenStats.Repository;
using FineUIMvc;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;

namespace DispatchScreenStats.Areas.ScreenStats.Controllers
{
    public class AccessoryController : BaseController
    {
        private readonly IMongoRepository<Accessory> _rep = new MongoRepository<Accessory>();
        private readonly IMongoRepository<BasicData> _repBasic = new MongoRepository<BasicData>();
        private readonly IMongoRepository<ScreenRecDetail> _repDetail = new MongoRepository<ScreenRecDetail>();
        //
        // GET: /ScreenStats/Accessory/
        //[OutputCache(Duration = 120 ,VaryByParam = "*")]
        public ActionResult Index(string id,string isLog)
        {
            int count;
            var list = _rep.QueryByPage(0, PageSize, out count,
                string.IsNullOrEmpty(isLog) ? Builders<Accessory>.Filter.Eq(t => t.RecId, int.Parse(id)) : Builders<Accessory>.Filter.Eq(t => t.RecDetailId, int.Parse(id)));
            ViewBag.RecordCount = count;
            ViewBag.PageSize = PageSize;
            ViewBag.id = id;
            ViewBag.isLog = isLog;
            ViewBag.Names =
                _repBasic.Find(t => t.Type == "配件").ToList().Select(t => new ListItem(t.Name, t.Name)).ToArray();
            return View(list);
        }
        public FileResult Export(string id,string isLog)
        {
            var list = _rep.Find(string.IsNullOrEmpty(isLog) ? Builders<Accessory>.Filter.Eq(t => t.RecId, int.Parse(id)) : Builders<Accessory>.Filter.Eq(t => t.RecDetailId, int.Parse(id))).ToList();
            const string thHtml = "<th>{0}</th>";
            const string tdHtml = "<td style=\"text-align: center;\">{0}</td>";

            var sb = new StringBuilder();
            sb.Append("<table cellspacing=\"0\" rules=\"all\" border=\"1\" style=\"border-collapse:collapse;\">");
            sb.Append("<tr>");
            sb.AppendFormat(thHtml, "");
            sb.AppendFormat(thHtml, "名称");
            sb.AppendFormat(thHtml, "数量");
            sb.AppendFormat(thHtml, "类型");
            sb.AppendFormat(thHtml, "布局");
            sb.AppendFormat(thHtml, "价格");
            sb.AppendFormat(thHtml, "备注");
            sb.Append("</tr>");

            var rowIndex = 1;
            foreach (var item in list)
            {
                sb.Append("<tr>");
                sb.AppendFormat(tdHtml, rowIndex++);
                sb.AppendFormat(tdHtml, item.Name);
                sb.AppendFormat(tdHtml, item.Count);
                sb.AppendFormat(tdHtml, item.Type);
                sb.AppendFormat(tdHtml, item.Layout);
                sb.AppendFormat(tdHtml, item.Price);
                sb.AppendFormat(tdHtml, item.Remark);
                sb.Append("</tr>");
            }
            sb.Append("</table>");
            return File(Encoding.UTF8.GetBytes(sb.ToString()), "application/excel", "配件列表.xls");
        }
        public ActionResult btnSubmit_Click(JArray Grid1_fields, JArray Grid1_modifiedData, int Grid1_pageIndex,
            int Grid1_pageSize,string id,string isLog)
        {
            if (!Grid1_modifiedData.Any())
            {
                ShowNotify("无修改数据！");
                return UIHelper.Result();
            }
            //var detail = _repDetail.Get(t => t._id == int.Parse(id));
            //var detailIds = new List<int>();
            //if (detail.IsLog)
            //{
            //    detailIds.Add(detail._id);
            //}
            //else
            //{
            //    detailIds.AddRange(
            //        _repDetail.Find(t => t.DeviceNum == detail.DeviceNum && !t.IsLog)
            //            .ToList()
            //            .Select(t => t._id)
            //            .ToList());
            //}

            foreach (var jToken in Grid1_modifiedData)
            {
                var modifiedRow = (JObject)jToken;
                string status = modifiedRow.Value<string>("status");
                var rowId = modifiedRow.Value<string>("id");

                var rowDic = modifiedRow.Value<JObject>("values").ToObject<Dictionary<string, object>>();
                if (status == "newadded")
                {

                    var model = new Accessory();
                    foreach (var p in rowDic)
                    {
                         typeof(Accessory).GetProperty(p.Key).SetValue(model, p.Value);
                    }
                    //model.RecDetailIds = detailIds.ToArray();
                    if (string.IsNullOrEmpty(isLog)) model.RecId = int.Parse(id);
                    else model.RecDetailId = int.Parse(id);
                    _rep.Add(model);
                }
                else if (status == "modified")
                {
                    foreach (var p in rowDic)
                    {
                        var param = Expression.Parameter(typeof(Accessory), "x");
                        var body = Expression.Property(param, typeof(Accessory), p.Key);
                        var lambda =
                            Expression.Lambda<Func<Accessory, object>>(Expression.Convert(body, typeof(object)), param);
                        _rep.Update(t => t._id == int.Parse(rowId), Builders<Accessory>.Update.Set(lambda, p.Value));
                    }
                }
                else if (status == "deleted")
                {
                    _rep.Delete(t => t._id == int.Parse(rowId));
                }
            }
            int count;
            var source = _rep.QueryByPage(Grid1_pageIndex, Grid1_pageSize, out count, string.IsNullOrEmpty(isLog) ? Builders<Accessory>.Filter.Eq(t => t.RecId, int.Parse(id)) : Builders<Accessory>.Filter.Eq(t => t.RecDetailId, int.Parse(id)));
            var grid1 = UIHelper.Grid("Grid1");
            grid1.RecordCount(count);
            grid1.DataSource(source, Grid1_fields);
            ShowNotify("数据保存成功！");
            return UIHelper.Result();

        }
        public ActionResult DoSearch(FormCollection values)
        {
            UpdateGrid(values);
            return UIHelper.Result();
        }

        private void UpdateGrid(NameValueCollection values)
        {
            var fields = JArray.Parse(values["Grid1_fields"]);
            var pageIndex = Convert.ToInt32(values["Grid1_pageIndex"] ?? "0");
            var pageSize = Convert.ToInt32(values["Grid1_pageSize"] ?? "0");
            var id = values["id"];
            var isLog = values["isLog"];
            int count;
            var list = _rep.QueryByPage(pageIndex, pageSize, out count, string.IsNullOrEmpty(isLog) ? Builders<Accessory>.Filter.Eq(t => t.RecId, int.Parse(id)) : Builders<Accessory>.Filter.Eq(t => t.RecDetailId, int.Parse(id)));
            var grid1 = UIHelper.Grid("Grid1");
            grid1.RecordCount(count);
            grid1.PageSize(pageSize);
            grid1.DataSource(list, fields);
        }
    }
}