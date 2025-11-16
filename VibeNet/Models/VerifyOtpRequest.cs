namespace VibeNet.Models
{
    public class VerifyOtpRequest
    {
        public string UserName { get; set; } = string.Empty;
        public string OtpCode { get; set; } = string.Empty;
    }
}
