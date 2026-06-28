namespace BloodBank.Models
{
    public class Donor
    {
        public int Id { get; set; }

        public string NationalId   { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Gender       { get; set; } = string.Empty;
        public string BloodType    { get; set; } = string.Empty;
        public string PhoneNumber  { get; set; } = string.Empty;
        public string Governorate  { get; set; } = string.Empty;
        public string? FullAddress { get; set; }
        public double? Weight      { get; set; }

        public string Status           { get; set; } = "Pending"; // Pending / Accepted / Rejected
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
        public string? RejectionReason { get; set; }

        public int AccountId { get; set; }
        public virtual Account Account { get; set; } = null!;
    }
}
