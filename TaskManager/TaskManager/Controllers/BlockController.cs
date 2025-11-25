using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using TaskManager.DBContext;
using TaskManager.DTOs;
using TaskManager.Hubs;
using TaskManager.Models;

namespace TaskManager.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BlockController : ControllerBase
    {
        private readonly TaskManagerDbContext _context;
        private readonly IHubContext<TaskHubs> _hubContext;
        private readonly UserManager<User> _userManager;
        private readonly IDistributedCache _cache;
        private string GetUserId()
        {
            var userId = User?.FindFirstValue("UserId");
            return userId;
        }
        public BlockController(TaskManagerDbContext context, IHubContext<TaskHubs> hubContext, UserManager<User> user, IDistributedCache cache)
        {
            _context = context;
            _hubContext = hubContext;
            _userManager = user;
            _cache = cache;
        }
        private async Task<PermissionLevel?> GetUserPermissionForPage(int pageId, string userId)
        {
            var page = await _context.TodoItems.FindAsync(pageId);
            if (page == null)
            {
                return null;
            }
            if (page.OwnerId == userId)
            {
                return PermissionLevel.FullAccess;
            }
            var permission = await _context.PagePermissions
                .FirstOrDefaultAsync(p => p.PageId == pageId && p.UserId == userId);

            if (permission != null)
            {
                return permission.Level;
            }
            return null;
        }
        private async Task<bool> CanEditPage(int pageId)
        {
            var userId = GetUserId();
            var page = await _context.TodoItems.FindAsync(pageId);
            if (page == null) return false;
            if (page.OwnerId == userId)
            {
                return true;
            }
            var permission = await GetUserPermissionForPage(pageId, userId);
            return permission == PermissionLevel.CanEdit || permission == PermissionLevel.FullAccess;
             
        }
        private async Task<bool> CanViewPage(int pageId)
        {
            var userId = GetUserId();
            var page = await _context.TodoItems.FindAsync(pageId);
            if (page == null) return false;
            if (page.OwnerId == userId)
            {
                return true;
            }
            var permission = await GetUserPermissionForPage(pageId, userId);
            return permission != null;
        }
        private async Task<bool> CanAccessPage(int pageId)
        {
            var userId = GetUserId();
            var permission = await GetUserPermissionForPage(pageId, userId);
            return permission == PermissionLevel.FullAccess;
        }
        [HttpPost]
        public async Task<IActionResult> CreateBlock(int pageId, [FromBody] CreateBlockDto dto)
        {
            if (!await CanEditPage(pageId))
            {
                return Forbid();
            }
            var maxOrder = await _context.ContentBlocks
                .Where(b => b.PageId == pageId)
                .MaxAsync(b => (int?)b.Order) ?? -1;
            var newBlock = new ContentBlock
            {
                PageId = pageId,
                Type = dto.Type,
                Content = dto.Content,
                Order = maxOrder + 1
            };
            await _context.ContentBlocks.AddAsync(newBlock);
            await _context.SaveChangesAsync();
            var userId = GetUserId();
            await _hubContext.Clients.Group($"Page-{pageId}").SendAsync("BlockCreated", pageId, newBlock);
            return CreatedAtAction(nameof(GetBlockById), new { pageId = pageId, id = newBlock.Id }, newBlock);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBlockById(int pageId, int id)
        {
            if (!await CanViewPage(pageId))
            {
                return Forbid();
            }

            string keyCache = $"page_{pageId}";
            string cacheData = await _cache.GetStringAsync(keyCache);
            if (cacheData != null)
            {
                return Ok(JsonSerializer.Deserialize<object>(cacheData));
            }

            var block = await _context.ContentBlocks
                .FirstOrDefaultAsync(b => b.Id == id && b.PageId == pageId);
            if (block == null)
            {
                return NotFound();
            }

            await _cache.SetStringAsync(keyCache, JsonSerializer.Serialize(block), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            });
            return Ok(block);
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBlockContent(int pageId, int id, [FromBody] UpdateBlockContentDto dto)
        {
            if (!await CanEditPage(pageId))
            {
                return Forbid();
            }
            var block = await _context.ContentBlocks
                .FirstOrDefaultAsync(b => b.Id == id && b.PageId == pageId);
            if (block == null)
            {
                return NotFound();
            }
            block.Content = dto.Content;
            _context.ContentBlocks.Update(block);
            await _context.SaveChangesAsync();

            var senderId = GetUserId();
            var sender = await _userManager.FindByIdAsync(senderId);
            var page = await _context.TodoItems.FindAsync(pageId);

            var regex = new Regex(@"data-type=""mention"" data-id=""([^""]+)""");
            var matches = regex.Matches(dto.Content);
            foreach (Match match in matches)
            {
                var mentionedUserId = match.Groups[1].Value;
                if (mentionedUserId == senderId) continue;

                var notificationMessage = $"{sender.Email} mentioned you in a comment on '{page.TaskName}'";
                var notification = new Notification
                {
                    UserId = mentionedUserId,
                    Message = notificationMessage,
                    LinkToPageId = pageId,
                };
                await _context.Notifications.AddAsync(notification);
                await _hubContext.Clients.Group($"User-{mentionedUserId}")
                    .SendAsync("NewNotificationReceived");
            }
            await _context.SaveChangesAsync();

            var userId = GetUserId();
            await _hubContext.Clients.Group($"Page-{pageId}")
                .SendAsync("BlockUpdated", pageId, block);
            return NoContent();

        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBlock(int pageId, int id)
        {
            if (!await CanEditPage(pageId))
            {
                return Forbid();
            }
            var block = await _context.ContentBlocks
                .FirstOrDefaultAsync(b => b.Id == id && b.PageId == pageId);
            if (block == null)
            {
                return NotFound();
            }
            _context.ContentBlocks.Remove(block);
            var subsequentBlocks = await _context.ContentBlocks
                .Where(b => b.PageId == pageId && b.Order > block.Order)
                .ToListAsync();
            foreach (var subsequentBlock in subsequentBlocks)
            {
                subsequentBlock.Order -= 1;
                _context.ContentBlocks.Update(subsequentBlock);
            }
            await _context.SaveChangesAsync();
            var userId = GetUserId();
            await _hubContext.Clients.Group($"Page-{pageId}")
                .SendAsync("BlockDeleted", pageId, id);
            return NoContent();
        }
        [HttpPut("reorder")]
        public async Task<IActionResult> ReorderBlocks(int pageId, [FromBody] List<ReorderBlockDto> orderedBlocks)
        {
            if (!await CanEditPage(pageId))
            {
                return Forbid();
            }
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var blocksInPage = await _context.ContentBlocks
                    .Where(b => b.PageId == pageId)
                    .ToListAsync();
                foreach (var orderedBlock in orderedBlocks)
                {
                    var block = blocksInPage.FirstOrDefault(b => b.Id == orderedBlock.Id);
                    if (block != null && block.Order != orderedBlock.NewOrder)
                    {
                        block.Order = orderedBlock.NewOrder;
                        _context.ContentBlocks.Update(block);
                    }
                }
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "An error occurred while reordering blocks.");
            }
            var userId = GetUserId();
            await _hubContext.Clients.Group($"Page - {pageId}") 
                .SendAsync("BlocksReordered", pageId, orderedBlocks);
            return NoContent();
        }
    }
}
