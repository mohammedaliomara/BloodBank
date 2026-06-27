using System.ComponentModel.DataAnnotations;

namespace BloodBank.Models
{
    public class BloodUnitRequest
    {
        [Key]
        public int Id { get; set; }

        public int       BloodUnitId { get; set; }
        public BloodUnit BloodUnit   { get; set; } = null!;

        public int     RequestId { get; set; }
        public Request Request   { get; set; } = null!;
    }
}
