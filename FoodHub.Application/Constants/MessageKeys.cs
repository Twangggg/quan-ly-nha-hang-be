using System.Reflection.Metadata;

namespace FoodHub.Application.Constants
{
    public static class MessageKeys
    {
        public static class Common
        {
            public const string DatabaseConflict = "DatabaseConflict";
            public const string DatabaseUpdateError = "DatabaseUpdateError";
            public const string OperationCancelled = "OperationCancelled";
            public const string Unauthorized = "Unauthorized";
            public const string InvalidFormat = "Common.InvalidFormat";
            public const string InvalidDate = "Common.InvalidDate";
            public const string InvalidFile = "Common.InvalidFile";
            public const string FileSizeExceeded = "Common.FileSizeExceeded";
            public const string UploadFailed = "Common.UploadFailed";
            public const string IdMismatch = "Common.IdMismatch";
            public const string IdRequired = "Common.IdRequired";
            public const string NoFileProvided = "Common.NoFileProvided";
            public const string UploadSuccess = "Common.UploadSuccess";
            public const string DeleteSuccess = "Common.DeleteSuccess";
            public const string DeleteFailed = "Common.DeleteFailed";
            public const string InternalServerError = "Common.InternalServerError";
            public const string ValidationFailed = "Common.ValidationFailed";
            public const string NotFound = "Common.NotFound";
            public const string PageNumberAtLeastOne = "Common.PageNumberAtLeastOne";
            public const string PageSizeBetween = "Common.PageSizeBetween";
            public const string ToDateAfterFromDate = "Common.ToDateAfterFromDate";
            public const string DateNotInFuture = "Common.DateNotInFuture";
        }

        public static class Password
        {
            public const string MinLength = "Password.MinLength";
            public const string RequireUppercase = "Password.RequireUppercase";
            public const string RequireLowercase = "Password.RequireLowercase";
            public const string RequireDigit = "Password.RequireDigit";
            public const string RequireSpecial = "Password.RequireSpecial";
            public const string NotEmpty = "Password.NotEmpty";
            public const string ConfirmationMismatch = "Password.ConfirmationMismatch";
            public const string MustBeDifferent = "Password.MustBeDifferent";
            public const string IncorrectCurrent = "Password.IncorrectCurrent";
        }

        public static class ResetPassword
        {
            public const string OnlyManagerCanReset = "OnlyManagerCanReset";
            public const string OnlyActiveEmployeeCanReset = "OnlyActiveEmployeeCanReset";
            public const string ReasonRequired = "ResetPassword.ReasonRequired";
            public const string ReasonMinLength = "ResetPassword.ReasonMinLength";
            public const string ReasonMaxLength = "ResetPassword.ReasonMaxLength";
            public const string SuccessWithEmail = "ResetPassword.SuccessWithEmail";
            public const string SuccessNoEmail = "ResetPassword.SuccessNoEmail";
            public const string StatusNoEmail = "ResetStatusNoEmail";
        }

        public static class Auth
        {
            public const string InvalidCredentials = "Auth.InvalidCredentials";
            public const string AccountInactive = "Auth.AccountInactive";
            public const string AccountBlocked = "Auth.AccountBlocked";
            public const string InvalidToken = "Auth.InvalidToken";
            public const string RefreshTokenNotFound = "Auth.RefreshTokenNotFound";
            public const string RefreshTokenExpired = "Auth.RefreshTokenExpired";
            public const string RefreshTokenRevoked = "Auth.RefreshTokenRevoked";
            public const string AccountCreationEmailFailed = "Auth.AccountCreationEmailFailed";
            public const string ResetRequestLimit = "Auth.ResetRequestLimit";
            public const string InvalidResetLink = "Auth.InvalidResetLink";
            public const string UserNotLoggedIn = "Auth.UserNotLoggedIn";
            public const string InvalidAction = "Auth.InvalidAction";
            public const string TooManyAttempts = "Auth.TooManyAttempts";
            public const string PasswordChangedSuccess = "Auth.PasswordChangedSuccess";
            public const string PasswordResetSuccess = "Auth.PasswordResetSuccess";
            public const string PasswordResetGenericMessage = "Auth.PasswordResetGenericMessage";
            public const string TokenRequired = "Auth.TokenRequired";
            public const string NewPasswordRequired = "Auth.NewPasswordRequired";
            public const string ConfirmPasswordRequired = "Auth.ConfirmPasswordRequired";
            public const string ConfirmPasswordMismatch = "Auth.ConfirmPasswordMismatch";
            public const string EmployeeCodeRequired = "Auth.EmployeeCodeRequired";
            public const string InvalidTokenClaims = "Auth.InvalidTokenClaims";
        }

