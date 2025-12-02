using Microsoft.AspNetCore.SignalR;
using ProjectTest1.ViewModels;
using System.Threading.Tasks;

public class OrderHub : Hub
{
    public async Task SendOrderNotification(string orderId,  string time, int count, int notificationId, double price, int product, string tittle, string message)
    {
        await Clients.All.SendAsync("ReceiveOrderNotification", orderId, time, count, notificationId,price,product,tittle,message);
    }
    public async Task SendOrderNotificationUser(Guid userId, OrderNotificationViewModel notif)
    {
        await Clients.User(userId.ToString())
                     .SendAsync("ReceiveOrderNotificationUser", notif);
    }

}