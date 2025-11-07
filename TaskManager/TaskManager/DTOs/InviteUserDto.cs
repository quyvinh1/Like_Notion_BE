using System.ComponentModel.DataAnnotations;
using TaskManager.Models;

namespace TaskManager.DTOs
{
    public class InviteUserDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        public PermissionLevel Level { get; set; }

    }
}
