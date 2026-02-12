namespace FoodHub.Domain.Common
{
    public class DomainResult
    {
        public bool IsSuccess { get; }
        public string? ErrorCode { get; }

        protected DomainResult(bool isSuccess, string? errorCode)
        {
            IsSuccess = isSuccess;
            ErrorCode = errorCode;
        }

        public static DomainResult Success() => new DomainResult(true, null);

        public static DomainResult Failure(string errorCode) => new DomainResult(false, errorCode);
    }

    public class DomainResult<T> : DomainResult
    {
        public T? Value { get; }

        private DomainResult(bool isSuccess, T? value, string? errorCode)
            : base(isSuccess, errorCode)
        {
            Value = value;
        }

        public static DomainResult<T> Success(T value) => new DomainResult<T>(true, value, null);

        public static new DomainResult<T> Failure(string errorCode) =>
            new DomainResult<T>(false, default, errorCode);
    }
}
