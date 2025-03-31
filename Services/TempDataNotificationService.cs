using Microsoft.AspNetCore.Mvc.ViewFeatures;
using StockPortfolioApp.Services.Interfaces;

namespace StockPortfolioApp.Services
{
    public class TempDataNotificationService : INotificationService
    {
        private readonly ITempDataDictionary _tempData;

        public TempDataNotificationService(ITempDataDictionary tempData)
        {
            _tempData = tempData;
        }

        public void AddNotification(string message, string type = "info")
        {
            _tempData["Notification"] = message;
            _tempData["NotificationType"] = type;
        }

        public (string? message, string? type) GetNotification()
        {
            return (_tempData["Notification"]?.ToString(), _tempData["NotificationType"]?.ToString());
        }

        public void SetWarningMessage(string message)
        {
            _tempData["WarningMessage"] = message;
        }

        public void SetErrorMessage(string message)
        {
            _tempData["ErrorMessage"] = message;
        }

        public void SetSuccessMessage(string message)
        {
            _tempData["SuccessMessage"] = message;
        }

        public void SetLastPriceUpdate(DateTime? updateTime)
        {
            if (updateTime.HasValue)
            {
                _tempData["LastPriceUpdate"] = updateTime;
            }
        }

        public string GetWarningMessage()
        {
            return _tempData["WarningMessage"]?.ToString() ?? string.Empty;
        }
    }
} 