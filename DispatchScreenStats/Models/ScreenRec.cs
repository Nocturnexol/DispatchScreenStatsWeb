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
        public int _id { get;set; }
        public string DeviceNum { get; set; }
        public int Owner { get; set; }
        public string LineName { get; set; }
        public int? ScreenCount { get; set; }
        public string InstallStation { get; set; }
         [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true, NullDisplayText = "")]
        [Display(Name = "安装日期")]
        public DateTime? InstallDate { get; set; }
         public Materials Materials { get; set; }
         public string ExtraRemark { get; set; }
    }

    public class ScreenRecDetail
    {
        public ScreenRecDetail()
        {
            Materials = new Materials();
        }
        public int _id { get; set; }
        public string DeviceNum { get; set; }
        public int Owner { get; set; }
        public string LineName { get; set; }
        public string LinesInSameScreen { get; set; }
        public ScreenTypeEnum? ScreenType { get; set; }
        public int? ScreenCount { get; set; }
        public string InstallStation { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true, NullDisplayText = "")]
        [Display(Name = "安装日期")]
        public DateTime? InstallDate { get; set; }
         [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime SaveTime { get; set; }
        public Materials Materials { get; set; }
        public string ExtraRemark { get; set; }
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
        public string PatchBoard{get; set; }
        public string OneToTwoSwitch { get; set; }
        public string UsbAdapter { get; set; }
        public string ThreePinPlug { get; set; }
        public string SquareTube { get; set; }
        public string Power { get; set; }
        public string UnitBoard { get; set; }
        public string Canopy { get; set; }
        public string Remark { get; set; }
    }

}