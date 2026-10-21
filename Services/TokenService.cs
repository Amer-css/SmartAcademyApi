using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SmartAcademyApi.Data;
using SmartAcademyApi.Models;
using SmartAcademyApi.Permissions;

namespace SmartAcademyApi.Services
{
    public class TokenService 
    {
        private readonly IConfiguration _config;
        private readonly SymmetricSecurityKey _key;
        private readonly AppDbContext _context;

        public TokenService(IConfiguration config , AppDbContext context)
        {
            _config = config;
            _context = context;
            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
        }


        public async Task<string> CreateToken(User user)
        {

            var claims = new List<Claim>
            {
                          
               new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
               new Claim(ClaimTypes.Email, user.Email),
               new Claim(ClaimTypes.Role, user.Role?.Name ?? string.Empty),
               new Claim("FullName", user.FullName)

            };

            // fetching permissions from the database
            var permissions = await _context.RolePermissions
                .Where(rp => rp.RoleId == user.RoleId)
                .Select(rp => rp.Permission.Name)
                .ToListAsync();

            foreach (var permission in permissions)
            {
                claims.Add(new Claim("permission", permission));
            }


            var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256Signature);

            // 3.Token description (expiration, issuer, and permissions)
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(30), //The token is valid for 30 minutes.
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"],
                SigningCredentials = creds
            };

            // 4.Generating the token in its final, encrypted format
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);

        }
    }
}
