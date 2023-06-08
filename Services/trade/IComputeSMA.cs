using Alpaca.Markets;

namespace Services.Trade;

public interface IComputeSMA
{
    public Task<OrderType?> ComputeSMASignal( String symbol, IAlpacaTradingClient client, double threshold = 5.0);
    
}