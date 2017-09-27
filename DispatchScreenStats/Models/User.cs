namespace DispatchScreenStats.Models
{
    public class User
    {
        public int _id { set; get; }
        public int DefaultRoleId { set; get; }
        public string LoginName { set; get; }
        public string UserName { set; get; }
        public string EnglishName { set; get; }
        public string UserMark { set; get; }
        public string dept { set; get; }
        public string dept_New { set; get; }
        public string UserPwd { set; get; }
        public string PassWord { set; get; }
        public string Remark { set; get; }

        /// <summary>
        /// 失败次数
        /// </summary>
        public int FailTimes { set; get; }
    }
}