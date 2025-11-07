using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManager.Models
{
    public class PagePermission
    {
        public int Id { get; set; }
        [Required]
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }
        [Required]
        public int PageId { get; set; }
        [ForeignKey("PageId")]
        public TodoItem Page { get; set; }
        [Required]
        public PermissionLevel Level { get; set; }
    }
    public enum PermissionLevel
    {
        ReadOnly,
        FullAccess
    }
}
