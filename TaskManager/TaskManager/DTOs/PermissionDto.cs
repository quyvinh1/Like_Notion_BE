using TaskManager.Models;

namespace TaskManager.DTOs
{
    public class PermissionDto
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public PermissionLevel Level { get; set; }
    }
}
