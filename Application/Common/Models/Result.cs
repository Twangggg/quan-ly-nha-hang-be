namespace FoodHub.Application.Common.Models
{
    public enum ResultErrorType
    {
        None,
        BadRequest,
        NotFound,
        Unauthorized,
        Forbidden
    }

    public class Result<T>
    {
        public bool IsSuccess { get; private set; }
        public T? Data { get; private set; }
        public string? Error { get; private set; }
        public ResultErrorType ErrorType { get; private set; }

        private Result(bool isSuccess, T? data, string? error, ResultErrorType errorType)
        {
            IsSuccess = isSuccess;
            Data = data;
            Error = error;
            ErrorType = errorType;
        }

        public static Result<T> Success(T data) => new(true, data, null, ResultErrorType.None);
        public static Result<T> Failure(string error, ResultErrorType errorType = ResultErrorType.BadRequest) 
            => new(false, default, error, errorType);
        
        public static Result<T> NotFound(string error) => Failure(error, ResultErrorType.NotFound);
    }
}
