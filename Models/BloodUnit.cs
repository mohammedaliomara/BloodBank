using System;
using System.ComponentModel.DataAnnotations;

namespace BloodBank.Models
{
    public class BloodUnit
    {
        [Key]
        public int BloodUnitId { get; set; }

        [Required]
        [StringLength(5)]
        public string BloodType { get; set; } = string.Empty;
        // A+, A-, B+, B-, AB+, AB-, O+, O-

        [Required]
        public int Quantity { get; set; } // عدد الوحدات

        [Required]
        public int HospitalId { get; set; }

        [Required]
        public DateTime CollectionDate { get; set; }

        [Required]
        public DateTime ExpiryDate { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Available";
        // Available / Reserved / Used / Expired

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public Hospital? Hospital { get; set; }
    }
}
