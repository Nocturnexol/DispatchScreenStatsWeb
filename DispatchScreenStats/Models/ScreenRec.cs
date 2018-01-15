using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using DispatchScreenStats.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DispatchScreenStats.Models
{
    public class ScreenRec
    {
        public ScreenRec()
        {
            Materials = new Materials();
        }

        public int _id { get; set; }
        public string DeviceNum { get; set; }
        public string Owner { get; set; }
        public string LineName { get; set; }
        public ScreenTypeEnum? ScreenType { get; set; }
        public string ConstructionType { get; set; }
        public int? ScreenCount { get; set; }
        public string InstallStation { get; set; }
        public bool IsWireLess { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true, NullDisplayText = "")]
        [Display(Name = "安装日期")]
        public DateTime? InstallDate { get; set; }
        public Materials Materials { get; set; }
        public string ExtraRemark { get; set; }
        public bool IsLog { get; set; }
        public double Price { get; set; }
        public string PaymentStatus { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? ChargeTime { get; set; }
    }

    public class ScreenImage
    {
        public ScreenImage(string devNum, string name, string path,string md5)
        {
            DevNum = devNum;
            Name = name;
            Path = path;
            Md5 = md5;
        }
        public ObjectId _id { get; set; }
        public string DevNum { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string Md5 { get; set; }
    }
    public class ScreenRecDetail
    {
        public ScreenRecDetail()
        {
            Materials = new Materials();
        }

        public int _id { get; set; }
         [Display(Name = "设备编号")]
        public string DeviceNum { get; set; }
         [Display(Name = "营运公司")]
        public string Owner { get; set; }
         [Display(Name = "线路名")]
        public string LineName { get; set; }
        public string LinesInSameScreen { get; set; }
         [Display(Name = "施工类型")]
        public string ConstructionType { get; set; }
         [Display(Name = "屏幕类型")]
        public ScreenTypeEnum? ScreenType { get; set; }
         [Display(Name = "屏数")]
        public int? ScreenCount { get; set; }
         [Display(Name = "安装站点")]
        public string InstallStation { get; set; }
         public bool IsWireLess { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true, NullDisplayText = "")]
        [Display(Name = "安装日期")]
        public DateTime? InstallDate { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true, NullDisplayText = "")]
        [Display(Name = "巡检日期")]
        public DateTime? Date { get; set; }

        public string HandlingType { get; set; }
        public string ChargeType { get; set; }
        public string PaymentStatus { get; set; }
        public Materials Materials { get; set; }
        public string ExtraRemark { get; set; }
        public bool IsLog { get; set; }
        public string LogType { get; set; }
         [Display(Name = "金额")]
        public double Price { get; set; }
         [BsonDateTimeOptions(Kind = DateTimeKind.Local), Display(Name = "收费时间")]
         public DateTime? ChargeTime { get; set; }
         public DateTime? OccurredTime { get; set; }
    }

    public class ScreenLog
    {
        public int _id { get; set; }
        public string DeviceNum { get; set; }
        public int Owner { get; set; }
        public string LineName { get; set; }
        public string InstallStation { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? InstallDate { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime SaveTime { get; set; }

        public string OperContent { get; set; }
    }

    public class ScreenRecStats
    {
        public string DeviceNum { get; set; }
        public string Details { get; set; }
        public string ConstructionType { get; set; }
        public DateTime? Date { get; set; }
        public string Remark { get; set; }
    }

    public class ScreenRecallStats
    {
        public string DeviceNum { get; set; }
        public string Details { get; set; }
        public string HandlingType { get; set; }
        public string Price { get; set; }
        public DateTime? OccurredTime { get; set; }
        public string Remark { get; set; }
    }

    public class ScreenPriceStats
    {
        public string DeviceNum { get; set; }
        public string Details { get; set; }
        public ScreenTypeEnum? ScreenType { get; set; }
        public string Price { get; set; }
        public string PaymentStatus { get; set; }
        public DateTime? OccurredTime { get; set; }
        public DateTime? ChargeTime { get; set; }
    }
    public class ScreenDetailPriceStats
    {
        public string DeviceNum { get; set; }
        public string Details { get; set; }
        public string Price { get; set; }
        public DateTime? ChargeTime { get; set; }
        public string HandlingType { get; set; }
        public string ChargeType { get; set; }
        public string PaymentStatus { get; set; }
        public string Remark { get; set; }
    }

    public class Materials
    {
        [Display(Name = "电源线")]
        public string PowerCord { get; set; }
          [Display(Name = "网线")]
        public string Cable { get; set; }
          [Display(Name = "网络跳线")]
        public string GridLines { get; set; }
          [Display(Name = "小交换机")]
        public string SmallExchange { get; set; }
          [Display(Name = "大交换机")]
        public string BigExchange { get; set; }
          [Display(Name = "电源接线板")]
        public string PatchBoard { get; set; }
          [Display(Name = "一分二电源开关")]
        public string OneToTwoSwitch { get; set; }
          [Display(Name = "USB网卡")]
        public string UsbAdapter { get; set; }
          [Display(Name = "三线插头")]
        public string ThreePinPlug { get; set; }
          [Display(Name = "不锈钢方管")]
        public string SquareTube { get; set; }
          [Display(Name = "电源")]
        public string Power { get; set; }
          [Display(Name = "单元板")]
        public string UnitBoard { get; set; }
          [Display(Name = "雨棚")]
        public string Canopy { get; set; }
         [Display(Name = "备注")]
        public string Remark { get; set; }
    }

    public class ScreenRepairs
    {
        public int _id { get; set; }
        public string DeviceNum { get; set; }
        [Display(Name = "报修日期"),Required]
        public DateTime RepairsDate { get; set; }
        [Display(Name = "线路"), Required]
        public string LineName { get; set; }
        [Display(Name = "站点"), Required]
        public string Station { get; set; }
        [Display(Name = "营运公司"), Required]
        public string Owner { get; set; }
        [Display(Name = "报修来源")]
        public string RepairsSource { get; set; }
        [Display(Name = "故障接报人")]
        public string Accepter { get; set; }
        [Display(Name = "故障处理人")]
        public string Handler { get; set; }
        [Display(Name = "故障类型")]
        public string HitchType { get; set; }
        [Display(Name = "故障状态")]
        public string Status { get; set; }
        [Display(Name = "故障问题")]
        public string HitchContent { get; set; }
        [Display(Name = "解决方法")]
        public string Solution { get; set; }

        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string FileMd5 { get; set; }
    }

}