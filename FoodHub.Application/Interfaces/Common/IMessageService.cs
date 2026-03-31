namespace FoodHub.Application.Interfaces.Common
{
    public interface IMessageService
    {
        string GetMessage(string key);
        string GetMessage(string key, params object[] args);
        bool HasKey(string key);
    }
}
