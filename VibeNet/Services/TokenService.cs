using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using VibeNet.Models;

namespace VibeNet.Services
{
    public class TokenService
    {
        private readonly string _jwtSecret;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _accessExpiryInMinutes;
        private readonly int _refreshExpiryInDays;

        public TokenService(IConfiguration config)
        {
            _jwtSecret = config["JwtSettings:Secret"];
            _issuer = config["JwtSettings:Issuer"];
            _audience = config["JwtSettings:Audience"];
            _accessExpiryInMinutes = int.Parse(config["JwtSettings:AccessTokenExpiryInMinutes"]);
            _refreshExpiryInDays = int.Parse(config["JwtSettings:RefreshTokenExpiryInDays"]);
        }
        public string CreateAccessToken(UserRequest user)
        {
            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_jwtSecret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                 new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                 new Claim("username", user.Username),
                 new Claim("email", user.Email),
                 new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                 new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };
            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_accessExpiryInMinutes),
                signingCredentials: creds
                );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        public string GenerateRefreshToken()
        {
            var bytes = new byte[64];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes);
        }
        public async Task SaveRefreshTokenAsync(SqlConnection connection, Guid userId, string refreshToken)
        {
            var query = @"
            INSERT INTO RefreshTokens (UserId, Token, ExpiresAt)
            VALUES (@UserId, @Token, DATEADD(day, @Days, SYSUTCDATETIME()));";
            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@Token", refreshToken);
            cmd.Parameters.AddWithValue("@Days", _refreshExpiryInDays);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
