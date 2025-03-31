using System.Collections.Generic;
using System.Threading.Tasks;
using StockPortfolioApp.Models;



namespace StockPortfolioApp.Services.Interfaces
{
    public interface INotificationService
    {
        void AddNotification(string message, string type = "info");
        (string? message, string? type) GetNotification();
        void SetWarningMessage(string message);
        void SetErrorMessage(string message);
        void SetSuccessMessage(string message);
        void SetLastPriceUpdate(DateTime? updateTime);
        string GetWarningMessage();
    }
} 