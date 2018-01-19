using System;
using System.Collections.Specialized;
using System.Linq;
using System.Web.Mvc;
using DispatchScreenStats.Common;
using DispatchScreenStats.Enums;
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
        private readonly IMongoRepository<Auth> _repAuth = new MongoRepository<Auth>();
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
                        model.UserPwd = CommonHelper.GetMd5(model.UserPwd, true);
                        _rep.Add(model);
                        // 关闭本窗体（触发窗体的关闭事件）
                        PageContext.RegisterStartupScript(ActiveWindow.GetHidePostBackReference());
                    }
                }
                else
                {
                    if (!(model.UserPwd.Length > 18)) model.UserPwd = CommonHelper.GetMd5(model.UserPwd, true);
                    _rep.Update(t => t._id == model._id,
                        Builders<User>.Update.Set(t => t.UserName, model.UserName)
                            .Set(t => t.UserPwd,model.UserPwd)
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


        public ViewResult Auth(string id)
        {
            var userAuth = _repAuth.Get(t => t.UserId == int.Parse(id));
            ViewBag.cblScreen = CommonHelper.GetMenuEnumList(typeof(MenuScreenEnum));
            ViewBag.cblPrice = CommonHelper.GetMenuEnumList(typeof(MenuPriceEnum));
            if (userAuth != null)
            {
                ViewBag.cblScreenSelected = userAuth.Values != null
                    ? userAuth.Values.Select(t => t.ToString()).ToArray()
                    : new string[] {};
                ViewBag.cblPriceSelected = userAuth.Values2 != null
                    ? userAuth.Values2.Select(t => t.ToString()).ToArray()
                    : new string[] {};
            }
            else
            {
                ViewBag.cblScreenSelected = new string[] {};
                ViewBag.cblPriceSelected = new string[] {};
            }
            ViewBag.id = id;
            return View(userAuth);
        }

        [HttpPost]
        public ActionResult Auth(string id, int[] screen, int[] price, int range, int permission)
        {
            var auth = _repAuth.Get(t => t.UserId == int.Parse(id));
            if (auth == null)
            {
                _repAuth.Add(new Auth {UserId = int.Parse(id), Values = screen, Values2 = price, Range = range,Permission = permission});
            }
            else
            {
                _repAuth.Update(t => t.UserId == int.Parse(id),
                    Builders<Auth>.Update.Set(t => t.Values, screen)
                        .Set(t => t.Values2, price)
                        .Set(t => t.Range, range)
                        .Set(t => t.Permission, permission));
            }
            ShowNotify("授权成功");
            PageContext.RegisterStartupScript(ActiveWindow.GetHidePostBackReference());
            return UIHelper.Result();
        }

        public ViewResult ChangePwd(string id)
        {
            return View(int.Parse(id));
        }

        [HttpPost]
        public ActionResult ChangePwd(int? id,string pwd)
        {
            _rep.Update(t => t._id == id, Builders<User>.Update.Set(t => t.UserPwd, CommonHelper.GetMd5(pwd, true)));
            ShowNotify("修改成功");
            PageContext.RegisterStartupScript(ActiveWindow.GetHidePostBackReference());
            return UIHelper.Result();
        }
	}
}