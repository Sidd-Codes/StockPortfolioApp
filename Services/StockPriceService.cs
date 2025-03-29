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

        public StockPriceService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
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
                var data = JsonDocument.Parse(content);
                
                // Extract the price from the "Global Quote" object
                if (data.RootElement.TryGetProperty("Global Quote", out var quote) &&
                    quote.TryGetProperty("05. price", out var priceElement))
                {
                    if (decimal.TryParse(priceElement.GetString(), out decimal price))
                    {
                        return price;
                    }
                }
                
                throw new Exception($"Failed to parse price data for {symbol}");
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error getting price for {symbol}: {ex.Message}");
                throw;
            }
        }
    }
}