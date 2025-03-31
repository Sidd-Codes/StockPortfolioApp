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
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace StockPortfolioApp.Controllers
{
    [Authorize]
    public class PortfolioController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IStockPriceService _stockPriceService;
        private readonly IStockService _stockService;
        private readonly ILogger<PortfolioController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        public PortfolioController(
            ApplicationDbContext context, 
            IStockPriceService stockPriceService,
            IStockService stockService,
            ILogger<PortfolioController> logger,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _stockPriceService = stockPriceService;
            _stockService = stockService;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Portfolio/Index
        [HttpPost]
        public async Task<IActionResult> AddStock(string tickerSymbol, int shares)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            try 
            {
                await _stockService.AddStockAsync(userId, tickerSymbol, shares);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding stock");
                ModelState.AddModelError(string.Empty, "An error occurred while adding the stock. Please try again.");
                return RedirectToAction("Index");
            }
        }

        // Method to refresh stock prices
        private async Task<(bool anyPriceUpdated, bool rateLimitHit, DateTime? lastUpdateTime)> UpdateStockPrices(Portfolio portfolio)
        {
            bool anyPriceUpdated = false;
            bool rateLimitHit = false;
            DateTime? lastUpdateTime = null;
            try
            {
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
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update stock prices, using existing prices");
            }
            return (anyPriceUpdated, rateLimitHit, lastUpdateTime);
        }

        // Modify the Index action to update prices on page load
        public async Task<IActionResult> Index()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Identity/Account/Login", new { returnUrl = Url.Action("Index", "Portfolio") });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var portfolio = await _context.Portfolios
                .Include(p => p.Stocks)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (portfolio == null)
            {
                var user = await _userManager.GetUserAsync(User);
                portfolio = new Portfolio
                {
                    UserId = userId,
                    Name = "My Portfolio",
                    CreatedDate = DateTime.UtcNow,
                    Stocks = new List<Stock>(),
                    User = user
                };
                _context.Portfolios.Add(portfolio);
                await _context.SaveChangesAsync();
            }

            var (anyPriceUpdated, rateLimitHit, lastUpdateTime) = await UpdateStockPrices(portfolio);

            var viewModel = new PortfolioViewModel
            {
                Id = portfolio.PortfolioId,
                Name = portfolio.Name,
                TotalValue = portfolio.Stocks.Sum(s => s.Shares * s.Price),
                StockCount = portfolio.Stocks.Count,
                LastPriceUpdate = lastUpdateTime,
                RateLimitHit = rateLimitHit,
                Stocks = portfolio.Stocks.Select(s => new StockViewModel
                {
                    StockId = s.StockId,
                    TickerSymbol = s.TickerSymbol,
                    Shares = s.Shares,
                    Price = s.Price,
                    PriceUpdatedAt = s.PriceUpdatedAt
                }).ToList()
            };

            return View(viewModel);
        }

        // GET: Portfolio/RemoveStock/{stockId}
        public async Task<IActionResult> RemoveStock(int stockId, int quantityToRemove)
        {
            var user = await _userManager.GetUserAsync(User);
            var portfolio = await _context.Portfolios
                                        .Include(p => p.Stocks)
                                        .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (portfolio == null)
            {
                ModelState.AddModelError(string.Empty, "Portfolio not found.");
                return RedirectToAction("Index");
            }

            var stock = portfolio.Stocks.FirstOrDefault(s => s.StockId == stockId);
            
            if (stock == null)
            {
                ModelState.AddModelError(string.Empty, "Stock not found in your portfolio.");
                return RedirectToAction("Index");
            }

            if (stock.Shares < quantityToRemove)
            {
                ModelState.AddModelError(string.Empty, "Not enough shares to remove.");
                return RedirectToAction("Index");
            }

            stock.Shares -= quantityToRemove;

            if (stock.Shares == 0)
            {
                _context.Stocks.Remove(stock);
            }

            await _context.SaveChangesAsync();
            
            return RedirectToAction("Index");
        }

        // POST: Portfolio/UpdateStock/{stockId}
        [HttpPost]
        public async Task<IActionResult> UpdateStock(int stockId, int newQuantity)
        {
            try
            {
                await _stockService.UpdateStockAsync(stockId, newQuantity);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating stock");
                ModelState.AddModelError(string.Empty, "An error occurred while updating the stock. Please try again.");
                return RedirectToAction("Index");
            }
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
                    User = await _context.Users.FindAsync(userId)
                };

                _context.Portfolios.Add(portfolio);
                await _context.SaveChangesAsync();
            }

            return portfolio;
        }
    }
}