using Azure.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;
using Microsoft.Win32;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using System;
using static System.Net.WebRequestMethods;
using static System.Runtime.InteropServices.JavaScript.JSType;
using VibeNet.Interfaces;
using VibeNet.Models;
using Microsoft.Azure.Cosmos.Serialization.HybridRow;
using Microsoft.Data.SqlClient;
using VibeNet.Helper;

namespace VibeNet.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController(IUsers users) : ControllerBase
    {
        private readonly IUsers _users = users;

        [HttpPost("register")]
        public async Task<IActionResult> RegisterUsers([FromBody] RegisterRequest request)
        {
            var response = await _users.RegisterUsers(request);
            if (response != null)
            {
                return Ok(new VibenetResponse(true, response.Message, response.Data));
            }
            else
            {
                return BadRequest(new VibenetResponse(false, "Something went wrong", null));
            }
        }
        [HttpPost("request-otp")]
        public async Task<IActionResult> RequestOtp([FromBody] RequestOtpRequest request)
        {
            var response = await _users.RequestOtp(request);
            if (response.Success)
                return Ok(response);
            else
                return BadRequest(response);
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            var response = await _users.VerifyOtp(request);
            if (response != null)
            {
                return Ok(new VibenetResponse(true, response.Message, response.Data));
            }
            else
            {
                return BadRequest(new VibenetResponse(false, "Something went wrong", null));
            }
        }
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserById(Guid userId)
        {
            var result = await _users.GetUserById(userId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("users/{username}")]
        public async Task<IActionResult> GetPublicProfile(string username)
        {
            var response = await _users.GetPublicProfileByUsername(username);

            if (response.Success)
                return Ok(response);

            return NotFound(response);
        }

        [HttpPut("{userId}/profile-picture")]
        public async Task<IActionResult> UploadProfilePicture(Guid userId, [FromForm] IFormFile file)
        {
            if (file == null)
                return BadRequest(new VibenetResponse(false, "No file uploaded.", null));

            var result = await _users.UploadProfilePictureAsync(userId, file);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpDelete("user/{userId}")]
        public async Task<IActionResult> DeleteUser(Guid userId)
        {
            var result = await _users.SoftDeleteUser(userId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{userId}")]
        public async Task<IActionResult> UpdateProfile(Guid userId, [FromBody] UpdateProfileRequest request)
        {
            var res = await _users.UpdateProfile(userId, request);
            return res.Success ? Ok(res) : BadRequest(res);
        }

        [HttpGet("interests")]
        public async Task<IActionResult> GetInterests()
        {
            var response = await _users.GetInterestsAsync();
            return Ok(response);
        }


        [HttpPut("{userId}/interests")]
        public async Task<IActionResult> UpdateInterests(Guid userId, [FromBody] UpdateInterestsRequest req)
        {
            if (req.InterestIds == null || req.InterestIds.Count == 0)
                return BadRequest(new VibenetResponse(false, "No interests provided.", null));

            var response = await _users.UpdateInterestsAsync(userId, req.InterestIds);
            return Ok(response);
        }
        [HttpGet("debug-config")]
        public IActionResult DebugConfig([FromServices] IConfiguration config)
        {
            return Ok(new
            {
                sql = config["ConnectionStrings:SqlDatabase"] != null,
                cosmos = config["ConnectionStrings:CosmosDb"] != null,
                blob = config["ConnectionStrings:BlobStorage"] != null,
                storageBaseUrl = config["AzureStorage:BaseUrl"] != null,
                imagesContainer = config["AzureStorage:ImagesContainer"] != null,
                jwtSecret = config["Jwt:SecretKey"] != null
            });
        }

    }
}

//POST / verify - email -> DELAYED UNTIL FRONTEND IS READY
