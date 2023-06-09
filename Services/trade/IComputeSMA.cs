using Alpaca.Markets;

namespace Services.Trade;

public interface IComputeSMA
{
    public Task<IAccount> AccountDetails(IAlpacaTradingClient client);

    public Task<OrderCustomSide?> ComputeSMASignal( String symbol, IAlpacaTradingClient client, double threshold = 5.0);
    
}