using System.ComponentModel.DataAnnotations;
using TaskManager.Attributes;

namespace TaskManager.DTOs
{
    public class UpdateTaskDto
    {
        [Required]
        [MaxLength(255)]
        public string TaskName { get; set; }
        public bool IsCompleted { get; set; }
        [FutureDate]
        public DateTime? DueDate { get; set; }
    }
}
