using System.Globalization;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;

namespace FoodHub.Application.Services
{
    public class MessageService : IMessageService
    {
        public string GetMessage(string key)
        {
            // Try to get from Messages first, then ErrorMessages
            var message = Resources.Messages.ResourceManager.GetString(key, CultureInfo.CurrentUICulture);

            if (string.IsNullOrEmpty(message))
            {
                message = Resources.ErrorMessages.ResourceManager.GetString(key, CultureInfo.CurrentUICulture);
            }

            return message ?? key; // Fallback to key if not found
        }

        public string GetMessage(string key, params object[] args)
        {
            var message = GetMessage(key);
            return string.Format(message, args);
        }

        public bool HasKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            
            var message = Resources.Messages.ResourceManager.GetString(key, CultureInfo.CurrentUICulture);
            if (string.IsNullOrEmpty(message))
            {
                message = Resources.ErrorMessages.ResourceManager.GetString(key, CultureInfo.CurrentUICulture);
            }
            
            return !string.IsNullOrEmpty(message);
        }
    }
}
