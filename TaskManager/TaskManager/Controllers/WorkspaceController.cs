using Microsoft.AspNetCore.Authorization;
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
    [Authorize]
    public class WorkspaceController : ControllerBase
    {
        private readonly TaskManagerDbContext _context;
        public WorkspaceController(TaskManagerDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public async Task<IActionResult> GetMyWorkspaces() {
            var userId = User.FindFirstValue("UserId");

            var workspaces = _context.WorkspaceMembers
                .Where(wm => wm.UserId == userId)
                .Include(wm => wm.Workspace)
                .Select(wm => new
                {
                    wm.Workspace.Id,
                    wm.Workspace.Name,
                    wm.Role
                })
                .ToListAsync();

            return Ok(workspaces);
        }
        [HttpPost]
        public async Task<IActionResult> CreateWorkspace([FromBody] CreateWorkspaceDto dto)
        {
            var userId = User.FindFirstValue("UserId");

            var workspace = new Workspace
            {
                Name = dto.Name,
                OwnerId = userId
            };

            _context.Workspaces.Add(workspace);
            await _context.SaveChangesAsync();

            var member = new WorkspaceMember
            {
                WorkspaceId = workspace.Id,
                UserId = userId,
                Role = "Admin"
            };
            _context.WorkspaceMembers.Add(member);
            await _context.SaveChangesAsync();

            return Ok(workspace);
        }
    }
    public class CreateWorkspaceDto { public string Name { get; set; } }
}
