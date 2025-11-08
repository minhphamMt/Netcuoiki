using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace BTAPLON.Hubs
{
    public class NotificationHub : Hub
    {
        public async Task SendNotification(string user, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            var safeUser = string.IsNullOrWhiteSpace(user) ? "Ẩn danh" : user.Trim();
            var timestamp = DateTimeOffset.Now.ToString("HH:mm:ss");

            await Clients.All.SendAsync("ReceiveNotification", safeUser, message.Trim(), timestamp);
        }
    }
}