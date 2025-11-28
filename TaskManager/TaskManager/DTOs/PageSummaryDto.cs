namespace TaskManager.DTOs
{
    public class PageSummaryDto
    {
        public int Id { get; set; }
        public string TaskName { get; set; }
        public bool HasChildren { get; set; }
        public int? ParentId { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public string? Icon { get; set; }
        public string Status { get; set; }
    }
}
