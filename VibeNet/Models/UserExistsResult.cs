namespace VibeNet.Models
{
    public class UserExistsResult
    {
        public bool UsernameTaken { get; set; }
        public bool EmailTaken { get; set; }
    }
}
