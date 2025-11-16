using VibeNet.Models;

namespace VibeNet.Interfaces
{
    public interface IUsers
    {
        Task<VibenetResponse> RegisterUsers(RegisterRequest registerRequest);
        Task<VibenetResponse> RequestOtp(RequestOtpRequest requestOtpRequest);
        Task<VibenetResponse> VerifyOtp(VerifyOtpRequest request);
        Task<VibenetResponse> GetUserById(Guid userId);
        Task<VibenetResponse> GetPublicProfileByUsername(string username);
        Task<VibenetResponse> UploadProfilePictureAsync(Guid userId, IFormFile file);
        Task<VibenetResponse> SoftDeleteUser(Guid userId);
        Task<VibenetResponse> UpdateProfile(Guid userId, UpdateProfileRequest req);
        Task<VibenetResponse> GetInterestsAsync();
        Task<VibenetResponse> UpdateInterestsAsync(Guid userId, List<int> interestIds);
    }
}
