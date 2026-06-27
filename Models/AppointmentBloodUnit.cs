using System.ComponentModel.DataAnnotations;

namespace BloodBank.Models
{
    public class AppointmentBloodUnit
    {
        [Key]
        public int Id { get; set; }

        public int       BloodUnitId  { get; set; }
        public BloodUnit BloodUnit    { get; set; } = null!;

        public int         AppointmentId { get; set; }
        public Appointment Appointment   { get; set; } = null!;
    }
}
