
using Alpaca.Markets;
using Services.Trade;

namespace Services;


// TODO : steps :
// Check amount of cash available -> if no cash ping telegram bot
// If cash available take X percent of the cash available
// Check if we have already a position on this symbol
// If not create ( sell or buy ) 
// If yes, close it ? notify bot
// Bonus : If market soon to be closed, close the position


public class TradeBackgroundService : BackgroundService

{

    private readonly int _percentCashToUse = 10;
    private readonly int _intervalBackgroundRunSeconds = 5;
    
    // TODO once one symbol has been validated the strategy, consider plug a redis DB and use a list of symbols to trade
    private readonly string _symbol = "NVDA"; 
    private readonly ILogger<TradeBackgroundService> _logger;
    private readonly IAlpacaTradingClient _client;
    private readonly IAlpacaDataClient _dataClient;
    private Boolean? _marketIsOpen = null;
    

    private readonly IComputeSMA _computeSMA;

    public TradeBackgroundService(ILogger<TradeBackgroundService> logger,IAlpacaTradingClient client, IAlpacaDataClient dataClient, IComputeSMA computeSMA)
    {
        _client = client;
        // will be limited in free tier ! 
        _dataClient = dataClient;
        _logger = logger;
        _computeSMA = computeSMA;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        
        while (!stoppingToken.IsCancellationRequested)
        {
            // Perform background task logic here
            _logger.LogInformation("{Timestamp:yyyy-MM-dd HH:mm:ss} - {Message}", DateTime.Now, "Background service is running...");
 
            var clock = await _client.GetClockAsync();

            if (_marketIsOpen == null) 
            {
                _marketIsOpen = clock.IsOpen;
                _logger.LogWarning("{Timestamp:yyyy-MM-dd HH:mm:ss} - {Message}", DateTime.Now, $"Market is now {(clock.IsOpen ? "open" : "closed")}.");
            }
            
            if ( _marketIsOpen != clock.IsOpen ) 
            {
                // TODO : notify telegram bot !
                _marketIsOpen = clock.IsOpen;
                _logger.LogWarning("{Timestamp:yyyy-MM-dd HH:mm:ss} - {Message}", DateTime.Now, $"Market is now {(clock.IsOpen ? "open" : "closed")}.");
            }

            var order = await _computeSMA.ComputeSMASignal(_symbol, _client, 5.0);
            if ( order == null ) 
            {
                _logger.LogError("{Timestamp:yyyy-MM-dd HH:mm:ss} - {Message}", DateTime.Now, $" Error while computing SMA signal. Cannot defined order type.");

            } else {

                var accountDetails = await _computeSMA.AccountDetails(_client);
                Console.WriteLine(accountDetails.TradableCash);
                

                // Algo 

                // Check quantity to invest is enough 
                // check already one order pending 
                // if not create order with X percent of the cash available
                // Lock the investment for this symbol ( redis ? )
                // Compute number of asset to buy based on current price / and money allocated for it ( 10 % of the cash available for exemple )


                // If market soon closed , close the position ?
                // If order open was type SELL but new signal is BUY for exemple, then close the position and open a new one



                // var quantityStockAssetWanted = 1;
                // var newOrder = await _client.PostOrderAsync(MarketOrder.Buy(_symbol, 1));




                // TODO : notify telegram bot !
                _logger.LogInformation("{Timestamp:yyyy-MM-dd HH:mm:ss} - {Message}", DateTime.Now, $" >>>> {order}");
            }



            // Delay for a specific interval
            await Task.Delay(TimeSpan.FromSeconds(_intervalBackgroundRunSeconds), stoppingToken);
        }
    }
}