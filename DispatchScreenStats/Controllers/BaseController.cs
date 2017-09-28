using System;
using System.Collections.Generic;
using System.Web.Mvc;
using FineUIMvc;

namespace DispatchScreenStats.Controllers
{
    public class BaseController : Controller
    {
        protected const int PageSize = 20;
        /// <summary>
        /// 显示通知对话框
        /// </summary>
        /// <param name="message"></param>
        public virtual void ShowNotify(string message)
        {
            ShowNotify(message, MessageBoxIcon.Information);
        }

        /// <summary>
        /// 显示通知对话框
        /// </summary>
        /// <param name="message"></param>
        /// <param name="messageIcon"></param>
        public virtual void ShowNotify(string message, MessageBoxIcon messageIcon)
        {
            ShowNotify(message, messageIcon, Target.Top);
        }

        /// <summary>
        /// 显示通知对话框
        /// </summary>
        /// <param name="message"></param>
        /// <param name="messageIcon"></param>
        /// <param name="target"></param>
        public virtual void ShowNotify(string message, MessageBoxIcon messageIcon, Target target)
        {
            var n = new Notify
            {
                Target = target,
                Message = message,
                MessageBoxIcon = messageIcon,
                PositionX = Position.Center,
                PositionY = Position.Top,
                DisplayMilliseconds = 3000,
                ShowHeader = false
            };

            n.Show();
        }
         #region 上传文件类型判断

        protected readonly IList<string> ValidFileTypes = new[]
        {
            "xls",
            "xlsx"
        };

        protected string GetFileType(string fileName)
        {
            var fileType = string.Empty;
            var lastDotIndex = fileName.LastIndexOf(".", StringComparison.Ordinal);
            if (lastDotIndex >= 0)
            {
                fileType = fileName.Substring(lastDotIndex + 1).ToLower();
            }

            return fileType;
        }


        #endregion
	}
}