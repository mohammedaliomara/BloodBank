namespace BloodBank.Models
{
    public class BloodCenter
    {
        public int    Id          { get; set; }
        public string Name        { get; set; } = string.Empty;
        public string Address     { get; set; } = string.Empty;
        public string Governorate { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Status      { get; set; } = "Active";
    }
}
