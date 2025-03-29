using System.Threading.Tasks;

namespace StockPortfolioApp.Services.Interfaces
{
    public interface IStockPriceService
    {
        Task<decimal> GetCurrentPriceAsync(string symbol);
    }
}