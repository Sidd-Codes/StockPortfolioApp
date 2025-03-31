using System.Collections.Generic;
using System.Threading.Tasks;
using StockPortfolioApp.Models;

namespace StockPortfolioApp.Services.Interfaces
{
    public interface IStockService
    {
        Task<IEnumerable<Stock>> GetStocksForUserAsync(string userId);
        Task<Stock> GetStockByIdAsync(int stockId);
        Task<Stock> AddStockAsync(string userId, string tickerSymbol, int shares);
        Task UpdateStockAsync(int stockId, int shares);
        Task RemoveStockAsync(int stockId);
        Task RemoveStockQuantityAsync(int stockId, int quantityToRemove);
        Task<decimal> GetPortfolioValueAsync(string userId);
        Task RefreshStockPricesAsync(string userId);
    }
}