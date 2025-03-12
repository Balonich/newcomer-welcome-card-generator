using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Common.Interfaces.Messaging
{
    public interface IMessageConsumer<T> where T : class
    {
        Task StartConsumingAsync(Func<T, Task> handler);
        Task StopConsumingAsync();
    }
}