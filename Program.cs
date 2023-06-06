using Alpaca.Markets;
using Microsoft.Extensions.Logging;
using System;


// strategy : https://alpaca.markets/learn/mean-reversion-stock-trading-csharp/

var builder = WebApplication.CreateBuilder(args);
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

        if ( builder.Environment.IsDevelopment() ) 
            {
                app.Logger.LogInformation("Development environment detected - Alpaca Paper client will be used");
                client = Alpaca.Markets.Environments.Paper.GetAlpacaTradingClient(new SecretKey(alpacaSecretPublic, alpacaSecretKey));
            }


        if ( builder.Environment.IsProduction() ) 
            {
                app.Logger.LogWarning("Production environment detected - Alpaca Live client will be used");
                client = Alpaca.Markets.Environments.Live.GetAlpacaTradingClient(new SecretKey(alpacaSecretPublic, alpacaSecretKey));
            }

        if ( client == null ) 
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
        


        app.MapGet("/", () => "Hello World!");

        app.Run();
    }

catch ( Exception ex ) 

    {

        app.Logger.LogError($"Error occured : {ex.Message}");
    
    }





