namespace VibeNet.Models
{
    public class UpdateProfileRequest
    {
        public string? FullName { get; set; }
        public string? MobileNumber { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? Bio { get; set; }
    }
}
