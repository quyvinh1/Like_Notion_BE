using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace TaskManager.Models
{
    public class Attachment
    {
        public int Id { get; set; }
        public string OriginalFileName {  get; set;}
        public string StoredFileName { get; set; }
        public string ContentType { get; set; }
        public long FileSize { get; set; }
        public string FilePath { get; set; }
        public string? PublicId { get; set; }
        public int TodoItemId { get; set; }
        [ForeignKey("TodoItemId")]
        [JsonIgnore]
        public TodoItem TodoItem { get; set; }
    }
}
