using Alpaca.Markets;
using Microsoft.Extensions.Logging;


var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

try 

    {


    if ( builder.Environment.IsDevelopment() ) 
        {
            app.Logger.LogInformation("Development environment detected - Alpaca Paper client will be used");
        }


    if ( builder.Environment.IsProduction() ) 
        {
            app.Logger.LogWarning("Production environment detected - Alpaca Live client will be used");
        }


    String alpacaSecretKey = builder.Configuration.GetSection("Alpaca").GetSection("ApiKeySecret").Value;
    String alpacaSecretPublic = builder.Configuration.GetSection("Alpaca").GetSection("ApiKeyPublic").Value;

    if ( string.IsNullOrEmpty(alpacaSecretKey) || string.IsNullOrEmpty(alpacaSecretPublic) ) 
        {
            throw new Exception("Alpaca API Key not found in appsettings.json");
        }

        app.Logger.LogInformation("Alpaca API Key found in appsettings.json starting new client ...");

        app.MapGet("/", () => "Hello World!");

        app.Run();
    }

catch ( Exception ex ) 

    {

        app.Logger.LogError($"Error occured : {ex.Message}");
    
    }





