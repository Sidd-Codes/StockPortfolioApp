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

        private async Task<decimal> GetStockPriceAsync(string symbol)
        {
            return await _stockPriceService.GetCurrentPriceAsync(symbol);
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
                var portfolio = await _portfolioService.GetOrCreatePortfolioAsync(userId);
                var price = await GetStockPriceAsync(tickerSymbol);
                DateTime? priceTimestamp = null;
                
                // If API call failed (price <= 0), try to get last known price from database
                if (price <= 0)
                {
                    var existingStockWithSymbol = await _context.Stocks
                        .FirstOrDefaultAsync(s => s.TickerSymbol.ToUpper() == tickerSymbol.ToUpper());
                    
                    if (existingStockWithSymbol != null)
                    {
                        price = existingStockWithSymbol.Price;
                        priceTimestamp = existingStockWithSymbol.PriceUpdatedAt;
                        _notificationService.SetWarningMessage($"Using last known price for {tickerSymbol} due to API unavailability");
                    }
                    else if (!_notificationService.GetWarningMessage().Contains("default price"))
                    {
                        throw new Exception($"Could not retrieve valid price for {tickerSymbol} and no historical price available");
                    }
                }
                else
                {
                    priceTimestamp = DateTime.UtcNow;
                }
                
                var existingStock = portfolio.Stocks
                    .FirstOrDefault(s => s.TickerSymbol.ToUpper() == tickerSymbol.ToUpper());
                
                if (existingStock != null)
                {
                    existingStock.Shares += shares;
                    if (price > 0)
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

        public async Task UpdateStockAsync(int stockId, int shares)
        {
            var stock = await _context.Stocks.FindAsync(stockId);
            if (stock == null)
            {
                throw new KeyNotFoundException($"Stock with ID {stockId} not found");
            }
            
            stock.Shares = shares;
            
            var currentPrice = await GetStockPriceAsync(stock.TickerSymbol);
            if (currentPrice > 0)
            {
                stock.Price = currentPrice;
            }
            
            await _context.SaveChangesAsync();
        }

        public async Task RemoveStockAsync(int stockId)
        {
            var stock = await GetStockByIdAsync(stockId);
            if (stock == null)
            {
                throw new KeyNotFoundException($"Stock with ID {stockId} not found");
            }

            _context.Stocks.Remove(stock);
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

        public async Task<decimal> GetPortfolioValueAsync(string userId)
        {
            var portfolio = await GetOrCreatePortfolioAsync(userId);
            return portfolio.Stocks.Sum(s => s.Shares * s.Price);
        }

        public async Task RefreshStockPricesAsync(string userId)
        {
            var portfolio = await GetOrCreatePortfolioAsync(userId);
            bool anyPriceUpdated = false;
            
            foreach (var stock in portfolio.Stocks)
            {
                var currentPrice = await GetStockPriceAsync(stock.TickerSymbol);
                if (currentPrice > 0)
                {
                    stock.Price = currentPrice;
                    anyPriceUpdated = true;
                }
                else if (currentPrice == -1)
                {
                    _logger.LogWarning($"Rate limit hit for {stock.TickerSymbol}");
                    anyPriceUpdated = true;
                }
            }
            
            if (anyPriceUpdated)
            {
                await _context.SaveChangesAsync();
            }
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