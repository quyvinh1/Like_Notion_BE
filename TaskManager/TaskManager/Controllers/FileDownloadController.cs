using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManager.DBContext;

namespace TaskManager.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FileDownloadController : ControllerBase
    {
        private readonly TaskManagerDbContext _dbContext;
        private string GetUserId()
        {
            var userId = User.FindFirst("UserId").Value;
            return userId;
        }
        public FileDownloadController(
                       TaskManagerDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> DownloadAttachment(int id)
        {
            var userId = GetUserId();
            var attachment = await _dbContext.Attachments
                .Include(a => a.TodoItem)
                .FirstOrDefaultAsync(a => a.Id == id && a.TodoItem.OwnerId == userId);
            if (attachment == null)
            {
                return NotFound(new { Message = "Attachment not found or you do not have permission." });
            }
            var memory = new MemoryStream();
            using (var stream = new FileStream(attachment.FilePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory, attachment.ContentType, attachment.OriginalFileName);   
        }
    }
}
