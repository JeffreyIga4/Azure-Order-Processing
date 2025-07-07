using System;
using System.Text;
using System.Text.Json;
using System.Net.Http;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Company.Function;

public class ProcessOrderFunction
{
    private readonly ILogger<ProcessOrderFunction> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _logicAppUrl;
    private readonly string _alertLogicAppUrl;


    public ProcessOrderFunction(ILogger<ProcessOrderFunction> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
        _logicAppUrl = Environment.GetEnvironmentVariable("LogicAppUrl") ?? throw new InvalidOperationException("LogicAppUrl not configured.");
        _alertLogicAppUrl = Environment.GetEnvironmentVariable("AlertLogicAppUrl") ?? throw new InvalidOperationException("AlertLogicAppUrl not configured.");

    }

    [Function("ProcessOrderFunction")]
    public async Task Run(
        [ServiceBusTrigger("orderqueue", Connection = "orderservicebusjiga1234_SERVICEBUS")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        try
        {
            _logger.LogInformation("Message ID: {id}", message.MessageId);
            var messageBody = message.Body.ToString();
            _logger.LogInformation("Message Body: {body}", message.Body);

            var order = JsonSerializer.Deserialize<Order>(messageBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
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

            if (string.IsNullOrWhiteSpace(order.Item) || order.Quantity <= 0)
            {
                _logger.LogWarning("Validation failed - Item: '{Item}', Quantity: {Quantity}", order.Item, order.Quantity);
                _logger.LogInformation("Posting validation alert to Logic App at: {url}", _alertLogicAppUrl);

                var alertPayload = new
                {
                    orderId = order.OrderId,
                    item = order.Item,
                    quantity = order.Quantity,
                    messageId = message.MessageId,
                    reason = "ValidationError",
                    detail = "Missing item or invalid quantity"
                };

                 var alertContent = new StringContent(JsonSerializer.Serialize(alertPayload), Encoding.UTF8, "application/json");

                try
                {
                    var alertResponse = await _httpClient.PostAsync(_alertLogicAppUrl, alertContent);
                     _logger.LogInformation("Validation alert sent. Status: {Status}", alertResponse.StatusCode);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send alert.");
                }

                await messageActions.DeadLetterMessageAsync(message,
                new Dictionary<string, object>
                {
                    { "DeadLetterReason", "ValidationError" },
                    { "DeadLetterErrorDescription", "Missing item or invalid quantity" }
                });
                return;
            }
            _logger.LogInformation("Order Received - ID: {OrderId}, Item: {Item}, Quantity: {Quantity}",
            order.OrderId, order.Item, order.Quantity);

            var orderJson = JsonSerializer.Serialize(order);
            var content = new StringContent(orderJson, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_logicAppUrl, content);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to POST to Logic App. Status: {Status}, Reason: {Reason}",
                    response.StatusCode, response.ReasonPhrase);
                throw new Exception("Logic App POST failed.");
            }

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