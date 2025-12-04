using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace TaskManager.Models
{
    public class WorkspaceMember
    {
        public int Id { get; set; }
        public int WorkspaceId { get; set; }
        [ForeignKey("WorkspaceId")]
        [JsonIgnore]
        public virtual Workspace Workspace { get; set; }
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User User { get; set; }
        public string Role { get; set; } = "Member";
        public DateTime JoinAt { get; set; } = DateTime.UtcNow;
    }
}
