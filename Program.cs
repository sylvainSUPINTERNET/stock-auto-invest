using Alpaca.Markets;
using Microsoft.Extensions.Logging;
using Services;
using System;


// strategy : https://alpaca.markets/learn/mean-reversion-stock-trading-csharp/
// formula for SMA ( 20 / 50 ) : https://www.investopedia.com/terms/s/sma.asp

// period can be days or minute based on the requirements / volatility of the stock ...

// Compare the values of the shorter-term SMA and the longer-term SMA : 
// If the shorter-term SMA is above the longer-term SMA, it indicates a potential bullish crossover, which suggests a buying opportunity.
// If the shorter-term SMA is below the longer-term SMA, it indicates a potential bearish crossover, which suggests a selling opportunity.

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHostedService<TradeBackgroundService>();

var app = builder.Build();


try 

    {

    String alpacaSecretKey = builder.Configuration.GetSection("Alpaca").GetSection("ApiKeySecret").Value;
    String alpacaSecretPublic = builder.Configuration.GetSection("Alpaca").GetSection("ApiKeyPublic").Value;

    if ( string.IsNullOrEmpty(alpacaSecretKey) || string.IsNullOrEmpty(alpacaSecretPublic) ) 
        {
            throw new Exception("Alpaca API Key not found in appsettings.json");
        }

        app.Logger.LogInformation("Alpaca API Key found in appsettings.json starting new client ...");

        IAlpacaTradingClient? client = null;
        IAlpacaDataClient? dataClient = null;

        if ( builder.Environment.IsDevelopment() ) 
            {
                app.Logger.LogInformation("Development environment detected - Alpaca Paper client will be used");
                client = Alpaca.Markets.Environments.Paper.GetAlpacaTradingClient(new SecretKey(alpacaSecretPublic, alpacaSecretKey));
                dataClient = Alpaca.Markets.Environments.Paper.GetAlpacaDataClient(new SecretKey(alpacaSecretPublic, alpacaSecretKey));
            }


        if ( builder.Environment.IsProduction() ) 
            {
                app.Logger.LogWarning("Production environment detected - Alpaca Live client will be used");
                client = Alpaca.Markets.Environments.Live.GetAlpacaTradingClient(new SecretKey(alpacaSecretPublic, alpacaSecretKey));
                dataClient = Alpaca.Markets.Environments.Live.GetAlpacaDataClient(new SecretKey(alpacaSecretPublic, alpacaSecretKey));
            }

        if ( client == null || dataClient == null ) 
            {
                throw new Exception("Alpaca client failed to be created !");
            }


        var clock = await client.GetClockAsync();

            if (clock != null)
            {
                Console.WriteLine(
                    "Timestamp: {0}, NextOpen: {1}, NextClose: {2}",
                    clock.TimestampUtc, clock.NextOpenUtc, clock.NextCloseUtc);
            }

        IAccount account = await client.GetAccountAsync();
        Console.WriteLine($"Account: {account.TradableCash}");


        int PERIOD_SHORT = 8;
        int PERIOD_LONG = 13;
        string symbol = "NVDA";

        Interval<DateTime> intervalShort = new Interval<DateTime>(DateTime.Today.AddDays(-PERIOD_SHORT), DateTime.Today);
        Interval<DateTime> intervalLong = new Interval<DateTime>(DateTime.Today.AddDays(-PERIOD_LONG), DateTime.Today);

        IMultiPage<IBar> dataShort = await dataClient.GetHistoricalBarsAsync(new HistoricalBarsRequest(symbol, BarTimeFrame.Day, intervalShort));


        Console.WriteLine($"Short : {dataShort.Items.Count()}");
        
        dataShort.Items.ToList().ForEach( x => Console.WriteLine($"Short : {x.Key}"));


        IMultiPage<IBar> dataLong = await dataClient.GetHistoricalBarsAsync(new HistoricalBarsRequest(symbol, BarTimeFrame.Day, intervalLong));

        
        // {
        //     Limit = 14,
        //     StartDateTimeInclusive = DateTime.Today.AddDays(-14),
        //     EndDateTimeExclusive = DateTime.Today
        // }); 


        // var bars = await client.ListHistoricalBarsAsync(new HistoricalBarsRequest(14, TimeFrame.Day)
        // {
        //     Limit = PERIOD,
        //     StartDateTimeInclusive = DateTime.Today.AddDays(-PERIOD),
        //     EndDateTimeExclusive = DateTime.Today
        // });

        

        // Idea 
        // get day bars
        // average of them price
        // get current price 
        // do the average mean 
        // based on threshold buy or sell ( in our case send a notification to telegram bot )

        app.MapGet("/", () => "Hello World!");

        app.Run();
    }

catch ( Exception ex ) 

    {

        app.Logger.LogError($"Error occured : {ex.Message}");
    
    }





