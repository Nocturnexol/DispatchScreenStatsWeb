using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using DispatchScreenStats.Controllers;
using DispatchScreenStats.IRepository;
using DispatchScreenStats.Models;
using DispatchScreenStats.Repository;
using FineUIMvc;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace DispatchScreenStats.Areas.ScreenStats.Controllers
{
    public class ScreenRepairsController : BaseController
    {
        private readonly IMongoRepository<ScreenRepairs> _rep = new MongoRepository<ScreenRepairs>();
        private readonly IMongoRepository<ScreenRecDetail> _repDetail = new MongoRepository<ScreenRecDetail>();
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
            var fields = JArray.Parse(values["Grid1_fields"]);
            var pageIndex = Convert.ToInt32(values["Grid1_pageIndex"] ?? "0");
            var pageSize = Convert.ToInt32(values["Grid1_pageSize"] ?? "0");

            var owner = values["ddlOwner"] != null && values["ddlOwner"] != "[]" ? JArray.Parse(values["ddlOwner"]) : null;
            var line = values["tbLine"];
            var isFuzzyLine = values["cbIsFuzzyLine"];
            var station = values["tbStation"];
            var isFuzzyStation = values["cbIsFuzzyStation"];
            var tbAccepter = values["tbAccepter"];
            var tbHandler = values["tbHandler"];
            var tbType = values["tbType"];
            var tbStatus = values["tbStatus"];
            var date = values["tbDate"];

            var filter = new List<FilterDefinition<ScreenRepairs>>();

            if (!string.IsNullOrWhiteSpace(tbAccepter))
            {
                filter.Add(Builders<ScreenRepairs>.Filter.Eq(t => t.Accepter, tbAccepter));
            }
            if (!string.IsNullOrWhiteSpace(tbHandler))
            {
                filter.Add(Builders<ScreenRepairs>.Filter.Eq(t => t.Handler, tbHandler));
            }
            if (!string.IsNullOrWhiteSpace(tbType))
            {
                filter.Add(Builders<ScreenRepairs>.Filter.Eq(t => t.HitchType, tbType));
            }
            if (!string.IsNullOrWhiteSpace(tbStatus))
            {
                filter.Add(Builders<ScreenRepairs>.Filter.Eq(t => t.Status, tbStatus));
            }
            if (owner != null)
            {
                filter.Add(Builders<ScreenRepairs>.Filter.In(t => t.Owner, owner));
            }
            if (!string.IsNullOrWhiteSpace(line))
            {
                var isFuzzy = bool.Parse(isFuzzyLine);
                if (isFuzzy)
                {
                    filter.Add(Builders<ScreenRepairs>.Filter.Regex(t => t.LineName,
                        new BsonRegularExpression(new Regex(line.Trim()))));
                }
                else
                {
                    filter.Add(Builders<ScreenRepairs>.Filter.Eq(t => t.LineName, line.Trim()));
                }
            }
            if (!string.IsNullOrWhiteSpace(station))
            {
                var isFuzzy = bool.Parse(isFuzzyStation);
                if (isFuzzy)
                {
                    filter.Add(Builders<ScreenRepairs>.Filter.Regex(t => t.Station,
                        new BsonRegularExpression(new Regex(station.Trim()))));
                }
                else
                {
                    filter.Add(Builders<ScreenRepairs>.Filter.Eq(t => t.Station, station.Trim()));
                }
            }
            if (!string.IsNullOrWhiteSpace(date))
            {
                filter.Add(Builders<ScreenRepairs>.Filter.Eq(t => t.RepairsDate, DateTime.Parse(date)));
            }

            int count;
            var list = _rep.QueryByPage(pageIndex, pageSize, out count, filter.Any() ? Builders<ScreenRepairs>.Filter.And(filter) : null);
            var grid1 = UIHelper.Grid("Grid1");
            grid1.RecordCount(count);
            grid1.PageSize(pageSize);
            grid1.DataSource(list, fields);
        }
        public ViewResult Search()
        {
            ViewBag.Owners = _rep.Distinct(t => t.Owner).OrderBy(t => t).Select(t => new ListItem(t.ToString(), t.ToString(), false)).ToArray();
            return View();
        }
        public ActionResult DoSearch(FormCollection values)
        {
            UpdateGrid(values);
            return UIHelper.Result();
        }
        public ActionResult DeleteAll(FormCollection values)
        {
            _rep.Delete(t => true);
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

        public ViewResult RepairsImport()
        {
            return View();
        }
        [HttpPost]
        public ActionResult RepairsImport(HttpPostedFileBase file)
        {
            if (file == null || file.ContentLength == 0) return UIHelper.Result();
            var fileName = file.FileName;
            var fileType = GetFileType(fileName);
            if (!ValidFileTypes.Contains(fileType))
            {
                // 清空文件上传组件
                UIHelper.FileUpload("file").Reset();
                ShowNotify("无效的文件类型！");
            }
            else
            {
                try
                {
                    var list = new List<ScreenRepairs>();
                    var listLog = new List<ScreenRecDetail>();
                      IWorkbook wb;
                    if (fileType == "xls") wb = new HSSFWorkbook(file.InputStream);
                    else wb = new XSSFWorkbook(file.InputStream);
                    var sheet = wb.GetSheetAt(0);
                    var maxId = (int)(_rep.Max(t => t._id) ?? 0) + 1;
                    var maxLogId = (int)(_repDetail.Max(t => t._id) ?? 0) + 1;
                    var logs = _repDetail.Find(t=>!t.IsLog).ToList();
                    for (var i = 1; i <= sheet.LastRowNum; i++)
                    {
                        var row = sheet.GetRow(i);
                        var model = new ScreenRepairs();
                        model._id = maxId + i - 1;
                        row.GetCell(1).SetCellType(CellType.Numeric);
                        model.RepairsDate = row.GetCell(1).DateCellValue;
                        row.GetCell(2).SetCellType(CellType.String);
                        model.LineName = row.GetCell(2).StringCellValue;
                        model.Station = row.GetCell(3).StringCellValue;
                        model.Owner = row.GetCell(4).StringCellValue;
                        row.GetCell(5).SetCellType(CellType.String);
                        model.RepairsSoucre = row.GetCell(5).StringCellValue;
                        model.Accepter = row.GetCell(6).StringCellValue;
                        model.Handler = row.GetCell(7).StringCellValue;
                        model.HitchType = row.GetCell(8).StringCellValue;
                        model.Status = row.GetCell(9).StringCellValue;
                        model.HitchContent = row.GetCell(10).StringCellValue;
                        model.Solution = row.GetCell(11).StringCellValue;

                        list.Add(model);

                        var lines = model.LineName.Replace("、", "/")
                            .Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
                        //var filter =
                        //    Builders<ScreenRecDetail>.Filter.And(
                        //        Builders<ScreenRecDetail>.Filter.Eq(t => t.InstallStation, model.Station),
                        //        Builders<ScreenRecDetail>.Filter.Or(
                        //            Builders<ScreenRecDetail>.Filter.In(t => t.LineName, lines),
                        //            Builders<ScreenRecDetail>.Filter.Eq(t => t.LineName, model.LineName),Builders<ScreenRecDetail>.Filter.Regex(t=>t.LineName,new BsonRegularExpression())));
                        if (model.LineName == null) model.LineName = "";
                        var devs =
                            logs.Where(
                                t =>
                                    (t.InstallStation == model.Station ||
                                     t.InstallStation.Contains(model.Station??"")) &&
                                    (model.LineName.Contains(t.LineName) || t.LineName == model.LineName)).ToList();
                        if (devs.Any())
                        {
                            var devNums = new HashSet<string>();
                            foreach (var dev in devs)
                            {
                                if (!devNums.Add(dev.DeviceNum)) continue;
                                var log = new ScreenRecDetail();
                                log._id = maxLogId;
                                maxLogId++;
                                log.DeviceNum = dev.DeviceNum;
                                log.LogType = "服务类型";
                                log.HandlingType = "维修";
                                log.Date = model.RepairsDate;
                                log.IsLog = true;
                                log.Materials.Remark = string.Format("故障问题：{0}；解决方法：{1}", model.HitchContent,
                                    model.Solution);
                                listLog.Add(log);
                            }
                        }
                    }
                    if(list.Any())_rep.BulkInsert(list);
                    if(listLog.Any())_repDetail.BulkInsert(listLog);
                    // 关闭本窗体（触发窗体的关闭事件）
                    PageContext.RegisterStartupScript(ActiveWindow.GetHidePostBackReference());
                    ShowNotify("导入成功");
                }
                //catch (Exception e)
                //{
                //    Alert.Show(e.Message, MessageBoxIcon.Warning);
                //    return UIHelper.Result();
                //}
                finally
                {
                    file.InputStream.Close();
                }
            }
            return UIHelper.Result();
        }

        public FileResult Export()
        {
            var filter = (List<FilterDefinition<ScreenRepairs>>)Session["filter"];
            var list =
                _rep.Find(filter != null && filter.Any() ? Builders<ScreenRepairs>.Filter.And(filter) : null)
                    .ToList();
            const string thHtml = "<th>{0}</th>";
            const string tdHtml = "<td style=\"text-align: center;\">{0}</td>";

            var sb = new StringBuilder();
            sb.Append("<table cellspacing=\"0\" rules=\"all\" border=\"1\" style=\"border-collapse:collapse;\">");
            sb.Append("<tr>");
            sb.AppendFormat(thHtml, "");
            sb.AppendFormat(thHtml, "报修日期");
            sb.AppendFormat(thHtml, "线路");
            sb.AppendFormat(thHtml, "站点");
            sb.AppendFormat(thHtml, "营运公司");
            sb.AppendFormat(thHtml, "报修来源");
            sb.AppendFormat(thHtml, "故障接报人");
            sb.AppendFormat(thHtml, "故障处理人");
            sb.AppendFormat(thHtml, "故障类型");
            sb.AppendFormat(thHtml, "故障状态");
            sb.AppendFormat(thHtml, "故障问题");
            sb.AppendFormat(thHtml, "解决方法");
            sb.Append("</tr>");

            var rowIndex = 1;
            foreach (var item in list)
            {
                sb.Append("<tr>");
                sb.AppendFormat(tdHtml, rowIndex++);
                sb.AppendFormat(tdHtml, item.RepairsDate);
                sb.AppendFormat(tdHtml, item.LineName);
                sb.AppendFormat(tdHtml, item.Station);
                sb.AppendFormat(tdHtml, item.Owner);
                sb.AppendFormat(tdHtml, item.RepairsSoucre);
                sb.AppendFormat(tdHtml, item.Accepter);
                sb.AppendFormat(tdHtml, item.Handler);
                sb.AppendFormat(tdHtml, item.HitchType);
                sb.AppendFormat(tdHtml, item.Status);
                sb.AppendFormat(tdHtml, item.HitchContent);
                sb.AppendFormat(tdHtml, item.Solution);
                sb.Append("</tr>");
            }
            sb.Append("</table>");
            return File(Encoding.UTF8.GetBytes(sb.ToString()), "application/excel", "发车屏维修列表.xls");
        }


	}
}