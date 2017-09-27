using System.Text.RegularExpressions;
using System.Web.Mvc;
using System.Web.Security;
using DispatchScreenStats.Common;
using DispatchScreenStats.IRepository;
using DispatchScreenStats.Models;
using DispatchScreenStats.Repository;
using FineUIMvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DispatchScreenStats.Controllers
{
    public class LoginController : BaseController
    {
        private readonly IMongoRepository<User> _rep = new MongoRepository<User>();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult btnLogin_Click(string tbxUserName, string tbxPassword)
        {
            var filter =
                Builders<User>.Filter.And(
                    Builders<User>.Filter.Regex(t => t.UserPwd,
                        new BsonRegularExpression(new Regex(CommonHelper.GetMd5(tbxPassword), RegexOptions.IgnoreCase))),
                    Builders<User>.Filter.Eq(t => t.LoginName, tbxUserName));
            if (_rep.Get(filter) != null)
            {
                FormsAuthentication.RedirectFromLoginPage(tbxUserName, false);
            }
            else
            {
                ShowNotify("用户名或密码错误！", MessageBoxIcon.Error);
            }

            return UIHelper.Result();
        }

        public ActionResult Index()
        {
            return View();
        }
    }
}