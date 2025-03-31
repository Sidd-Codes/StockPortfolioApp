using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockPortfolioApp.Data;
using StockPortfolioApp.Models;
using StockPortfolioApp.Services.Interfaces;

namespace StockPortfolioApp.Services
{
    public abstract class BaseStockService
    {
        protected readonly ApplicationDbContext _context;
        protected readonly IStockPriceService _stockPriceService;
        protected readonly ILogger<BaseStockService> _logger;
        protected readonly INotificationService _notificationService;

        protected BaseStockService(
            ApplicationDbContext context,
            IStockPriceService stockPriceService,
            ILogger<BaseStockService> logger,
            INotificationService notificationService)
        {
            _context = context;
            _stockPriceService = stockPriceService;
            _logger = logger;
            _notificationService = notificationService;
        }

        protected async Task<decimal> GetStockPriceAsync(string symbol)
        {
            return await _stockPriceService.GetCurrentPriceAsync(symbol);
        }
    }
} 