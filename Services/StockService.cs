using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockPortfolioApp.Data;
using StockPortfolioApp.Models;
using StockPortfolioApp.Services.Interfaces;

namespace StockPortfolioApp.Services
{
    public class StockService : IStockService
    {
        private readonly ApplicationDbContext _context;
        private readonly IStockPriceService _stockPriceService;
        private readonly ILogger<StockService> _logger;

        public StockService(
            ApplicationDbContext context, 
            IStockPriceService stockPriceService, 
            ILogger<StockService> logger)
        {
            _context = context;
            _stockPriceService = stockPriceService;
            _logger = logger;
        }

        public async Task<IEnumerable<Stock>> GetStocksForUserAsync(string userId)
        {
            try
            {
                var portfolio = await GetOrCreatePortfolioAsync(userId);
                return portfolio.Stocks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving stocks for user {userId}");
                return new List<Stock>();
            }
        }

        public async Task<Stock> GetStockByIdAsync(int stockId)
        {
            return await _context.Stocks.FindAsync(stockId);
        }

        public async Task<Stock> AddStockAsync(string userId, string tickerSymbol, int shares)
        {
            try
            {
                var portfolio = await GetOrCreatePortfolioAsync(userId);
                
                // Get current price
                var price = await _stockPriceService.GetCurrentPriceAsync(tickerSymbol);
                
                if (price <= 0)
                {
                    throw new Exception($"Could not retrieve valid price for {tickerSymbol}");
                }
                
                // Check if stock already exists in portfolio
                var existingStock = portfolio.Stocks
                    .FirstOrDefault(s => s.TickerSymbol.ToUpper() == tickerSymbol.ToUpper());
                
                if (existingStock != null)
                {
                    // Update existing stock
                    existingStock.Shares += shares;
                    existingStock.Price = price; // Update to latest price
                    await _context.SaveChangesAsync();
                    return existingStock;
                }
                else
                {
                    // Create new stock
                    var newStock = new Stock
                    {
                        TickerSymbol = tickerSymbol.ToUpper(),
                        Shares = shares,
                        Price = price,
                        PortfolioId = portfolio.PortfolioId
                    };
                    
                    _context.Stocks.Add(newStock);
                    await _context.SaveChangesAsync();
                    return newStock;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding stock {tickerSymbol} for user {userId}");
                throw;
            }
        }

        public async Task UpdateStockAsync(int stockId, int shares)
        {
            var stock = await _context.Stocks.FindAsync(stockId);
            if (stock == null)
            {
                throw new KeyNotFoundException($"Stock with ID {stockId} not found");
            }
            
            stock.Shares = shares;
            
            // Update price
            var currentPrice = await _stockPriceService.GetCurrentPriceAsync(stock.TickerSymbol);
            if (currentPrice > 0)
            {
                stock.Price = currentPrice;
            }
            
            await _context.SaveChangesAsync();
        }

        public async Task RemoveStockAsync(int stockId)
        {
            var stock = await _context.Stocks.FindAsync(stockId);
            if (stock == null)
            {
                throw new KeyNotFoundException($"Stock with ID {stockId} not found");
            }
            
            _context.Stocks.Remove(stock);
            await _context.SaveChangesAsync();
        }

        public async Task<decimal> GetPortfolioValueAsync(string userId)
        {
            var portfolio = await GetOrCreatePortfolioAsync(userId);
            return portfolio.Stocks.Sum(s => s.Shares * s.Price);
        }

        public async Task RefreshStockPricesAsync(string userId)
        {
            var portfolio = await GetOrCreatePortfolioAsync(userId);
            
            foreach (var stock in portfolio.Stocks)
            {
                var currentPrice = await _stockPriceService.GetCurrentPriceAsync(stock.TickerSymbol);
                if (currentPrice > 0)
                {
                    stock.Price = currentPrice;
                }
            }
            
            await _context.SaveChangesAsync();
        }

        private async Task<Portfolio> GetOrCreatePortfolioAsync(string userId)
        {
            var portfolio = await _context.Portfolios
                .Include(p => p.Stocks)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (portfolio == null)
            {
                portfolio = new Portfolio
                {
                    UserId = userId,
                    Name = "My Portfolio",
                    CreatedDate = DateTime.UtcNow,
                    Stocks = new List<Stock>()
                };
                
                _context.Portfolios.Add(portfolio);
                await _context.SaveChangesAsync();
            }

            return portfolio;
        }
    }
}