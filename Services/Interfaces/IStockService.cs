using System.Collections.Generic;
using System.Threading.Tasks;
using StockPortfolioApp.Models;

namespace StockPortfolioApp.Services
{
    public interface IStockService
    {
        Task<IEnumerable<Stock>> GetStocksForUserAsync(string userId);
        Task<Stock> GetStockByIdAsync(int stockId);
        Task<Stock> AddStockAsync(string userId, string tickerSymbol, int shares);
        Task UpdateStockAsync(int stockId, int shares);
        Task RemoveStockAsync(int stockId);
        Task<decimal> GetPortfolioValueAsync(string userId);
        Task RefreshStockPricesAsync(string userId);
    }
}