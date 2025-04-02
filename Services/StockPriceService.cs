using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using StockPortfolioApp.Data;
using StockPortfolioApp.Services.Interfaces;

namespace StockPortfolioApp.Services
{
    public class StockPriceService : IStockPriceService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly IConfiguration _configuration;
        private readonly ILogger<StockPriceService> _logger;
        private readonly ApplicationDbContext _context;

        public StockPriceService(
            HttpClient httpClient, 
            IConfiguration configuration, 
            ILogger<StockPriceService> logger,
            ApplicationDbContext context)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _context = context;
            _apiKey = configuration["AlphaVantage:ApiKey"] ?? throw new ArgumentNullException("Alpha Vantage API key is missing in configuration");
        }

        public async Task<decimal> GetCurrentPriceAsync(string symbol)
        {
            try
            {
                string url = $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={symbol}&apikey={_apiKey}";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Alpha Vantage API response for {symbol}: {content}");

                var data = JsonDocument.Parse(content);

                if (data.RootElement.TryGetProperty("Error Message", out var errorMessage))
                {
                    _logger.LogWarning($"Alpha Vantage API error: {errorMessage.GetString()}");
                    return await GetLastKnownPriceAsync(symbol);
                }

                if (data.RootElement.TryGetProperty("Note", out var note))
                {
                    _logger.LogWarning($"Rate limit note for {symbol}: {note.GetString()}");
                    return await GetLastKnownPriceAsync(symbol);
                }

                if (data.RootElement.TryGetProperty("Global Quote", out var globalQuote))
                {
                    if (globalQuote.TryGetProperty("05. price", out var priceElement))
                    {
                        var price = decimal.Parse(priceElement.GetString() ?? "0");
                        return price;
                    }
                    else
                    {
                        _logger.LogWarning($"Price information for symbol '{symbol}' not found.");
                        return await GetLastKnownPriceAsync(symbol);
                    }
                }
                else
                {
                    _logger.LogWarning($"Error getting current price for '{symbol}'. Symbol may be invalid or API limit may be reached.");
                    return await GetLastKnownPriceAsync(symbol);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting current price for {symbol}");
                return await GetLastKnownPriceAsync(symbol);
            }
        }

        private async Task<decimal> GetLastKnownPriceAsync(string symbol)
        {
            try
            {
                var existingStock = await _context.Stocks
                    .FirstOrDefaultAsync(s => s.TickerSymbol.ToUpper() == symbol.ToUpper());

                if (existingStock != null)
                {
                    _logger.LogInformation($"Using last known price {existingStock.Price} for {symbol}");
                    return existingStock.Price;
                }
                
                _logger.LogWarning($"No historical price found for {symbol}, using default price");
                return 1.00m;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting last known price for {symbol}");
                return 1.00m;
            }
        }

        public async Task<(decimal price, bool rateLimitHit)> GetCurrentPriceWithRateLimitAsync(string symbol)
        {
            try
            {
                string url = $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={symbol}&apikey={_apiKey}";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var data = JsonDocument.Parse(content);

                bool rateLimitHit = false;
                decimal price = 0;

                if (data.RootElement.TryGetProperty("Note", out var note) && 
                    note.GetString().Contains("rate limit"))
                {
                    rateLimitHit = true;
                    price = await GetLastKnownPriceAsync(symbol);
                    return (price, rateLimitHit);
                }

                if (data.RootElement.TryGetProperty("Error Message", out var errorMessage))
                {
                    rateLimitHit = true;
                    price = await GetLastKnownPriceAsync(symbol);
                    return (price, rateLimitHit);
                }

                if (data.RootElement.TryGetProperty("Global Quote", out var globalQuote))
                {
                    if (globalQuote.TryGetProperty("05. price", out var priceElement))
                    {
                        price = decimal.Parse(priceElement.GetString() ?? "0");
                        return (price, false);
                    }
                }
                
                price = await GetLastKnownPriceAsync(symbol);
                return (price, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetCurrentPriceWithRateLimitAsync for {symbol}");
                var price = await GetLastKnownPriceAsync(symbol);
                return (price, true);
            }
        }
    }
}