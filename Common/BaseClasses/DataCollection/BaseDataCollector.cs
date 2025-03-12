using Common.Interfaces.DataCollection;
using Common.Interfaces.Messaging;
using Common.Models;

namespace Common.BaseClasses.DataCollection
{
    public abstract class BaseDataCollector : IDataCollector
    {
        protected readonly IMessageProducer<NewcomerData> _messageProducer;
        protected bool _isRunning;

        protected BaseDataCollector(IMessageProducer<NewcomerData> messageProducer)
        {
            _messageProducer = messageProducer ?? throw new ArgumentNullException(nameof(messageProducer));
        }

        public virtual Task StartAsync()
        {
            _isRunning = true;
            return Task.CompletedTask;
        }

        public virtual Task StopAsync()
        {
            _isRunning = false;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Publishes newcomer data to the message queue
        /// </summary>
        protected async Task PublishNewcomerDataAsync(NewcomerData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            await _messageProducer.PublishAsync(data);
        }
    }
}