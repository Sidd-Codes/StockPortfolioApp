using System.ComponentModel.DataAnnotations;

public class AddStockViewModel
{
    public int PortfolioId { get; set; }

    [Required(ErrorMessage = "Ticker Symbol is required")]
    [StringLength(10, ErrorMessage = "Ticker Symbol cannot be longer than 10 characters")]
    public required string TickerSymbol { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Shares must be at least 1")]
    public int Shares { get; set; }
}