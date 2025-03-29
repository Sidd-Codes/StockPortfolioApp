using Microsoft.AspNetCore.Mvc;
using StockPortfolioApp.Data;
using StockPortfolioApp.Models;
using StockPortfolioApp.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using StockPortfolioApp.Services.Interfaces;

namespace StockPortfolioApp.Controllers
{
    public class PortfolioController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IStockPriceService _stockPriceService;
        private readonly ILogger<PortfolioController> _logger;
        
        public PortfolioController(ApplicationDbContext context, IStockPriceService stockPriceService, ILogger<PortfolioController> logger)
        {
            _context = context;
            _stockPriceService = stockPriceService;
            _logger = logger;
        }

        // GET: Portfolio/Index
        [HttpPost]
        public async Task<IActionResult> AddStock(string tickerSymbol, int shares)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // Ensure portfolio exists
            var portfolio = await _context.Portfolios
                .Include(p => p.Stocks)
                .FirstOrDefaultAsync(p => p.UserId == userId);
            
            if (portfolio == null)
            {
                return RedirectToAction("Index");
            }

            // Get current stock price (switching to new method name)
            var price = await _stockPriceService.GetCurrentPriceAsync(tickerSymbol);
            
            if (price > 0)
            {
                // Try to find existing stock in the portfolio
                var existingStock = portfolio.Stocks
                    .FirstOrDefault(s => s.TickerSymbol.ToUpper() == tickerSymbol.ToUpper());

                if (existingStock != null)
                {
                    // Update existing stock
                    existingStock.Shares += shares;
                    existingStock.Price = price; // Update to latest price
                }
                else
                {
                    // Create new stock if not exists
                    var newStock = new Stock
                    {
                        TickerSymbol = tickerSymbol,
                        Shares = shares,
                        Price = price,
                        PortfolioId = portfolio.PortfolioId
                    };
                    _context.Stocks.Add(newStock);
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        // Method to refresh stock prices
        private async Task UpdateStockPrices(Portfolio portfolio)
        {
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

        // Modify the Index action to update prices on page load
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
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

            // Update stock prices before creating the view model
            await UpdateStockPrices(portfolio);

            var viewModel = new PortfolioViewModel
            {
                Id = portfolio.PortfolioId,
                Name = portfolio.Name,
                TotalValue = portfolio.Stocks.Sum(s => s.Shares * s.Price),
                StockCount = portfolio.Stocks.Count,
                Stocks = portfolio.Stocks.Select(s => new StockViewModel
                {
                    StockId = s.StockId,
                    TickerSymbol = s.TickerSymbol,
                    Shares = s.Shares,
                    Price = s.Price
                }).ToList()
            };

            return View(viewModel);
        }

        // GET: Portfolio/RemoveStock/{stockId}
        public async Task<IActionResult> RemoveStock(int stockId)
        {
            var stock = await _context.Stocks.FindAsync(stockId);
            if (stock == null)
            {
                return NotFound();
            }

            _context.Stocks.Remove(stock);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // Utility method to ensure the portfolio exists
        private async Task<Portfolio> EnsurePortfolioExists(string userId)
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
                };

                _context.Portfolios.Add(portfolio);
                await _context.SaveChangesAsync();
            }

            return portfolio;
        }
    }
}