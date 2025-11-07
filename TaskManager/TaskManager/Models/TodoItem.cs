using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using TaskManager.Attributes;

namespace TaskManager.Models
{
    public class TodoItem
    {
        public int Id { get; set; }
        public string TaskName { get; set; }
        public bool IsCompleted { get; set; } = false;
        public DateTime? DueDate { get; set; }
        public string OwnerId { get; set; }
        public int? ParentId { get; set; }
        public string? Icon { get; set; }
        public string? CoverImage { get; set; }
        [ForeignKey("ParentId")]
        [JsonIgnore]
        public virtual TodoItem Parent { get; set; }
        public virtual ICollection<TodoItem> Children { get; set; }
        public int? CategoryId { get; set; }
        [ForeignKey("OwnerId")]
        public User Owner { get; set; }
        [ForeignKey("CategoryId")]
        public Category Category { get; set; }
        public ICollection<Attachment> Attachments { get; set; }
        public virtual ICollection<ContentBlock> ContentBlocks { get; set; }
        public virtual ICollection<PagePermission> SharedWith { get; set; }
        public virtual ICollection<Comment> Comments { get; set; }
    }
}
