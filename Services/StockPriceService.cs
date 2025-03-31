using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using StockPortfolioApp.Services.Interfaces;

namespace StockPortfolioApp.Services
{
    public class StockPriceService : IStockPriceService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly IConfiguration _configuration;
        private readonly ILogger<StockPriceService> _logger;

        public StockPriceService(HttpClient httpClient, IConfiguration configuration, ILogger<StockPriceService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _apiKey = configuration["AlphaVantage:ApiKey"] ?? throw new ArgumentNullException("Alpha Vantage API key is missing in configuration");
        }

        public async Task<decimal> GetCurrentPriceAsync(string symbol)
        {
            try
            {
                // Use the "GLOBAL_QUOTE" endpoint for current price data
                string url = $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={symbol}&apikey={_apiKey}";
                
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Alpha Vantage API response for {symbol}: {content}");
                
                var data = JsonDocument.Parse(content);
                
                // Check for API error messages
                if (data.RootElement.TryGetProperty("Error Message", out var errorMessage))
                {
                    throw new Exception($"Alpha Vantage API error: {errorMessage.GetString()}");
                }

                // Check for API note (rate limiting)
                if (data.RootElement.TryGetProperty("Note", out var note))
                {
                    _logger.LogWarning($"Rate limit note for {symbol}: {note.GetString()}");
                    return -1; // Return -1 to indicate rate limit hit
                }

                // Extract the price from the response
                var globalQuote = data.RootElement.GetProperty("Global Quote");
                var price = decimal.Parse(globalQuote.GetProperty("05. price").GetString() ?? "0");
                
                return price;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting current price for {symbol}");
                return 0;
            }
        }

        public async Task<decimal> GetStockPriceAsync(string symbol)
        {
            // Alias for GetCurrentPriceAsync for backward compatibility
            return await GetCurrentPriceAsync(symbol);
        }

        public async Task<(decimal price, bool rateLimitHit)> GetCurrentPriceWithRateLimitAsync(string symbol)
        {
            var price = await GetCurrentPriceAsync(symbol);
            return (price, price == -1);
        }
    }
}