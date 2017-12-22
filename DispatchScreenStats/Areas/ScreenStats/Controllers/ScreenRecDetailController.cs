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
    public class ScreenRecDetailController : BaseController
    {
        private readonly IMongoRepository<ScreenRecDetail> _rep = new MongoRepository<ScreenRecDetail>();
        //
        // GET: /ScreenStats/ScreenRecDetail/
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
            var key = values["tbxKey"];
            var installDate = values["dpInstallDate"];

            var filter = new List<FilterDefinition<ScreenRecDetail>>();
            if (!string.IsNullOrEmpty(key))
            {
                var filters = new List<FilterDefinition<ScreenRecDetail>>
                {
                    Builders<ScreenRecDetail>.Filter.Regex(t => t.LineName,
                        new BsonRegularExpression(new Regex(key, RegexOptions.IgnoreCase))),
                    Builders<ScreenRecDetail>.Filter.Regex(t => t.InstallStation,
                        new BsonRegularExpression(new Regex(key, RegexOptions.IgnoreCase))),
                        Builders<ScreenRecDetail>.Filter.Regex(t => t.Materials.Remark,
                        new BsonRegularExpression(new Regex(key, RegexOptions.IgnoreCase)))
                };

                //int keyInt;
                //if (int.TryParse(key, out keyInt))
                //{
                //    Expression<Func<ScreenRecDetail, bool>> exp = t => t.Owner == keyInt || t.ScreenCount == keyInt;
                //    filters.Add(exp);
                //}

                filter.Add(Builders<ScreenRecDetail>.Filter.Or(filters));
            }
            if (!string.IsNullOrEmpty(installDate))
            {
                DateTime date;
                if (DateTime.TryParse(installDate, out date))
                {
                    Expression<Func<ScreenRecDetail, bool>> f = t => t.InstallDate == date;
                    filter.Add(f);
                }
                else
                {
                    Alert.Show("无效的日期格式！", MessageBoxIcon.Warning);
                    return;
                }
            }

            int count;
            var list = _rep.QueryByPage(pageIndex, PageSize, out count,
                filter.Any() ? Builders<ScreenRecDetail>.Filter.And(filter) : null);
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