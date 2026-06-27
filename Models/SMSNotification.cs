using System.ComponentModel.DataAnnotations;

namespace BloodBank.Models
{
    public class SMSNotification
    {
        [Key]
        public int IdSMS { get; set; }

        [Required]
        public string BloodNeeded { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        public DateTime SentAt  { get; set; } = DateTime.Now;
        public bool     IsRead  { get; set; } = false;

        // FKs
        public int             IdDonor     { get; set; }
        public Donor           Donor       { get; set; } = null!;

        public int?            IdAppoint   { get; set; }
        public Appointment?    Appointment { get; set; }
    }
}
