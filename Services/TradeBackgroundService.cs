
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


    private Boolean isInit = true;
    private readonly int _percentCashToUse = 10;
    private readonly int _intervalBackgroundRunSeconds = 10;
    
    // TODO once one symbol has been validated the strategy, consider plug a redis DB and use a list of symbols to trade
    private readonly string _symbol = "NVDA"; 
    private readonly ILogger<TradeBackgroundService> _logger;
    private readonly IAlpacaTradingClient _client;
    private readonly IAlpacaDataClient _dataClient;
    private Boolean? _marketIsOpen = null;


    // TODO replace with database upstash
    private Boolean _isOrderOpen = false;

    private Guid _lastOrderId = Guid.Empty;
    private OrderType? _lastOrderType = null;
    

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

            try {


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


             
             // TODO set to true 
             if (  _marketIsOpen == false ) {

                var orderType = await _computeSMA.ComputeSMASignal(_symbol, _client, 5.0);
                if ( orderType == null ) 
                {
                    _logger.LogError("{Timestamp:yyyy-MM-dd HH:mm:ss} - {Message}", DateTime.Now, $" Error while computing SMA signal. Cannot defined order type.");

                } 
                

                var accountDetails = await _computeSMA.AccountDetails(_client);
                _logger.LogInformation("{Timestamp:yyyy-MM-dd HH:mm:ss} - {Message}", DateTime.Now, $" >>>> Buying Power : {accountDetails.BuyingPower}");

                // si il y a deja un order et qu'on pas sur un init on rentre pas ici
                if ( accountDetails.TradableCash > 0 && isInit ) {

                    // TODO : notify telegram bot !
                    _logger.LogInformation("{Timestamp:yyyy-MM-dd HH:mm:ss} - {Message}", DateTime.Now, $" >>>> {orderType}");

                    _logger.LogInformation("{Timestamp:yyyy-MM-dd HH:mm:ss} - {Message}", DateTime.Now, $"Init detected, closing all orders");

                    ListOrdersRequest ordersRequest = new ListOrdersRequest();
                    ordersRequest.OrderStatusFilter = OrderStatusFilter.Closed;
                    ordersRequest.OrderListSorting = SortDirection.Descending;    
                    ordersRequest.WithSymbol(_symbol);
                
                    // check si il y a des position ouverte, si oui on ferme ( init ) 
                    var orders = await _client.ListOrdersAsync(ordersRequest);
                    if ( orders.Count > 0 ) {
                        _logger.LogInformation("{Timestamp:yyyy-MM-dd HH:mm:ss} - {Message}", DateTime.Now, $"Deleting orders: {orders.Count} ...");
                        foreach (var order in orders)
                        {
                            var del = await _client.CancelOrderAsync(order.OrderId);
                            _logger.LogInformation("{Timestamp:yyyy-MM-dd HH:mm:ss} - {Message}", DateTime.Now, $"Order deleted : {order.OrderId}");
                        }
                    }

                    // get latest price
                    ITrade priceAsset = await _dataClient.GetLatestTradeAsync(new LatestMarketDataRequest( symbol: _symbol));
                
                    var quantityStockAssetWanted = (int) ( accountDetails.BuyingPower! * _percentCashToUse / 100 / priceAsset.Price);
                    

                    _logger.LogInformation("{Timestamp:yyyy-MM-dd HH:mm:ss} - {Message}", DateTime.Now, $"{accountDetails.BuyingPower} * {_percentCashToUse} / 100 / {priceAsset.Price} = {quantityStockAssetWanted}");
                    _logger.LogInformation("{Timestamp:yyyy-MM-dd HH:mm:ss} - {Message}", DateTime.Now, $" Quantity of stock asset wanted : {quantityStockAssetWanted}");

                    // market price ( no stop loss etc ... )
                    
                    if ( orderType.Equals("SELL") ) {

                    }


                    IOrder? newOrder = null;

                    if (orderType.ToString().Equals("Buy", StringComparison.OrdinalIgnoreCase)) {
                        newOrder = await _client.PostOrderAsync(MarketOrder.Buy(_symbol, quantityStockAssetWanted));
                    } else {
                        newOrder = await _client.PostOrderAsync(MarketOrder.Sell(_symbol, quantityStockAssetWanted));
                    }

                    
                    _lastOrderId = newOrder.OrderId;
                    _lastOrderType = newOrder.OrderType;
                    _logger.LogInformation("{Timestamp:yyyy-MM-dd HH:mm:ss} - {Message}", DateTime.Now, $" >>>> New order created with success : {newOrder}");

                    isInit = false;
                    _isOrderOpen = true;

                } else if ( accountDetails.BuyingPower > 0 && !isInit ) {
                    

                    if ( !_isOrderOpen ) {
                            
                            // TODO : notify telegram bot !
                            _logger.LogInformation("{Timestamp:yyyy-MM-dd HH:mm:ss} - {Message}", DateTime.Now, $" >>>> {orderType}");
    
                            // get latest price
                            ITrade priceAsset = await _dataClient.GetLatestTradeAsync(new LatestMarketDataRequest( symbol: _symbol));
                        
                            var quantityStockAssetWanted = (int) ( accountDetails.BuyingPower! * _percentCashToUse / 100 / priceAsset.Price);
                            
    
                            _logger.LogInformation("{Timestamp:yyyy-MM-dd HH:mm:ss} - {Message}", DateTime.Now, $"{accountDetails.BuyingPower} * {_percentCashToUse} / 100 / {priceAsset.Price} = {quantityStockAssetWanted}");
                            _logger.LogInformation("{Timestamp:yyyy-MM-dd HH:mm:ss} - {Message}", DateTime.Now, $" Quantity of stock asset wanted : {quantityStockAssetWanted}");
    
                            // market price ( no stop loss etc ... )
                            IOrder? newOrder = null;
                            
                            if (orderType.ToString().Equals("Buy", StringComparison.OrdinalIgnoreCase)) {
                                newOrder = await _client.PostOrderAsync(MarketOrder.Buy(_symbol, quantityStockAssetWanted));
                            } else {
                                newOrder = await _client.PostOrderAsync(MarketOrder.Sell(_symbol, quantityStockAssetWanted));
                            }

    
                            _logger.LogInformation("{Timestamp:yyyy-MM-dd HH:mm:ss} - {Message}", DateTime.Now, $" >>>> New order created with success : {newOrder}");
    
                            _isOrderOpen = true;
                        } else {
                            _logger.LogInformation("{Timestamp:yyyy-MM-dd HH:mm:ss} - {Message}", DateTime.Now, $" >>>> Order already open, doing nothing ...");
                    }

                    if ( _isOrderOpen ) {
                        var order = await _client.GetOrderAsync(_lastOrderId);
                        
                        if ( order.FilledQuantity > 0 ) {
                            // create new order 
                            
                            // TODO : notify telegram bot !
                            _logger.LogInformation("{Timestamp:yyyy-MM-dd HH:mm:ss} - {Message}", DateTime.Now, $" >>>> {orderType}");
    
                            // get latest price
                            ITrade priceAsset = await _dataClient.GetLatestTradeAsync(new LatestMarketDataRequest( symbol: _symbol));
                        
                            var quantityStockAssetWanted = (int) ( accountDetails.BuyingPower! * _percentCashToUse / 100 / priceAsset.Price);
                            
    
                            _logger.LogInformation("{Timestamp:yyyy-MM-dd HH:mm:ss} - {Message}", DateTime.Now, $"{accountDetails.BuyingPower} * {_percentCashToUse} / 100 / {priceAsset.Price} = {quantityStockAssetWanted}");
                            _logger.LogInformation("{Timestamp:yyyy-MM-dd HH:mm:ss} - {Message}", DateTime.Now, $" Quantity of stock asset wanted : {quantityStockAssetWanted}");
    
                            // market price ( no stop loss etc ... )
                            
                            IOrder? newOrder = null;
                            
                            if (orderType.ToString().Equals("Buy", StringComparison.OrdinalIgnoreCase)) {
                                newOrder = await _client.PostOrderAsync(MarketOrder.Buy(_symbol, quantityStockAssetWanted));
                            } else {
                                newOrder = await _client.PostOrderAsync(MarketOrder.Sell(_symbol, quantityStockAssetWanted));
                            }

    
                            _logger.LogInformation("{Timestamp:yyyy-MM-dd HH:mm:ss} - {Message}", DateTime.Now, $" >>>> New order created with success : {newOrder}");
    
                            _isOrderOpen = true;

                        } else  {
                                // cancel order 
                                ListOrdersRequest ordersRequest = new ListOrdersRequest();
                                ordersRequest.OrderStatusFilter = OrderStatusFilter.Closed;
                                ordersRequest.OrderListSorting = SortDirection.Descending;    
                                ordersRequest.WithSymbol(_symbol);
                                var orders = await _client.ListOrdersAsync(ordersRequest);
                                if ( orders.Count > 0 ) {
                                    _logger.LogInformation("{Timestamp:yyyy-MM-dd HH:mm:ss} - {Message}", DateTime.Now, $"Deleting orders: {orders.Count} ...");
                                    foreach (var o in orders)
                                    {
                                        var del = await _client.CancelOrderAsync(o.OrderId);
                                        _logger.LogInformation("{Timestamp:yyyy-MM-dd HH:mm:ss} - {Message}", DateTime.Now, $"Order deleted : {o.OrderId}");
                                    }
                                }
                                // create a new order
                                // TODO : notify telegram bot !
                                _logger.LogInformation("{Timestamp:yyyy-MM-dd HH:mm:ss} - {Message}", DateTime.Now, $" >>>> {orderType}");
        
                                // get latest price
                                ITrade priceAsset = await _dataClient.GetLatestTradeAsync(new LatestMarketDataRequest( symbol: _symbol));
                            
                                var quantityStockAssetWanted = (int) ( accountDetails.BuyingPower! * _percentCashToUse / 100 / priceAsset.Price);
                                
        
                                _logger.LogInformation("{Timestamp:yyyy-MM-dd HH:mm:ss} - {Message}", DateTime.Now, $"{accountDetails.BuyingPower} * {_percentCashToUse} / 100 / {priceAsset.Price} = {quantityStockAssetWanted}");
                                _logger.LogInformation("{Timestamp:yyyy-MM-dd HH:mm:ss} - {Message}", DateTime.Now, $" Quantity of stock asset wanted : {quantityStockAssetWanted}");
        
                                // market price ( no stop loss etc ... )
                                IOrder? newOrder = null;
                                
                                if (orderType.ToString().Equals("Buy", StringComparison.OrdinalIgnoreCase)) {
                                    newOrder = await _client.PostOrderAsync(MarketOrder.Buy(_symbol, quantityStockAssetWanted));
                                } else {
                                    newOrder = await _client.PostOrderAsync(MarketOrder.Sell(_symbol, quantityStockAssetWanted));
                                }

        
                                _logger.LogInformation("{Timestamp:yyyy-MM-dd HH:mm:ss} - {Message}", DateTime.Now, $" >>>> New order created with success : {newOrder}");
        
                                _isOrderOpen = true;
                            
                        }
                        
                    }
                    

        
                } else {
                    _logger.LogInformation("{Timestamp:yyyy-MM-dd HH:mm:ss} - {Message}", DateTime.Now, $" >>>> No cash available, waiting for next run");
                }
                
            
            }



            // Delay for a specific interval
            await Task.Delay(TimeSpan.FromSeconds(_intervalBackgroundRunSeconds), stoppingToken);

            
            } catch ( Exception ex ) {
                _logger.LogError("{Timestamp:yyyy-MM-dd HH:mm:ss} - {Message}", DateTime.Now, $"Error while running background service : {ex.Message}");
                
                await Task.Delay(TimeSpan.FromSeconds(_intervalBackgroundRunSeconds), stoppingToken);

            }

        }
    }
}