using StockPortfolioApp.Models;

public class PortfolioViewModel
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public decimal TotalValue { get; set; }
    public int StockCount { get; set; }
    public decimal DailyChange { get; set; }
    public List<decimal> AllocationPercentages { get; set; } = [];
    public List<StockViewModel> Stocks { get; set; } = [];
    public DateTime? LastPriceUpdate { get; set; }
    public bool RateLimitHit { get; set; }
}
