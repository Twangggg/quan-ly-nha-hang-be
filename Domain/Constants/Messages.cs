namespace FoodHub.Domain.Constants
{
    public static class Messages
    {
        // Authentication Messages
        public const string InvalidCredentials = "Sai tên người dùng hoặc mật khẩu";
        public const string LoginSuccess = "Đăng nhập thành công";
        public const string LogoutSuccess = "Đăng xuất thành công";
        public const string AccountLocked = "Tài khoản đã bị khóa";
        public const string AccountInactive = "Tài khoản không hoạt động";
        
        // Validation Messages
        public const string UsernameRequired = "Tên người dùng không được để trống";
        public const string UsernameMinLength = "Tên người dùng phải có ít nhất 3 ký tự";
        public const string PasswordRequired = "Mật khẩu không được để trống";
        public const string PasswordMinLength = "Mật khẩu phải có ít nhất 6 ký tự";
    }
}
