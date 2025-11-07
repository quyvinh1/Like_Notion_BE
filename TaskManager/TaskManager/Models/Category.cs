using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace TaskManager.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string OwnerId { get; set; } = null!;
        [ForeignKey("OwnerId")]
        public User Owner { get; set; } = null!;
        [JsonIgnore]
        public ICollection<TodoItem> TodoItems { get; set; }
    }
}
