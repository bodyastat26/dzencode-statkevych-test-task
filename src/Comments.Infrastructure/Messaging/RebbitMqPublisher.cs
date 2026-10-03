using System.Text.Json;
using RabbitMQ.Client;

namespace Comments.Infrastructure.Messaging;

public sealed class RabbitMqPublisher(RabbitMqConnection rabbit) : IMessagePublisher
{
    public async Task PublishAsync<T>(string queue, T message, CancellationToken ct)
    {
        var connection = await rabbit.GetConnectionAsync(ct);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);

        await channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);

        var body = JsonSerializer.SerializeToUtf8Bytes(message);
        var properties = new BasicProperties { Persistent = true, ContentType = "application/json" };

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: queue,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: ct);
    }
}