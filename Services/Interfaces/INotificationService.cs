using System;
using System.Threading.Tasks;

namespace StockPortfolioApp.Services.Interfaces
{
    public interface INotificationService
    {
        void SetWarningMessage(string message);
        void SetErrorMessage(string message);
        void SetSuccessMessage(string message);
        void SetLastPriceUpdate(DateTime? updateTime);
        string GetWarningMessage();
    }
} 