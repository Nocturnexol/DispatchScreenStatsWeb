namespace DispatchScreenStats.Models
{
    /// <summary>
    /// 配件
    /// </summary>
    public class Accessory
    {
        public int _id { get; set; }
        public string DevNum { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Layout { get; set; }
        public float Price { get; set; }
        public string Remark { get; set; }
    }
}