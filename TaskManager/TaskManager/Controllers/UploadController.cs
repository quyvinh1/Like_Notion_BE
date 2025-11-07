using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Services;

namespace TaskManager.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UploadController : ControllerBase
    {
        private readonly IPhotoService _photoService;
        public UploadController(IPhotoService photoService)
        {
            _photoService = photoService;
        }
        [HttpPost("image")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if(file == null || file.Length==0)
            {
                return BadRequest(new { Message = "No file uploaded." });
            }
            var uploadResult = await _photoService.AddPhotoAsync(file);
            if(uploadResult.Error != null)
            {
                return BadRequest(new { Message = "Photo upload failed.", Details = uploadResult.Error.Message });
            }
            return Ok(new { url = uploadResult.SecureUrl.AbsoluteUri });
        }
    }
}
