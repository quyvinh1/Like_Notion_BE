using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskManager.DBContext;
using TaskManager.DTOs;
using TaskManager.Hubs;
using TaskManager.Models;

namespace TaskManager.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CommentController : ControllerBase
    {
        private readonly TaskManagerDbContext _context;
        private readonly IHubContext<TaskHubs> _hubContext;
        private readonly IMapper _mapper;

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
        public CommentController(TaskManagerDbContext context, IHubContext<TaskHubs> hubContext, IMapper mapper)
        {
            _context = context;
            _hubContext = hubContext;
            _mapper = mapper;
        }
        [HttpGet]
        public async Task<ActionResult<List<CommentDto>>> GetComments(int pageId)
        {
            var userId = GetUserId();
            var permission = await GetUserPermissionForPage(pageId, userId);
            if(permission == null)
            {
                return Forbid();
            }
            var comments = await _context.Comments
                .Where(c => c.PageId == pageId)
                .Include(c => c.User)
                .OrderBy(c => c.CreatedAt)
                .ProjectTo<CommentDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
            return Ok(comments);
        }

        [HttpPost]
        public async Task<ActionResult<CommentDto>> PostComment(int pageId, [FromBody] CreateCommentDto dto)
        {
            var userId = GetUserId();
            var permission = await GetUserPermissionForPage(pageId, userId);
            if(permission == null) return Forbid();
            var comment = new Comment
            {
                Content = dto.Content,
                PageId = pageId,
                UserId = userId,
            };
            await _context.Comments.AddAsync(comment);
            await _context.SaveChangesAsync();
            var newCommentWithUser = await _context.Comments
                .Include(c => c.User)
                .ProjectTo<CommentDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(c => c.Id == comment.Id);

            await _hubContext.Clients.Group($"Page-{pageId}")
                .SendAsync("NewCommentReceived", newCommentWithUser);
            return CreatedAtAction(nameof(GetComments), new { pageId = pageId }, newCommentWithUser);
        }
    }
}
