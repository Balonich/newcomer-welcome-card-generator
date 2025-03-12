namespace Common.Interfaces.DataCollection
{
    public interface IDataCollector
    {
        Task StartAsync();
        Task StopAsync();
    }
}