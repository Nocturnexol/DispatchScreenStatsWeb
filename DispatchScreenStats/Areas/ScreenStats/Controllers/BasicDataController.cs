using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
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
    public class BasicDataController : BaseController
    {
        private readonly IMongoRepository<BasicData> _rep = new MongoRepository<BasicData>();

        private readonly SortDefinition<BasicData> _sort =
            new SortDefinitionBuilder<BasicData>().Ascending(t => t.Type);

        //
        // GET: /ScreenStats/BasicData/
        public ActionResult Index()
        {
            int count;
            var list = _rep.QueryByPage(0, PageSize, out count, null, _sort);
            ViewBag.RecordCount = count;
            ViewBag.PageSize = PageSize;
            return View(list);
        }

        private void UpdateGrid(NameValueCollection values)
        {
            var fields = JArray.Parse(values["Grid1_fields"]);
            var pageIndex = Convert.ToInt32(values["Grid1_pageIndex"] ?? "0");
            var pageSize = Convert.ToInt32(values["Grid1_pageSize"] ?? "0");
            int count;
            var list = _rep.QueryByPage(pageIndex, pageSize, out count, null, _sort);
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

        public ActionResult Delete(JArray selectedRows, FormCollection values)
        {
            var ids = selectedRows.Select(Convert.ToInt32).ToList();
            _rep.Delete(t => ids.Contains(t._id));
            UpdateGrid(values);
            return UIHelper.Result();
        }

        public ActionResult AddOrEdit(string id)
        {
            var types = ConfigurationManager.AppSettings["basicTypes"];
            if (string.IsNullOrEmpty(types))
            {
                Alert.Show("缺少类型配置", MessageBoxIcon.Warning);
                return UIHelper.Result();
            }
            ViewBag.TypeList =
                types.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => new ListItem(t, t))
                    .ToArray();
            if (string.IsNullOrEmpty(id)) return View();
            var model = _rep.Get(t => t._id == int.Parse(id));
            if (model == null)
            {
                return HttpNotFound();
            }
            return View(model);
        }

        [HttpPost]
        public ActionResult AddOrEdit(BasicData model)
        {
            if (!ModelState.IsValid) return UIHelper.Result();
            try
            {
                if (model._id == 0)
                {
                    if (!CheckRepeat(model))
                    {
                        Alert.Show("已有相同记录存在！", MessageBoxIcon.Warning);
                    }
                    else
                    {
                        _rep.Add(model);
                        // 关闭本窗体（触发窗体的关闭事件）
                        PageContext.RegisterStartupScript(ActiveWindow.GetHidePostBackReference());
                    }
                }
                else
                {
                    _rep.Update(t => t._id == model._id,
                        Builders<BasicData>.Update.Set(t => t.Name, model.Name)
                            .Set(t => t.Type, model.Type)
                            .Set(t => t.Remark, model.Remark));
                    // 关闭本窗体（触发窗体的关闭事件）
                    PageContext.RegisterStartupScript(ActiveWindow.GetHidePostBackReference());
                }
            }
            catch (Exception ex)
            {
                Alert.Show(ex.Message, MessageBoxIcon.Warning);
            }
            return UIHelper.Result();
        }

        private bool CheckRepeat(BasicData model)
        {
            var modelDb = _rep.Get(t => t.Name == model.Name);
            if (modelDb == null) return true;
            return model._id == modelDb._id;
        }
    }
}