using Microsoft.Data.SqlClient;
using System.Net.Mail;
using System.Net;
using VibeNet.Interfaces;
using VibeNet.Models;
using VibeNet.Helper;
using System.Security.Cryptography;
using System.Data;
using Azure.Core;

namespace VibeNet.Services
{
    public class UsersService(IConfiguration configuration, Helpers helpers, TokenService tokenService, CosmosService cosmosService, BlobService blobService) : IUsers
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly Helpers _helpers = helpers;
        private readonly TokenService _tokenService = tokenService;
        private readonly CosmosService _cosmosService = cosmosService;
        private readonly BlobService _blobService = blobService;

        public async Task<VibenetResponse> RegisterUsers(RegisterRequest registerRequest)
        {
            try
            {
                string connectionString = _configuration.GetSection("ConnectionStrings:SqlDatabase").Value;
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string fullName = registerRequest.FullName.Trim();
                    string username = registerRequest.UserName.Trim();
                    string email = registerRequest.Email.Trim().ToLower();

                    var exists = await _helpers.CheckIfUserExistsAsync(connection, username, email);

                    if (exists.UsernameTaken)
                        return new VibenetResponse(false, "Username already exists.", null);

                    if (exists.EmailTaken)
                        return new VibenetResponse(false, "Email already registered.", null);

                    var createdUser = await _helpers.InsertUserAsync(connection, fullName, username, email);

                    if (createdUser != null)
                        return new VibenetResponse(true, "User registered successfully.", createdUser);

                    return new VibenetResponse(false, "Failed to register user.", null);
                }
            }
            catch (Exception ex)
            {
                return new VibenetResponse(false, ex.Message, null);
            }
        }
        public async Task<VibenetResponse> RequestOtp(RequestOtpRequest requestOtpRequest)
        {
            try
            {
                string connectionString = _configuration.GetSection("ConnectionStrings:SqlDatabase").Value;

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var user = await _helpers.GetUserAsync(connection, requestOtpRequest.UserName.Trim());
                    if (user == null)
                        return new VibenetResponse(false, "User not found.", null);

                    await _helpers.CleanupExpiredOtpsAsync(connection, user.UserId);

                    var existing = await _helpers.GetActiveOtpAsync(connection, user.UserId);
                    if (existing != null)
                    {
                        return new VibenetResponse(
                            true,
                            $"OTP already sent",
                            null
                        );
                    }

                    int expiryMinutes = 10;
                    string otp = RandomNumberGenerator.GetInt32(0, 1000000).ToString("D6");
                    DateTime expiryUtc = DateTime.UtcNow.AddMinutes(expiryMinutes);

                    await _helpers.InsertOtpAsync(connection, user.UserId, otp, expiryUtc);

                    var mailSent = await _helpers.SendOtpEmailAsync(user.Email, otp);
                    if (!mailSent)
                        return new VibenetResponse(false, "Could not send OTP email.", null);

                    return new VibenetResponse(true, "OTP sent successfully.", null);
                }
            }
            catch (Exception ex)
            {
                return new VibenetResponse(false, ex.Message, null);
            }
        }
        public async Task<VibenetResponse> VerifyOtp(VerifyOtpRequest request)
        {
            try
            {
                string connectionString = _configuration.GetSection("ConnectionStrings:SqlDatabase").Value;
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    var user = await _helpers.GetUserAsync(connection, request.UserName.Trim());

                    if (user == null)
                        return new VibenetResponse(false, "User not found.", null);

                    await _helpers.CleanupExpiredOtpsAsync(connection, user.UserId);

                    bool isValid = await _helpers.ValidateOtp(connection, user.UserId, request.OtpCode);
                    if (!isValid)
                        return new VibenetResponse(false, "Invalid or expired OTP.", null);

                    string accessToken = _tokenService.CreateAccessToken(user);
                    string refreshToken = _tokenService.GenerateRefreshToken();
                    await _tokenService.SaveRefreshTokenAsync(connection, user.UserId, refreshToken);

                    var responseData = new
                    {
                        user.UserId,
                        user.Username,
                        user.Email,
                        AccessToken = accessToken,
                        RefreshToken = refreshToken
                    };

                    return new VibenetResponse(true, "OTP verified successfully.", responseData);
                }
            }
            catch (Exception ex)
            {
                return new VibenetResponse(false, ex.Message, null);
            }
        }
        public async Task<VibenetResponse> GetUserById(Guid userId)
        {
            try
            {
                string connectionString = _configuration.GetSection("ConnectionStrings:SqlDatabase").Value;

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var user = await _helpers.GetUserByIdAsync(connection, userId);

                if (user == null)
                    return new VibenetResponse(false, "User not found.", null);

                return new VibenetResponse(true, "User profile retrieved successfully.", user);
            }
            catch (Exception ex)
            {
                return new VibenetResponse(false, ex.Message, null);
            }
        }
        public async Task<VibenetResponse> GetPublicProfileByUsername(string username)
        {
            try
            {
                string connectionString = _configuration.GetSection("ConnectionStrings:SqlDatabase").Value;
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                PublicUserProfile userProfile = await _helpers.GetPublicUserAsync(connection, username.Trim());
                if (userProfile == null)
                    return new VibenetResponse(false, "User not found.", null);

                var connectionCount = await _cosmosService.GetConnectionCountAsync(username);
                userProfile.ConnectionCount = connectionCount;
                return new VibenetResponse(true, "Profile fetched.", userProfile);
            }
            catch (Exception ex)
            {
                return new VibenetResponse(false, ex.Message, null);
            }
        }
        public async Task<VibenetResponse> UploadProfilePictureAsync(Guid userId, IFormFile file)
        {
            try
            {
                string connectionString = _configuration["ConnectionStrings:SqlDatabase"];

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var user = await _helpers.GetUserByIdAsync(connection, userId);
                if (user == null)
                    return new VibenetResponse(false, "User not found.", null);

                var imageUrl = await _blobService.UploadProfilePictureAsync(userId, file);

                bool updated = await _helpers.UpdateProfilePictureUrlAsync(connection, userId, imageUrl);
                if (!updated)
                    return new VibenetResponse(false, "Failed to update database.", null);

                var responseObj = new { Url = imageUrl };
                return new VibenetResponse(true, "Profile picture updated.", responseObj);
            }
            catch (Exception ex)
            {
                return new VibenetResponse(false, ex.Message, null);
            }
        }
        public async Task<VibenetResponse> SoftDeleteUser(Guid userId)
        {
            try
            {
                string connectionString = _configuration["ConnectionStrings:SqlDatabase"];

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var user = await _helpers.GetUserByIdAsync(connection, userId);
                if (user == null)
                    return new VibenetResponse(false, "User not found.", null);

                bool deleted = await _helpers.SoftDeleteUserAsync(connection, userId);
                if (!deleted)
                    return new VibenetResponse(false, "Failed to delete user.", null);

                await _helpers.RevokeTokensAsync(connection, userId);

                return new VibenetResponse(true, "Account deleted successfully.", null);
            }
            catch (Exception ex)
            {
                return new VibenetResponse(false, ex.Message, null);
            }
        }
        public async Task<VibenetResponse> UpdateProfile(Guid userId, UpdateProfileRequest req)
        {
            try
            {
                string connectionString = _configuration["ConnectionStrings:SqlDatabase"];

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // 1. Fetch existing profile
                var existing = await _helpers.GetFullProfileAsync(connection, userId);
                if (existing == null)
                    return new VibenetResponse(false, "User not found.", null);

                // 2. Merge new values (partial update)
                existing.FullName = req.FullName ?? existing.FullName;
                existing.MobileNumber = req.MobileNumber ?? existing.MobileNumber;
                existing.Gender = req.Gender ?? existing.Gender;
                existing.DateOfBirth = req.DateOfBirth ?? existing.DateOfBirth;
                existing.City = req.City ?? existing.City;
                existing.State = req.State ?? existing.State;
                existing.Country = req.Country ?? existing.Country;
                existing.Bio = req.Bio ?? existing.Bio;

                // 3. Update DB
                bool ok = await _helpers.UpdateUserProfileAsync(connection, existing);

                if (!ok)
                    return new VibenetResponse(false, "Failed to update profile.", null);

                var updated = await _helpers.GetFullProfileAsync(connection, userId);
                return new VibenetResponse(true, "Profile updated successfully.", updated);
            }
            catch (Exception ex)
            {
                return new VibenetResponse(false, ex.Message, null);
            }
        }
        public async Task<VibenetResponse> GetInterestsAsync()
        {
            try
            {
                string connectionString = _configuration["ConnectionStrings:SqlDatabase"];

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var list = await _helpers.GetAllInterestsAsync(connection);

                return new VibenetResponse(true, "Interests fetched.", list);
            }
            catch (Exception ex)
            {
                return new VibenetResponse(false, ex.Message, null);
            }
        }

        public async Task<VibenetResponse> UpdateInterestsAsync(Guid userId, List<int> interestIds)
        {
            try
            {
                string connectionString = _configuration["ConnectionStrings:SqlDatabase"];

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Validate interest IDs
                var valid = await _helpers.ValidateInterestIdsAsync(connection, interestIds);
                if (!valid)
                    return new VibenetResponse(false, "One or more invalid interest IDs.", null);

                // Update user
                var ok = await _helpers.UpdateUserInterestsAsync(connection, userId, interestIds);

                if (!ok)
                    return new VibenetResponse(false, "Failed to update interests.", null);

                return new VibenetResponse(true, "Interests updated successfully.", null);
            }
            catch (Exception ex)
            {
                return new VibenetResponse(false, ex.Message, null);
            }
        }


    }
}
