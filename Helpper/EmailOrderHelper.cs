using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using ProjectTest1.Models;
using ProjectTest1.ViewModels;

namespace ProjectTest1.Helpper
{
    public class EmailOrderHelper
    {
        private readonly IConfiguration _config;

        public EmailOrderHelper(IConfiguration config)
        {
            _config = config;
        }

        // Gửi email xác nhận đơn hàng
        public async Task SendOrderConfirmationAsync(string toEmail, OrderViewModel order)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress("Shop ABC", _config["EmailSettings:From"]));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = $"Shop ABC - Xác nhận đơn hàng #{order.OrderId}";

            // Build body HTML
            var body = $@"
<h2>Shop ABC - Xác nhận đơn hàng</h2>
<p>Xin chào <strong>{order.userName}</strong>,</p>
<p>Cảm ơn bạn đã đặt hàng. Đơn hàng của bạn đã được tiếp nhận:</p>

<h3>Thông tin đơn hàng</h3>
<ul>
  <li>Ngày đặt: {order.CreatedAt:dd/MM/yyyy HH:mm}</li>
  <li>Trạng thái: {order.Status}</li>
  <li>Tổng tiền: <strong>{order.Amount:N0} VND</strong></li>
</ul>

<h3>Chi tiết sản phẩm</h3>
<table border='1' cellpadding='6' cellspacing='0' width='100%'>
  <tr>
    <th>Ảnh</th>
    <th>Sản phẩm</th>
    <th>Màu</th>
    <th>Size</th>
    <th>Số lượng</th>
    <th>Đơn giá</th>
    <th>Thành tiền</th>
  </tr>";

            foreach (var item in order.Items)
            {
                body += $@"
  <tr>
    <td><img src='{item.image}' alt='{item.ProductName}' width='50'/></td>
    <td>{item.ProductName}</td>
    <td>{item.Color}</td>
    <td>{item.Size}</td>
    <td>{item.Quantity}</td>
    <td>{item.UnitPrice:N0} VND</td>
    <td>{item.TotalPrice:N0} VND</td>
  </tr>";
            }

            body += $@"
</table>

<h3>Địa chỉ giao hàng</h3>
<p>
  {order.ShippingAddress}<br />
</p>

<p>Chúng tôi sẽ liên hệ và giao hàng trong thời gian sớm nhất.</p>
<p>Nếu có thắc mắc, vui lòng liên hệ hotline: 0123-456-789</p>";

            email.Body = new TextPart(TextFormat.Html) { Text = body };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                _config["EmailSettings:Host"],
                int.Parse(_config["EmailSettings:Port"]),
                SecureSocketOptions.StartTls);

            await smtp.AuthenticateAsync(
                _config["EmailSettings:From"],
                _config["EmailSettings:Password"]);

            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}