        public static class Employee
        {
            public const string NotFound = "EmployeeNotFound";
            public const string NotActive = "EmployeeNotActive";
            public const string CannotUpdateInactive = "Employee.CannotUpdateInactive";
            public const string CannotIdentifyUser = "CannotIdentifyUser";
            public const string CannotIdentifyManager = "CannotIdentifyManager";
            public const string CannotPromoteToManager = "CannotPromoteToManager";
            public const string NewRoleMustBeDifferent = "NewRoleMustBeDifferent";
            public const string RoleChangedButEmailFailed = "RoleChangedButEmailFailed";
            public const string CodeInvalidFormat = "Employee.CodeInvalidFormat";
        }

        public static class Profile
        {
            public const string UsernameExists = "Profile.UsernameExists";
            public const string PhoneExists = "Profile.PhoneExists";
            public const string EmailExists = "Profile.EmailExists";
            public const string EmployeeIdRequired = "Profile.EmployeeIdRequired";
            public const string FullNameRequired = "Profile.FullNameRequired";
            public const string FullNameMaxLength = "Profile.FullNameMaxLength";
            public const string EmailRequired = "Profile.EmailRequired";
            public const string EmailInvalid = "Profile.EmailInvalid";
            public const string PhoneRequired = "Profile.PhoneRequired";
            public const string PhoneInvalid = "Profile.PhoneInvalid";
        }

        public static class Order
        {
            public const string NotFound = "Order.NotFound";
            public const string InvalidType = "Order.InvalidType";
            public const string SelectTable = "Order.SelectTable";
            public const string InvalidQuantity = "Order.InvalidQuantity";
            public const string InvalidAction = "Order.InvalidAction";
            public const string InvalidActionWithStatus = "Order.InvalidActionWithStatus";
            public const string MustHaveItem = "Order.MustHaveItem";
            public const string WrongTotalAmount = "Order.WrongTotalAmount";
            public const string ReasonRequired = "Order.ReasonRequired";
            public const string TableAlreadyOccupied = "Order.TableAlreadyOccupied";
            public const string InvalidStatusForCancel = "Order.InvalidStatusForCancel";
            public const string OrderNotReadyForCompletion = "Order.OrderNotReadyForCompletion";
            public const string AlreadyPaid = "Order.AlreadyPaid";
            public const string InsufficientAmount = "Order.InsufficientAmount";
            public const string ItemsNotFinished = "Order.ItemsNotFinished";
        }

        public static class OrderItem
        {
            public const string InvalidQuantity = "OrderItem.InvalidQuantity";
            public const string NotFound = "OrderItem.NotFound";

            // KDS state transitions
            public const string MustBePreparingToStartCooking =
                "OrderItem.MustBePreparingToStartCooking";
            public const string MustBeCookingToReady = "OrderItem.MustBeCookingToReady";
            public const string MustBeCookingToReject = "OrderItem.MustBeCookingToReject";
            public const string RejectionReasonRequired = "OrderItem.RejectionReasonRequired";
            public const string MustBeRejectedToReturn = "OrderItem.MustBeRejectedToReturn";
        }

        public static class KDS
        {
            public const string WipLimitExceeded = "KDS.WipLimitExceeded";
            public const string StationMismatch = "KDS.StationMismatch";
            public const string ManagerRoleRequired = "KDS.ManagerRoleRequired";
            public const string StationMaxLength = "KDS.StationMaxLength";
            public const string ActionMaxLength = "KDS.ActionMaxLength";
        }

