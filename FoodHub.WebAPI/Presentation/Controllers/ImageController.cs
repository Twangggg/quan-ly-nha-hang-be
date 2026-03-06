using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Presentation.Controllers;
using FoodHub.WebAPI.Presentation.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers
{
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
        private readonly IMessageService _messageService;

        public ImageController(
            ICloudinaryService cloudinaryService,
            ILogger<ImageController> logger,
            IMessageService messageService
        )
        {
            _cloudinaryService = cloudinaryService;
            _logger = logger;
            _messageService = messageService;
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
        public async Task<IActionResult> UploadImage(
            IFormFile file,
            [FromQuery] string folder = "menu-items"
        )
        {
            try
            {
                // if (file == null || file.Length == 0)
                // {
                //     return BadRequest(
                //         new ErrorResponse(
                //             StatusCodes.Status400BadRequest,
                //             _messageService.GetMessage(MessageKeys.Common.NoFileProvided)
                //         )
                //     );
                // }

                var imageUrl = await _cloudinaryService.UploadImageAsync(file, folder);

                return Ok(
                    new
                    {
                        success = true,
                        imageUrl = imageUrl,
                        message = _messageService.GetMessage(MessageKeys.Common.UploadSuccess),
                    }
                );
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid file upload attempt");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image to Cloudinary");
                return StatusCode(
                    500,
                    new ErrorResponse(
                        500,
                        _messageService.GetMessage(MessageKeys.Common.UploadFailed)
                    )
                );
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
                    return BadRequest(
                        new ErrorResponse(
                            StatusCodes.Status400BadRequest,
                            _messageService.GetMessage(MessageKeys.Common.IdRequired)
                        )
                    );
                }

                var success = await _cloudinaryService.DeleteImageAsync(publicId);

                if (success)
                {
                    return Ok(
                        new
                        {
                            success = true,
                            message = _messageService.GetMessage(MessageKeys.Common.DeleteSuccess),
                        }
                    );
                }

                return NotFound(
                    new ErrorResponse(
                        StatusCodes.Status404NotFound,
                        _messageService.GetMessage(MessageKeys.Common.NotFound)
                    )
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image from Cloudinary");
                return StatusCode(
                    500,
                    new ErrorResponse(
                        500,
                        _messageService.GetMessage(MessageKeys.Common.DeleteFailed)
                    )
                );
            }
        }
    }
}
