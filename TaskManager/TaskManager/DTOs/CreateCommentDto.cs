using System.ComponentModel.DataAnnotations;

namespace TaskManager.DTOs
{
    public class CreateCommentDto
    {
        [Required]
        public string Content { get; set; }
    }
}
