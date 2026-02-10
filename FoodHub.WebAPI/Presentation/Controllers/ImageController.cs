using FoodHub.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Manager")]
    public class ImageController : ControllerBase
    {
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ILogger<ImageController> _logger;

        public ImageController(ICloudinaryService cloudinaryService, ILogger<ImageController> logger)
        {
            _cloudinaryService = cloudinaryService;
            _logger = logger;
        }

        /// <summary>
        /// Upload an image to Cloudinary
        /// </summary>
        /// <param name="file">The image file (jpg, jpeg, png, webp - max 5MB)</param>
        /// <param name="folder">Optional folder name in Cloudinary (default: menu-items)</param>
        /// <returns>The URL of the uploaded image</returns>
        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage(IFormFile file, [FromQuery] string folder = "menu-items")
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "No file provided" });
                }

                var imageUrl = await _cloudinaryService.UploadImageAsync(file, folder);

                return Ok(new
                {
                    success = true,
                    imageUrl = imageUrl,
                    message = "Image uploaded successfully"
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid file upload attempt");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image to Cloudinary");
                return StatusCode(500, new { message = "An error occurred while uploading the image" });
            }
        }

        /// <summary>
        /// Delete an image from Cloudinary
        /// </summary>
        /// <param name="publicId">The public ID of the image to delete</param>
        /// <returns>Success status</returns>
        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteImage([FromQuery] string publicId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(publicId))
                {
                    return BadRequest(new { message = "Public ID is required" });
                }

                var success = await _cloudinaryService.DeleteImageAsync(publicId);

                if (success)
                {
                    return Ok(new { success = true, message = "Image deleted successfully" });
                }

                return NotFound(new { message = "Image not found or already deleted" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image from Cloudinary");
                return StatusCode(500, new { message = "An error occurred while deleting the image" });
            }
        }
    }
}
