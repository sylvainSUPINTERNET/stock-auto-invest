
namespace Services;

public class TradeBackgroundService : BackgroundService

{

    private readonly ILogger<TradeBackgroundService> _logger;

    public TradeBackgroundService(ILogger<TradeBackgroundService> logger)
    {
        _logger = logger;
    }	

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        
        while (!stoppingToken.IsCancellationRequested)
        {
            // Perform background task logic here
            _logger.LogInformation("Background task is running...");
            

            // Delay for a specific interval
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}