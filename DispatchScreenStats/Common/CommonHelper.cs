using System;
using System.Security.Cryptography;
using System.Text;

namespace DispatchScreenStats.Common
{
    public class CommonHelper
    {
        /// <summary>
        /// 获取md5
        /// </summary>
        /// <param name="str">需转的字符串</param>
        /// <param name="isLower">是否32位小写  默认32位大写</param>
        /// <returns></returns>
        public static string GetMd5(string str, bool isLower = false)
        {
            try
            {
                var md5 = new MD5CryptoServiceProvider();
                var reStr = BitConverter.ToString(md5.ComputeHash(Encoding.Default.GetBytes(str))).Replace("-", "");
                reStr = isLower ? reStr.ToLower() : reStr;
                return reStr;
            }
            catch (Exception ex)
            {
                throw new Exception("GetMD5() failed,error:" + ex.Message);
            }
        }
    }
}