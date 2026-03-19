using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FoodHub.Infrastructure.Settings;
using FoodHub.Application.Interfaces.External;

namespace FoodHub.Infrastructure.Services.External
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;
        private readonly CloudinarySettings _settings;
        private readonly IMessageService _messageService;
        private readonly ILogger<CloudinaryService> _logger;

        public CloudinaryService(
            IOptions<CloudinarySettings> settings,
            IMessageService messageService,
            ILogger<CloudinaryService> logger
        )
        {
            _settings = settings.Value;
            _messageService = messageService;
            _logger = logger;

            if (
                string.IsNullOrWhiteSpace(_settings.CloudName)
                || string.IsNullOrWhiteSpace(_settings.ApiKey)
                || string.IsNullOrWhiteSpace(_settings.ApiSecret)
            )
            {
                _logger.LogError(
                    "Cloudinary configuration is incomplete. CloudName set: {HasCloudName}, ApiKey set: {HasApiKey}, ApiSecret set: {HasApiSecret}",
                    !string.IsNullOrWhiteSpace(_settings.CloudName),
                    !string.IsNullOrWhiteSpace(_settings.ApiKey),
                    !string.IsNullOrWhiteSpace(_settings.ApiSecret)
                );
            }

            var account = new Account(
                _settings.CloudName,
                _settings.ApiKey,
                _settings.ApiSecret
            );

            _cloudinary = new Cloudinary(account);
        }

        public async Task<string> UploadImageAsync(IFormFile file, string folder = "menu-items")
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException(_messageService.GetMessage(MessageKeys.Common.InvalidFile));
            }

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                throw new ArgumentException(_messageService.GetMessage(MessageKeys.Common.InvalidFormat));
            }

            // Validate file size (max 5MB)
            const int maxFileSize = 5 * 1024 * 1024; // 5MB
            if (file.Length > maxFileSize)
            {
                throw new ArgumentException(_messageService.GetMessage(MessageKeys.Common.FileSizeExceeded));
            }

            using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder,
                Transformation = new Transformation()
                    .Width(800)
                    .Height(800)
                    .Crop("limit")
                    .Quality("auto:good")
                    .FetchFormat("auto"),
                UseFilename = true,
                UniqueFilename = true,
                Overwrite = false
            };

            ImageUploadResult uploadResult;
            try
            {
                uploadResult = await _cloudinary.UploadAsync(uploadParams);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Cloudinary upload threw an exception for file {FileName} to folder {Folder}",
                    file.FileName,
                    folder
                );
                throw new Exception(_messageService.GetMessage(MessageKeys.Common.UploadFailed));
            }

            if (uploadResult.Error != null)
            {
                _logger.LogError(
                    "Cloudinary upload failed for file {FileName} to folder {Folder}. Error message: {ErrorMessage}",
                    file.FileName,
                    folder,
                    uploadResult.Error.Message
                );
                throw new Exception(_messageService.GetMessage(MessageKeys.Common.UploadFailed));
            }

            if (uploadResult.SecureUrl == null)
            {
                _logger.LogError(
                    "Cloudinary upload returned no SecureUrl for file {FileName} to folder {Folder}",
                    file.FileName,
                    folder
                );
                throw new Exception(_messageService.GetMessage(MessageKeys.Common.UploadFailed));
            }

            return uploadResult.SecureUrl.ToString();
        }

        public async Task<bool> DeleteImageAsync(string publicId)
        {
            if (string.IsNullOrWhiteSpace(publicId))
            {
                return false;
            }

            var deleteParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deleteParams);

            return result.Result == "ok";
        }
    }
}
