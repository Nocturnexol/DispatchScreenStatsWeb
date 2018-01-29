using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
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
        private readonly IMongoRepository<Auth> _repAuth = new MongoRepository<Auth>();
        private readonly IMongoRepository<ScreenRec> _rep = new MongoRepository<ScreenRec>();
        private readonly IMongoRepository<ScreenRecDetail> _repDetail = new MongoRepository<ScreenRecDetail>();
        private readonly IMongoRepository<BasicData> _repBasic = new MongoRepository<BasicData>();
        private readonly IMongoRepository<ScreenRepairs> _repRepairs = new MongoRepository<ScreenRepairs>();
        private readonly IMongoRepository<Accessory> _repAcc = new MongoRepository<Accessory>();
        private readonly IMongoRepository<ScreenImage> _repImg = new MongoRepository<ScreenImage>();
        private readonly Expression<Func<ScreenRec, bool>> _filter = t => !t.IsLog;
        private readonly bool _isAuth;

        public DispatchScreenRecController()
        {
            var user = CommonHelper.User;
            if (user != "admin")
            {
                var auth = _repAuth.Get(t => t.UserId == int.Parse(CommonHelper.UserId));
                if (auth != null)
                {
                    _isAuth = auth.Permission == 1;
                }
            }
            else
            {
                _isAuth = true;
            }
        }
        //
        // GET: /ScreenStats/DispatchScreenRec/
        [OutputCache(Duration = 600)]
        public ActionResult Index()
        {
            int count;
            var list = _rep.QueryByPage(0, PageSize, out count, _filter);
            ViewBag.RecordCount = count;
            ViewBag.PageSize = PageSize;

            var lines = new List<ListItem>
            {
                new ListItem("全部线路", "")
            };
            lines.AddRange(_rep.Distinct(t => t.LineName).Select(t => new ListItem(t, t)));
            ViewBag.Lines = lines.ToArray();

            var stations = new List<ListItem>
            {
                new ListItem("全部站点", "")
            };
            stations.AddRange(_rep.Distinct(t => t.InstallStation).Select(t => new ListItem(t, t)));
            ViewBag.Stations = stations.ToArray();
            ViewBag.HandlingTypes = GetBasicDdlByName("处理类型");
            ViewBag.ChargeTypes = GetBasicDdlByName("收费类型");
            ViewBag.PaymentStatuses = GetBasicDdlByName("付款状态");
            ViewBag.LogTypes = GetBasicDdlByName("日志类型");

            ViewBag.isAuth = _isAuth;
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
                var sb = new StringBuilder();
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
                        row.GetCell(1).SetCellType(CellType.String);
                        model.Owner = row.GetCell(1).StringCellValue;
                        row.GetCell(2).SetCellType(CellType.String);
                        model.LineName = row.GetCell(2).StringCellValue;
                        row.GetCell(3).SetCellType(CellType.String);
                        model.ConstructionType = row.GetCell(3).StringCellValue;

                        row.GetCell(4).SetCellType(CellType.String);
                        var scStr = row.GetCell(4).StringCellValue;
                        int sc;
                        if (int.TryParse(scStr, out sc))
                            model.ScreenCount = sc;
                        else model.ScreenCount = null;
                        model.InstallStation = row.GetCell(5).StringCellValue;
                        model.InstallDate = row.GetCell(6).DateCellValue;
                        row.GetCell(7).SetCellType(CellType.String);
                        model.Materials.PowerCord = row.GetCell(7).StringCellValue;
                        row.GetCell(8).SetCellType(CellType.String);
                        model.Materials.Cable = row.GetCell(8).StringCellValue;
                        row.GetCell(9).SetCellType(CellType.String);
                        model.Materials.GridLines = row.GetCell(9).StringCellValue;
                        row.GetCell(10).SetCellType(CellType.String);
                        model.Materials.SmallExchange = row.GetCell(10).StringCellValue;
                        row.GetCell(11).SetCellType(CellType.String);
                        model.Materials.BigExchange = row.GetCell(11).StringCellValue;
                        row.GetCell(12).SetCellType(CellType.String);
                        model.Materials.PatchBoard = row.GetCell(12).StringCellValue;
                        row.GetCell(13).SetCellType(CellType.String);
                        model.Materials.OneToTwoSwitch = row.GetCell(13).StringCellValue;
                        row.GetCell(14).SetCellType(CellType.String);
                        model.Materials.UsbAdapter = row.GetCell(14).StringCellValue;
                        row.GetCell(15).SetCellType(CellType.String);
                        model.Materials.ThreePinPlug = row.GetCell(15).StringCellValue;
                        row.GetCell(16).SetCellType(CellType.String);
                        model.Materials.SquareTube = row.GetCell(16).StringCellValue;
                        row.GetCell(17).SetCellType(CellType.String);
                        model.Materials.Power = row.GetCell(17).StringCellValue;
                        row.GetCell(18).SetCellType(CellType.String);
                        model.Materials.UnitBoard = row.GetCell(18).StringCellValue;
                        row.GetCell(19).SetCellType(CellType.String);
                        model.Materials.Canopy = row.GetCell(19).StringCellValue;
                        model.Materials.Remark = row.GetCell(20).StringCellValue;
                        if (row.GetCell(21) != null)
                        {
                            if (!string.IsNullOrWhiteSpace(row.GetCell(21).StringCellValue))
                                model.ExtraRemark += row.GetCell(21).StringCellValue;
                        }
                        if (row.GetCell(22) != null)
                        {
                            if (!string.IsNullOrWhiteSpace(row.GetCell(22).StringCellValue))
                            {
                                if (!string.IsNullOrEmpty(model.ExtraRemark))
                                    model.ExtraRemark += "；";
                                model.ExtraRemark += row.GetCell(22).StringCellValue;
                            }
                        }

                        if (model.Owner == "0" && string.IsNullOrEmpty(model.InstallStation) &&
                            model.InstallDate < DateTime.Parse("1970/1/1")) continue;

                        if (model.ConstructionType == "线路更变")
                        {
                            if (model.Materials.Remark.Contains("改为"))
                            {
                                model.IsLog = true;
                                var roads = model.Materials.Remark.Split(new[] {'改', '为'},
                                    StringSplitOptions.RemoveEmptyEntries);
                                var dev =
                                    list.FirstOrDefault(
                                        t => t.LineName == roads[0] && t.InstallStation == model.InstallStation);
                                if (dev != null)
                                    model.DeviceNum = dev.DeviceNum;
                            }
                        }
                        if (model.Materials.Remark.Contains("设备编号"))
                        {
                            if (model.Materials.Remark.Replace("设备编号：", "").Replace("设备编号:", "").Length > 6)
                            {
                                sb.AppendLine("第" + (i + 1) + "行设备编号后有无法识别的数据");
                            }
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
                                    t =>
                                        (t.LineName.Replace("、", "/")
                                             .Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries).OrderBy(x => x)
                                             .SequenceEqual(model.LineName.Replace("、", "/")
                                                 .Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries)
                                                 .OrderBy(x => x)) ||
                                         t.LineName == model.LineName || t.LineName.Replace("、", "/")
                                             .Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries)
                                             .Contains(model.LineName)) &&
                                        (t.InstallStation == model.InstallStation ||
                                         t.InstallStation.Contains(model.InstallStation) ||
                                         model.InstallStation.Contains(t.InstallStation)));
                            if (listModel != null)
                            {
                                model.DeviceNum = listModel.DeviceNum;
                                model.IsLog = true;
                            }
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

                        if (list.Any(t => t.DeviceNum == model.DeviceNum && !t.IsLog && !model.IsLog))
                        {
                            sb.AppendLine("第" + (i + 1) + "行数据设备编号重复");
                        }
                        if (model.Materials.Remark.Contains("原") || model.Materials.Remark.Contains("搬") ||
                            model.Materials.Remark.Contains("从") ||
                            (model.Materials.Remark.Contains("改") && Regex.IsMatch(model.Materials.Remark, "[0-9]*")))
                        {
                            sb.AppendLine("第" + (i + 1) + "行有无法识别的数据");
                        }


                        var flag = model.Materials.Remark.Contains("无线");

                        model.IsWireLess = flag;
                        if (model.IsLog)
                        {
                            foreach (var screenRec in list.Where(t => t.DeviceNum == model.DeviceNum))
                            {
                                screenRec.IsWireLess = flag;
                            }
                        }
                        if (string.IsNullOrWhiteSpace(model.Owner) && string.IsNullOrWhiteSpace(model.LineName) &&
                            string.IsNullOrWhiteSpace(model.InstallStation)) continue;
                        list.Add(model);
                    }

                    System.IO.File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "/problems.txt", sb.ToString());

                    //if (sb.Length>0)
                    //{
                    //    Confirm.Show("存在未识别的数据，是否仍要导入？","提示",MessageBoxIcon.Warning,"","");
                    //}
                    if (list.Any())
                    {
                        var detailList = new List<ScreenRecDetail>();
                        var accList = new List<Accessory>();
                        var maxDetailId = (int) (_repDetail.Max(t => t._id) ?? 0) + 1;
                        var maxAccId = (int) (_repAcc.Max(t => t._id) ?? 0) + 1;
                        foreach (var rec in list)
                        {
                            rec.LineName =
                                rec.LineName.Replace("、", "/").Replace("/", ",").Replace("\r\n", "").Replace(" ", "");
                            var lines = rec.LineName.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
                            var detailIdList = new List<int>();

                            if (lines.Length > 2)
                                rec.ScreenType = ScreenTypeEnum.表格定制屏;
                            else if (lines.Length == 2)
                            {
                                int unit;
                                if (int.TryParse(rec.Materials.UnitBoard, out unit))
                                {
                                    if (unit == 28) rec.ScreenType = ScreenTypeEnum.七行双线屏;
                                    else if (unit == 35) rec.ScreenType = ScreenTypeEnum.定制双线屏;
                                }
                            }
                            else if (lines.Length == 1)
                                rec.ScreenType = ScreenTypeEnum.标准单线屏;
                            if (rec.IsLog)
                            {
                                var detail = new ScreenRecDetail();
                                detail._id = maxDetailId;
                                detailIdList.Add(maxDetailId++);
                                detail.LineName = rec.LineName;
                                //detail.LinesInSameScreen = string.Join("、", lines.Except(new[] {line}));
                                detail.ConstructionType = rec.ConstructionType;
                                detail.Materials = rec.Materials;
                                detail.Owner = rec.Owner;
                                detail.InstallStation = rec.InstallStation;
                                detail.InstallDate = rec.InstallDate;
                                detail.ScreenCount = rec.ScreenCount;
                                detail.DeviceNum = rec.DeviceNum;

                                detail.ExtraRemark = rec.ExtraRemark;
                                detail.IsLog = rec.IsLog;

                                detail.Date = detail.InstallDate;
                                detail.LogType = "日志类型";
                                detail.IsWireLess = rec.IsWireLess;
                                detailList.Add(detail);

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

                            accList.AddRange(from prop in typeof(Materials).GetProperties()
                                let val = prop.GetValue(rec.Materials).ToString()
                                where !string.IsNullOrWhiteSpace(val) && prop.Name != "Remark"
                                select new Accessory
                                {
                                    _id = maxAccId++,
                                    Count = val,
                                    DevNum = rec.DeviceNum,
                                    Name = prop.GetCName(),
                                    RecDetailIds = detailIdList.ToArray(),
                                    RecId = rec._id
                                });
                        }
                        _rep.BulkInsert(list);
                        if (detailList.Any()) _repDetail.BulkInsert(detailList);
                        if (accList.Any()) _repAcc.BulkInsert(accList);
                    }
                    // 关闭本窗体（触发窗体的关闭事件）
                    PageContext.RegisterStartupScript(ActiveWindow.GetHidePostBackReference());
                    ShowNotify("导入成功");
                }
                catch (Exception e)
                {
                    Alert.Show(e.Message, MessageBoxIcon.Warning);
                    return UIHelper.Result();
                }
                finally
                {
                    file.InputStream.Dispose();
                }

            }

            return UIHelper.Result();
        }

        public ViewResult Upload(string devNum)
        {
            ViewBag.devNum = devNum;
            return View();
        }

        [HttpPost]
        public ActionResult Upload(HttpPostedFileBase file, string devNum)
        {
            if (file == null || file.ContentLength == 0) return UIHelper.Result();
            try
            {
                var fileName = file.FileName;
                var fileType = GetFileType(fileName);
                if (!ValidPicTypes.Contains(fileType))
                {
                    // 清空文件上传组件
                    UIHelper.FileUpload("file").Reset();
                    ShowNotify("无效的文件类型！");
                }
                else
                {
                    var md5 = file.InputStream.GetMd5();
                    if (_repImg.Get(t => t.Md5 == md5) != null)
                    {
                        Alert.Show("文件已存在", MessageBoxIcon.Warning);
                        return UIHelper.Result();
                    }
                    var name = file.FileName;
                    if (_repImg.Get(t => t.DevNum == devNum && t.Name == file.FileName) != null)
                    {
                        name = file.FileName + "_" + Guid.NewGuid();
                    }
                    var path = Server.MapPath("~/screenImgs/") + devNum;
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    var imgPath = Path.Combine(path, name);
                    file.SaveAs(imgPath);
                    _repImg.Add(new ScreenImage(devNum, name, imgPath,md5));
                }
            }
            catch (Exception e)
            {
                Alert.Show(e.Message, MessageBoxIcon.Warning);
                return UIHelper.Result();
            }
            finally
            {
                file.InputStream.Close();
            }   
            // 关闭本窗体（触发窗体的关闭事件）
            PageContext.RegisterStartupScript(ActiveWindow.GetHidePostBackReference());
            ShowNotify("上传成功!");
            return UIHelper.Result();
        }

        public ViewResult ViewImage(string devNum)
        {
            ViewBag.devNum = devNum;
            var imgs = _repImg.Find(t => t.DevNum == devNum).ToList();
            return View(imgs);
        }
        public ActionResult GetImg(string path)
        {
            return File(System.IO.File.OpenRead(path), "image/jpeg");
        }

        public FileResult Export()
        {
            var filter = (List<FilterDefinition<ScreenRec>>) Session["filterRec"];
            if (filter != null) filter.RemoveAt(0);
            var list =
                _rep.Find(filter != null && filter.Any() ? Builders<ScreenRec>.Filter.And(filter) : null)
                    .SortBy(t => t.DeviceNum)
                    .ToList();
            var devNums =
                list.Where(t => !string.IsNullOrEmpty(t.LineName) && !string.IsNullOrEmpty(t.InstallStation))
                    .OrderBy(t => t.InstallDate)
                    .Select(t => t.DeviceNum)
                    .Distinct()
                    .ToList();
            var mats = _repAcc.Find().ToList();
            const string thHtml = "<th>{0}</th>";
            const string thHtmlMulti = "<th rowspan=\"2\">{0}</th>";
            const string tdHtml = "<td style=\"text-align: center;\">{0}</td>";
            const string tdHtmlMulti = "<td style=\"text-align: center;\" rowspan=\"{1}\">{0}</td>";

            var sb = new StringBuilder();
            sb.Append("<table cellspacing=\"0\" rules=\"all\" border=\"1\" style=\"border-collapse:collapse;\">");
            sb.Append("<tr>");
            sb.AppendFormat(thHtmlMulti, "");
            sb.AppendFormat(thHtmlMulti, "设备编号");
            sb.AppendFormat(thHtmlMulti, "营运公司");
            sb.AppendFormat(thHtmlMulti, "安装线路");
            sb.AppendFormat(thHtmlMulti, "安装站点");
            sb.AppendFormat(thHtmlMulti, "屏幕类型");
            sb.AppendFormat(thHtmlMulti, "屏数");
            sb.AppendFormat(thHtmlMulti, "安装日期");
            sb.AppendFormat("<th colspan=\"14\">{0}</th>", "使用材料");
            sb.Append("</tr>");

            sb.Append("<tr>");
            sb.AppendFormat(thHtml, "电源线");
            sb.AppendFormat(thHtml, "网线");
            sb.AppendFormat(thHtml, "网络跳线");
            sb.AppendFormat(thHtml, "小交换机");
            sb.AppendFormat(thHtml, "大交换机");
            sb.AppendFormat(thHtml, "电源接线板");
            sb.AppendFormat(thHtml, "一分二电源开关");
            sb.AppendFormat(thHtml, "USB网卡");
            sb.AppendFormat(thHtml, "三线插头");
            sb.AppendFormat(thHtml, "不锈钢方管");
            sb.AppendFormat(thHtml, "电源");
            sb.AppendFormat(thHtml, "单元板");
            sb.AppendFormat(thHtml, "雨棚");
            sb.AppendFormat(thHtml, "备注");
            sb.Append("</tr>");

            var rowIndex = 1;
            foreach (var devNum in devNums)
            {
                var recs = list.Where(t => t.DeviceNum == devNum).ToList();
                var recCount = recs.Count;
                for (var i = 0; i < recs.Count; i++)
                {
                    var item = recs[i];
                    var mat = mats.Where(t => t.RecId == item._id).ToList();

                    sb.Append("<tr>");
                    sb.AppendFormat(tdHtml, rowIndex++);

                    if (i == 0)
                    {
                        sb.AppendFormat(tdHtmlMulti, devNum, recCount);
                        sb.AppendFormat(tdHtmlMulti, item.Owner, recCount);
                        sb.AppendFormat(tdHtmlMulti, item.LineName, recCount);
                        sb.AppendFormat(tdHtmlMulti, item.InstallStation, recCount);
                        sb.AppendFormat(tdHtmlMulti, item.ScreenType, recCount);
                    }
                    sb.AppendFormat(tdHtml, item.ScreenCount);
                    sb.AppendFormat(tdHtml,
                        item.InstallDate != null ? item.InstallDate.Value.ToString("yyyy-MM-dd") : "");
                    if (mat.Any())
                    {
                        var cord = mat.FirstOrDefault(t => t.Name == "电源线");
                        sb.AppendFormat(tdHtml, cord != null ? cord.Count : "");
                        var cable = mat.FirstOrDefault(t => t.Name == "网线");
                        sb.AppendFormat(tdHtml, cable != null ? cable.Count : "");
                        var grid = mat.FirstOrDefault(t => t.Name == "网络跳线");
                        sb.AppendFormat(tdHtml, grid == null ? "" : grid.Count);
                        var small = mat.FirstOrDefault(t => t.Name == "小交换机");
                        sb.AppendFormat(tdHtml, small == null ? "" : small.Count);
                        var big = mat.FirstOrDefault(t => t.Name == "大交换机");
                        sb.AppendFormat(tdHtml, big == null ? "" : big.Count);
                        var panel = mat.FirstOrDefault(t => t.Name == "电源接线板");
                        sb.AppendFormat(tdHtml, panel == null ? "" : panel.Count);
                        var ps = mat.FirstOrDefault(t => t.Name == "一分二电源开关");
                        sb.AppendFormat(tdHtml, ps == null ? "" : ps.Count);
                        var adp = mat.FirstOrDefault(t => t.Name == "USB网卡");
                        sb.AppendFormat(tdHtml, adp == null ? "" : adp.Count);
                        var plug = mat.FirstOrDefault(t => t.Name == "三线插头");
                        sb.AppendFormat(tdHtml, plug == null ? "" : plug.Count);
                        var tube = mat.FirstOrDefault(t => t.Name == "不锈钢方管");
                        sb.AppendFormat(tdHtml, tube == null ? "" : tube.Count);
                        var power = mat.FirstOrDefault(t => t.Name == "电源");
                        sb.AppendFormat(tdHtml, power == null ? "" : power.Count);
                        var unit = mat.FirstOrDefault(t => t.Name == "单元板");
                        sb.AppendFormat(tdHtml, unit == null ? "" : unit.Count);
                        var canopy = mat.FirstOrDefault(t => t.Name == "雨棚");
                        sb.AppendFormat(tdHtml, canopy == null ? "" : canopy.Count);
                    }
                    else
                    {
                        sb.AppendFormat(tdHtml, "");
                        sb.AppendFormat(tdHtml, "");
                        sb.AppendFormat(tdHtml, "");
                        sb.AppendFormat(tdHtml, "");
                        sb.AppendFormat(tdHtml, "");
                        sb.AppendFormat(tdHtml, "");
                        sb.AppendFormat(tdHtml, "");
                        sb.AppendFormat(tdHtml, "");
                        sb.AppendFormat(tdHtml, "");
                        sb.AppendFormat(tdHtml, "");
                        sb.AppendFormat(tdHtml, "");
                        sb.AppendFormat(tdHtml, "");
                        sb.AppendFormat(tdHtml, "");
                    }
                    sb.AppendFormat(tdHtml, item.Materials.Remark);
                    sb.Append("</tr>");
                }
            }
            sb.Append("</table>");
            return File(Encoding.UTF8.GetBytes(sb.ToString()), "application/excel", "发车屏导出列表.xls");
        }

        private void UpdateGrid(NameValueCollection values)
        {
            JArray fields = JArray.Parse(values["Grid1_fields"]);
            int pageIndex = Convert.ToInt32(values["Grid1_pageIndex"]??"0");
            var pageSize = Convert.ToInt32(values["Grid1_pageSize"] ?? "0");

            var devNum = values["tbDevNum"];
            var owner = values["ddlOwner"] != null && values["ddlOwner"]!="[]" ? JArray.Parse(values["ddlOwner"]) : null;
            var line = values["tbLine"];
            var isFuzzyLine = values["cbIsFuzzyLine"];
            var station = values["tbStation"];
            var isFuzzyStation = values["cbIsFuzzyStation"];
            var screenType = values["tbType"];
            var screenCount = values["nbCount"];
            var inStallDateLower = values["tbDateLower"];
            var inStallDateUpper = values["tbDateUpper"];
            var chargeDateLower = values["dpDateLower"];
            var chargeDateUpper = values["dpDateUpper"];
            var lower = values["nbLower"];
            var upper = values["nbUpper"];
            var paymentStatus = values["ddlStatus"];
            var isWireless = values["ddlIsWireless"];
            var remark = values["tbRemark"];

            var sortField = values["Grid1_sortField"];
            var sortDirection = values["Grid1_sortDirection"];
            SortDefinition<ScreenRec> sort = null;
            if (!string.IsNullOrEmpty(sortField))
            {
                if (!string.IsNullOrEmpty(sortDirection))
                {
                    var exp = sortField.ToLambda<ScreenRec>();
                    if (sortDirection.Equals("ASC", StringComparison.CurrentCultureIgnoreCase))
                        sort = Builders<ScreenRec>.Sort.Ascending(exp);
                    else if (sortDirection.Equals("DESC", StringComparison.CurrentCultureIgnoreCase))
                        sort = Builders<ScreenRec>.Sort.Descending(exp);
                }
            }
            //var filter = new List<FilterDefinition<ScreenRecDetail>>
            //{
            //    _filter
            //};
            var filterRec = new List<FilterDefinition<ScreenRec>>
            {
                _filter
            };
            if (!string.IsNullOrWhiteSpace(devNum))
            {
                //filter.Add(Builders<ScreenRecDetail>.Filter.Eq(t => t.DeviceNum, devNum));
                filterRec.Add(Builders<ScreenRec>.Filter.Eq(t => t.DeviceNum, devNum));
            }
            if (owner!=null)
            {
                //filter.Add(Builders<ScreenRecDetail>.Filter.In(t => t.Owner,owner));
                filterRec.Add(Builders<ScreenRec>.Filter.In(t => t.Owner, owner));
            }
            if (!string.IsNullOrWhiteSpace(line))
            {
                var isFuzzy = bool.Parse(isFuzzyLine);
                if (isFuzzy)
                {
                    //filter.Add(Builders<ScreenRecDetail>.Filter.Regex(t => t.LineName,
                    //    new BsonRegularExpression(new Regex(line.Trim()))));
                    filterRec.Add(Builders<ScreenRec>.Filter.Regex(t => t.LineName, new BsonRegularExpression(new Regex(line.Trim()))));
                }
                else
                {
                    //filter.Add(Builders<ScreenRecDetail>.Filter.Eq(t => t.LineName, line.Trim()));
                    filterRec.Add(Builders<ScreenRec>.Filter.Eq(t => t.LineName, line.Trim()));
                }
            }
            if (!string.IsNullOrWhiteSpace(station))
            {
                var isFuzzy = bool.Parse(isFuzzyStation);
                if (isFuzzy)
                {
                    //filter.Add(Builders<ScreenRecDetail>.Filter.Regex(t => t.InstallStation,
                    //    new BsonRegularExpression(new Regex(station.Trim()))));
                    filterRec.Add(Builders<ScreenRec>.Filter.Regex(t => t.InstallStation, new BsonRegularExpression(new Regex(station.Trim()))));
                }
                else
                {
                    //filter.Add(Builders<ScreenRecDetail>.Filter.Eq(t => t.InstallStation, station.Trim()));
                    filterRec.Add(Builders<ScreenRec>.Filter.Eq(t => t.InstallStation, station.Trim()));
                }
            }
            if (!string.IsNullOrWhiteSpace(screenType))
            {
                filterRec.Add(Builders<ScreenRec>.Filter.Eq(t => t.ScreenType,
                    Enum.Parse(typeof(ScreenTypeEnum), screenType)));
            }
            if (!string.IsNullOrWhiteSpace(screenCount))
            {
                int c;
                if (int.TryParse(screenCount, out c))
                {
                    //filter.Add(Builders<ScreenRecDetail>.Filter.Eq(t => t.ScreenCount, c));
                    filterRec.Add(Builders<ScreenRec>.Filter.Eq(t => t.ScreenCount, c));
                }
            }
            if (!string.IsNullOrWhiteSpace(inStallDateLower))
            {
                //filter.Add(Builders<ScreenRecDetail>.Filter.Eq(t => t.InstallDate, DateTime.Parse(inStallDate)));
                filterRec.Add(Builders<ScreenRec>.Filter.Gte(t => t.InstallDate, DateTime.Parse(inStallDateLower)));
            }
            if (!string.IsNullOrWhiteSpace(inStallDateUpper))
            {
                filterRec.Add(Builders<ScreenRec>.Filter.Lte(t => t.InstallDate, DateTime.Parse(inStallDateUpper)));
            }
            if (!string.IsNullOrWhiteSpace(chargeDateLower))
            {
                //filter.Add(Builders<ScreenRecDetail>.Filter.Eq(t => t.ChargeTime, DateTime.Parse(chargeDate)));
                filterRec.Add(Builders<ScreenRec>.Filter.Gte(t => t.ChargeTime, DateTime.Parse(chargeDateLower)));
            }
            if (!string.IsNullOrWhiteSpace(chargeDateUpper))
            {
                //filter.Add(Builders<ScreenRecDetail>.Filter.Eq(t => t.ChargeTime, DateTime.Parse(chargeDate)));
                filterRec.Add(Builders<ScreenRec>.Filter.Lte(t => t.ChargeTime, DateTime.Parse(chargeDateUpper)));
            }
            if (!string.IsNullOrWhiteSpace(lower))
            {
                double d;
                if (double.TryParse(lower, out d) && !double.IsNaN(d))
                {
                    //filter.Add(Builders<ScreenRecDetail>.Filter.Gte(t => t.Price, d));
                    filterRec.Add(Builders<ScreenRec>.Filter.Gte(t => t.Price, d));
                }
            }
            if (!string.IsNullOrWhiteSpace(upper))
            {
                double d;
                if (double.TryParse(upper, out d)&&!double.IsNaN(d))
                {
                    //filter.Add(Builders<ScreenRecDetail>.Filter.Lte(t => t.Price, d));
                    filterRec.Add(Builders<ScreenRec>.Filter.Lte(t => t.Price,d));
                }
            }
            if (!string.IsNullOrWhiteSpace(paymentStatus))
            {
                //filter.Add(Builders<ScreenRecDetail>.Filter.Eq(t => t.PaymentStatus, paymentStatus));
                filterRec.Add(Builders<ScreenRec>.Filter.Eq(t => t.PaymentStatus, paymentStatus));
            }
            if (!string.IsNullOrWhiteSpace(isWireless))
            {
                bool r;
                if (bool.TryParse(isWireless, out r))
                {
                        //filter.Add(Builders<ScreenRecDetail>.Filter.Eq(t => t.IsWireLess, r));
                        filterRec.Add(Builders<ScreenRec>.Filter.Eq(t => t.IsWireLess, r));
                }
            }
            if (!string.IsNullOrWhiteSpace(remark))
            {
                filterRec.Add(Builders<ScreenRec>.Filter.Regex(t => t.Materials.Remark,
                    new BsonRegularExpression(new Regex(remark.Trim(),RegexOptions.IgnoreCase))));
            }
            Session["filter"] = filterRec;
            Session["filterRec"] = filterRec;

            int count;
            var list = _rep.QueryByPage(pageIndex, pageSize, out count,
                filterRec.Any() ? Builders<ScreenRec>.Filter.And(filterRec) : null, sort);
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
            var owners = new List<ListItem>
            {
                //new ListItem("全部", "")
            };
            owners.AddRange(
                _rep.Distinct(t => t.Owner).OrderBy(t => t).Select(t => new ListItem(t.ToString(), t.ToString(), false)));
            ViewBag.Owners = owners.ToArray();
            var l = GetBasicDdlByName("付款状态").ToList(); l.Insert(0, new ListItem("全部", ""));
            ViewBag.ddlStatus = l.ToArray();
            ViewBag.ddlIsWireless = new[]
                {new ListItem("全部", ""), new ListItem("是", "true"), new ListItem("否", "false")};
            return View();
        }

        public ViewResult Add()
        {
            ViewBag.ScreenTypes =
            (from object e in Enum.GetValues(typeof(ScreenTypeEnum))
                select new ListItem(e.ToString(), ((int) e).ToString())).ToArray();
            ViewBag.Owners = _rep.Distinct(t => t.Owner).Where(t=>!string.IsNullOrEmpty(t)).OrderBy(t => t).Select(t => new ListItem(t.ToString(), t.ToString(), false)).ToArray();
            return View();
        }

        [HttpPost]
        public ActionResult Add(ScreenRec model)
        {
            if (_rep.Get(t => t.DeviceNum == model.DeviceNum) != null)
            {
                Alert.Show("设备编号已存在", MessageBoxIcon.Warning);
                return UIHelper.Result();
            }
            model._id = (int) (_rep.Max(t => t._id) ?? 0) + 1;
            _rep.Add(model);

            //_rep.Add(new ScreenRec
            //{
            //    _id = (int) (_rep.Max(t => t._id) ?? 0) + 1,
            //    DeviceNum = model.DeviceNum,
            //    Owner = model.Owner,
            //    LineName = string.Format("{0}、{1}", model.LineName, model.LinesInSameScreen),
            //    ConstructionType = model.ConstructionType,
            //    ScreenType = model.ScreenType,
            //    IsWireLess = model.IsWireLess,
            //    ScreenCount = model.ScreenCount,
            //    InstallStation = model.InstallStation,
            //    InstallDate = model.InstallDate,
            //    Materials = model.Materials
            //});

            PageContext.RegisterStartupScript(ActiveWindow.GetHidePostBackReference());
            return UIHelper.Result();
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
             var filter = (List<FilterDefinition<ScreenRec>>) Session["filterRec"];
            var screens =
                _rep.Find(filter != null && filter.Any() ? Builders<ScreenRec>.Filter.And(filter) : _filter).ToList();
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

        public ActionResult DeleteAll(FormCollection values)
        {
            _repDetail.Delete(t => true);
            _rep.Delete(t => true);
            _repAcc.Delete(t => true);
            UpdateGrid(values);
            return UIHelper.Result();
        }
        public ActionResult Delete(JArray selectedRows, FormCollection values)
        {
            var ids = selectedRows.Select(Convert.ToInt32).ToList();
            var screens =
                _rep.Find(t => ids.Contains(t._id))
                    .ToList()
                    .GroupBy(t => new {t.LineName, t.InstallStation})
                    .Select(t => t.Key)
                    .ToList();
            foreach (var screen in screens)
            {
                var s = screen;
                _rep.Delete(t => t.LineName == s.LineName && t.InstallStation == s.InstallStation);
                //_rep.Delete(
                //    Builders<ScreenRec>.Filter.And(
                //        Builders<ScreenRec>.Filter.Eq(t => t.InstallStation, s.InstallStation),
                //        Builders<ScreenRec>.Filter.Regex(t => t.LineName, new BsonRegularExpression(s.LineName))));
            }
            UpdateGrid(values);
            return UIHelper.Result();
        }

        public ActionResult btnSubmit_Click(JArray Grid1_fields, JArray Grid1_modifiedData, int Grid1_pageIndex, int Grid1_pageSize, JArray Grid2_modifiedData, JArray Grid2_fields, JArray Grid3_modifiedData, JArray Grid4_modifiedData, JArray Grid4_fields, JArray Grid5_modifiedData)
        {
            if (string.IsNullOrEmpty(CommonHelper.User)) RedirectToAction("Index", "Login");
            if (!Grid1_modifiedData.Any() && !Grid2_modifiedData.Any() && !Grid3_modifiedData.Any() && !Grid4_modifiedData.Any())
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
                var sb = new StringBuilder("修改主记录：");
                var srcDetail = _rep.Get(t => t._id == rowId);
                foreach (var p in rowDic)
                {
                    var param = Expression.Parameter(typeof(ScreenRec), "x");
                    var paramArr = p.Key.Split(new[] {'_'}, StringSplitOptions.RemoveEmptyEntries);
                    var body = paramArr.Length > 1
                        ? Expression.Property(Expression.Property(param, typeof(ScreenRec), paramArr[0]),
                            typeof(Materials), paramArr[1])
                        : Expression.Property(param, typeof(ScreenRec), paramArr[0]);
                    var lambda =
                        Expression.Lambda<Func<ScreenRec, object>>(Expression.Convert(body, typeof(object)), param);
                    _rep.Update(t => t.DeviceNum == srcDetail.DeviceNum, Builders<ScreenRec>.Update.Set(lambda, p.Value));
                    if (p.Key == "Price" || p.Key == "PaymentStatus" || p.Key == "ChargeTime" || p.Key == "IsWireLess")
                    {
                        //var paramRec = Expression.Parameter(typeof(ScreenRec));
                        //var bodyRec = Expression.Property(paramRec, typeof(ScreenRec), p.Key);
                        //  var lambdaRec =
                        //Expression.Lambda<Func<ScreenRec, object>>(Expression.Convert(bodyRec, typeof(object)), paramRec);
                        //_rep.Update(t => t.DeviceNum == srcDetail.DeviceNum,
                        //    Builders<ScreenRec>.Update.Set(lambdaRec, p.Value));
                        //_rep.UpdateMany(t => t.DeviceNum == srcDetail.DeviceNum, Builders<ScreenRecDetail>.Update.Set(lambda, p.Value));
                    }
                    var original = paramArr.Length > 1
                        ? srcDetail.Materials.Remark
                        : typeof(ScreenRec).GetProperty(p.Key).GetValue(srcDetail);
                    if (original != null && p.Key != original)
                        sb.AppendFormat("{0}由{1}改为{2}；", paramArr.Length > 1 ? "备注" : typeof(ScreenRec).GetCName(p.Key),
                            original, p.Value);
                    else
                        sb.AppendFormat("{0}改为{1}；", paramArr.Length > 1 ? "备注" : typeof(ScreenRec).GetCName(p.Key),
                            p.Value);
                }

                _repDetail.Add(new ScreenRecDetail
                {
                    IsLog = true,
                    LogType = "日志类型",
                    Date = DateTime.Now,
                    DeviceNum = srcDetail.DeviceNum,
                    Operator = CommonHelper.UserName,
                    Materials = new Materials { Remark = sb.ToString() }
                });
            }

            int count;
            var source = _rep.QueryByPage(Grid1_pageIndex, Grid1_pageSize, out count, _filter);
            //var source = _repDetail.Find(t => string.IsNullOrEmpty(t.Materials.Remark)).ToList();
            var grid1 = UIHelper.Grid("Grid1");
            grid1.RecordCount(count);
            grid1.DataSource(source, Grid1_fields);

            foreach (var jToken in Grid2_modifiedData)
            {
                var modifiedRow = (JObject) jToken;
                string status = modifiedRow.Value<string>("status");
                var rowId = modifiedRow.Value<string>("id");

                if (status == "newadded")
                {
                    var rowDic = modifiedRow.Value<JObject>("values").ToObject<Dictionary<string, object>>();

                    var model = new ScreenRepairs();
                    model._id = (int) (_repRepairs.Max(t => t._id) ?? 0) + 1;
                    foreach (var p in rowDic)
                    {
                        var prop = typeof(ScreenRepairs).GetProperty(p.Key);
                        if (prop.PropertyType == typeof(int))
                            prop.SetValue(model, Convert.ToInt32(p.Value));
                        else if (prop.PropertyType == typeof(DateTime?) || prop.PropertyType == typeof(DateTime))
                        {
                            if(!string.IsNullOrEmpty(p.Value.ToString()))
                                prop.SetValue(model, DateTime.Parse(p.Value.ToString()));
                        }
                        else if (prop.PropertyType == typeof(double))
                        {
                            double d;
                            if (double.TryParse(p.Value.ToString(), out d))
                            {
                                prop.SetValue(model, d);
                            }
                        }
                        else if (prop.PropertyType != typeof(int?))
                        {
                            prop.SetValue(model, p.Value);
                        }
                    }
                    _repRepairs.Add(model);
                }
                else if (status == "deleted")
                {
                    _repRepairs.Delete(t => t._id == int.Parse(rowId));
                }
            }

            foreach (var jToken in Grid3_modifiedData)
            {
                var modifiedRow = (JObject)jToken;
                string status = modifiedRow.Value<string>("status");
                var rowId = modifiedRow.Value<string>("id");

                if (status == "newadded")
                {
                    var rowDic = modifiedRow.Value<JObject>("values").ToObject<Dictionary<string, object>>();

                    var model = new ScreenRecDetail();
                    model._id = (int)(_repDetail.Max(t => t._id) ?? 0) + 1;
                    foreach (var p in rowDic)
                    {
                        var paramArr = p.Key.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
                        if (paramArr.Length > 1)
                            typeof(Materials).GetProperty(paramArr[1])
                                .SetValue(typeof(ScreenRecDetail).GetProperty(paramArr[0]).GetValue(model), p.Value);
                        else
                        {
                            var prop = typeof(ScreenRecDetail).GetProperty(paramArr[0]);
                            if (prop.PropertyType == typeof(int))
                                prop.SetValue(model, Convert.ToInt32(p.Value));
                            else if (prop.PropertyType == typeof(DateTime?) || prop.PropertyType == typeof(DateTime))
                            {
                                if (!string.IsNullOrEmpty(p.Value.ToString()))
                                    prop.SetValue(model, DateTime.Parse(p.Value.ToString()));
                            }
                            else if (prop.PropertyType == typeof(double))
                            {
                                double d;
                                if (double.TryParse(p.Value.ToString(), out d))
                                {
                                    prop.SetValue(model, d);
                                }
                            }
                            else if (prop.PropertyType != typeof(int?))
                            {
                                prop.SetValue(model, p.Value);
                            }
                        }
                    }
                    model.IsLog = true;
                    model.LogType = LogType.收费类型.ToString();

                    var rec = _rep.Get(t => t.DeviceNum == model.DeviceNum && !t.IsLog);
                    model.Owner = rec.Owner;
                    model.InstallStation = rec.InstallStation;
                    model.LineName = rec.LineName;
                    _repDetail.Add(model);
                }
                else if (status == "deleted")
                {
                    _repDetail.Delete(t => t._id == int.Parse(rowId));
                }
            }

            string devNum = null;
            foreach (var jToken in Grid4_modifiedData)
            {
                var modifiedRow = (JObject)jToken;
                string status = modifiedRow.Value<string>("status");
                var rowId = modifiedRow.Value<string>("id");
                if (string.IsNullOrEmpty(devNum)&&status!="deleted")
                {
                    devNum =
                        modifiedRow.Value<JObject>("values").ToObject<Dictionary<string, object>>()["DeviceNum"]
                            .ToString();
                }

                if (status == "newadded")
                {
                    var rowDic = modifiedRow.Value<JObject>("values").ToObject<Dictionary<string, object>>();
                    if (!rowDic.ContainsKey("Materials_Remark"))
                    {
                        Alert.Show("备注不能为空！");
                        return UIHelper.Result();
                    }

                    var model = new ScreenRecDetail();
                    model._id = (int)(_repDetail.Max(t => t._id) ?? 0) + 1;
                    foreach (var p in rowDic)
                    {
                        var paramArr = p.Key.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
                        if (paramArr.Length > 1)
                            typeof(Materials).GetProperty(paramArr[1])
                                .SetValue(typeof(ScreenRecDetail).GetProperty(paramArr[0]).GetValue(model), p.Value);
                        else
                        {
                            var prop = typeof(ScreenRecDetail).GetProperty(paramArr[0]);
                            if (prop.PropertyType == typeof(int))
                                prop.SetValue(model, Convert.ToInt32(p.Value));
                            else if (prop.PropertyType == typeof(DateTime?) || prop.PropertyType == typeof(DateTime))
                            {
                                if (!string.IsNullOrEmpty(p.Value.ToString()))
                                    prop.SetValue(model, DateTime.Parse(p.Value.ToString()));
                            }
                            else if (prop.PropertyType == typeof(double))
                            {
                                double d;
                                if (double.TryParse(p.Value.ToString(), out d))
                                {
                                    prop.SetValue(model, d);
                                }
                            }
                            else if (prop.PropertyType != typeof(int?))
                            {
                                prop.SetValue(model, p.Value);
                            }
                        }
                    }
                    model.IsLog = true;
                    model.LogType ="日志类型";
                    _repDetail.Add(model);
                }
                else if (status == "deleted")
                {
                    _repDetail.Delete(t => t._id == int.Parse(rowId));
                }
            }
            UIHelper.Grid("Grid4").DataSource(
                _repDetail.Find(t => t.DeviceNum == devNum && t.IsLog && t.LogType == "日志类型").ToList(), Grid4_fields);

            foreach (var jToken in Grid5_modifiedData)
            {
                var modifiedRow = (JObject)jToken;
                string status = modifiedRow.Value<string>("status");
                var rowId = modifiedRow.Value<string>("id");

                if (status == "newadded")
                {
                    var rowDic = modifiedRow.Value<JObject>("values").ToObject<Dictionary<string, object>>();
                    if (!rowDic.ContainsKey("Materials_Remark"))
                    {
                        Alert.Show("备注不能为空！");
                        return UIHelper.Result();
                    }

                    var model = new ScreenRecDetail();
                    model._id = (int)(_repDetail.Max(t => t._id) ?? 0) + 1;
                    foreach (var p in rowDic)
                    {
                        var paramArr = p.Key.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
                        if (paramArr.Length > 1)
                            typeof(Materials).GetProperty(paramArr[1])
                                .SetValue(typeof(ScreenRecDetail).GetProperty(paramArr[0]).GetValue(model), p.Value);
                        else
                        {
                            var prop = typeof(ScreenRecDetail).GetProperty(paramArr[0]);
                            if (prop.PropertyType == typeof(int))
                                prop.SetValue(model, Convert.ToInt32(p.Value));
                            else if (prop.PropertyType == typeof(DateTime?) || prop.PropertyType == typeof(DateTime))
                            {
                                if (!string.IsNullOrEmpty(p.Value.ToString()))
                                    prop.SetValue(model, DateTime.Parse(p.Value.ToString()));
                            }
                            else if (prop.PropertyType == typeof(double))
                            {
                                double d;
                                if (double.TryParse(p.Value.ToString(), out d))
                                {
                                    prop.SetValue(model, d);
                                }
                            }
                            else if (prop.PropertyType != typeof(int?))
                            {
                                prop.SetValue(model, p.Value);
                            }
                        }
                    }
                    model.IsLog = true;
                    model.LogType = "巡检类型";
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
        public ActionResult Grid1_RowClick(string rowText, JArray Grid2_fields, JArray Grid3_fields, JArray Grid4_fields, JArray Grid5_fields)
        {
            UIHelper.Grid("Grid2").DataSource(
                _repRepairs.Find(t => t.DeviceNum == rowText)
                    .Sort(
                        new SortDefinitionBuilder<ScreenRepairs>().Descending(t => t.LineName))
                    .ToList(), Grid2_fields);
            UIHelper.Grid("Grid3").DataSource(
                _repDetail.Find(t => t.DeviceNum == rowText && t.IsLog && t.LogType == "收费类型")
                    .Sort(
                        new SortDefinitionBuilder<ScreenRecDetail>().Descending(t => t.LineName))
                    .ToList(), Grid3_fields);
            UIHelper.Grid("Grid4").DataSource(
                _repDetail.Find(t => t.DeviceNum == rowText && t.IsLog && t.LogType == "日志类型")
                    .Sort(
                        new SortDefinitionBuilder<ScreenRecDetail>().Descending(t => t.LineName))
                    .ToList(), Grid4_fields);
            UIHelper.Grid("Grid5").DataSource(
               _repDetail.Find(t => t.DeviceNum == rowText && t.IsLog && t.LogType == "巡检类型")
                   .Sort(
                       new SortDefinitionBuilder<ScreenRecDetail>().Descending(t => t.LineName))
                   .ToList(), Grid5_fields);
            return UIHelper.Result();
        }

        private ListItem[] GetBasicDdlByName(string name)
        {
            return _repBasic.Find(t => t.Type == name).ToList().Select(t => new ListItem(t.Name, t.Name)).ToArray();
        }
    }
}