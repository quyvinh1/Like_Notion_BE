using System.ComponentModel.DataAnnotations;

namespace TaskManager.Models
{
    public class Workspace
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string? Icon { get; set; }
        public string OwnerId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public virtual ICollection<WorkspaceMember> Members { get; set; }
        public virtual ICollection<TodoItem> Pages { get; set; }
    }
}
