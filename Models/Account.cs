namespace BloodBank.Models
{
    public class Account
    {
        public int    id             { get; set; }
        public string FirstName      { get; set; } = string.Empty;
        public string LastName       { get; set; } = string.Empty;
        public string Email          { get; set; } = string.Empty;
        public string Password       { get; set; } = string.Empty;
        public string Role           { get; set; } = string.Empty;
        public int?   BloodCenterId  { get; set; }

        public BloodCenter? BloodCenter { get; set; }
    }
}
