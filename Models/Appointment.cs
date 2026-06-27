using System;
using System.ComponentModel.DataAnnotations;

namespace BloodBank.Models
{
    public class Appointment
    {
        [Key]
        public int AppointmentId { get; set; }

        [Required]
        public int DonorId { get; set; }

        [Required]
        public int HospitalId { get; set; }

        [Required]
        public DateTime AppointmentDate { get; set; }

        [Required]
        public TimeSpan AppointmentTime { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; 
        // Pending / Confirmed / Cancelled / Completed

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public Hospital? Hospital { get; set; }
    }
}
