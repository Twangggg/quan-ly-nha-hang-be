using Microsoft.AspNetCore.Http;

namespace FoodHub.Application.Interfaces
{
    public interface ICloudinaryService
    {
        /// <summary>
        /// Upload an image to Cloudinary
        /// </summary>
        /// <param name="file">The image file to upload</param>
        /// <param name="folder">The folder path in Cloudinary (optional)</param>
        /// <returns>The secure URL of the uploaded image</returns>
        Task<string> UploadImageAsync(IFormFile file, string folder = "menu-items");

        /// <summary>
        /// Delete an image from Cloudinary using its public ID
        /// </summary>
        /// <param name="publicId">The public ID of the image to delete</param>
        /// <returns>True if deletion was successful, false otherwise</returns>
        Task<bool> DeleteImageAsync(string publicId);
    }
}
