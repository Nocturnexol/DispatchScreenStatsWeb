using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
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
            int count;
            var list = _rep.QueryByPage(pageIndex, pageSize, out count);
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
                    var logs = _repDetail.Find().ToList();
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
                                log.LogType = "日志类型";
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
	}
}