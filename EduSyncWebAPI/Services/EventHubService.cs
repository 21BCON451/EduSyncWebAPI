using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using System.Text;
using System.Text.Json;

public class EventHubService
{
    private readonly EventHubProducerClient _producer;

    public EventHubService(IConfiguration config)
    {
        string connectionString = config["EventHub:ConnectionString"];
        string hubName = config["EventHub:HubName"];
        _producer = new EventHubProducerClient(connectionString, hubName);
    }

    public async Task SendAsync(object data)
    {
        string json = JsonSerializer.Serialize(data);
        Console.WriteLine("Sending to Event Hub: " + json); 

        EventDataBatch batch = await _producer.CreateBatchAsync();
        var eventData = new EventData(Encoding.UTF8.GetBytes(json));

        if (!batch.TryAdd(eventData))
            throw new Exception("Failed to add event to the batch");

        await _producer.SendAsync(batch);
    }
}

