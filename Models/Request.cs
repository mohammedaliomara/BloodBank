using System;
using System.ComponentModel.DataAnnotations;

namespace BloodBank.Models
{
    public class Request
    {
        [Key]
        public int RequestId { get; set; }

        [Required]
        public int HospitalId { get; set; }

        [Required]
        [StringLength(5)]
        public string BloodType { get; set; } = string.Empty;
        // A+, A-, B+, B-, AB+, AB-, O+, O-

        [Required]
        public int QuantityNeeded { get; set; }

        [Required]
        [StringLength(50)]
        public string UrgencyLevel { get; set; } = "Normal";
        // Normal / Urgent / Critical

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending";
        // Pending / Approved / Fulfilled / Rejected

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime RequestDate { get; set; } = DateTime.UtcNow;

        public DateTime? FulfilledDate { get; set; }

        // Navigation Properties
        public Hospital? Hospital { get; set; }
    }
}
