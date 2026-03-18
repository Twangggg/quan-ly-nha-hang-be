namespace FoodHub.Domain.Enums
{
    public enum AuditAction
    {
        Create = 1,
        Update = 2,
        Delete = 3,
        StatusChange = 4,
        Deactivate = 5,
        Activate = 6,
        ResetPassword = 7,
        ChangeRole = 8,
        EmailFailure = 9,
        Login = 10,
        Logout = 11,
        Export = 12,
        Submit = 13,
        Cancel = 14,
        Complete = 15,
        Checkout = 16,
        CheckIn = 17,
        NoShow = 18,
        LoginFailed = 19
    }
}
