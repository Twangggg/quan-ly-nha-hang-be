namespace FoodHub.Domain.Enums
{
    public enum AuditAction : short
    {
        Create = 1,
        Update = 2,
        Deactivate = 3,
        Activate = 4,
        ResetPassword = 5,
        ChangeRole = 6
    }
}
