using System.ComponentModel.DataAnnotations;

namespace coretex_finalproj.Models
{
    public class Info
    {
        public int Id { get; set; }
        [MaxLength(50)]
        public string LN { get; set; } = " ";
        [MaxLength(50)]
        public string FN { get; set; } = " ";
        [MaxLength(50)]
        public string MD { get; set; } = " ";
        [MaxLength(2)]
        public int Age { get; set; }
    }
}
