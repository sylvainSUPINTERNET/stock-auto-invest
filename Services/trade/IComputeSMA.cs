namespace Services.Trade;

public interface IComputeSMA
{
    public Task<OrderType?> ComputeSMASignal( String symbol, double threshold = 5.0 );
    
}