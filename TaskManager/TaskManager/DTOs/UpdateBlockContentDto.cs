using System.ComponentModel.DataAnnotations;

namespace TaskManager.DTOs
{
    public class UpdateBlockContentDto
    {
        [Required]
        public string Content { get; set; }
    }
}
