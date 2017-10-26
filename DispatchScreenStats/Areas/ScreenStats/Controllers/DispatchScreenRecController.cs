using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using DispatchScreenStats.Common;
using DispatchScreenStats.Controllers;
using DispatchScreenStats.Enums;
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
        private readonly Expression<Func<ScreenRecDetail, bool>> _filter = t => string.IsNullOrEmpty(t.Materials.Remark);
        //
        // GET: /ScreenStats/DispatchScreenRec/
        public ActionResult Index()
        {
            int count;
            var list = _repDetail.QueryByPage(0, PageSize, out count, _filter);
            ViewBag.RecordCount = count;
            ViewBag.PageSize = PageSize;

            var lines = new List<ListItem>
            {
                new ListItem("全部线路", "")
            };
            lines.AddRange(_repDetail.Distinct(t => t.LineName).Select(t => new ListItem(t, t)));
            ViewBag.Lines = lines.ToArray();

            var stations = new List<ListItem>
            {
                new ListItem("全部站点", "")
            };
            stations.AddRange(_repDetail.Distinct(t => t.InstallStation).Select(t => new ListItem(t, t)));
            ViewBag.Stations = stations.ToArray();
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
                    var gIdList = new List<string>();
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
                        if (row.GetCell(20) != null)
                        {
                            if (!string.IsNullOrWhiteSpace(row.GetCell(20).StringCellValue))
                                model.ExtraRemark += row.GetCell(20).StringCellValue;
                        }
                        if (row.GetCell(21) != null)
                        {
                            if (!string.IsNullOrWhiteSpace(row.GetCell(21).StringCellValue))
                            {
                                if (!string.IsNullOrEmpty(model.ExtraRemark))
                                    model.ExtraRemark += "；";
                                model.ExtraRemark += row.GetCell(21).StringCellValue;
                            }
                        }

                        if (model.Owner == 0 && string.IsNullOrEmpty(model.InstallStation) &&
                            model.InstallDate < DateTime.Parse("1970/1/1")) continue;

                        if (model.Materials.Remark.Contains("设备编号"))
                        {
                            var numStr = model.Materials.Remark.Replace("设备编号：", "");
                            int num;
                            if (int.TryParse(numStr, out num)) model.DeviceNum = num.ToString();
                            else
                            {
                                //var max =
                                //    _rep.Find(Builders<ScreenRec>.Filter.Regex(t => t.DeviceNum,
                                //            new BsonRegularExpression(new Regex("C", RegexOptions.IgnoreCase))))
                                //        .Sort(new SortDefinitionBuilder<ScreenRec>().Descending(t => t.DeviceNum))
                                //        .Limit(1)
                                //        .ToList()
                                //        .FirstOrDefault();
                                //var maxNum = max != null ? max.DeviceNum : "";
                                var maxNum = gIdList.LastOrDefault();
                                if (string.IsNullOrEmpty(maxNum)) model.DeviceNum = "C001";
                                else
                                {
                                    var rem = maxNum.TrimStart('C');
                                    model.DeviceNum = "C" +
                                                      (rem.TrimStart('0').Length > 2
                                                          ? (int.Parse(rem) + 1).ToString()
                                                          : (int.Parse(rem) + 1).ToString("d3"));
                                }
                                gIdList.Add(model.DeviceNum);
                            }
                            model.Materials.Remark = "";
                        }
                        else
                        {
                            //var dbModel =
                            //    _rep.Find(t => t.LineName == model.LineName && t.InstallStation == model.InstallStation)
                            //        .ToList()
                            //        .FirstOrDefault(t => !string.IsNullOrEmpty(t.DeviceNum));
                            var listModel =
                                list.FirstOrDefault(
                                    t => t.LineName == model.LineName && t.InstallStation == model.InstallStation);
                            if (listModel != null)
                                model.DeviceNum = listModel.DeviceNum;
                            else
                            {
                                var maxNum = gIdList.LastOrDefault();
                                if (string.IsNullOrEmpty(maxNum)) model.DeviceNum = "C001";
                                else
                                {
                                    var rem = maxNum.TrimStart('C');
                                    model.DeviceNum = "C" +
                                                      (rem.TrimStart('0').Length > 2
                                                          ? (int.Parse(rem) + 1).ToString()
                                                          : (int.Parse(rem) + 1).ToString("d3"));
                                }
                                gIdList.Add(model.DeviceNum);
                            }
                        }

                        list.Add(model);
                        _rep.Add(model);
                    }

                    if (list.Any())
                    {
                        foreach (var rec in list)
                        {
                            rec.LineName = rec.LineName.Replace("、", "/");
                            var lines = rec.LineName.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var line in lines)
                            {
                                var detail = new ScreenRecDetail();
                                detail._id = (int) (_repDetail.Max(t => t._id) ?? 0) + 1;
                                detail.LineName = line;
                                detail.LinesInSameScreen = string.Join("、", lines.Except(new[] {line}));
                                detail.Materials = rec.Materials;
                                detail.Owner = rec.Owner;
                                detail.InstallStation = rec.InstallStation;
                                detail.InstallDate = rec.InstallDate;
                                detail.ScreenCount = rec.ScreenCount;
                                detail.DeviceNum = rec.DeviceNum;
                                detail.SaveTime = DateTime.Now;

                                detail.ExtraRemark = rec.ExtraRemark;

                                if (lines.Length > 2)
                                    detail.ScreenType = ScreenTypeEnum.表格定制屏;
                                else if (lines.Length == 2)
                                {
                                    int unit;
                                    if (int.TryParse(detail.Materials.UnitBoard, out unit))
                                    {
                                        if (unit == 28) detail.ScreenType = ScreenTypeEnum.七行双线屏;
                                        else if (unit == 35) detail.ScreenType = ScreenTypeEnum.定制双线屏;
                                    }
                                }
                                else if (lines.Length == 1)
                                    detail.ScreenType = ScreenTypeEnum.标准单线屏;

                                _repDetail.Add(detail);

                                //if (rec.ScreenCount.HasValue) continue;
                                //var log = new ScreenLog();
                                //log._id = (int) (_repLog.Max(t => t._id) ?? 0) + 1;
                                //log.LineName = line;
                                //log.DeviceNum = rec.DeviceNum;
                                //log.Owner = rec.Owner;
                                //log.InstallStation = rec.InstallStation;
                                //log.InstallDate = rec.InstallDate;
                                //log.SaveTime = DateTime.Now;
                                //log.OperContent = rec.Materials.Remark;
                                //_repLog.Add(log);
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
            var pageSize = Convert.ToInt32(values["Grid1_pageSize"] ?? "0");

            var devNum = values["tbDevNum"];
            var line = values["tbLine"];
            var isFuzzyLine = values["cbIsFuzzyLine"];
            var station = values["tbStation"];
            var isFuzzyStation = values["cbIsFuzzyStation"];
            var screenType = values["tbType"];
            var screenCount = values["nbCount"];
            //var remark = values["tbRemark"];
            var filter = new List<FilterDefinition<ScreenRecDetail>>
            {
                _filter
            };
            if (!string.IsNullOrWhiteSpace(devNum))
            {
                filter.Add(Builders<ScreenRecDetail>.Filter.Eq(t => t.DeviceNum, devNum));
            }
            if (!string.IsNullOrWhiteSpace(line))
            {
                var isFuzzy = bool.Parse(isFuzzyLine);
                if (isFuzzy)
                {
                    filter.Add(Builders<ScreenRecDetail>.Filter.Regex(t => t.LineName,
                        new BsonRegularExpression(new Regex(line.Trim()))));
                }
                else
                {
                    filter.Add(Builders<ScreenRecDetail>.Filter.Eq(t => t.LineName, line.Trim()));
                }
            }
            if (!string.IsNullOrWhiteSpace(station))
            {
                var isFuzzy = bool.Parse(isFuzzyStation);
                if (isFuzzy)
                {
                    filter.Add(Builders<ScreenRecDetail>.Filter.Regex(t => t.InstallStation,
                        new BsonRegularExpression(new Regex(station.Trim()))));
                }
                else
                {
                    filter.Add(Builders<ScreenRecDetail>.Filter.Eq(t => t.InstallStation, station.Trim()));
                }
            }
            if (!string.IsNullOrWhiteSpace(screenType))
            {
                filter.Add(Builders<ScreenRecDetail>.Filter.Eq(t => t.ScreenType,
                    Enum.Parse(typeof(ScreenTypeEnum), screenType)));
            }
            if (!string.IsNullOrWhiteSpace(screenCount))
            {
                int c;
                if (int.TryParse(screenCount, out c))
                    filter.Add(Builders<ScreenRecDetail>.Filter.Eq(t => t.ScreenCount, c));
            }
            //if (!string.IsNullOrWhiteSpace(remark))
            //{
            //    filter.Add(Builders<ScreenRecDetail>.Filter.Regex(t => t.Materials.Remark,
            //        new BsonRegularExpression(new Regex(remark.Trim()))));
            //}


            int count;
            var list = _repDetail.QueryByPage(pageIndex, pageSize, out count,
                filter.Any() ? Builders<ScreenRecDetail>.Filter.And(filter) : null);
            //var list = _repDetail.Find(filter.Any() ? Builders<ScreenRecDetail>.Filter.And(filter) : null).ToList();
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

        public ViewResult Search()
        {
            ViewBag.ScreenTypes = CommonHelper.GetEnumSelectList(typeof(ScreenTypeEnum));
            return View();
        }

        public ViewResult Locate(string line,string station)
        {
            var point = new OracleHelper().GetPointByLine(line,station);
            ViewBag.station = station;
            return View(point ?? GetPoint(station));
        }

        public ViewResult LocateAll()
        {
            return View();
        }

        [HttpPost]
        public JsonResult GetPoints()
        {
            var screens = _rep.Find(t=>string.IsNullOrEmpty(t.Materials.Remark)).ToList();
            //var stations = screens.Select(t => t.InstallStation).Distinct().ToList();
            //var points = new List<dynamic>();
            //foreach (var station in stations)
            //{
            //    var point = GetPoint(station);
            //    if (point != null)
            //        points.Add(new { station, x = point.X, y = point.Y });
            //}
            var res = new OracleHelper().GetPointsByLines(screens);
            //foreach (var screen in screens)
            //{
            //    var rec = screen;
            //    //var point = points.FirstOrDefault(t => t.station == rec.InstallStation);
            //    var lines = screen.LineName.Split(new[] {'、'}, StringSplitOptions.RemoveEmptyEntries);
            //    var point = new OracleHelper().GetPointByLine(lines[0]) ?? GetPoint(screen.InstallStation);
            //    if (point != null)
            //        res.Add(new {rec, point});
            //}

            return Json(res);
        }
        private Point GetPoint(string station)
        {
            const string url = "http://api.map.baidu.com/place/v2/search";
            var data = string.Format("{0}?q={1}-公交站&scope=1&region=上海&output=json&ak=6a21ea12a8e3c744047f0efab47c8473", url,
                station);
            //var data = "http://api.map.baidu.com/geocoder/v2/?city=上海&address=" + station +
            //           "-公交车站&output=json&ak=6a21ea12a8e3c744047f0efab47c8473";
            var json = CommonHelper.HttpGet(data);
            var obj = JObject.Parse(json);
            var results = obj.Value<JArray>("results");
            if (!results.Any()) return null;
            var location = results.First().Value<JObject>("location");
            if (location==null) return null;
            //var location = obj.Value<JObject>("result").Value<JObject>("location");
            var point = new Point(location.Value<string>("lng"), location.Value<string>("lat"));
            return point;
        }
        public ActionResult Delete(JArray selectedRows, FormCollection values)
        {
            var ids = selectedRows.Select(Convert.ToInt32).ToList();
            _repDetail.Delete(t => ids.Contains(t._id));
            UpdateGrid(values);
            return UIHelper.Result();
        }

        public ActionResult btnSubmit_Click(JArray Grid1_fields, JArray Grid1_modifiedData, int Grid1_pageIndex, int Grid1_pageSize, JArray Grid2_modifiedData, JArray Grid2_fields)
        {
            if (!Grid1_modifiedData.Any() && !Grid2_modifiedData.Any())
            {
                ShowNotify("无修改数据！");
                return UIHelper.Result();
            }
            foreach (var jToken in Grid1_modifiedData)
            {
                var modifiedRow = (JObject) jToken;
                string status = modifiedRow.Value<string>("status");
                int rowId = Convert.ToInt32(modifiedRow.Value<string>("id"));

                if (status != "modified") continue;
                var rowDic = modifiedRow.Value<JObject>("values").ToObject<Dictionary<string, object>>();
                foreach (var p in rowDic)
                {
                    var param = Expression.Parameter(typeof(ScreenRecDetail), "x");
                    var paramArr = p.Key.Split(new[] {'_'}, StringSplitOptions.RemoveEmptyEntries);
                    var body = paramArr.Length > 1 ? Expression.Property(Expression.Property(param, typeof(ScreenRecDetail), paramArr[0]), typeof(Materials), paramArr[1]) : Expression.Property(param, typeof(ScreenRecDetail), paramArr[0]);
                    var lambda =
                        Expression.Lambda<Func<ScreenRecDetail, object>>(Expression.Convert(body, typeof(object)), param);
                    _repDetail.Update(t => t._id == rowId, Builders<ScreenRecDetail>.Update.Set(lambda, p.Value));
                }
            }

            int count;
            var source = _repDetail.QueryByPage(Grid1_pageIndex, Grid1_pageSize, out count, _filter);
            //var source = _repDetail.Find(t => string.IsNullOrEmpty(t.Materials.Remark)).ToList();
            var grid1 = UIHelper.Grid("Grid1");
            grid1.RecordCount(count);
            grid1.DataSource(source, Grid1_fields);

            foreach (var jToken in Grid2_modifiedData)
            {
                var modifiedRow = (JObject)jToken;
                string status = modifiedRow.Value<string>("status");
                var rowId = modifiedRow.Value<string>("id");

                if (status == "newadded")
                {
                    var rowDic = modifiedRow.Value<JObject>("values").ToObject<Dictionary<string, object>>();
                if (!rowDic.ContainsKey("InstallDate") || !rowDic.ContainsKey("Materials_Remark"))
                {
                    Alert.Show("安装日期和备注不能为空！");
                    return UIHelper.Result();
                }

                var model = new ScreenRecDetail();
                model._id = (int) (_repDetail.Max(t => t._id) ?? 0) + 1;
                foreach (var p in rowDic)
                {
                    var paramArr = p.Key.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
                    if (paramArr.Length > 1)
                        typeof(Materials).GetProperty(paramArr[1])
                            .SetValue(typeof(ScreenRecDetail).GetProperty(paramArr[0]).GetValue(model), p.Value);
                    else
                    {
                        var prop =typeof(ScreenRecDetail).GetProperty(paramArr[0]);
                        if(prop.PropertyType==typeof(int))
                            prop.SetValue(model, Convert.ToInt32(p.Value));
                        else if (prop.PropertyType == typeof(DateTime?) || prop.PropertyType == typeof(DateTime))
                        {
                            prop.SetValue(model,Convert.ToDateTime(p.Value));
                        }
                        else if (prop.PropertyType != typeof(int?))
                        {
                            prop.SetValue(model, p.Value);
                        }
                    }
                }
                _repDetail.Add(model);
                }
                else if (status == "deleted")
                {
                    _repDetail.Delete(t => t._id == int.Parse(rowId));
                }
            }


            ShowNotify("数据保存成功！");

            return UIHelper.Result();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Grid1_RowClick(string rowText, JArray Grid2_fields)
        {
            var grid2 = UIHelper.Grid("Grid2");
            grid2.DataSource(
                _repDetail.Find(t => t.DeviceNum == rowText && !string.IsNullOrEmpty(t.Materials.Remark))
                    .Sort(
                        new SortDefinitionBuilder<ScreenRecDetail>().Descending(t => t.LineName)
                            .Descending(t => t.ScreenCount))
                    .ToList(), Grid2_fields);

            return UIHelper.Result();
        }
    }
}