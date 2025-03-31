# Stock Portfolio App

A web application for managing and tracking your stock portfolio. Built with ASP.NET Core MVC.

## Features

- User authentication and authorization
- Add and remove stocks from your portfolio
- Track number of shares and current market value
- Real-time stock price updates using Alpha Vantage API
- Portfolio value tracking
- Detailed holdings view with percentage allocation

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- SQL Server (Azure SQL Database or local)
- Alpha Vantage API key (for stock price data)

## Setup Instructions

1. Clone the repository:
```bash
git clone <repository-url>
cd StockPortfolioApp
```

2. Update the connection string in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-server;Database=your-database;User Id=your-username;Password=your-password;"
  }
}
```

3. Update the Alpha Vantage API key in `appsettings.json`:
```json
{
  "AlphaVantage": {
    "ApiKey": "your-api-key"
  }
}
```
You can get an API key from [Alpha Vantage](https://www.alphavantage.co/support/#api-key).

4. Run the database migrations:
```bash
dotnet ef database update
```

5. Run the application:
```bash
dotnet run
```

6. Open your browser and navigate to:
```
https://localhost:5001
```

## Project Structure

- `Controllers/` - Contains the application's controllers
  - `HomeController.cs` - Handles the dashboard view
  - `PortfolioController.cs` - Manages portfolio operations
- `Models/` - Contains the data models
  - `Portfolio.cs` - Portfolio entity
  - `Stock.cs` - Stock entity
- `Views/` - Contains the Razor views
  - `Home/` - Dashboard views
  - `Portfolio/` - Portfolio management views
- `Services/` - Contains business logic and external service integrations
  - `StockPriceService.cs` - Handles stock price updates via Alpha Vantage API

## Dependencies

- ASP.NET Core MVC
- Entity Framework Core
- Microsoft.AspNetCore.Identity
- Alpha Vantage API

## Configuration

The application uses the following configuration in `appsettings.json`:

- Database connection string
- Alpha Vantage API key
- Email settings
- Logging configuration

## Development

To run the application in development mode:

```bash
dotnet run
```

This will enable hot reloading for development.

## Deployment

The application is configured for deployment to Azure App Services. Make sure to:

1. Set up an Azure SQL Database
2. Configure the connection string in Azure App Service settings
3. Set the Alpha Vantage API key in Azure App Service settings
4. Deploy using Visual Studio or Azure CLI