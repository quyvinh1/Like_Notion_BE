using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskManager.DBContext;

namespace TaskManager.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CategoryController : ControllerBase
    {
        private readonly TaskManagerDbContext _dbContext;
        public CategoryController(
            TaskManagerDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        private string GetUserId()
        {
            return User.FindFirstValue("UserId");
        }
        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var userId = GetUserId();
            var categories = await _dbContext.Categories
                .Where(c => c.OwnerId == userId)
                .ToListAsync();
            return Ok(categories);
        }
        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] string name)
        {
            if(string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("Category name cannot be empty.");
            }
            var userId = GetUserId();
            var category = new Models.Category
            {
                Name = name,
                OwnerId = userId,

            };
            _dbContext.Categories.Add(category);
            await _dbContext.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCategories), new { id = category.Id }, category);
        }
        [HttpDelete("id")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var userId = GetUserId();
            var category = await _dbContext.Categories
                .FirstOrDefaultAsync(c => c.Id == id && c.OwnerId == userId);
            if(category == null)
            {
                return NotFound();
            }
            _dbContext.Categories.Remove(category);
            await _dbContext.SaveChangesAsync();
            return NoContent();
        }
    }
}
