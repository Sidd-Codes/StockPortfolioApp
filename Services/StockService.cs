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
        private readonly INotificationService _notificationService;
        private readonly IPortfolioService _portfolioService;

        public StockService(
            ApplicationDbContext context,
            IStockPriceService stockPriceService,
            ILogger<StockService> logger,
            INotificationService notificationService,
            IPortfolioService portfolioService)
        {
            _context = context;
            _stockPriceService = stockPriceService;
            _logger = logger;
            _notificationService = notificationService;
            _portfolioService = portfolioService;
        }
        //Retrieves price of stock by calling IStockPriceService
        private async Task<(decimal price, bool isRateLimited)> GetStockPriceAsync(string symbol)
        {
            try
            {
                var result = await _stockPriceService.GetCurrentPriceWithRateLimitAsync(symbol);
                
                if (result.rateLimitHit)
                {
                    _notificationService.SetWarningMessage($"Using cached or default price for {symbol} due to API limitations");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error getting price for {symbol}");
                
                var existingStock = await _context.Stocks
                    .FirstOrDefaultAsync(s => s.TickerSymbol.ToUpper() == symbol.ToUpper());
                
                decimal price = existingStock?.Price ?? 1.00m;
                _notificationService.SetWarningMessage($"Using fallback price for {symbol} due to unexpected error");
                
                return (price, true);
            }
        }
        //Retrieves stocks from a user's portfolio
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
        //Adds a stock to user's portfolio
        public async Task<Stock> AddStockAsync(string userId, string tickerSymbol, int shares)
        {
            try
            {
                var portfolio = await _portfolioService.GetOrCreatePortfolioAsync(userId);
                
                var priceResult = await GetStockPriceAsync(tickerSymbol);
                var price = priceResult.price;
                var isRateLimited = priceResult.isRateLimited;
                
                DateTime priceTimestamp = isRateLimited ? 
                    DateTime.UtcNow.AddDays(-1) :   
                    DateTime.UtcNow;
                
                var existingStock = portfolio.Stocks
                    .FirstOrDefault(s => s.TickerSymbol.ToUpper() == tickerSymbol.ToUpper());
                
                if (existingStock != null)
                {
                    existingStock.Shares += shares;
                    
                    if (!isRateLimited)
                    {
                        existingStock.Price = price;
                        existingStock.PriceUpdatedAt = priceTimestamp;
                    }
                    
                    await _context.SaveChangesAsync();
                    return existingStock;
                }
                else
                {
                    var newStock = new Stock
                    {
                        TickerSymbol = tickerSymbol.ToUpper(),
                        Shares = shares,
                        Price = price,
                        PriceUpdatedAt = priceTimestamp,
                        PortfolioId = portfolio.PortfolioId,
                        Portfolio = portfolio
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
        //Updates share count for existing stock
        public async Task UpdateStockAsync(int stockId, int shares)
        {
            var stock = await _context.Stocks.FindAsync(stockId);
            if (stock == null)
            {
                throw new KeyNotFoundException($"Stock with ID {stockId} not found");
            }
            
            stock.Shares = shares;
            
            try
            {
                var priceResult = await GetStockPriceAsync(stock.TickerSymbol);
                
                if (!priceResult.isRateLimited)
                {
                    stock.Price = priceResult.price;
                    stock.PriceUpdatedAt = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error updating price for {stock.TickerSymbol}");
            }
            
            await _context.SaveChangesAsync();
        }

        public async Task RemoveStockQuantityAsync(int stockId, int quantityToRemove)
        {
            var stock = await GetStockByIdAsync(stockId);
            if (stock == null)
            {
                throw new KeyNotFoundException($"Stock with ID {stockId} not found");
            }

            if (stock.Shares < quantityToRemove)
            {
                throw new InvalidOperationException("Not enough shares to remove");
            }

            stock.Shares -= quantityToRemove;
            if (stock.Shares == 0)
            {
                _context.Stocks.Remove(stock);
            }

            await _context.SaveChangesAsync();
        }
        //Gets total portfolio value
        public async Task<decimal> GetPortfolioValueAsync(string userId)
        {
            var portfolio = await GetOrCreatePortfolioAsync(userId);
            return portfolio.Stocks.Sum(s => s.Shares * s.Price);
        }
        //Calls GetStockPriceAsync to refresh stock prices
        public async Task RefreshStockPricesAsync(string userId)
        {
            var portfolio = await GetOrCreatePortfolioAsync(userId);
            bool anyPriceUpdated = false;
            int rateLimitedCount = 0;
            
            foreach (var stock in portfolio.Stocks)
            {
                var priceResult = await GetStockPriceAsync(stock.TickerSymbol);
                
                if (!priceResult.isRateLimited)
                {
                    stock.Price = priceResult.price;
                    stock.PriceUpdatedAt = DateTime.UtcNow;
                    anyPriceUpdated = true;
                }
                else
                {
                    rateLimitedCount++;
                }
            }
            
            if (anyPriceUpdated)
            {
                await _context.SaveChangesAsync();
            }
            
            if (rateLimitedCount > 0)
            {
                _notificationService.SetWarningMessage($"{rateLimitedCount} stock prices could not be updated due to API limitations");
            }
        }
        //Creates a portfolio for a user if it doesn't exist
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
                    Stocks = new List<Stock>(),
                    User = await _context.Users.FindAsync(userId)
                };
                
                _context.Portfolios.Add(portfolio);
                await _context.SaveChangesAsync();
            }

            return portfolio;
        }
    }
}