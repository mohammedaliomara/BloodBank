using System.ComponentModel.DataAnnotations;

namespace BloodBank.Models
{
    public class User
    {
        [Key]
        public int    Id          { get; set; }
        public string Name        { get; set; } = string.Empty;
        public string Phone       { get; set; } = string.Empty;
        public string Address     { get; set; } = string.Empty;
        public string Governorate { get; set; } = string.Empty;
        public string Shift       { get; set; } = string.Empty;
    }
}
