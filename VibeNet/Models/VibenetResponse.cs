using static System.Runtime.InteropServices.JavaScript.JSType;

namespace VibeNet.Models
{
    public class VibenetResponse(bool success, string message, object? data)
    {
        public bool Success { get; set; } = success;
        public string Message { get; set; } = message;
        public object? Data { get; set; } = data;
    }
}
