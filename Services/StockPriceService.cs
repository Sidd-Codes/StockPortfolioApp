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
                string url = $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={symbol}&apikey={_apiKey}";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Alpha Vantage API response for {symbol}: {content}");

                var data = JsonDocument.Parse(content);

                if (data.RootElement.TryGetProperty("Error Message", out var errorMessage))
                {
                    throw new Exception($"Alpha Vantage API error: {errorMessage.GetString()}");
                }

                if (data.RootElement.TryGetProperty("Note", out var note))
                {
                    _logger.LogWarning($"Rate limit note for {symbol}: {note.GetString()}");
                    throw new Exception("API rate limit has been reached. Please try again later.");
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
                        throw new Exception($"Price information for symbol '{symbol}' not found.");
                    }
                }
                else
                {
                    throw new Exception($"Error getting current price for '{symbol}'. Symbol may be invalid or API limit may be reached.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting current price for {symbol}");
                throw;
            }
        }


        public async Task<(decimal price, bool rateLimitHit)> GetCurrentPriceWithRateLimitAsync(string symbol)
        {
            try
            {
                var price = await GetCurrentPriceAsync(symbol);
                bool rateLimitHit = price == -1;
                return (price, rateLimitHit);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("rate limit"))
                {
                    _logger.LogWarning($"Rate limit hit while fetching price for symbol {symbol}");
                }
                else
                {
                    _logger.LogError($"Failed to fetch price for symbol {symbol}: {ex.Message}");
                }

                throw new Exception($"Error retrieving price for {symbol}: {ex.Message}");
            }
        }

    }
}