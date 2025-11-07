using System.ComponentModel.DataAnnotations;

namespace TaskManager.DTOs
{
    public class ReorderBlockDto
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public int NewOrder { get; set; }
    }
}
