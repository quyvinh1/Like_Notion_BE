using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManager.Models
{
    public class ContentBlock
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Content { get; set; }
        public int Order { get; set; }
        public int PageId { get; set; }
        [ForeignKey("PageId")]
        [JsonIgnore]
        public virtual TodoItem Page { get; set; }
    }
}
