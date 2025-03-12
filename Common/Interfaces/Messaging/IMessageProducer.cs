namespace Common.Interfaces.Messaging
{
    public interface IMessageProducer<T> where T : class
    {
        Task PublishAsync(T message);
    }
}