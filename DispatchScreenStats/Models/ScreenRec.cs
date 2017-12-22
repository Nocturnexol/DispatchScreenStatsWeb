using System;
using System.ComponentModel.DataAnnotations;
using DispatchScreenStats.Enums;
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
        public string ConstructionType { get; set; }
        public int? ScreenCount { get; set; }
        public string InstallStation { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true, NullDisplayText = "")]
        [Display(Name = "安装日期")]
        public DateTime? InstallDate { get; set; }
        public Materials Materials { get; set; }
        public string ExtraRemark { get; set; }
        public bool IsLog { get; set; }
        public double Price { get; set; }
    }

    public class ScreenRecDetail
    {
        public ScreenRecDetail()
        {
            Materials = new Materials();
        }

        public int _id { get; set; }
        public string DeviceNum { get; set; }
        public string Owner { get; set; }
        public string LineName { get; set; }
        public string LinesInSameScreen { get; set; }
        public string ConstructionType { get; set; }
        public ScreenTypeEnum? ScreenType { get; set; }
        public int? ScreenCount { get; set; }
        public string InstallStation { get; set; }

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
        public double Price { get; set; }
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

    public class Materials
    {
        public string PowerCord { get; set; }
        public string Cable { get; set; }
        public string GridLines { get; set; }
        public string SmallExchange { get; set; }
        public string BigExchange { get; set; }
        public string PatchBoard { get; set; }
        public string OneToTwoSwitch { get; set; }
        public string UsbAdapter { get; set; }
        public string ThreePinPlug { get; set; }
        public string SquareTube { get; set; }
        public string Power { get; set; }
        public string UnitBoard { get; set; }
        public string Canopy { get; set; }
        public string Remark { get; set; }
    }

    public class ScreenRepairs
    {
        public int _id { get; set; }
        public DateTime RepairsDate { get; set; }
        public string LineName { get; set; }
        public string Station { get; set; }
        public string Owner { get; set; }
        public string RepairsSoucre { get; set; }
        public string Accepter { get; set; }
        public string Handler { get; set; }
        public string HitchType { get; set; }
        public string Status { get; set; }
        public string HitchContent { get; set; }
        public string Solution { get; set; }
    }

}