using System;
using System.Collections.Specialized;
using System.Linq;
using System.Web.Mvc;
using DispatchScreenStats.Common;
using DispatchScreenStats.IRepository;
using DispatchScreenStats.Models;
using DispatchScreenStats.Repository;
using FineUIMvc;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;

namespace DispatchScreenStats.Controllers
{
    public class UserController : BaseController
    {
        private readonly IMongoRepository<User> _rep = new MongoRepository<User>();
        //
        // GET: /User/
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
            var pageSize = Convert.ToInt32(values["Grid1_pageSize"] ?? "0");


            int count;
            var list = _rep.QueryByPage(pageIndex, pageSize, out count);
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
            if (string.IsNullOrEmpty(id)) return View();
            var model = _rep.Get(t => t._id == int.Parse(id));
            if (model == null)
            {
                return HttpNotFound();
            }
            return View(model);
        }
        [HttpPost]
        public ActionResult AddOrEdit(User model)
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
                        model._id = (int)(_rep.Max(t => t._id) ?? 0) + 1;
                        model.UserPwd = CommonHelper.GetMd5("123456", true);
                        _rep.Add(model);
                        // 关闭本窗体（触发窗体的关闭事件）
                        PageContext.RegisterStartupScript(ActiveWindow.GetHidePostBackReference());
                    }
                }
                else
                {
                    _rep.Update(t => t._id == model._id,
                        Builders<User>.Update.Set(t => t.UserName, model.UserName)
                            .Set(t => t.LoginName, model.LoginName)
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

        private bool CheckRepeat(User model)
        {
            var modelDb =_rep.Get(t =>t.LoginName==model.LoginName);
            if (modelDb == null) return true;
            return model._id == modelDb._id;
        }
	}
}