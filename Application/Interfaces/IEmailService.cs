namespace FoodHub.Application.Interfaces
{
    public interface IEmailService
    {
        /// <summary>
        /// Sends a password reset email to the specified email address
        /// </summary>
        /// <param name="email">Recipient email address</param>
        /// <param name="resetLink">Password reset link</param>
        /// <param name="employeeName">Name of the employee</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if email sent successfully, false otherwise</returns>
        Task<bool> SendPasswordResetEmailAsync(
            string email, 
            string resetLink, 
            string employeeName,
            CancellationToken cancellationToken = default);
    }
}
