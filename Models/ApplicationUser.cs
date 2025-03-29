using Microsoft.AspNetCore.Identity;
using StockPortfolioApp.Models; 
public class ApplicationUser : IdentityUser
{
    public Portfolio? Portfolio { get; set; }
    public string? Name { get; set; }
}
