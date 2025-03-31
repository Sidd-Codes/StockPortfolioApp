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