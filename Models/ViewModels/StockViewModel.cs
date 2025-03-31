namespace StockPortfolioApp.Models
{
    public class StockViewModel
    {
        public int StockId { get; set; }
        public required string TickerSymbol { get; set; }
        public int Shares { get; set; }
        public decimal Price { get; set; }
        public decimal MarketValue => Shares * Price;
        public DateTime? PriceUpdatedAt { get; set; }

    }

}
