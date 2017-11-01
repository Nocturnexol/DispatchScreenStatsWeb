using System.ComponentModel.DataAnnotations;

namespace DispatchScreenStats.Models
{
    public class BasicData
    {
        public int _id { get; set; }

        [Required, Display(Name = "类型")]
        public string Type { get; set; }

        [Required, Display(Name = "名称")]
        public string Name { get; set; }

        [Display(Name = "备注")]
        public string Remark { get; set; }
    }
}