        public static class MenuItem
        {
            public const string NotFound = "MenuItem.NotFound";
            public const string OutOfStock = "MenuItem.OutOfStock";
            public const string InvalidQuantity = "MenuItem.InvalidQuantity";
            public const string CodeExists = "MenuItem.CodeExists";
            public const string UpdateCostForbidden = "MenuItem.UpdateCostForbidden";
            public const string UpdateStockForbidden = "MenuItem.UpdateStockForbidden";
        }

        public static class Category
        {
            public const string NotFound = "Category.NotFound";
            public const string Inactive = "Category.Inactive";
            public const string InvalidType = "Category.InvalidType";
            public const string CannotChangeTypeNotEmpty = "Category.CannotChangeTypeNotEmpty";
        }

        public static class OptionGroup
        {
            public const string NotFound = "OptionGroup.NotFound";
        }

        public static class OptionItem
        {
            public const string NotFound = "OptionItem.NotFound";
        }

        public static class SetMenu
        {
            public const string NotFound = "SetMenu.NotFound";
            public const string CodeExists = "SetMenu.CodeExists";
            public const string UpdateForbidden = "SetMenu.UpdateForbidden";
            public const string DeleteForbidden = "SetMenu.DeleteForbidden";
        }

        public static class ActiveUserBehavior
        {
            public const string InActiveAccount = "ActiveUserBehavior.InActiveAccount";
            public const string Unauthorized = "ActiveUserBehavior.Unauthorized";
        }

        public static class Table
        {
            public const string NotFound = "Table.NotFound";
            public const string AlreadyOccupied = "Table.AlreadyOccupied";
            public const string NotAvailable = "Table.NotAvailable";
            public const string CodeExists = "Table.CodeExists";
            public const string UpdateForbidden = "Table.UpdateForbidden";
            public const string UpdateFail = "Table.UpdateFail";
        }

        public static class Area
        {
            public const string NotFound = "Area.NotFound";
            public const string CodeExists = "Area.CodeExists";
            public const string NameRequired = "Area.NameRequired";
            public const string CodeRequired = "Area.CodeRequired";
            public const string UpdateForbidden = "Area.UpdateForbidden";
            public const string DeleteForbidden = "Area.DeleteForbidden";
            public const string DeactivateForbidden = "Area.DeactivateForbidden";
            public const string Inactive = "Area.Inactive";
        }

        public static class Reservation
        {
            public const string NotFound = "Reservation.NotFound";
            // Keys from feature/reservation (HEAD)
            public const string Overlapped = "Reservation.Overlapped";
            public const string TableCapacityExceeded = "Reservation.TableCapacityExceeded";
            public const string InvalidStatusForCheckIn = "Reservation.InvalidStatusForCheckIn";
            public const string TableOccupied = "Reservation.TableOccupied";
            public const string AlreadyCheckedIn = "Reservation.AlreadyCheckedIn";
            // Keys from feature/reservation-liemdt
            public const string NameRequired = "Reservation.NameRequired";
            public const string NameMaxLength = "Reservation.NameMaxLength";
            public const string PhoneRequired = "Reservation.PhoneRequired";
            public const string PhoneInvalid = "Reservation.PhoneInvalid";
            public const string InvalidGuestCount = "Reservation.InvalidGuestCount";
            public const string NoTableAvailable = "Reservation.NoTableAvailable";
            public const string CancelReservationSuccess = "Reservation.CancelReservationSuccess";
            public const string InvalidTime = "Reservation.InvalidTime";
            public const string TimeTooSoon = "Reservation.TimeTooSoon";
            public const string VipRequired = "Reservation.VipRequired";
        }
        public static class SalesAnalytics
        {
            public const string MonthMustBeBetween1And12 =
                "SalesAnalytics.MonthMustBeBetween1And12";
            public const string YearMustBePositive = "SalesAnalytics.YearMustBePositive";
            public const string TopMustBeGreaterThanZero =
                "SalesAnalytics.TopMustBeGreaterThanZero";
            public const string TopMustNotExceed100 = "SalesAnalytics.TopMustNotExceed100";
            public const string MovingAverageDaysMustBePositive =
                "SalesAnalytics.MovingAverageDaysMustBePositive";
            public const string MovingAverageDaysMustNotExceed365 =
                "SalesAnalytics.MovingAverageDaysMustNotExceed365";
            public const string EndDateMustBeAfterStartDate =
                "SalesAnalytics.EndDateMustBeAfterStartDate";
            public const string DateNotInFuture = "SalesAnalytics.DateNotInFuture";
            public const string CannotCombineDateWithYearMonth =
                "SalesAnalytics.CannotCombineDateWithYearMonth";
            public const string YearRequiredWithMonth = "SalesAnalytics.YearRequiredWithMonth";
            public const string MonthRequiredWithYear = "SalesAnalytics.MonthRequiredWithYear";
            public const string StartDateRequiredWithEndDate =
                "SalesAnalytics.StartDateRequiredWithEndDate";
            public const string EndDateRequiredWithStartDate =
                "SalesAnalytics.EndDateRequiredWithStartDate";
            public const string MaxDateRangeExceeded = "SalesAnalytics.MaxDateRangeExceeded";
        }

