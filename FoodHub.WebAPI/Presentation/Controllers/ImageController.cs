using FoodHub.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using FoodHub.WebAPI.Presentation.Attributes;

    /// <summary>
    /// Quản lý hình ảnh và upload lên Cloudinary.
    /// </summary>
    [Authorize(Roles = "Manager")]
    [Tags("Hình ảnh (Images)")]
    [RateLimit(maxRequests: 10, windowMinutes: 5, blockMinutes: 15)]
    public class ImageController : ApiControllerBase
    {
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ILogger<ImageController> _logger;

        public ImageController(ICloudinaryService cloudinaryService, ILogger<ImageController> logger)
        {
            _cloudinaryService = cloudinaryService;
            _logger = logger;
        }

        /// <summary>
        /// Tải hình ảnh lên Cloudinary.
        /// </summary>
        /// <remarks>
        /// Định dạng hỗ trợ: jpg, jpeg, png, webp. 
        /// Dung lượng tối đa: 5MB.
        /// </remarks>
        /// <param name="file">File hình ảnh cần upload.</param>
        /// <param name="folder">Thư mục lưu trữ trên Cloudinary (mặc định: menu-items).</param>
        /// <response code="200">Upload thành công, trả về URL hình ảnh.</response>
        /// <response code="400">File không hợp lệ hoặc quá lớn.</response>
        [HttpPost("upload")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
        /// Xóa hình ảnh khỏi Cloudinary.
        /// </summary>
        /// <param name="publicId">Mã định danh (Public ID) của ảnh trên Cloudinary.</param>
        /// <response code="200">Xóa thành công.</response>
        /// <response code="404">Không tìm thấy ảnh.</response>
        [HttpDelete("delete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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
