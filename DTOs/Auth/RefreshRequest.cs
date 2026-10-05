using Azure.Core;

namespace SmartAcademyApi.DTOs.Auth
{
    public class RefreshRequest
    {
        public string Email { get; set; }
        public string RefreshToken { get; set;}
    }
}
