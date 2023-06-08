using Alpaca.Markets;
using YahooFinanceApi;

namespace Services.Trade;

public enum OrderType
{
    Buy,
    Sell
}

class ComputeSMA : IComputeSMA
{

    private readonly ILogger<ComputeSMA> _logger;

    public ComputeSMA (ILogger<ComputeSMA> _logger) {
        this._logger = _logger;
    }


    public async Task<IAccount> AccountDetails(IAlpacaTradingClient client)
    {
        
        IAccount account = await client.GetAccountAsync();

        return account;
    }

    async Task<OrderType?> IComputeSMA.ComputeSMASignal(string symbol, IAlpacaTradingClient client, double threshold) 
    {

 

       IAccount account = await AccountDetails(client);

        _logger.LogInformation("{Timestamp:yyyy-MM-dd HH:mm:ss} - {Message}", DateTime.Now, $"Cash for trading : {account.TradableCash}$");
        // TODO : steps :
        // Check amount of cash available -> if no cash ping telegram bot
        // If cash available take X percent of the cash available
        // Check if we have already a position on this symbol
        // If not create ( sell or buy ) 
        // If yes, close it ? notify bot
        // Bonus : If market soon to be closed, close the position


        try {

            IAccount account = await AccountDetails(client);

            _logger.LogInformation("{Timestamp:yyyy-MM-dd HH:mm:ss} - {Message}", DateTime.Now, $"Using threshold: {threshold} for SMA5 and SMA10");

            string dateString = "2023-06-08";
            var endDate =  DateTime.Parse(dateString);

            // For the past 5 days
            var startDate5 = endDate.AddDays(-7);
            var historical5 = await Yahoo.GetHistoricalAsync(symbol, startDate5, endDate, Period.Daily);
            var realShortPeriod5 = historical5.Count(); // does not contain the weekends etc .. that's the real value for period to use

            _logger.LogInformation("{Timestamp:yyyy-MM-dd HH:mm:ss} - {Message}", DateTime.Now, $"Real Short Period 5: {realShortPeriod5}");

            var sma5 = historical5.ToList().Average(x => x.Close);

            // For the past 10 days
            var startDate10 = endDate.AddDays(-15);
            var historical10 = await Yahoo.GetHistoricalAsync(symbol, startDate10, endDate, Period.Daily);
            var realShortPeriod10 = historical10.Count(); // does not contain the weekends etc .. that's the real value for period to use        

            _logger.LogInformation("{Timestamp:yyyy-MM-dd HH:mm:ss} - {Message}", DateTime.Now, $"Real Short Period 10: {realShortPeriod10}");

            var sma10 = historical10.ToList().Average(x => x.Close);
        
            _logger.LogInformation("{Timestamp:yyyy-MM-dd HH:mm:ss} - {Message}", DateTime.Now, $"Average 5 Days: {sma5}");
            _logger.LogInformation("{Timestamp:yyyy-MM-dd HH:mm:ss} - {Message}", DateTime.Now, $"Average 10 Days: {sma10}");

            // Calculate the difference between SMA5 and SMA10
            double diff = (double)(sma5 - sma10);

            // Buy signal: SMA5 crosses above SMA10
            if(diff > threshold)
            {
                _logger.LogInformation("{Timestamp:yyyy-MM-dd HH:mm:ss} - {Message}", DateTime.Now, "Buy signal: SMA5 crossed above SMA10.");
                // Perform some action (like buying the stock)
                return OrderType.Buy;
            }

            // Sell signal: SMA5 crosses below SMA10
            else if(diff < -threshold)
            {
                _logger.LogInformation("{Timestamp:yyyy-MM-dd HH:mm:ss} - {Message}", DateTime.Now, "Sell signal: SMA5 crossed below SMA10.");
                // Perform some action (like selling the stock)
                return OrderType.Sell;
            }


            return null;

        } catch ( Exception exception ) {
            
            _logger.LogError("{Timestamp:yyyy-MM-dd HH:mm:ss} - {Message}", DateTime.Now, $"Error while computing SMA and define order type : {exception.Message}");

            return null;
        }

    }
}