using Microsoft.AspNetCore.SignalR;
using NuGet.Protocol.Plugins;
using ProjectTest1.Models;
using ProjectTest1.Repository;
using System;
using System.Data;
using System.Security.Claims;
using System.Threading.Tasks;

public class ChatHub : Hub
{
    private readonly DataContext _db;

    public ChatHub(DataContext db)
    {
        _db = db;
    }

    // Join vào một conversation
    public async Task JoinConversation(int conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation-{conversationId}");
    }
    public async Task LeaveConversation(int conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation-{conversationId}");
    }

    // Gửi tin nhắn (dùng cho cả buyer lẫn admin)
    public async Task SendMessage(int conversationId, string content, int? productId, int? replyToMessage)
    {
        var senderIdClaim = Context.User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(senderIdClaim))
        {
            throw new HubException("UserId claim is missing.");
        }
        var senderId = Guid.Parse(senderIdClaim);
        // Lấy role từ claims
        var senderRole = Context.User.IsInRole("Admin") ? "admin" : "buyer";
        var message = new MessageModel
        {
            ConversationId = conversationId,
            SenderId = senderId,
            Content = content,
            CreatedAt = DateTime.Now,
            IsRead = false,
            ProductId = productId,
            ReplyToMessageId = replyToMessage,
        };
        await _db.Messages.AddAsync(message);
        await _db.SaveChangesAsync();
        var BuyerName = Context.User.FindFirst(ClaimTypes.Name)?.Value;
        var msg = new
        {
            MessageId = message.Id,
            ConversationId = message.ConversationId,
            SenderId = message.SenderId,
            SenderRole = senderRole,
            Content = content,
            BuyerName = BuyerName,
            CreatedAt = message.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
            ProductId = message.ProductId,
            ReplyToMessageId = message.ReplyToMessageId,
            SenderName = BuyerName
        };
        await Clients.Group($"conversation-{conversationId}")
                     .SendAsync("ReceiveMessage", msg);
        

    }

}
