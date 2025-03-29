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
    public string Symbol { get; set; }
    public string Name { get; set; }
    public decimal Shares { get; set; }
    public decimal LastPrice { get; set; }
    public decimal MarketValue { get; set; }
    public decimal PercentageOfPortfolio { get; set; }

    public decimal CostBasis { get; set; }
}

public class DashboardViewModel
{
    public decimal TotalPortfolioValue { get; set; }
    public decimal DailyChange { get; set; }
    public decimal DailyChangePercentage { get; set; }
    public int ActivePortfolios { get; set; }
    public List<StockHoldingViewModel> TopHoldings { get; set; } = new List<StockHoldingViewModel>();
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
            decimal previousTotalValue = 0;
            var topHoldings = new List<StockHoldingViewModel>();

            foreach (var stock in portfolio.Stocks)
            {
                _logger.LogInformation($"Processing stock: {stock.TickerSymbol}");

                // Get current price
                var price = await _stockPriceService.GetCurrentPriceAsync(stock.TickerSymbol);
                
                _logger.LogInformation($"Retrieved price for {stock.TickerSymbol}: {price}");

                // Skip if price is 0
                if (price == 0)
                {
                    _logger.LogWarning($"Price retrieval failed for {stock.TickerSymbol}");
                    continue;
                }

                stock.Price = price;

                var marketValue = stock.Shares * price;
                totalPortfolioValue += marketValue;

                topHoldings.Add(new StockHoldingViewModel
                {
                    Symbol = stock.TickerSymbol,
                    Name = stock.TickerSymbol,
                    Shares = stock.Shares,
                    LastPrice = price,
                    MarketValue = marketValue,
                    PercentageOfPortfolio = 0,
                    CostBasis = stock.Price * stock.Shares
                });
            }

            _logger.LogInformation($"Total portfolio value: {totalPortfolioValue}");

            var dailyChange = topHoldings.Sum(h => h.LastPrice * h.Shares) - previousTotalValue;
            decimal dailyChangePercentage = 0;
            if (previousTotalValue > 0)
            {
                dailyChangePercentage = (dailyChange / previousTotalValue) * 100;
            }

            var viewModel = new DashboardViewModel
            {
                TotalPortfolioValue = totalPortfolioValue,
                DailyChange = dailyChange,
                DailyChangePercentage = dailyChangePercentage,
                ActivePortfolios = 1,
                TopHoldings = topHoldings.OrderByDescending(h => h.MarketValue).ToList()
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