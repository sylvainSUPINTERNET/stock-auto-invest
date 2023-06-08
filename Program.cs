using Alpaca.Markets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Services;
using Services.Trade;
using System;
using YahooFinanceApi;


// strategy : https://alpaca.markets/learn/mean-reversion-stock-trading-csharp/
// formula for SMA ( 20 / 50 ) : https://www.investopedia.com/terms/s/sma.asp

// period can be days or minute based on the requirements / volatility of the stock ...

// Compare the values of the shorter-term SMA and the longer-term SMA : 
// If the shorter-term SMA is above the longer-term SMA, it indicates a potential bullish crossover, which suggests a buying opportunity.
// If the shorter-term SMA is below the longer-term SMA, it indicates a potential bearish crossover, which suggests a selling opportunity.



var builder = WebApplication.CreateBuilder(args);


// Beware, you cannot use provider to get the service, it will not initialize if build() is not called ! - you have to instanciate by yourself the instance !
builder.Services.AddScoped<IComputeSMA, ComputeSMA>();
builder.Services.AddHostedService<TradeBackgroundService>( provider => {
    IAlpacaTradingClient? client = null;
    // will be limited in free tier ! 
    IAlpacaDataClient? dataClient = null;

    try {

        String alpacaSecretKey = builder.Configuration.GetSection("Alpaca").GetSection("ApiKeySecret").Value;
        String alpacaSecretPublic = builder.Configuration.GetSection("Alpaca").GetSection("ApiKeyPublic").Value;

        if ( string.IsNullOrEmpty(alpacaSecretKey) || string.IsNullOrEmpty(alpacaSecretPublic) ) 
        {
            throw new Exception("Alpaca API Key not found in appsettings.json");
        }
        
        Console.WriteLine("Alpaca API Key found in appsettings.json starting new client ...");



        if ( builder.Environment.IsDevelopment() ) 
            {
                Console.WriteLine("Development environment detected - Alpaca Paper client will be used");
                client = Alpaca.Markets.Environments.Paper.GetAlpacaTradingClient(new SecretKey(alpacaSecretPublic, alpacaSecretKey));
                dataClient = Alpaca.Markets.Environments.Paper.GetAlpacaDataClient(new SecretKey(alpacaSecretPublic, alpacaSecretKey));
            }


        if ( builder.Environment.IsProduction() ) 
            {
                Console.WriteLine("Production environment detected - Alpaca Live client will be used");
                client = Alpaca.Markets.Environments.Live.GetAlpacaTradingClient(new SecretKey(alpacaSecretPublic, alpacaSecretKey));
                dataClient = Alpaca.Markets.Environments.Live.GetAlpacaDataClient(new SecretKey(alpacaSecretPublic, alpacaSecretKey));
            }

        if ( client == null || dataClient == null ) 
            {
                throw new Exception("Alpaca client failed to be created !");
            }


        // var clock = await client.GetClockAsync();

        //     if (clock != null)
        //     {
        //         Console.WriteLine(
        //             "Timestamp: {0}, NextOpen: {1}, NextClose: {2}",
        //             clock.TimestampUtc, clock.NextOpenUtc, clock.NextCloseUtc);
        //     }

    } catch ( Exception ex ) {
        Console.WriteLine($"Error : {ex.Message}");
    }

    if ( client == null || dataClient == null ) 
        {
            throw new Exception("Alpaca client failed to be created !");
        }

    return new TradeBackgroundService(provider.GetRequiredService<ILogger<TradeBackgroundService>>(), client, dataClient, new ComputeSMA(provider.GetRequiredService<ILogger<ComputeSMA>>()));

    
    // init background service !
});

