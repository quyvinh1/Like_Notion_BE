using System.ComponentModel.DataAnnotations;

namespace TaskManager.DTOs
{
    public class CreateBlockDto
    {
        [Required]
        public string Type { get; set; }
        public string Content { get; set; }
    }
}
