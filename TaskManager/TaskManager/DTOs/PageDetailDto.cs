using TaskManager.Models;

namespace TaskManager.DTOs
{
    public class PageDetailDto
    {
        public int Id { get; set; }
        public string taskName { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? DueDate { get; set; } 
        public int? ParentId { get; set; }
        public string? Icon { get; set; }
        public string? CoverImage { get; set; }
        public bool IsOwner { get; set; }
        public PermissionLevel CurrentUserPermission { get; set; }
        public CategoryDto Category { get; set; }
        public List<PageSummaryDto> Children { get; set; }
        public List<BlockDto> ContentBlocks { get; set; }
        public List<AttachmentDto> Attachments { get; set; }
    }
}