var app = builder.Build();
app.MapGet("/", () => "Hello World!");
app.Run();



    // String alpacaSecretKey = builder.Configuration.GetSection("Alpaca").GetSection("ApiKeySecret").Value;
    // String alpacaSecretPublic = builder.Configuration.GetSection("Alpaca").GetSection("ApiKeyPublic").Value;

    // if ( string.IsNullOrEmpty(alpacaSecretKey) || string.IsNullOrEmpty(alpacaSecretPublic) ) 
    //     {
    //         throw new Exception("Alpaca API Key not found in appsettings.json");
    //     }

    //     app.Logger.LogInformation("Alpaca API Key found in appsettings.json starting new client ...");

    //     IAlpacaTradingClient? client = null;

    //     // will be limited in free tier ! 
    //     IAlpacaDataClient? dataClient = null;

    //     if ( builder.Environment.IsDevelopment() ) 
    //         {
    //             app.Logger.LogInformation("Development environment detected - Alpaca Paper client will be used");
    //             client = Alpaca.Markets.Environments.Paper.GetAlpacaTradingClient(new SecretKey(alpacaSecretPublic, alpacaSecretKey));
    //             dataClient = Alpaca.Markets.Environments.Paper.GetAlpacaDataClient(new SecretKey(alpacaSecretPublic, alpacaSecretKey));
    //         }


    //     if ( builder.Environment.IsProduction() ) 
    //         {
    //             app.Logger.LogWarning("Production environment detected - Alpaca Live client will be used");
    //             client = Alpaca.Markets.Environments.Live.GetAlpacaTradingClient(new SecretKey(alpacaSecretPublic, alpacaSecretKey));
    //             dataClient = Alpaca.Markets.Environments.Live.GetAlpacaDataClient(new SecretKey(alpacaSecretPublic, alpacaSecretKey));
    //         }

    //     if ( client == null || dataClient == null ) 
    //         {
    //             throw new Exception("Alpaca client failed to be created !");
    //         }


    //     var clock = await client.GetClockAsync();

    //         if (clock != null)
    //         {
    //             Console.WriteLine(
    //                 "Timestamp: {0}, NextOpen: {1}, NextClose: {2}",
    //                 clock.TimestampUtc, clock.NextOpenUtc, clock.NextCloseUtc);
    //         }

    //     IAccount account = await client.GetAccountAsync();
    //     Console.WriteLine($"Account: {account.TradableCash}");

    //     string dateString = "2023-06-08";
    //     var endDate =  DateTime.Parse(dateString);

    //     // For the past 5 days
    //     var startDate5 = endDate.AddDays(-7);
    //     var historical5 = await Yahoo.GetHistoricalAsync("NVDA", startDate5, endDate, Period.Daily);
    //     var realShortPeriod5 = historical5.Count(); // does not contain the weekends etc .. that's the real value for period to use        
    //     Console.WriteLine($"Real Short Period 5: {realShortPeriod5}");

    //     var sma5 = historical5.ToList().Average(x => x.Close);

    //     // For the past 10 days
    //     var startDate10 = endDate.AddDays(-15);
    //     var historical10 = await Yahoo.GetHistoricalAsync("NVDA", startDate10, endDate, Period.Daily);
    //     var realShortPeriod10 = historical10.Count(); // does not contain the weekends etc .. that's the real value for period to use        
    //     Console.WriteLine($"Real Short Period 10: {realShortPeriod10}");

    //     var sma10 = historical10.ToList().Average(x => x.Close);

    //     // TODO 
    //     // until we cross again
    
    //     Console.WriteLine($"Average 5 Days: {sma5}");
    //     Console.WriteLine($"Average 10 Days: {sma10}");


    //     // sensitivity of the strategy ( lower = more signals, higher = less signals)
    //     double thresholdDiff = 5.0;

    //     // Calculate the difference between SMA5 and SMA10
    //     double diff = (double)(sma5 - sma10);

    //     // Buy signal: SMA5 crosses above SMA10
    //     if(diff > thresholdDiff)
    //     {
    //         Console.WriteLine("Buy signal: SMA5 crossed above SMA10.");
    //         // Perform some action (like buying the stock)
    //     }

    //     // Sell signal: SMA5 crosses below SMA10
    //     else if(diff < -thresholdDiff)
    //     {
    //         Console.WriteLine("Sell signal: SMA5 crossed below SMA10.");
    //         // Perform some action (like selling the stock)
    //     }







