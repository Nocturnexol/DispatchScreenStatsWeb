using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Linq.Expressions;
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
    public class DispatchScreenRecController : BaseController
    {
        private readonly IMongoRepository<ScreenRec> _rep = new MongoRepository<ScreenRec>();
        private readonly IMongoRepository<ScreenRecDetail> _repDetail = new MongoRepository<ScreenRecDetail>();
        private readonly IMongoRepository<ScreenLog> _repLog = new MongoRepository<ScreenLog>();
        //
        // GET: /ScreenStats/DispatchScreenRec/
        public ActionResult Index()
        {
            int count;
            var list = _rep.QueryByPage(0, PageSize, out count);
            ViewBag.RecordCount = count;
            ViewBag.PageSize = PageSize;
            return View(list);
        }

        public ViewResult Import()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Import(HttpPostedFileBase file)
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
                IWorkbook wb;
                if (fileType == "xls") wb = new HSSFWorkbook(file.InputStream);
                else wb = new XSSFWorkbook(file.InputStream);
                try
                {
                    var sheet = wb.GetSheetAt(0);
                    var list = new List<ScreenRec>();
                    var maxId = (int) (_rep.Max(t => t._id) ?? 0) + 1;
                    for (var i = 2; i <= sheet.LastRowNum; i++)
                    {
                        var row = sheet.GetRow(i);
                        var model = new ScreenRec();
                        model._id = maxId + i - 2;
                        model.Owner = (int) row.GetCell(1).NumericCellValue;
                        row.GetCell(2).SetCellType(CellType.String);
                        model.LineName = row.GetCell(2).StringCellValue;
                        row.GetCell(3).SetCellType(CellType.String);
                        var scStr = row.GetCell(3).StringCellValue;
                        int sc;
                        if (int.TryParse(scStr, out sc))
                            model.ScreenCount = sc;
                        else model.ScreenCount = null;
                        model.InstallStation = row.GetCell(4).StringCellValue;
                        model.InstallDate = row.GetCell(5).DateCellValue;
                        row.GetCell(6).SetCellType(CellType.String);
                        model.Materials.PowerCord = row.GetCell(6).StringCellValue;
                        row.GetCell(7).SetCellType(CellType.String);
                        model.Materials.Cable = row.GetCell(7).StringCellValue;
                        row.GetCell(8).SetCellType(CellType.String);
                        model.Materials.GridLines = row.GetCell(8).StringCellValue;
                        row.GetCell(9).SetCellType(CellType.String);
                        model.Materials.SmallExchange = row.GetCell(9).StringCellValue;
                        row.GetCell(10).SetCellType(CellType.String);
                        model.Materials.BigExchange = row.GetCell(10).StringCellValue;
                        row.GetCell(11).SetCellType(CellType.String);
                        model.Materials.PatchBoard = row.GetCell(11).StringCellValue;
                        row.GetCell(12).SetCellType(CellType.String);
                        model.Materials.OneToTwoSwitch = row.GetCell(12).StringCellValue;
                        row.GetCell(13).SetCellType(CellType.String);
                        model.Materials.UsbAdapter = row.GetCell(13).StringCellValue;
                        row.GetCell(14).SetCellType(CellType.String);
                        model.Materials.ThreePinPlug = row.GetCell(14).StringCellValue;
                        row.GetCell(15).SetCellType(CellType.String);
                        model.Materials.SquareTube = row.GetCell(15).StringCellValue;
                        row.GetCell(16).SetCellType(CellType.String);
                        model.Materials.Power = row.GetCell(16).StringCellValue;
                        row.GetCell(17).SetCellType(CellType.String);
                        model.Materials.UnitBoard = row.GetCell(17).StringCellValue;
                        row.GetCell(18).SetCellType(CellType.String);
                        model.Materials.Canopy = row.GetCell(18).StringCellValue;
                        model.Materials.Remark = row.GetCell(19).StringCellValue;

                        if (model.Owner == 0 && string.IsNullOrEmpty(model.InstallStation) &&
                            model.InstallDate < DateTime.Parse("1970/1/1")) continue;
                        list.Add(model);
                    }

                    if (list.Any())
                    {
                        _rep.BulkInsert(list);

                        foreach (var rec in list)
                        {
                            rec.LineName = rec.LineName.Replace("、", "/");
                            var lines = rec.LineName.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var line in lines)
                            {
                                var detail = new ScreenRecDetail();
                                detail._id = (int) (_repDetail.Max(t => t._id) ?? 0) + 1;
                                detail.LineName = line;
                                detail.Materials = rec.Materials;
                                detail.Owner = rec.Owner;
                                detail.InstallStation = rec.InstallStation;
                                detail.InstallDate = rec.InstallDate;
                                detail.ScreenCount = rec.ScreenCount;
                                detail.RecId = rec._id;
                                detail.SaveTime = DateTime.Now;

                                _repDetail.Add(detail);

                                if (rec.ScreenCount.HasValue) continue;
                                var log = new ScreenLog();
                                log._id = (int) (_repLog.Max(t => t._id) ?? 0) + 1;
                                log.LineName = line;
                                log.Owner = rec.Owner;
                                log.InstallStation = rec.InstallStation;
                                log.InstallDate = rec.InstallDate;
                                log.SaveTime = DateTime.Now;
                                log.OperContent = rec.Materials.Remark;
                                _repLog.Add(log);
                            }

                        }
                    }
                    // 关闭本窗体（触发窗体的关闭事件）
                    PageContext.RegisterStartupScript(ActiveWindow.GetHidePostBackReference());
                    ShowNotify("导入成功");
                }
                catch (Exception e)
                {
                    Alert.Show(e.Message, MessageBoxIcon.Warning);
                    throw;
                }
                finally
                {
                    file.InputStream.Dispose();
                }
               
            }

            return UIHelper.Result();
        }

        private void UpdateGrid(NameValueCollection values)
        {
            JArray fields = JArray.Parse(values["Grid1_fields"]);
            int pageIndex = Convert.ToInt32(values["Grid1_pageIndex"]??"0");

            var key = values["tbxKey"];
            var installDate = values["dpInstallDate"];

            var filter = new List<FilterDefinition<ScreenRec>>();
            if (!string.IsNullOrEmpty(key))
            {
                var filters = new List<FilterDefinition<ScreenRec>>
                {
                    Builders<ScreenRec>.Filter.Regex(t => t.LineName,
                        new BsonRegularExpression(new Regex(key, RegexOptions.IgnoreCase))),
                    Builders<ScreenRec>.Filter.Regex(t => t.InstallStation,
                        new BsonRegularExpression(new Regex(key, RegexOptions.IgnoreCase))),
                        Builders<ScreenRec>.Filter.Regex(t => t.Materials.Remark,
                        new BsonRegularExpression(new Regex(key, RegexOptions.IgnoreCase)))
                };

                int keyInt;
                if (int.TryParse(key, out keyInt))
                {
                    Expression<Func<ScreenRec, bool>> exp = t => t.Owner == keyInt || t.ScreenCount == keyInt;
                    filters.Add(exp);
                }

                filter.Add(Builders<ScreenRec>.Filter.Or(filters));
            }
            if (!string.IsNullOrEmpty(installDate))
            {
                DateTime date;
                if (DateTime.TryParse(installDate, out date))
                {
                    Expression<Func<ScreenRec, bool>> f = t => t.InstallDate == date;
                    filter.Add(f);
                }
                else
                {
                    Alert.Show("无效的日期格式！",MessageBoxIcon.Warning);
                    return;
                }
            }

            int count;
            var list = _rep.QueryByPage(pageIndex, PageSize, out count,
                filter.Any() ? Builders<ScreenRec>.Filter.And(filter) : null);

            var grid1 = UIHelper.Grid("Grid1");
            grid1.RecordCount(count);
            grid1.DataSource(list, fields);
        }
        public ActionResult DoSearch(FormCollection values)
        {
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
    }
}