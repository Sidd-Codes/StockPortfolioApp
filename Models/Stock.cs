using StockPortfolioApp.Models;  // Add this at the top of the file
namespace StockPortfolioApp.Models
{
    public class Stock
    {
        public int StockId { get; set; }
        public int PortfolioId { get; set; }
        public Portfolio Portfolio { get; set; }  // Portfolio class reference

        public string TickerSymbol { get; set; }
        public int Shares { get; set; }
        public decimal Price { get; set; }
    }
}
