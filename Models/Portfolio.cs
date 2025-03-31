namespace StockPortfolioApp.Models
{
    public class Portfolio
    {
        public int PortfolioId { get; set; }
        public required string Name { get; set; }
        public required string UserId { get; set; }
        public required ApplicationUser User { get; set; } 
        public DateTime CreatedDate { get; set; }
        public List<Stock> Stocks { get; set; } = [];
    }
}
