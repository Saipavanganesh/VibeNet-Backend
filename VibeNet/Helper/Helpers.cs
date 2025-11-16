using System.Net.Mail;
using System.Net;
using Microsoft.Data.SqlClient;
using VibeNet.Models;
using System.Data;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace VibeNet.Helper
{
    public class Helpers
    {
        public async Task<UserExistsResult> CheckIfUserExistsAsync(SqlConnection connection, string username, string email)
        {
            var result = new UserExistsResult();

            string query = @"
        SELECT Username, Email 
        FROM Users 
        WHERE Username = @Username OR Email = @Email;
    ";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@Email", email);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        if (reader["Username"].ToString() == username)
                            result.UsernameTaken = true;

                        if (reader["Email"].ToString()?.ToLower() == email.ToLower())
                            result.EmailTaken = true;
                    }
                }
            }

            return result;
        }
        public async Task<object?> InsertUserAsync(SqlConnection connection, string fullName, string username, string email)
        {
            var query = @"
        INSERT INTO Users (FullName, UserName, Email)
        OUTPUT INSERTED.UserId, INSERTED.FullName, INSERTED.UserName, INSERTED.Email, INSERTED.CreatedAt
        VALUES (@FullName, @UserName, @Email);
    ";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@FullName", fullName);
                cmd.Parameters.AddWithValue("@UserName", username);
                cmd.Parameters.AddWithValue("@Email", email);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new
                        {
                            UserId = reader["UserId"],
                            FullName = reader["FullName"],
                            UserName = reader["UserName"],
                            Email = reader["Email"],
                            CreatedAt = reader["CreatedAt"]
                        };
                    }
                }
            }

            return null;
        }

        public async Task<UserRequest> GetUserAsync(SqlConnection connection, string username)
        {
            var query = "SELECT UserId, UserName, Email FROM Users WHERE UserName = @UserName";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@UserName", username);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new UserRequest
                {
                    UserId = (Guid)reader["UserId"],
                    Username = reader["UserName"].ToString()!,
                    Email = reader["Email"].ToString()!
                };
            }
            return null;
        }
        public async Task CleanupExpiredOtpsAsync(SqlConnection connection, Guid userId)
        {
            var query = @"
        UPDATE UserOtps
        SET IsUsed = 1
        WHERE UserId = @UserId 
          AND IsUsed = 0
          AND ExpiresAt <= SYSUTCDATETIME();
    ";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.Add("@UserId", SqlDbType.UniqueIdentifier).Value = userId;
            await cmd.ExecuteNonQueryAsync();
        }
        public async Task<(string OtpCode, DateTime ExpiresAt)?> GetActiveOtpAsync(SqlConnection connection, Guid userId)
        {
            var query = @"
        SELECT TOP 1 OtpCode, ExpiresAt
        FROM UserOtps
        WHERE UserId = @UserId
          AND IsUsed = 0
          AND ExpiresAt > SYSUTCDATETIME()
        ORDER BY Id DESC;
    ";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.Add("@UserId", SqlDbType.UniqueIdentifier).Value = userId;

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return (
                    reader["OtpCode"].ToString()!,
                    (DateTime)reader["ExpiresAt"]
                );
            }
            return null;
        }
        public async Task InsertOtpAsync(SqlConnection connection, Guid userId, string otp, DateTime expiryUtc)
        {
            var query = @"
        INSERT INTO UserOtps (UserId, OtpCode, ExpiresAt, IsUsed)
        VALUES (@UserId, @OtpCode, @ExpiresAt, 0);
    ";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.Add("@UserId", SqlDbType.UniqueIdentifier).Value = userId;
            cmd.Parameters.AddWithValue("@OtpCode", otp);
            cmd.Parameters.AddWithValue("@ExpiresAt", expiryUtc);

            await cmd.ExecuteNonQueryAsync();
        }
        public async Task<bool> SendOtpEmailAsync(string recipientEmail, string otp)
        {
            try
            {
                using (var client = new SmtpClient("smtp.gmail.com", 587))
                {
                    client.EnableSsl = true;
                    client.Credentials = new NetworkCredential("your-email@gmail.com", "your-app-password");

                    var mail = new MailMessage
                    {
                        From = new MailAddress("your-email@gmail.com", "VibeNet"),
                        Subject = "Your VibeNet OTP Code",
                        Body = $"Your OTP is: {otp}\nIt expires in 5 minutes.",
                        IsBodyHtml = false
                    };

                    mail.To.Add(recipientEmail);
                    await client.SendMailAsync(mail);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ValidateOtp(SqlConnection connection, Guid userId, string otp)
        {
            var query = @"
                        UPDATE UserOtps
                        SET IsUsed = 1
                        OUTPUT INSERTED.UserId
                        WHERE UserId = @UserId
                          AND OtpCode = @OtpCode
                          AND IsUsed = 0
                          AND ExpiresAt > SYSUTCDATETIME();
                    ";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.Add("@UserId", SqlDbType.UniqueIdentifier).Value = userId;
            cmd.Parameters.AddWithValue("@OtpCode", otp.Trim());

            var result = await cmd.ExecuteScalarAsync();

            return result != null;
        }
        public async Task<UserProfile?> GetUserByIdAsync(SqlConnection connection, Guid userId)
        {
            var query = @"
        SELECT UserId, FullName, UserName, Email,
               MobileNumber, Gender, DateOfBirth,
               City, State, Country,
               Bio, ProfilePictureUrl, Interests,
               IsEmailVerified, IsMobileVerified,
               IsSubscribed, CreatedAt, UpdatedAt
        FROM Users
        WHERE UserId = @UserId AND IsDeleted = 0;
    ";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@UserId", userId);

            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new UserProfile
                {
                    UserId = (Guid)reader["UserId"],
                    FullName = reader["FullName"].ToString()!,
                    Username = reader["UserName"].ToString()!,
                    Email = reader["Email"].ToString()!,
                    MobileNumber = reader["MobileNumber"] as string,
                    Gender = reader["Gender"] as string,
                    DateOfBirth = reader["DateOfBirth"] as DateTime?,
                    City = reader["City"] as string,
                    State = reader["State"] as string,
                    Country = reader["Country"] as string,
                    Bio = reader["Bio"] as string,
                    ProfilePictureUrl = reader["ProfilePictureUrl"] as string,
                    Interests = reader["Interests"] as string,
                    IsEmailVerified = (bool)reader["IsEmailVerified"],
                    IsMobileVerified = (bool)reader["IsMobileVerified"],
                    IsSubscribed = (bool)reader["IsSubscribed"],
                    CreatedAt = (DateTime)reader["CreatedAt"],
                    UpdatedAt = (DateTime)reader["UpdatedAt"]
                };
            }

            return null;
        }
        public async Task<PublicUserProfile?> GetPublicUserAsync(SqlConnection connection, string username)
        {
            var query = @"
                        SELECT 
                            FullName, 
                            UserName, 
                            Bio, 
                            ProfilePictureUrl, 
                            Interests, 
                            CreatedAt
                        FROM Users
                        WHERE UserName = @UserName AND IsDeleted = 0;
                    ";
            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@UserName", username);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new PublicUserProfile
                {
                    FullName = reader["FullName"].ToString()!,
                    Username = reader["UserName"].ToString()!,
                    Bio = reader["Bio"] as string,
                    ProfilePictureUrl = reader["ProfilePictureUrl"] as string,
                    Interests = reader["Interests"] as string,
                    CreatedAt = (DateTime)reader["CreatedAt"]
                };
            }

            return null;
        }
        public async Task<bool> UpdateProfilePictureUrlAsync(SqlConnection connection, Guid userId, string url)
        {
            var query = @"
                UPDATE Users
                SET ProfilePictureUrl = @Url,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE UserId = @UserId AND IsDeleted = 0;
            ";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Url", url);
            cmd.Parameters.Add("@UserId", System.Data.SqlDbType.UniqueIdentifier).Value = userId;

            int rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
        public async Task<bool> SoftDeleteUserAsync(SqlConnection connection, Guid userId)
        {
            var query = @"
                        UPDATE Users
                        SET IsDeleted = 1,
                            UpdatedAt = SYSUTCDATETIME()
                        WHERE UserId = @UserId;
                    ";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@UserId", userId);

            int rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
        public async Task RevokeTokensAsync(SqlConnection connection, Guid userId)
        {
            var query = @"
                        UPDATE RefreshTokens
                        SET IsRevoked = 1
                        WHERE UserId = @UserId;
                    ";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@UserId", userId);

            await cmd.ExecuteNonQueryAsync();
        }
        public async Task<UserProfile?> GetFullProfileAsync(SqlConnection connection, Guid userId)
        {
            var query = @"
        SELECT UserId, FullName, Username, Email, MobileNumber, Gender, DateOfBirth,
               City, State, Country, Bio, ProfilePictureUrl, Interests,
               IsEmailVerified, IsMobileVerified, IsSubscribed,
               CreatedAt, UpdatedAt
        FROM Users
        WHERE UserId = @UserId AND IsDeleted = 0;
    ";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@UserId", userId);

            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new UserProfile
                {
                    UserId = (Guid)reader["UserId"],
                    FullName = reader["FullName"].ToString()!,
                    Username = reader["Username"].ToString()!,
                    Email = reader["Email"].ToString()!,
                    MobileNumber = reader["MobileNumber"] as string,
                    Gender = reader["Gender"] as string,
                    DateOfBirth = reader["DateOfBirth"] as DateTime?,
                    City = reader["City"] as string,
                    State = reader["State"] as string,
                    Country = reader["Country"] as string,
                    Bio = reader["Bio"] as string,
                    ProfilePictureUrl = reader["ProfilePictureUrl"] as string,
                    Interests = reader["Interests"] as string,
                    IsEmailVerified = (bool)reader["IsEmailVerified"],
                    IsMobileVerified = (bool)reader["IsMobileVerified"],
                    IsSubscribed = (bool)reader["IsSubscribed"],
                    CreatedAt = (DateTime)reader["CreatedAt"],
                    UpdatedAt = (DateTime)reader["UpdatedAt"]
                };
            }

            return null;
        }

        public async Task<bool> UpdateUserProfileAsync(SqlConnection connection, UserProfile updated)
        {
            var query = @"
        UPDATE Users SET
            FullName = @FullName,
            MobileNumber = @MobileNumber,
            Gender = @Gender,
            DateOfBirth = @DateOfBirth,
            City = @City,
            State = @State,
            Country = @Country,
            Bio = @Bio,
            Interests = @Interests,
            UpdatedAt = SYSUTCDATETIME()
        WHERE UserId = @UserId AND IsDeleted = 0;
    ";

            using var cmd = new SqlCommand(query, connection);

            cmd.Parameters.AddWithValue("@UserId", updated.UserId);
            cmd.Parameters.AddWithValue("@FullName", updated.FullName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@MobileNumber", updated.MobileNumber ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Gender", updated.Gender ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@DateOfBirth", updated.DateOfBirth ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@City", updated.City ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@State", updated.State ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Country", updated.Country ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Bio", updated.Bio ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Interests", updated.Interests ?? (object)DBNull.Value);

            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<List<InterestResponse>> GetAllInterestsAsync(SqlConnection connection)
        {
            var list = new List<InterestResponse>();

            var query = "SELECT InterestId, Name FROM Interests ORDER BY Name";

            using var cmd = new SqlCommand(query, connection);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new InterestResponse
                {
                    InterestId = (int)reader["InterestId"],
                    Name = reader["Name"].ToString()!
                });
            }

            return list;
        }
        public async Task<bool> ValidateInterestIdsAsync(SqlConnection connection, List<int> ids)
        {
            if (ids == null || ids.Count == 0)
                return false;

            var query = $@"
        SELECT COUNT(*) 
        FROM Interests 
        WHERE InterestId IN ({string.Join(",", ids)});
    ";

            using var cmd = new SqlCommand(query, connection);
            var count = (int)await cmd.ExecuteScalarAsync();

            return count == ids.Count;
        }
        public async Task<bool> UpdateUserInterestsAsync(SqlConnection connection, Guid userId, List<int> interestIds)
        {
            string json = System.Text.Json.JsonSerializer.Serialize(interestIds);

            var query = @"
        UPDATE Users
        SET Interests = @Interests,
            UpdatedAt = SYSUTCDATETIME()
        WHERE UserId = @UserId AND IsDeleted = 0;
    ";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@Interests", json);

            int rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

    }
}
