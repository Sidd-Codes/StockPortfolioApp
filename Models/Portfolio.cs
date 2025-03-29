namespace StockPortfolioApp.Models
{
    public class Portfolio
    {
        public int PortfolioId { get; set; }
        public string Name { get; set; }
        public string UserId { get; set; }
        public ApplicationUser User { get; set; } 
        public DateTime CreatedDate { get; set; }
        public List<Stock> Stocks { get; set; }
    }
}
