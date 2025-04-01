using System.Threading.Tasks;

namespace StockPortfolioApp.Services.Interfaces
{
    public interface IStockPriceService
    {
        Task<decimal> GetCurrentPriceAsync(string symbol);
        Task<decimal> GetStockPriceAsync(string symbol);
        Task<(decimal price, bool rateLimitHit)> GetCurrentPriceWithRateLimitAsync(string symbol);
    }
}