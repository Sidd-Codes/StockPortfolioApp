using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using StockPortfolioApp.Data;
using StockPortfolioApp.Models;
using StockPortfolioApp.Services;
using StockPortfolioApp.Services.Interfaces;
public class StockHoldingViewModel
{
    public required string Symbol { get; set; }
    public required string Name { get; set; }
    public decimal Shares { get; set; }
    public decimal LastPrice { get; set; }
    public decimal MarketValue { get; set; }
    public decimal PercentageOfPortfolio { get; set; }

    public decimal CostBasis { get; set; }
}

public class DashboardViewModel
{
    public decimal TotalPortfolioValue { get; set; }
    public List<StockHoldingViewModel> TopHoldings { get; set; } = new List<StockHoldingViewModel>();
    public DateTime? LastPriceUpdate { get; set; }
    public bool RateLimitHit { get; set; }
}

[Authorize]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IStockPriceService _stockPriceService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        ApplicationDbContext context, 
        IStockPriceService stockPriceService,
        ILogger<HomeController> logger)
    {
        _context = context;
        _stockPriceService = stockPriceService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try 
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            _logger.LogInformation($"Retrieving portfolio for user {userId}");

            var portfolio = await _context.Portfolios
                .Include(p => p.Stocks)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (portfolio == null || !portfolio.Stocks.Any())
            {
                _logger.LogWarning($"No portfolio found for user {userId}");
                return View(new DashboardViewModel());
            }

            _logger.LogInformation($"Found {portfolio.Stocks.Count} stocks in portfolio");

            decimal totalPortfolioValue = 0;
            var topHoldings = new List<StockHoldingViewModel>();
            bool anyPriceUpdated = false;
            bool rateLimitHit = false;

            foreach (var stock in portfolio.Stocks)
            {
                _logger.LogInformation($"Processing stock: {stock.TickerSymbol}");

                try
                {
                    var price = await _stockPriceService.GetCurrentPriceAsync(stock.TickerSymbol);
                    
                    _logger.LogInformation($"Retrieved price for {stock.TickerSymbol}: {price}");

                    if (price > 0)
                    {
                        stock.Price = price;
                        anyPriceUpdated = true;
                    }
                    else if (price == -1)
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

                var marketValue = stock.Shares * stock.Price;
                totalPortfolioValue += marketValue;

                topHoldings.Add(new StockHoldingViewModel
                {
                    Symbol = stock.TickerSymbol,
                    Name = stock.TickerSymbol,
                    Shares = stock.Shares,
                    LastPrice = stock.Price,
                    MarketValue = marketValue,
                    PercentageOfPortfolio = 0,
                    CostBasis = stock.Price * stock.Shares
                });
            }

            if (anyPriceUpdated)
            {
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation($"Total portfolio value: {totalPortfolioValue}");

            var viewModel = new DashboardViewModel
            {
                TotalPortfolioValue = totalPortfolioValue,
                TopHoldings = topHoldings.OrderBy(h => h.Symbol).ToList(),
                RateLimitHit = rateLimitHit
            };

            foreach (var holding in viewModel.TopHoldings)
            {
                if (totalPortfolioValue != 0) {
                    holding.PercentageOfPortfolio = (holding.MarketValue / totalPortfolioValue) * 100;
                }
                else {
                    holding.PercentageOfPortfolio = 0;
                }
            }

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Index method");
            return View(new DashboardViewModel());
        }
    }
}