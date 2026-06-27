using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BloodBank.Models
{
    public class Hospital
    {
        [Key]
        public int HospitalId { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Address { get; set; } = string.Empty;

        [StringLength(100)]
        public string? City { get; set; }

        [Required]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Email { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Active";
        // Active / Inactive

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public ICollection<BloodUnit>? BloodUnits { get; set; }
        public ICollection<Appointment>? Appointments { get; set; }
        public ICollection<Request>? Requests { get; set; }
    }
}
