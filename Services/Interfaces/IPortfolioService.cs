using System.Threading.Tasks;
using StockPortfolioApp.Models;

namespace StockPortfolioApp.Services.Interfaces
{
    public interface IPortfolioService
    {
        Task<Portfolio> GetOrCreatePortfolioAsync(string userId);
        Task<Portfolio> GetPortfolioAsync(string userId);
        Task<decimal> GetPortfolioValueAsync(string userId);
        Task<(bool anyPriceUpdated, bool rateLimitHit, DateTime? lastUpdateTime)> UpdatePortfolioPricesAsync(string userId);
    }
} 