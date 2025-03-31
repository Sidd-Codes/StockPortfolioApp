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