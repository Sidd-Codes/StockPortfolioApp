using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockPortfolioApp.Data;
using StockPortfolioApp.Models;
using StockPortfolioApp.Services.Interfaces;

namespace StockPortfolioApp.Services
{
    public class PortfolioService : IPortfolioService
    {
        private readonly ApplicationDbContext _context;
        private readonly IStockPriceService _stockPriceService;
        private readonly ILogger<PortfolioService> _logger;

        public PortfolioService(
            ApplicationDbContext context,
            IStockPriceService stockPriceService,
            ILogger<PortfolioService> logger)
        {
            _context = context;
            _stockPriceService = stockPriceService;
            _logger = logger;
        }
        //Checks if a portfolio exists for a user otherwise it creates a new one and saves it to the database
        public async Task<Portfolio> GetOrCreatePortfolioAsync(string userId)
        {
            var portfolio = await GetPortfolioAsync(userId);
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
        //Retrieves the portfolio of a user from the database
        public async Task<Portfolio> GetPortfolioAsync(string userId)
        {
            return await _context.Portfolios
                .Include(p => p.Stocks)
                .FirstOrDefaultAsync(p => p.UserId == userId);
        }
        //Calculates and returns the total value of a user's portfolio 
        public async Task<decimal> GetPortfolioValueAsync(string userId)
        {
            var portfolio = await GetPortfolioAsync(userId);
            return portfolio?.Stocks.Sum(s => s.Shares * s.Price) ?? 0;
        }
        //Updates stock price by fetching latest price for each stock
        public async Task<(bool anyPriceUpdated, bool rateLimitHit, DateTime? lastUpdateTime)> UpdatePortfolioPricesAsync(string userId)
        {
            var portfolio = await GetPortfolioAsync(userId);
            if (portfolio == null) return (false, false, null);

            bool anyPriceUpdated = false;
            bool rateLimitHit = false;
            DateTime? lastUpdateTime = null;

            foreach (var stock in portfolio.Stocks)
            {
                try
                {
                    var currentPrice = await _stockPriceService.GetCurrentPriceAsync(stock.TickerSymbol);
                    if (currentPrice > 0)
                    {
                        stock.Price = currentPrice;
                        anyPriceUpdated = true;
                        lastUpdateTime = DateTime.UtcNow;
                    }
                    else if (currentPrice == -1)
                    {
                        _logger.LogWarning($"Rate limit hit for {stock.TickerSymbol}");
                        anyPriceUpdated = true;
                        rateLimitHit = true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Failed to update price for {stock.TickerSymbol}, using existing price");
                }
            }

            if (anyPriceUpdated)
            {
                await _context.SaveChangesAsync();
            }

            return (anyPriceUpdated, rateLimitHit, lastUpdateTime);
        }
    }
} 