namespace FoodHub.Infrastructure.Security
{
    public class EmailSettings
        {
        public string ApiUrl { get; set; } = string.Empty; // URL API thật
        public string ApiKey { get; set; } = string.Empty; // Key bảo mật
        public string SenderEmail { get; set; } = string.Empty; // Email gửi đi
        public string SenderName { get; set; } = string.Empty;    
        }
}
