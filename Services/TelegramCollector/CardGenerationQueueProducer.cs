using Common.BaseClasses.Messaging;
using Common.BaseClasses.Messaging.RabbitMQ;
using Common.Messaging.RabbitMQ;
using Common.Models;
using Microsoft.Extensions.Options;

namespace TelegramCollector
{
    public class CardGenerationQueueProducer : RabbitMQProducer<NewcomerData>
    {
        public CardGenerationQueueProducer(
        IOptions<RabbitMQSettings> settings,
        ILogger<CardGenerationQueueProducer> logger)
        : base(
            settings,
            logger,
            QueueNames.CardGenerationExchange,
            QueueNames.CardGenerationRoutingKey)
        {
        }
    }
}