using System;
using DispatchScreenStats.Enums;
using MongoDB.Bson.Serialization.Attributes;

namespace DispatchScreenStats.Models
{
    public class ComfortDegreeStats
    {
        public string _id { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Time { get; set; }
        public string LineName { get; set; }
        public string VehNo { get; set; }
        public int UpDown { get; set; }
        public int LevelId { get; set; }
        public string ImgPath { get; set; }
        public int RemainCount{ get; set; }
        public string PFComfort{ get; set; }
        public string ImgComfort{ get; set; }
        public string FinalComfort { get; set; }
        public bool IsAuth { get; set; }
        public bool IsSameRes { get; set; }
        public AuthDiffType? AuthDiffType { get; set; }
        public string AuthComfort { get; set; }
        public string TransImgPath { get; set; }
    }

    public class ImgStats
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public int Value { get; set; }
    }
}