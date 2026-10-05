using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartAcademyApi.Data;
using SmartAcademyApi.DTOs;
using SmartAcademyApi.DTOs.Auth;
using SmartAcademyApi.Models;
using SmartAcademyApi.Services;
using Microsoft.AspNetCore.RateLimiting;

namespace SmartAcademyApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController :Controller
    {
        private readonly AppDbContext _context;
        private readonly TokenService _tokenService;

        public AuthController(AppDbContext context, TokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }
        // generate a secure, random refresh token
        private static string GenerateRefreshToken()
        {
            var bytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }


        [HttpPost("register")]
           public async Task<IActionResult> Register(RegisterDto model)
           {
               if (await _context.Users.AnyAsync(u => u.Email == model.Email))
               {
                   return BadRequest("The email address is already in use.");
               }
          
               string passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
          
               var user = new User
               {
                   FullName = model.FullName,
                   Email = model.Email,
                   PasswordHash = passwordHash,
                   Role = "Student"
               };
          
               _context.Users.Add(user);
               await _context.SaveChangesAsync();
          
               return Ok(new { message = "The account has been created successfully!" });
           }
          
        
          [HttpPost("login")]
         [EnableRateLimiting("AuthLimiter")]
          public async Task<IActionResult> Login([FromBody]LoginDto model)
          {
              var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null)
            {
                return Unauthorized("Invalid credentials!");
            }

            bool isVBalidPassword = BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash);

            if (!isVBalidPassword)
            {
                return Unauthorized("Invalid credentials!");
            }


            var accessToken = _tokenService.CreateToken(user);

            // Create refresh token (random)
            var refreshToken = GenerateRefreshToken();

            // Store refresh token securely (hash + expiry + not revoked)
            user.RefreshTokenHash = BCrypt.Net.BCrypt.HashPassword(refreshToken);
            user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(3);
            user.RefreshTokenRevokedAt = null;

            await _context.SaveChangesAsync();

            return Ok(new TokenResponse
              {
                  AccessToken = accessToken,
                  RefreshToken = refreshToken,                 
              });
          }

        [HttpPost("refresh-token")]
        [EnableRateLimiting("AuthLimiter")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshRequest request)
        {
            // 1. Find the user associated with the refresh token 

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                return Unauthorized(new { message = "Invalid refresh request" });
            }

            // 2. Check if the refresh token has already been revoked?
            if (user.RefreshTokenRevokedAt != null)
            {
                return Unauthorized(new { message = "Refresh token is revoked" });
            }

            // 3. Check if the refresh token has expired?
            if (user.RefreshTokenExpiresAt == null || user.RefreshTokenExpiresAt <= DateTime.UtcNow)
            {
                return Unauthorized(new { message = "Refresh token expired" });
            }
            
            // 4. Verify that the raw refresh token matches the hash stored in the database.
            bool isRefreshTokenValid = BCrypt.Net.BCrypt.Verify(request.RefreshToken, user.RefreshTokenHash);
            if (!isRefreshTokenValid)
            {
                return Unauthorized(new { message = "Invalid refresh token" });
            }

            // 5. Issue a new Access Token (using our existing)
            var newAccessToken = _tokenService.CreateToken(user);

            // 6. Rotation Policy: Generating a completely new refresh token and invalidating/updating the old one.
            var newRefreshToken = GenerateRefreshToken();
            user.RefreshTokenHash = BCrypt.Net.BCrypt.HashPassword(newRefreshToken);
            user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(3);
            user.RefreshTokenRevokedAt = null;

            await _context.SaveChangesAsync();

            // 7. Return the new tokens to the client
            return Ok(new TokenResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            });
        }


        [HttpPost("Logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
                return Ok(); //Do not reveal if user exists

            bool refreshValid = BCrypt.Net.BCrypt.Verify(request.RefreshToken, user.RefreshTokenHash);
            if (!refreshValid)
                return Ok();

            // Invalidate the session by recording the current invalidation time
            user.RefreshTokenRevokedAt = DateTime.UtcNow;
          
            // Save the change to the database
            await _context.SaveChangesAsync();
           
            return Ok("Logged out successfully");
        }
    }
}
