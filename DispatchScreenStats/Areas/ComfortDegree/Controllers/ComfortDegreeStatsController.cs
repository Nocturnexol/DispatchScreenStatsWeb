using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using DispatchScreenStats.Controllers;
using DispatchScreenStats.Enums;
using DispatchScreenStats.IRepository;
using DispatchScreenStats.Models;
using DispatchScreenStats.Repository;
using FineUIMvc;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;

namespace DispatchScreenStats.Areas.ComfortDegree.Controllers
{
    public class ComfortDegreeStatsController : BaseController
    {
        private readonly IMongoRepository<ComfortDegreeStats> _rep = new MongoRepository<ComfortDegreeStats>();
        private readonly string _transPath = ConfigurationManager.AppSettings["transPath"];
        //
        // GET: /ComfortDegree/ComfortDegreeStats/
        public ActionResult Index()
        {
            int count;
            var list = _rep.QueryByPage(0, PageSize, out count);
            ViewBag.RecordCount = count;
            ViewBag.PageSize = PageSize;
            var nums=new List<ListItem>
            {
                new ListItem("全部车号","")
            };
            nums.AddRange(_rep.Distinct(t => t.VehNo).Select(t => new ListItem(t, t)));
            ViewBag.ddlVehNo = nums.ToArray();
            var lines = new List<ListItem>
            {
                new ListItem("全部线路","")
            };
            lines.AddRange(_rep.Distinct(t => t.LineName).Select(t => new ListItem(t, t)));
            ViewBag.ddlLine = lines.ToArray();
            var levels = new List<ListItem> { new ListItem("全部站级", "") };
            levels.AddRange(
                _rep.Distinct(t => t.LevelId).OrderBy(t => t).Select(t => new ListItem(t.ToString(), t.ToString())));
            ViewBag.ddlLevel = levels.ToArray();
            ViewBag.ddlUpdown =
                new List<ListItem> {new ListItem("上下行", ""), new ListItem("上行", "0"), new ListItem("下行", "1")}.ToArray();
            ViewBag.ddlIsSame = new[]
            {
                new ListItem("全部审核结果", ""),
                new ListItem("结果一致", "true"),
                new ListItem("结果不一致", "false")
            };
            return View(list);
        }
        private void UpdateGrid(NameValueCollection values)
        {
            var fields = JArray.Parse(values["Grid1_fields"]);
            var pageIndex = Convert.ToInt32(values["Grid1_pageIndex"] ?? "0");
            var pageSize = Convert.ToInt32(values["Grid1_pageSize"] ?? "0");

            var vehNo = values["ddlVehNo"];
            var line = values["ddlLine"];
            var isChecked = values["rblApprove"];
            var startTime = values["dpStartTime"];
            var endTime = values["dpEndTime"];
            var level = values["ddlLevel"];
            var updown = values["ddlUpdown"];
            var hasImage = values["ddlHasImage"];
            var isSameRes = values["ddlIsSame"];
            var filter = new List<FilterDefinition<ComfortDegreeStats>>();
            if (!string.IsNullOrEmpty(vehNo))
            {
                filter.Add(Builders<ComfortDegreeStats>.Filter.Eq(t => t.VehNo, vehNo));
            }
            if (!string.IsNullOrEmpty(line))
            {
                filter.Add(Builders<ComfortDegreeStats>.Filter.Eq(t => t.LineName, line));
            }
            if (!string.IsNullOrEmpty(isChecked))
            {
                if(isChecked=="1")
                    filter.Add(Builders<ComfortDegreeStats>.Filter.Eq(t => t.IsAuth, true));
                else if (isChecked == "0")
                    filter.Add(
                        Builders<ComfortDegreeStats>.Filter.Not(Builders<ComfortDegreeStats>.Filter.Eq(t => t.IsAuth,
                            true)));
            }
            if (!string.IsNullOrEmpty(startTime))
            {
                 filter.Add(Builders<ComfortDegreeStats>.Filter.Gte(t => t.Time, DateTime.Parse(startTime)));
            }
            if (!string.IsNullOrEmpty(endTime))
            {
                 filter.Add(Builders<ComfortDegreeStats>.Filter.Lte(t => t.Time, DateTime.Parse(endTime)));
            }
            if (!string.IsNullOrEmpty(level))
            {
                filter.Add(Builders<ComfortDegreeStats>.Filter.Eq(t => t.LevelId, int.Parse(level)));
            }
            if (!string.IsNullOrEmpty(updown))
            {
                filter.Add(Builders<ComfortDegreeStats>.Filter.Eq(t => t.UpDown, int.Parse(updown)));
            }
            if (!string.IsNullOrEmpty(hasImage))
            {
                filter.Add(
                    Builders<ComfortDegreeStats>.Filter.Or(
                        Builders<ComfortDegreeStats>.Filter.Eq(t => t.ImgPath, string.Empty),
                        Builders<ComfortDegreeStats>.Filter.Eq(t => t.ImgPath, null)));
            }
            else
            {
                filter.Add(
                    Builders<ComfortDegreeStats>.Filter.And(
                        Builders<ComfortDegreeStats>.Filter.Ne(t => t.ImgPath, string.Empty),
                        Builders<ComfortDegreeStats>.Filter.Ne(t => t.ImgPath, null)));
            }
            if (!string.IsNullOrEmpty(isSameRes))
            {
                filter.Add(Builders<ComfortDegreeStats>.Filter.Eq(t => t.IsSameRes, bool.Parse(isSameRes)));
            }

            int count;
            var list = _rep.QueryByPage(pageIndex, pageSize, out count,
                filter.Any() ? Builders<ComfortDegreeStats>.Filter.And(filter) : null);
            // 导出用
            Session["list"] = _rep.QueryByPage(0, int.MaxValue, out count,
                filter.Any() ? Builders<ComfortDegreeStats>.Filter.And(filter) : null);
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

        public ActionResult Authorize(string id)
        {
            var model = _rep.Get(t => t._id == id);
            if (model == null)
                return HttpNotFound();
            var rbs = new List<RadioItem>
            {
                new RadioItem("L", "L", model.FinalComfort == "L"),
                new RadioItem("M", "M", model.FinalComfort == "M"),
                new RadioItem("H", "H", model.FinalComfort == "H")
            };
            ViewBag.rblAuthComfort = rbs.ToArray();
            ViewBag.rblAuthDiffType =
            (from AuthDiffType a in Enum.GetValues(typeof(AuthDiffType))
                select new RadioItem(a.ToString(), a.ToString(), false)).ToArray();
            return View(model);
        }

        [HttpPost]
        public ActionResult Authorize(ComfortDegreeStats model)
        {
            try
            {
                var nameStartIndex = model.ImgPath.LastIndexOf("/", StringComparison.Ordinal);
                if (nameStartIndex == -1)
                    nameStartIndex = model.ImgPath.LastIndexOf("\\", StringComparison.Ordinal);
                var imgName = model.ImgPath.Substring(nameStartIndex + 1);
                if (string.IsNullOrEmpty(_transPath))
                {
                    Alert.Show("缺少文件转移路径配置！", MessageBoxIcon.Warning);
                    return UIHelper.Result();
                }

                var pathArr = model.ImgPath.Split(new[] {'\\'}, StringSplitOptions.RemoveEmptyEntries);
                var len = pathArr.Length;

                var pathTo = _transPath + pathArr[len - 4] +"\\"+ pathArr[len - 3] +"\\"+ pathArr[len - 2] +"\\";
                if (!Directory.Exists(pathTo))
                {
                    Directory.CreateDirectory(pathTo);
                }
                var fullPathTo = pathTo + model.FinalComfort + imgName;
                var finalBefore = _rep.Get(t => t._id == model._id).FinalComfort;
                var udb = Builders<ComfortDegreeStats>.Update.Set(t => t.AuthComfort, model.FinalComfort)
                    .Set(t => t.IsAuth, true)
                    .Set(t => t.IsSameRes, model.FinalComfort == finalBefore)
                    .Set(t => t.TransImgPath, fullPathTo);
                if (model.FinalComfort != finalBefore)
                {
                    if (model.AuthDiffType == null)
                    {
                        Alert.Show("请选择原因！", MessageBoxIcon.Warning);
                        return UIHelper.Result();
                    }
                    udb = udb.Set(t => t.AuthDiffType, model.AuthDiffType);
                }
                System.IO.File.Copy(model.ImgPath, fullPathTo);
                _rep.Update(t => t._id == model._id,udb);
                ShowNotify("审核成功！");
                // 关闭本窗体（触发窗体的关闭事件）
                PageContext.RegisterStartupScript(ActiveWindow.GetHidePostBackReference());
            }
            catch (Exception e)
            {
                throw;
            }
            return UIHelper.Result();
        }

        public ActionResult GetImg(string path)
        {
            return File(System.IO.File.OpenRead(path), "image/jpeg");
        }

        public ActionResult CompareImg(string id)
        {
            var model = _rep.Get(x => x._id == id);
            if(model==null)
                return HttpNotFound();
            if (string.IsNullOrEmpty(_transPath))
            {
                Alert.Show("缺少文件夹路径配置！", MessageBoxIcon.Warning);
                return UIHelper.Result();
            }
            var prefix = model.VehNo.Split(new[] {'-'}, StringSplitOptions.RemoveEmptyEntries)[0];

            var similarDegreeDic = new Dictionary<string, int>();

            var img = new SimilarPhoto(model.ImgPath);

            var localHash = img.GetHash();

            MvcApplication.HashList.Where(x=>x.Value.Contains(prefix)).ToList().ForEach(k =>
            {
                similarDegreeDic.Add(k.Key,SimilarPhoto.CalcSimilarDegree(localHash, k.Key));
            });
            var t = similarDegreeDic.OrderBy(p => p.Value);
            var list = new List<ImgStats>();
            var i = 0;
            foreach (var kvp in t)
            {
                    string filePath;
                    if (MvcApplication.HashList.TryGetValue(kvp.Key, out filePath))
                    {
                        list.Add(new ImgStats { FilePath = filePath, FileName = Path.GetFileName(filePath), Value = kvp.Value });
                    }
                i++;
                if (i > 5) break;
            }
            ViewBag.paths = list;
            return View(model);
        }
        public ActionResult ExportToExcel()
        {
            int count;
            var list = (List<ComfortDegreeStats>) Session["list"] ?? _rep.QueryByPage(0, int.MaxValue, out count);
            const string thHtml = "<th>{0}</th>";
            const string tdHtml = "<td style=\"text-align: center;\">{0}</td>";

            var sb = new StringBuilder();
            sb.Append("<table cellspacing=\"0\" rules=\"all\" border=\"1\" style=\"border-collapse:collapse;\">");
            sb.Append("<tr>");
            sb.AppendFormat(thHtml, "");
            sb.AppendFormat(thHtml, "时间");
            sb.AppendFormat(thHtml, "线路");
            sb.AppendFormat(thHtml, "车号");
            sb.AppendFormat(thHtml, "上下行");
            sb.AppendFormat(thHtml, "站级号");
            sb.AppendFormat(thHtml, "客流计留客人数");
            sb.AppendFormat(thHtml, "客流计舒适度");
            sb.AppendFormat(thHtml, "图片舒适度");
            sb.AppendFormat(thHtml, "最终舒适度");
            sb.AppendFormat(thHtml, "审核结果");
            sb.AppendFormat(thHtml, "审核结果是否一致");
            sb.AppendFormat(thHtml, "图片地址");
            sb.Append("</tr>");

            var rowIndex = 1;
            foreach (var item in list)
            {
                sb.Append("<tr>");
                sb.AppendFormat(tdHtml, rowIndex++);
                sb.AppendFormat(tdHtml, item.Time.ToString("yyyy-MM-dd HH:mm:ss"));
                sb.AppendFormat(tdHtml, item.LineName);
                
                sb.AppendFormat(tdHtml, item.VehNo);
                sb.AppendFormat(tdHtml, item.UpDown==0?"上行":"下行");
                sb.AppendFormat(tdHtml, item.LevelId);
                sb.AppendFormat(tdHtml, item.RemainCount);
                sb.AppendFormat(tdHtml, item.PFComfort);
                sb.AppendFormat(tdHtml, item.ImgComfort);
                sb.AppendFormat(tdHtml, item.FinalComfort);
                sb.AppendFormat(tdHtml, item.AuthComfort);
                sb.AppendFormat(tdHtml, item.IsSameRes ? "√" : "×");
                sb.AppendFormat(tdHtml, item.ImgPath);
                sb.Append("</tr>");
            }
            sb.Append("</table>");
            return File(Encoding.UTF8.GetBytes(sb.ToString()), "application/excel", "舒适度统计列表.xls");
        }
	}
}