using StockPortfolioApp.Models;
namespace StockPortfolioApp.Models
{
    public class Stock
    {
        public int StockId { get; set; }
        public int PortfolioId { get; set; }
        public required Portfolio Portfolio { get; set; }

        public required string TickerSymbol { get; set; }
        public int Shares { get; set; }
        public decimal Price { get; set; }
        public DateTime? PriceUpdatedAt { get; set; }

    }
}
