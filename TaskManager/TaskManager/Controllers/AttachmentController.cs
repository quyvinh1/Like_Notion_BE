using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SendGrid.Helpers.Mail;
using System.Security.Claims;
using TaskManager.DBContext;
using TaskManager.Models;
using TaskManager.Services;

namespace TaskManager.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AttachmentController : ControllerBase
    {
        private readonly TaskManagerDbContext _dbContext;
        private readonly IPhotoService _photoService;
        public AttachmentController(
            TaskManagerDbContext dbContext,
            IPhotoService photoService)
        {
            _dbContext = dbContext;
            _photoService = photoService;
        }
        private string GetUserId()
        {
            var userId = User?.FindFirstValue("UserId");
            return userId;
        }
        [HttpPost]
        public async Task<IActionResult> UploadAttachment(int taskId, IFormFile file)
        {
            var userId = GetUserId();
            var permission = await GetUserPermissionForPage(taskId, userId);
            if(permission != PermissionLevel.FullAccess)
            {
                return Forbid();
            }
            var task = await _dbContext.TodoItems.
                FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null)
            {
                return NotFound(new { Message = "Task not found or you do not have permission." });
            }
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { Message = "No file uploaded." });
            }
            var uploadResult = await _photoService.AddPhotoAsync(file);
            if(uploadResult.Error != null)
            {
                return BadRequest(new { Message = "Photo upload failed.", Details = uploadResult.Error.Message });
            }
            var attachment = new Models.Attachment
            {
               OriginalFileName = file.FileName,
               StoredFileName = file.FileName,
               FilePath = uploadResult.SecureUrl.AbsoluteUri,
               PublicId = uploadResult.PublicId,
               ContentType = file.ContentType,
               FileSize = file.Length,
               TodoItemId = taskId
            };
            await _dbContext.Attachments.AddAsync(attachment);
            await _dbContext.SaveChangesAsync();
            return Ok(new { Message = "File uploaded successfully.", AttachmentId = attachment.Id });
        }
        [HttpGet]
        public async Task<IActionResult> GetAttachmentsForTask(int taskId)
        {
            var userId = GetUserId();
            var permission = await GetUserPermissionForPage(taskId, userId);
            if(permission == null)
            {
                return Forbid();
            }
            var taskExists = await _dbContext.TodoItems.AnyAsync(t => t.Id == taskId);
            if(!taskExists)
            {
                return NotFound(new { Message = "Task not found or you do not have permission." });
            }
            var attachments = await _dbContext.Attachments
                .Where(a => a.TodoItemId == taskId)
                .Select(a => new { 
                    a.Id, 
                    a.OriginalFileName, 
                    a.FileSize,
                    a.ContentType, 
                    a.FilePath
                })
                .ToListAsync();
            return Ok(attachments);
        }
        private async Task<PermissionLevel?> GetUserPermissionForPage(int pageId, string userId)
        {
            var page = await _dbContext.TodoItems.FindAsync(pageId);
            if (page == null)
            {
                return null;
            }
            if (page.OwnerId == userId)
            {
                return PermissionLevel.FullAccess;
            }
            var permission = await _dbContext.PagePermissions
                .FirstOrDefaultAsync(p => p.PageId == pageId && p.UserId == userId);

            if (permission != null)
            {
                return permission.Level;
            }
            return null;
        }
    }
}
