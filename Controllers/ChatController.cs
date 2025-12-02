using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectTest1.Models;
using ProjectTest1.Repository;
using ProjectTest1.ViewModels;
namespace ProjectTest1.Controllers
{
    public class ChatController : Controller
    {
        private readonly DataContext db;
        public ChatController(DataContext db)
        {
            this.db = db;
        }
        [HttpPost]
        public async Task<JsonResult> StartConversation([FromBody] StartConversationRequest req)
        {
            var buyerId = req.BuyerId;

            // TODO: logic tìm hoặc tạo conversation
            var conversation = await db.Conversations
                .FirstOrDefaultAsync(c => c.BuyerId == buyerId);

            if (conversation == null)
            {
                conversation = new ConversationModel
                {
                    BuyerId = buyerId,
                    Title = "Chat với Admin",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                db.Conversations.Add(conversation);
                await db.SaveChangesAsync();
            }

            return Json(new { conversationId = conversation.Id });
        }

        [HttpGet]
        public async Task<IActionResult> GetMessages(int conversationId, DateTime? before, int take = 5)
        {
            var query = db.Messages
                          .Where(m => m.ConversationId == conversationId);

            // Nếu có before thì lấy tin nhắn cũ hơn mốc đó
            if (before.HasValue)
            {
                query = query.Where(m => m.CreatedAt < before.Value);
            }

            // Lấy theo CreatedAt DESC (mới -> cũ) rồi Take(take)
            var messages = await query.Include(m => m.Sender)
                .OrderByDescending(m => m.CreatedAt)
                .Take(take)
                .Select(m => new MessageViewModel
                {
                    ConversationId = m.ConversationId,
                    MessageId = m.Id,
                    SenderId = m.SenderId,
                    Content = m.Content,
                    CreatedAt = m.CreatedAt,
                    ProductId = m.ProductId,
                    ReplyToMessageId = m.ReplyToMessageId,
                    SenderName = m.Sender != null ? m.Sender.FullName : "Unknown",
                })
                .ToListAsync();


            return Json(messages);
        }
        [HttpGet]
        public async Task<IActionResult> GetLatestMessage(int conversationId)
        {
            var message = await db.Messages
                .Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.CreatedAt).
                Select(m => new MessageViewModel { 
                    ConversationId = m.ConversationId, 
                    MessageId = m.Id, 
                    SenderId = m.SenderId, 
                    Content = m.Content, 
                    CreatedAt = m.CreatedAt,
                    ProductId = m.ProductId, 
                    ReplyToMessageId = m.ReplyToMessageId,
                    SenderName = m.Sender != null ? m.Sender.FullName : "Unknown", })
                .FirstOrDefaultAsync(); 
            // <-- dùng async
            if (message == null) 
            { 
                return Json(new { success = false, message = "Không tìm thấy tin nhắn" }); 
            }
            return Json(new { success = true, message = message }); 
        }
        [HttpGet]
        public async Task<IActionResult> GetMessageById(int? messageId)
        {
            try
            {
                // ✅ Tìm tin nhắn theo Id
                var message = await db.Messages
                    .Where(m => m.Id == messageId)
                    .Select(m => new MessageViewModel
                    {
                        ConversationId = m.ConversationId,
                        MessageId = m.Id,
                        SenderId = m.SenderId,
                        SenderName = m.Sender != null ? m.Sender.FullName : "Unknown",
                        Content = m.Content,
                        CreatedAt = m.CreatedAt,
                        ProductId = m.ProductId,
                        ReplyToMessageId = m.ReplyToMessageId
                    })
                    .FirstOrDefaultAsync();

                // ✅ Kiểm tra nếu không tìm thấy
                if (message == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy tin nhắn" });
                }

                // ✅ Trả về JSON chuẩn để client dễ dùng
                return Json(new { success = true, message });
            }
            catch (Exception ex)
            {
                // ✅ Bắt lỗi an toàn, tránh crash app
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetConversation(Guid buyerId)
        {
            var conversationId = await db.Conversations
                .Where(c => c.BuyerId == buyerId)
                .Select(c => c.Id)
                .FirstOrDefaultAsync(); // <-- async

            if (conversationId == 0)
            {
                return Json(new { success = false, message = "Không tìm thấy cuộc trò chuyện" });
            }

            return Json(new { success = true, conversationId = conversationId });
        }
        [HttpGet]
        public async Task<IActionResult> GetAllConversations()
        {
            var conversations = await db.Conversations
                .Select(c => new { id = c.Id, buyerName = c.Buyer.FullName })
                .ToListAsync(); // <-- async

            return Json(conversations);
        }
        [HttpGet]
        public async Task<IActionResult> ListChatUser()
        {
            Guid adminId = Guid.Parse("06c3778b-cbf9-41da-91d2-d21bab91d25b");

            var allMessages = await db.Messages
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync(); // <-- async

            var latestMessages = allMessages
                .GroupBy(m => m.ConversationId)
                .Select(g => g.First()) // tin nhắn mới nhất
                .ToList();

            var result = latestMessages
                .Join(db.Conversations,
                      msg => msg.ConversationId,
                      c => c.Id,
                      (msg, c) => new { msg, conversation = c })
                .Join(db.User,
                      x => x.conversation.BuyerId,
                      u => u.UserId,
                      (x, u) => new ChatUserViewModel
                      {
                          BuyerId = u.UserId,
                          BuyerName = u.FullName,
                          LastMessage = x.msg.Content,
                          SentTime = x.msg.CreatedAt,
                          ConversationId = x.msg.ConversationId,
                          isRead = x.msg.IsRead,
                          AvatarUrl = u.AvatarUrl
                      })
                .OrderByDescending(x => x.SentTime)
                .ToList();

            return PartialView("ListChatUser", result);
        }
        [HttpPost]
        public async Task<IActionResult> ReadMessage(int conversationId)
        {
            var messages = await db.Messages.Where(m=> m.ConversationId ==conversationId && m.IsRead == false).ToListAsync();
            foreach (var msg in messages)
            {
                msg.IsRead = true;
            }
            await db.SaveChangesAsync();
            return Ok(new { success = true });
        }
        [HttpGet]
        public IActionResult GetUnreadConversationCount()
        {
            var count = db.Conversations
        .Select(c => c.Messages
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefault())
        .Count(m => m != null && !m.IsRead);

            return Json(count);
        }


        public IActionResult Index()
        {
            return View();
        }
    }
}
