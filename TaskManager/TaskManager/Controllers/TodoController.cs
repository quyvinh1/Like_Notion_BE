using AutoMapper;
using AutoMapper.QueryableExtensions;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using SendGrid.Helpers.Mail;
using System.Security.Claims;
using System.Threading.Tasks;
using TaskManager.DBContext;
using TaskManager.DTOs;
using TaskManager.Hubs;
using TaskManager.Models;
using TaskManager.Services;

namespace TaskManager.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TodoController : ControllerBase
    {
        private readonly TaskManagerDbContext _dbContext;
        private readonly UserManager<User> _userManager;
        private readonly IEmailService _emailService;
        private readonly IMapper _mapper;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IHubContext<TaskHubs> _hubContext;
        public TodoController(
            TaskManagerDbContext dbContext,
            UserManager<User> user,
            IEmailService emailService,
            IMapper mapper,
            IBackgroundJobClient backgroundJobClient,
            IHubContext<TaskHubs> hubContext)
        {
            _dbContext = dbContext;
            _userManager = user;
            _emailService = emailService;
            _mapper = mapper;
            _backgroundJobClient = backgroundJobClient;
            _hubContext = hubContext;
        }
        private string GetUserId()
        {
            var userId = User?.FindFirstValue("UserId");
            return userId;
        }
        //[HttpGet]
        //public async Task<ActionResult<PageResult<PageSummaryDto>>> GetTasks(
        //    [FromQuery] PaginationParams paginationParams,
        //    [FromQuery] string? status,
        //    [FromQuery] string? search,
        //    [FromQuery] int? categoryId)
        //{
        //    var userId = GetUserId();
        //    if (userId == null)
        //    {
        //        return Unauthorized();
        //    }

        //    var myPagesQuery = _dbContext.TodoItems
        //        .Where(t => t.OwnerId == userId && t.ParentId == null);

        //    var sharedPagesQuery = _dbContext.PagePermissions
        //        .Where(p => p.UserId == userId)
        //        .Select(p => p.Page)
        //        .Where(page => page.ParentId == null);
        //    var query = myPagesQuery.Union(sharedPagesQuery);
        //    if (!string.IsNullOrEmpty(status))
        //    {
        //        if (status.ToLower() == "completed")
        //        {
        //            query = query.Where(t => t.IsCompleted);
        //        }
        //        else if (status.ToLower() == "pending")
        //        {
        //            query = query.Where(t => !t.IsCompleted);
        //        }
        //    }
        //    if (!string.IsNullOrEmpty(search))
        //    {
        //        query = query.Where(t => t.TaskName.Contains(search));
        //    }
        //    if (categoryId.HasValue)
        //    {
        //        query = query.Where(t => t.CategoryId == categoryId.Value);
        //    }

        //    if (!string.IsNullOrEmpty(paginationParams.SortBy))
        //    {
        //        bool isDescending = paginationParams.SortOrder.ToLower() == "desc";

        //        switch (paginationParams.SortBy.ToLower())
        //        {
        //            case "taskname":
        //                query = isDescending
        //                    ? query.OrderByDescending(t => t.TaskName)
        //                    : query.OrderBy(t => t.TaskName);
        //                break;
        //            case "duedate":
        //                query = isDescending
        //                    ? query.OrderByDescending(t => t.DueDate)
        //                    : query.OrderBy(t => t.DueDate);
        //                break;
        //            default:
        //                query = query.OrderByDescending(t => t.Id);
        //                break;
        //        }
        //    }
        //    else
        //    {
        //        query = query.OrderByDescending(t => t.Id);
        //    }

        //    var totalCount = await query.CountAsync();

        //    var items = await query
        //        .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
        //        .Take(paginationParams.PageSize)
        //        .ProjectTo<PageSummaryDto>(_mapper.ConfigurationProvider)
        //        .ToListAsync();

        //    var pageResult = new PageResult<PageSummaryDto>
        //    {
        //        Items = items,
        //        PageNumber = paginationParams.PageNumber,
        //        PageSize = paginationParams.PageSize,
        //        TotalCount = totalCount
        //    };

        //    return Ok(pageResult);
        //}

        [HttpGet("sidebar")]
        public async Task<ActionResult<SidebarDto>> GetSidebarData()
        {
            var userId = GetUserId();
            if(string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            var myPages = await _dbContext.TodoItems
                .Where(t => t.OwnerId == userId && t.ParentId == null)
                .OrderBy(t => t.Id)
                .ProjectTo<PageSummaryDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
            var sharedPages = await _dbContext.PagePermissions
                .Where(p => p.UserId == userId)
                .Select(p => p.Page)
                .Where(page => page != null && page.ParentId == null)
                .ProjectTo<PageSummaryDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
            var sidebarData = new SidebarDto
            {
                MyPages = myPages,
                SharedPages = sharedPages
            };
            return Ok(sidebarData);
        }
        [HttpPost]
        public async Task<IActionResult> CreateTodo([FromBody] CreateTaskDto dto)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }
            var todoItem = _mapper.Map<TodoItem>(dto);
            todoItem.IsCompleted = false;
            todoItem.OwnerId = userId;
            todoItem.ParentId = dto.ParentId;
            //todoItem.CategoryId = dto.CategoryId;

            await _dbContext.TodoItems.AddAsync(todoItem);
            await _dbContext.SaveChangesAsync();

            var createdTaskWithCategory = await _dbContext.TodoItems
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.Id == todoItem.Id);

            await _hubContext.Clients.Group($"User-{userId}")
                .SendAsync("NewTaskCreated", createdTaskWithCategory);
            if (createdTaskWithCategory.DueDate.HasValue)
            {
                var remimderTime = createdTaskWithCategory.DueDate.Value.AddHours(-24);
                if (remimderTime > DateTime.UtcNow)
                {
                    var user = await _userManager.FindByIdAsync(userId);
                    if (user != null)
                    {
                        _backgroundJobClient.Schedule(() =>
                        _emailService.SendEmailReminderAsync(user.Email, createdTaskWithCategory.TaskName, createdTaskWithCategory.DueDate.Value),
                        remimderTime);
                    }
                }
            }
            return CreatedAtAction(nameof(GetTaskById), new { id = createdTaskWithCategory.Id }, createdTaskWithCategory);
        }
        [HttpGet("{id}")]
        public async Task<ActionResult<PageDetailDto>> GetTaskById(int id)
        {
            var userId = GetUserId();
            var permission = await GetUserPermissionForPage(id, userId);
            if (permission == null)
            {
                return Forbid();
            }
            var task = await _dbContext.TodoItems
                .Include(t => t.Category)
                .Include(t => t.Children)
                .Include(t => t.Attachments)
                .Include(t => t.ContentBlocks.OrderBy(b => b.Order))
                .Where(t => t.Id == id)
                .FirstOrDefaultAsync();
            if (task == null)
            {
                return NotFound();
            }
            var pageDto = _mapper.Map<PageDetailDto>(task);
            pageDto.IsOwner = task.OwnerId == userId;
            pageDto.CurrentUserPermission = permission.Value;
            return Ok(pageDto);
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, [FromBody] UpdateTaskDto dto)
        {
            var userId = GetUserId();
            var permission = await GetUserPermissionForPage(id, userId);
            if (permission != PermissionLevel.FullAccess)
            {
                return Forbid();
            }
            var todoItem = await _dbContext.TodoItems.FirstOrDefaultAsync(t => t.Id == id);
            if (todoItem == null)
            {
                return NotFound(new { Message = "Task not found or you don't have permission" });
            }
            _mapper.Map(dto, todoItem);

            _dbContext.TodoItems.Update(todoItem);
            await _dbContext.SaveChangesAsync();
            var updatedTaskWIthCategory = await _dbContext.TodoItems
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.Id == todoItem.Id);
            await _hubContext.Clients.Group($"User-{userId}")
                .SendAsync("TaskUpdated", updatedTaskWIthCategory);
            return NoContent();
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var userId = GetUserId();
            var permission = await GetUserPermissionForPage(id, userId);
            if (permission != PermissionLevel.FullAccess)
            {
                return Forbid();
            }
            var todoItem = await _dbContext.TodoItems.FindAsync(id);
            int? parentId = todoItem.ParentId;
            if (todoItem == null)
            {
                return NotFound();
            }
            if (todoItem.OwnerId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }
            todoItem.IsDelete = true;
            todoItem.DeleteAt = DateTime.UtcNow;
            await _hubContext.Clients.Group($"User-{userId}")
                .SendAsync("TaskDeleted", new { Id = id, ParentId = parentId });
            await _dbContext.SaveChangesAsync();
            return NoContent();
        }
        [HttpGet("{taskId}/children")]
        public async Task<IActionResult> GetTaskChildren(int taskId)
        {
            var userId = GetUserId();
            var permission = await GetUserPermissionForPage(taskId, userId);
            if (permission == null)
            {
                return Forbid();
            }
            var parentTaskExists = await _dbContext.TodoItems
                .AnyAsync(t => t.Id == taskId);
            if (!parentTaskExists)
            {
                return NotFound("Parent task not found or you do not have permission.");
            }
            var children = await _dbContext.TodoItems.Where(t => t.ParentId == taskId && t.OwnerId == userId)
                 .Include(t => t.Category)
                 .Include(t => t.Children)
                 .OrderBy(t => t.Id)
                 .ProjectTo<PageSummaryDto>(_mapper.ConfigurationProvider)
                 .ToListAsync();
            return Ok(children);
        }
        [HttpPut("{id}/move")]
        public async Task<IActionResult> MoveTask(int id, [FromBody] MoveTaskDto dto)
        {
            var userId = GetUserId();
            var permission = await GetUserPermissionForPage(id, userId);
            if (permission != PermissionLevel.FullAccess)
            {
                return Forbid();
            }
            var tasktoMove = await _dbContext.TodoItems.FirstOrDefaultAsync(t => t.Id == id);
            if (tasktoMove == null)
            {
                return NotFound(new { Message = "Task not found or permission denied" });
            }
            if (dto.NewParentId.HasValue)
            {
                var newParentTask = await _dbContext.TodoItems
                    .AnyAsync(t => t.Id == dto.NewParentId.Value && t.OwnerId == userId);
                if (!newParentTask)
                {
                    return BadRequest("Invalid new parent task");
                }
            }
            tasktoMove.ParentId = dto.NewParentId;
            _dbContext.TodoItems.Update(tasktoMove);
            await _dbContext.SaveChangesAsync();
            var movedTaskWithCategory = await _dbContext.TodoItems
                .Include(t => t.Category)
                .Include(t => t.Children)
                .FirstOrDefaultAsync(t => t.Id == tasktoMove.Id);

            await _hubContext.Clients.Group($"User-{userId}")
                .SendAsync("TaskMoved", movedTaskWithCategory);
            return NoContent();
        }
        [HttpPut("{id}/aesthetics")]
        public async Task<IActionResult> UpdatePageAesthetics(int id, [FromBody] UpdatePageAestheticsDto dto)
        {
            var userId = GetUserId();
            var task = await _dbContext.TodoItems.FirstOrDefaultAsync(t => t.Id == id && t.OwnerId == userId);
            if (task == null)
            {
                return NotFound(new { Message = "Task not found or you don't have permission" });
            }
            task.Icon = dto.Icon;
            task.CoverImage = dto.CoverImage;
            _dbContext.TodoItems.Update(task);
            await _dbContext.SaveChangesAsync();
            var updatedTaskDto = _mapper.Map<PageDetailDto>(
                await _dbContext.TodoItems
                    .Include(t => t.Category)
                    .Include(t => t.Children)
                    .Include(t => t.ContentBlocks.OrderBy(b => b.Order))
                    .Include(t => t.Attachments)
                    .FirstOrDefaultAsync(t => t.Id == task.Id)
            );
            await _hubContext.Clients.Group($"User-{userId}")
                .SendAsync("TaskAestheticsUpdated", updatedTaskDto);
            return NoContent();
        }
        [HttpGet("search")]
        public async Task<ActionResult<List<SearchResultDto>>> Search(string term)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(term))
            {
                return Ok(new List<SearchResultDto>());
            }
            term = term.ToLower();
            var titleMatches = await _dbContext.TodoItems
                .Where(t => t.OwnerId == userId &&
                            t.TaskName.ToLower().Contains(term))
                .Select(t => new SearchResultDto
                {
                    PageId = t.Id,
                    PageTitle = t.TaskName,
                    Icon = t.Icon,
                    MatchType = "Title",
                    MatchSnippet = null
                })
                .ToListAsync();
            var contentMatches = await _dbContext.ContentBlocks
        .Where(b => b.Page.OwnerId == userId &&
                    b.Content.ToLower().Contains(term))
        .Select(b => new SearchResultDto
        {
            PageId = b.PageId,
            PageTitle = b.Page.TaskName,
            Icon = b.Page.Icon,
            MatchType = "Content",
            MatchSnippet = b.Content.Length > 100 ? b.Content.Substring(0, 100) + "..." : b.Content
        })
        .ToListAsync();

            var allMatches = titleMatches
                .Concat(contentMatches.Where(c => !titleMatches.Any(t => t.PageId == c.PageId)))
                .Take(20)
                .ToList();

            return Ok(allMatches);
        }
        private async Task<PermissionLevel?> GetUserPermissionForPage (int pageId, string userId)
        {
            if(string.IsNullOrEmpty(userId))
            {
                return null;
            }
            var page = await _dbContext.TodoItems.FindAsync(pageId);
            if (page == null)
            {
                return null;
            }
            if (page.OwnerId == userId)
            {
                return PermissionLevel.FullAccess;
            }
            var directPermission = await _dbContext.PagePermissions
                .FirstOrDefaultAsync(p => p.PageId == pageId && p.UserId == userId);
            if (directPermission != null)
            {
                return directPermission.Level;
            }
            if(page.ParentId.HasValue)
            {
                return await GetUserPermissionForPage(page.ParentId.Value, userId);
            }
            return null;
        }
        //private async Task<PermissionLevel?> GetUserPermissionForPage(int pageId, string userId)
        //{
        //    var page = await _dbContext.TodoItems.FindAsync(pageId);
        //    if (page == null)
        //    {
        //        return null;
        //    }
        //    if (page.OwnerId == userId)
        //    {
        //        return PermissionLevel.FullAccess;
        //    }
        //    var permission = await _dbContext.PagePermissions
        //        .FirstOrDefaultAsync(p => p.PageId == pageId && p.UserId == userId);

        //    if (permission != null)
        //    {
        //        return permission.Level;
        //    }
        //    return null;
        //}
        [HttpGet("{pageId}/share")]
        public async Task<ActionResult<List<PermissionDto>>> GetShareList(int pageId)
        {
            var userId = GetUserId();
            var page = await _dbContext.TodoItems.FindAsync(pageId);
            if (page == null || page.OwnerId != userId)
            {
                return Forbid();
            }
            var permissions = await _dbContext.PagePermissions
                .Where(p => p.PageId == pageId)
                .Include(p => p.User)
                .Select(p => new PermissionDto
                {
                    UserId = p.UserId,
                    Email = p.User.Email,
                    Level = p.Level
                })
                .ToListAsync();
            return Ok(permissions);
        }
        [HttpPost("{pageId}/share")]
        public async Task<IActionResult> InviteUser(int pageId, [FromBody] InviteUserDto dto)
        {
            var ownerId = GetUserId();
            var page = await _dbContext.TodoItems.FindAsync(pageId);
            if (page == null || page.OwnerId != ownerId)
            {
                return Forbid();
            }
            var guestUser = await _userManager.FindByEmailAsync(dto.Email);
            if (guestUser == null)
            {
                return NotFound("User not found");
            }
            if (guestUser.Id == ownerId)
            {
                return BadRequest("Cannot invite yourself");
            }
            var existingPermission = await _dbContext.PagePermissions
                .FirstOrDefaultAsync(p => p.PageId == pageId && p.UserId == guestUser.Id);
            if (existingPermission != null)
            {
                existingPermission.Level = dto.Level;
                _dbContext.PagePermissions.Update(existingPermission);
            }
            else
            {
                var newPermission = new PagePermission
                {
                    PageId = pageId,
                    UserId = guestUser.Id,
                    Level = dto.Level
                };
                await _dbContext.PagePermissions.AddAsync(newPermission);
            }
            await _dbContext.SaveChangesAsync();

            var ownerUser = await _userManager.FindByIdAsync(ownerId);
            var notificationMessage = $"{ownerUser.Email} share the page '{page.TaskName}' with you";
            var newNotification = new Notification
            {
                UserId = guestUser.Id,
                Message = notificationMessage,
                IsRead = false,
                LinkToPageId = pageId,
            };

            await _dbContext.Notifications.AddAsync(newNotification);
            await _dbContext.SaveChangesAsync();
            await _hubContext.Clients.Group($"User-{guestUser.Id}")
                .SendAsync("NewNotificationReceived");


            var pageDto = _mapper.Map<PageSummaryDto>(page);
            await _hubContext.Clients.Group($"User-{guestUser.Id}")
                .SendAsync("PageShared", pageDto, dto.Level);
            return Ok();
        }
        [HttpDelete("{pageId}/share/{guestUserId}")]
        public async Task<IActionResult> RevokePermission(int pageId, string guestUserId)
        {
            var ownerId = GetUserId();
            var page = await _dbContext.TodoItems.FindAsync(pageId);
            if (page == null || page.OwnerId != ownerId)
            {
                return Forbid();
            }
            var permission = await _dbContext.PagePermissions
                .FirstOrDefaultAsync(p => p.PageId == pageId && p.UserId == guestUserId);
            if (permission == null)
            {
                return NotFound("Permission record not found");
            }
            _dbContext.PagePermissions.Remove(permission);
            await _dbContext.SaveChangesAsync();
            await _hubContext.Clients.Group($"User-{guestUserId}")
                .SendAsync("PageUnshared", pageId);
            return NoContent();
        }
        [HttpGet("notifications")]
        public async Task<ActionResult<List<Notification>>> GetNotifications()
        {
            var userId = GetUserId();
            var notifications = await _dbContext.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .ToListAsync(); 
            return Ok(notifications);
        }
        [HttpPost("notifications/mark-as-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = GetUserId();
            var notifications = await _dbContext.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();
            foreach(var notification in notifications)
            {
                notification.IsRead = true;
            }
            _dbContext.Notifications.UpdateRange(notifications);
            await _dbContext.SaveChangesAsync();
            return NoContent();
        }
        [HttpGet("trash")]
        public async Task<ActionResult<IEnumerable<TodoItem>>> GetTrashItems()
        {
            var userId = GetUserId();
            return await _dbContext.TodoItems
                .IgnoreQueryFilters()
                .Where(t => t.OwnerId == userId && t.IsDelete == true)
                .OrderByDescending(t => t.DeleteAt)
                .ToListAsync();
        }
        [HttpPost("trash/{id}/restore")]
        public async Task<IActionResult> RestoreFromTrash(int id)
        {
            var page = await _dbContext.TodoItems
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == id);
            if (page == null) return NotFound();
            page.IsDelete = false;
            page.DeleteAt = null;

            await _dbContext.SaveChangesAsync();
            return Ok( new { message = "Page restored"});

        }
        [HttpDelete("trash/{id}/permanent")]
        public async Task<IActionResult> DeletePermanent (int id)
        {
            var page = await _dbContext.TodoItems
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == id);
            if (page == null) return NotFound();
            _dbContext.TodoItems.Remove(page);
            await _dbContext.SaveChangesAsync();
            return NoContent();
        }
    }
}