        public static class Ingredient
        {
            public const string NotFound = "Ingredient.NotFound";
            public const string CodeExists = "Ingredient.CodeExists";
            public const string NameExists = "Ingredient.NameExists";
            public const string UsedInRecipe = "Ingredient.UsedInRecipe";

            // Validator keys
            public const string IdRequired = "Ingredient.IdRequired";
            public const string CodeRequired = "Ingredient.CodeRequired";
            public const string CodeMaxLength = "Ingredient.CodeMaxLength";
            public const string NameRequired = "Ingredient.NameRequired";
            public const string NameMaxLength = "Ingredient.NameMaxLength";
            public const string UnitRequired = "Ingredient.UnitRequired";
            public const string UnitMaxLength = "Ingredient.UnitMaxLength";
            public const string ThresholdMin = "Ingredient.ThresholdMin";
            public const string CostMin = "Ingredient.CostMin";
            public const string StockMin = "Ingredient.StockMin";
        }

        public static class InventorySettings
        {
            public const string ExpiryWarningDaysMin =
                "InventorySettings.ExpiryWarningDaysMin";
            public const string DefaultLowStockThresholdMin =
                "InventorySettings.DefaultLowStockThresholdMin";
            public const string MaxCostRecalcDaysRange =
                "InventorySettings.MaxCostRecalcDaysRange";
            public const string UpdateSuccess = "InventorySettings.UpdateSuccess";
        }

        public static class OpeningStock
        {
            public const string ItemsRequired = "OpeningStock.ItemsRequired";
            public const string IngredientIdRequired = "OpeningStock.IngredientIdRequired";
            public const string QuantityMin = "OpeningStock.QuantityMin";
            public const string CostPriceMin = "OpeningStock.CostPriceMin";
            public const string DuplicateIngredient = "OpeningStock.DuplicateIngredient";
            public const string ConfirmOverwrite = "OpeningStock.ConfirmOverwrite";
            public const string IngredientNotFound = "OpeningStock.IngredientNotFound";
            public const string ImportSuccess = "OpeningStock.ImportSuccess";
            public const string AlreadyLocked = "OpeningStock.AlreadyLocked";
        }
=======
            public const string NameRequired = "Reservation.NameRequired";
            public const string NameMaxLength = "Reservation.NameMaxLength";
            public const string PhoneRequired = "Reservation.PhoneRequired";
            public const string PhoneInvalid = "Reservation.PhoneInvalid";
            public const string InvalidGuestCount = "Reservation.InvalidGuestCount";
            public const string NoTableAvailable = "Reservation.NoTableAvailable";
            public const string CancelReservationSuccess = "Reservation.CancelReservationSuccess";
            public const string InvalidTime = "Reservation.InvalidTime";
            public const string TimeTooSoon = "Reservation.TimeTooSoon";
            public const string VipRequired = "Reservation.VipRequired";
        }

>>>>>>> origin/feature/reservation-liemdt
    }
}
