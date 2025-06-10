using System;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Company.Function;

public class ProcessOrderFunction
{
    private readonly ILogger<ProcessOrderFunction> _logger;

    public ProcessOrderFunction(ILogger<ProcessOrderFunction> logger)
    {
        _logger = logger;
    }

    [Function("ProcessOrderFunction")]
    public async Task Run(
        [ServiceBusTrigger("orderqueue", Connection = "orderservicebusjiga_SERVICEBUS")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        try
        {
            _logger.LogInformation("Message ID: {id}", message.MessageId);
            var messageBody = message.Body.ToString();
            _logger.LogInformation("Message Body: {body}", message.Body);

            var order = JsonSerializer.Deserialize<Order>(messageBody);
            if (order == null)
            {
                _logger.LogWarning("Could not deserialize message to Order.");
                await messageActions.DeadLetterMessageAsync(message,
                new Dictionary<string, object>
                {
                    {"DeadLetterReason", "Invalid message body"},
                    {"DeadLetterErrorDescription", "Deserialization returned null."}
                });
                return;
            }

            // Simulate order processing
            _logger.LogInformation("Order Received - ID: {OrderId}, Item: {Item}, Quantity: {Quantity}",
                order.OrderId, order.Item ?? "Unknown", order.Quantity);

            await messageActions.CompleteMessageAsync(message);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON format. Sending message to dead-letter queue.");
            await messageActions.DeadLetterMessageAsync(message,
            new Dictionary<string, object>
            {
                {"DeadLetterReason", "Invalid JSON"},
                {"DeadLetterErrorDescription", ex.Message}
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while processing order.");
            throw;
        }
        
    }
}

public class Order
{
    public int OrderId { get; set; }
    public string? Item { get; set; }
    public int Quantity { get; set; }
}