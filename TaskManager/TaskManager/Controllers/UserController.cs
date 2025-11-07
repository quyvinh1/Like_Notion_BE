using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskManager.DBContext;
using TaskManager.Models;

namespace TaskManager.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly TaskManagerDbContext _context;
        public UserController(TaskManagerDbContext context)
        {
            _context = context;
        }
        private string GetUserId()
        {
            var userId = User?.FindFirstValue("UserId");
            return userId;
        }
        private async Task<PermissionLevel?> GetUserPermissionForPage(int pageId, string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return null;
            }
            var page = await _context.TodoItems.FindAsync(pageId);
            if (page == null)
            {
                return null;
            }
            if (page.OwnerId == userId)
            {
                return PermissionLevel.FullAccess;
            }
            var directPermission = await _context.PagePermissions
                .FirstOrDefaultAsync(p => p.PageId == pageId && p.UserId == userId);
            if (directPermission != null)
            {
                return directPermission.Level;
            }
            if (page.ParentId.HasValue)
            {
                return await GetUserPermissionForPage(page.ParentId.Value, userId);
            }
            return null;
        }
        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers(int pageId, [FromQuery] string query)
        {
            var userId = GetUserId();
            var permission = await GetUserPermissionForPage(pageId, userId);
            if(permission == null)
            {
                return Forbid();
            }
            var pageOwnerId = (await _context.TodoItems.FindAsync(pageId)).OwnerId;
            var sharedUserIds = await _context.PagePermissions
                .Where(p => p.PageId == pageId)
                .Select(p => p.UserId)
                .ToListAsync();

            var allowedUserIds = sharedUserIds.Union(new[] { pageOwnerId });

            var searchQuery = query ?? "";

            var users = await _context.Users
                .Where(u => allowedUserIds.Contains(u.Id) && 
                        u.Email.Contains(searchQuery) && 
                        u.Id != userId)
                .Select(u => new { u.Id, u.Email })
                .ToListAsync();
            return Ok(users);
        }
    }
}
