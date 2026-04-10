using System;
using System.ComponentModel.DataAnnotations;

namespace coretex_finalproj.Models
{
    public class Branch
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(150)]
        public string Address { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        [StringLength(30)]
        public string BranchCode { get; set; } = string.Empty;
    }
}
