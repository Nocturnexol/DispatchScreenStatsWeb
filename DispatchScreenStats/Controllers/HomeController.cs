using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using DispatchScreenStats.Common;
using DispatchScreenStats.Enums;
using DispatchScreenStats.IRepository;
using DispatchScreenStats.Models;
using DispatchScreenStats.Repository;
using FineUIMvc;

namespace DispatchScreenStats.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IMongoRepository<Auth> _repAuth = new MongoRepository<Auth>();
        public ActionResult Index()
        {
            var treeNodes = new List<TreeNode>();

            var userManage = new TreeNode {Text = "用户管理", NavigateUrl = "User"};
            var sysManage=new TreeNode{Text = "系统管理",Expanded = true};
            sysManage.Nodes.Add(userManage);

            var user = CommonHelper.User;
            if (user == null) return View();
            int[] vals;
            int[] vals2;
            if (user == "admin")
            {
                treeNodes.Add(sysManage);
                vals = (from MenuScreenEnum e in Enum.GetValues(typeof(MenuScreenEnum)) select (int) e).ToArray();
                vals2 = (from MenuPriceEnum e in Enum.GetValues(typeof(MenuPriceEnum)) select (int) e).ToArray();
            }
            else
            {
                int uid;
                if (int.TryParse(CommonHelper.UserId, out uid))
                {
                    var auth = _repAuth.Get(t => t.UserId == int.Parse(CommonHelper.UserId));
                    vals = auth != null ? auth.Values ?? new int[] {} : new int[] {};
                    vals2 = auth != null ? auth.Values2 ?? new int[] {} : new int[] {};
                }
                else
                {
                    //获取权限出错
                    vals = new int[] {};
                    vals2 = new int[] { };
                }
            }

            if (vals.Any())
            {
                var m = new TreeNode { Text = typeof(MenuEnum).GetField(MenuEnum.ScreenStats.ToString()).GetCName(),Expanded = true};
                foreach (var val in vals)
                {
                    m.Nodes.Add(new TreeNode { Text = typeof(MenuScreenEnum).GetField(((MenuScreenEnum)val).ToString()).GetCName(), NavigateUrl = MenuEnum.ScreenStats + "/" + ((MenuScreenEnum)val).ToString().Replace("_","/") });
                }
                treeNodes.Add(m);
            }
            if (vals2.Any())
            {
                var m = new TreeNode { Text = typeof(MenuEnum).GetField(MenuEnum.ScreenPrice.ToString()).GetCName(), Expanded = true };
                foreach (var val in vals2)
                {
                    m.Nodes.Add(new TreeNode { Text = typeof(MenuPriceEnum).GetField(((MenuPriceEnum)val).ToString()).GetCName(), NavigateUrl = MenuEnum.ScreenPrice + "/" + (MenuPriceEnum)val });
                }
                treeNodes.Add(m);
            }
            return View(treeNodes.ToArray());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult btnHello_Click()
        {
            Alert.Show("你好 ！", MessageBoxIcon.Warning);

            return UIHelper.Result();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult onSignOut_Click()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Index", "Login");
        }

        // GET: Themes
        public ActionResult Themes()
        {
            return View();
        }
	}
}