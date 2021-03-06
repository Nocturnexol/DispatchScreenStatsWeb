﻿using System;
using System.Text.RegularExpressions;
using System.Web;
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
        //[ValidateAntiForgeryToken]
        public ActionResult btnLogin_Click(string tbxUserName, string tbxPassword)
        {
            var filter =
                Builders<User>.Filter.And(
                    Builders<User>.Filter.Regex(t => t.UserPwd,
                        new BsonRegularExpression(new Regex(CommonHelper.GetMd5(tbxPassword), RegexOptions.IgnoreCase))),
                    Builders<User>.Filter.Eq(t => t.LoginName, tbxUserName));

            var user = _rep.Get(filter);
            if (user != null)
            {
                Response.Cookies.Add(new HttpCookie("user", tbxUserName) {Expires = DateTime.Now.AddDays(1)});
                Response.Cookies.Add(new HttpCookie("userName",HttpUtility.UrlEncode(user.UserName)) { Expires = DateTime.Now.AddDays(1) });
                Response.Cookies.Add(new HttpCookie("userId", user._id.ToString()) {Expires = DateTime.Now.AddDays(1)});
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