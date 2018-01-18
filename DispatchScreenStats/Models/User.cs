using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;

namespace DispatchScreenStats.Models
{
    public class User
    {
        public int _id { set; get; }
        public int DefaultRoleId { set; get; }
        [Display(Name = "登录名"),Required]
        public string LoginName { set; get; }
        [Display(Name = "用户名"), Required]
        public string UserName { set; get; }
        public string EnglishName { set; get; }
        public string UserMark { set; get; }
        public string dept { set; get; }
        public string dept_New { set; get; }
        [Display(Name = "密码"), Required,PasswordPropertyText,MinLength(6)]
        public string UserPwd { set; get; }
        public string PassWord { set; get; }
        [Display(Name = "备注")]
        public string Remark { set; get; }

        /// <summary>
        /// 失败次数
        /// </summary>
        public int FailTimes { set; get; }
    }


    public class Auth
    {
        public ObjectId _id { get; set; }
        public int UserId { get; set; }
        public int[] Values { get; set; }
        public int[] Values2 { get; set; }
        public int Range { get; set; }
        public int Permission { get; set; }
    }
}