using System.ComponentModel.DataAnnotations;
using TaskManager.Attributes;

namespace TaskManager.DTOs
{
    public class CreateTaskDto
    {
        [Required]
        [MaxLength(255)]
        public string TaskName { get; set; }
        [FutureDate]
        public DateTime? DueDate { get; set; }
        public int? CategoryId { get; set; }
        public int? ParentId { get; set; }
    }
}
