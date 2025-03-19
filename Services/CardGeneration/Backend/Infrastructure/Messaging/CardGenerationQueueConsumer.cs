using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.BaseClasses.Messaging;
using Common.Messaging.RabbitMQ;
using Common.Models;
using Microsoft.Extensions.Options;

namespace Backend.Infrastructure.Messaging
{
    public class CardGenerationQueueConsumer : RabbitMQConsumer<NewcomerData>
    {
        public CardGenerationQueueConsumer(
        IOptions<RabbitMQSettings> settings,
        ILogger<CardGenerationQueueConsumer> logger)
        : base(
            settings,
            logger,
            QueueNames.CardGenerationQueue,
            QueueNames.CardGenerationExchange,
            QueueNames.CardGenerationRoutingKey)
        {
        }
    }
}