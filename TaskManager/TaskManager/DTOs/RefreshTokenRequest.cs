using System.ComponentModel.DataAnnotations;

namespace TaskManager.DTOs
{
    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; }
    }
}
