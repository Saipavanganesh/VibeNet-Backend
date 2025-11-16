namespace VibeNet.Models
{
    public class UserProfile
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string? MobileNumber { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? Bio { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string? Interests { get; set; } // JSON
        public bool IsEmailVerified { get; set; }
        public bool IsMobileVerified { get; set; }
        public bool IsSubscribed { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
