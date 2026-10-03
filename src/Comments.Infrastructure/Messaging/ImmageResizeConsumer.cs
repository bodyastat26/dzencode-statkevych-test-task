using System.Text.Json;
using Comments.Domain.Entities;
using Comments.Infrastructure.Caching;
using Comments.Infrastructure.Files;
using Comments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Comments.Infrastructure.Comments;
using Comments.Infrastructure.Events;


namespace Comments.Infrastructure.Messaging;

/// Background worker: takes messages from the "image-resize" queue and shrinks images to 320x240.
public sealed class ImageResizeConsumer(
    RabbitMqConnection rabbit,
    IServiceScopeFactory scopes,
    ILogger<ImageResizeConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var connection = await rabbit.GetConnectionAsync(stoppingToken);
                await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

                await channel.QueueDeclareAsync(Queues.ImageResize, durable: true, exclusive: false,
                    autoDelete: false, cancellationToken: stoppingToken);
                await channel.BasicQosAsync(0, 1, false, stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += async (_, args) =>
                {
                    try
                    {
                        var message = JsonSerializer.Deserialize<ImageResizeMessage>(args.Body.Span);
                        if (message is not null)
                            await ProcessAsync(message.AttachmentId, stoppingToken);

                        await channel.BasicAckAsync(args.DeliveryTag, false, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to process image-resize message");
                        await channel.BasicNackAsync(args.DeliveryTag, false, requeue: false, stoppingToken);
                    }
                };

                await channel.BasicConsumeAsync(Queues.ImageResize, autoAck: false, consumer: consumer,
                    cancellationToken: stoppingToken);
                logger.LogInformation("Listening to RabbitMQ queue '{Queue}'", Queues.ImageResize);

                while (!stoppingToken.IsCancellationRequested && channel.IsOpen)
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "RabbitMQ is unavailable, retrying in 5 seconds");
                try { await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); }
                catch (OperationCanceledException) { break; }
            }
        }
    }

       private async Task ProcessAsync(int attachmentId, CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CommentsDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
        var images = scope.ServiceProvider.GetRequiredService<ImageProcessor>();
        var events = scope.ServiceProvider.GetRequiredService<IEventDispatcher>();

        var attachment = await db.Attachments.FirstOrDefaultAsync(a => a.Id == attachmentId, ct);
        if (attachment is null || attachment.Status != AttachmentStatus.Processing)
            return;

        try
        {
            var source = await storage.ReadAsync(attachment.StoredFileName, ct);
            var result = images.FitInto(source, Attachment.MaxImageWidth, Attachment.MaxImageHeight);

            var storedName = attachment.StoredFileName;
            if (result.Changed)
            {
                storedName = await storage.SaveAsync(new MemoryStream(result.Data), result.Extension, ct);
                storage.Delete(attachment.StoredFileName);
            }

            attachment.MarkProcessed(result.Width, result.Height, result.Data.Length, storedName, result.ContentType);
            logger.LogInformation("Image {Id} processed: {W}x{H}, resized: {Changed}",
                attachmentId, result.Width, result.Height, result.Changed);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            attachment.MarkFailed();
            logger.LogError(ex, "Image {Id} processing failed", attachmentId);
        }

        await db.SaveChangesAsync(ct);

        var dto = new AttachmentDto(
            attachment.Id,
            attachment.Kind.ToString(),
            attachment.Status.ToString(),
            attachment.OriginalFileName,
            $"/uploads/{attachment.StoredFileName}",
            attachment.Width,
            attachment.Height);

        await events.PublishAsync(new AttachmentProcessedEvent(attachment.CommentId, dto), ct);
    }
}