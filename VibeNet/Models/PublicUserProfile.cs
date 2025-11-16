namespace VibeNet.Models
{
    public class PublicUserProfile
    {
        public string FullName { get; set; }
        public string Username { get; set; }
        public string? Bio { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string? Interests { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ConnectionCount { get; set; }
    }
}
