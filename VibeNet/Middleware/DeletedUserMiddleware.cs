using Microsoft.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;

namespace VibeNet.Middleware
{
    public class DeletedUserMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _connectionString;
        public DeletedUserMiddleware(RequestDelegate next, IConfiguration config)
        {
            _next = next;
            _connectionString = config["ConnectionStrings:SqlDatabase"];
        }
        public async Task InvokeAsync(HttpContext context)
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();
                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jwt = handler.ReadJwtToken(token);

                    var userIdString = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
                    if (Guid.TryParse(userIdString, out Guid userId))
                    {
                        using var connection = new SqlConnection(_connectionString);
                        await connection.OpenAsync();

                        var query = "SELECT IsDeleted FROM Users WHERE UserId = @UserId";

                        using var cmd = new SqlCommand(query, connection);
                        cmd.Parameters.AddWithValue("@UserId", userId);

                        var result = await cmd.ExecuteScalarAsync();

                        if (result != null && (bool)result == true)
                        {
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            await context.Response.WriteAsJsonAsync(new
                            {
                                success = false,
                                message = "Account is deleted. Access denied."
                            });
                            return;
                        }
                    }
                }
                catch
                {

                }
            }
            await _next(context);
        }
    }
}
