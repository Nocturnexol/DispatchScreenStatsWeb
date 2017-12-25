using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using FineUIMvc;

namespace DispatchScreenStats.Common
{
    public static class CommonHelper
    {
        public static string User { get
        {
            var cookie = HttpContext.Current.Request.Cookies["user"];
            return cookie != null ? cookie.Value : null;
        } }
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

        public static ListItem[] GetEnumSelectList(Type type)
        {
            var res=new List<ListItem>
            {
                new ListItem("全部","")
            };
            res.AddRange(from object val in Enum.GetValues(type) select new ListItem(val.ToString(), ((int) val).ToString()));

            return res.ToArray();
        }

        /// <summary>
        /// Http发送Post请求方法
        /// </summary>
        /// <param name="url"></param>
        /// <param name="postData"></param>
        /// <returns></returns>
        public static string HttpPost(string url, string postData)
        {
            //定义request并设置request的路径
            WebRequest request = WebRequest.Create(url);

            //定义请求的方式
            request.Method = "POST";

            //设置参数的编码格式，解决中文乱码
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);

            //设置request的MIME类型及内容长度
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = byteArray.Length;

            //打开request字符流
            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);

            //定义response为前面的request响应
            WebResponse response = request.GetResponse();

            //获取相应的状态代码
            Console.WriteLine(((HttpWebResponse)response).StatusDescription);

            //定义response字符流
            dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string responseFromServer = reader.ReadToEnd();//读取所有
            Console.WriteLine(responseFromServer);

            //关闭资源
            reader.Close();
            dataStream.Close();
            response.Close();
            return responseFromServer;
        }

        /// <summary>
        /// Http发送Get请求方法
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string HttpGet(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.ContentType = "text/html;charset=UTF-8";

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream rs = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(rs, Encoding.UTF8);
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            rs.Close();

            return retString;
        }

        /// <summary>
        /// 动态创建Lambda表达式
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static Expression<Func<T, object>> ToLambda<T>(this string propertyName)
        {
            //1.创建表达式参数（指定参数或变量的类型:p）  
            ParameterExpression param = Expression.Parameter(typeof(T));
            //2.构建表达式体(类型包含指定的属性:p.Name)  
            MemberExpression body = Expression.Property(param, propertyName);
            //var body = Expression.MakeMemberAccess(param, typeof(T).GetProperty(propertyName));
            //3.根据参数和表达式体构造一个lambda表达式  
            //var funType = typeof(Func<,>).MakeGenericType(typeof(T), typeof(object));
            return Expression.Lambda<Func<T, object>>(Expression.Convert(body, typeof(object)), param);
        }

        public static IList<T> ToList<T>(this DataTable dt) where T:new()
        {
            IList<T> ts = new List<T>();
            foreach (DataRow dr in dt.Rows)
            {
                T t = new T();
                // 获得此模型的公共属性      
                PropertyInfo[] propertys = t.GetType().GetProperties();
                foreach (PropertyInfo pi in propertys)
                {
                    var tempName = pi.Name;
                    if (dt.Columns.Contains(tempName))
                    {
                        // 判断此属性是否有Setter      
                        if (!pi.CanWrite) continue;
                        object value = dr[tempName];
                        if (value != DBNull.Value)
                            pi.SetValue(t, value, null);
                    }
                }
                ts.Add(t);
            }
            return ts;
        }

        public static string GetCName(this Type type,string key)
        {
            var prop = type.GetProperty(key);
            if (prop == null) return null;
            var attr = prop.GetCustomAttribute(typeof(DisplayAttribute)) as DisplayAttribute;
                return attr != null ? attr.Name : key;
        }

    }
}