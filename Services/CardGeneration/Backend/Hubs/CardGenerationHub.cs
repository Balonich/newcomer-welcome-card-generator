using Microsoft.AspNetCore.SignalR;

namespace Backend.Hubs
{
    public class CardGenerationHub : Hub
    {
        public async Task SendCardUpdate(string userId, string cardData)
        {
            await Clients.All.SendAsync("ReceiveCardUpdate", userId, cardData);
        }
    }
}