using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using System.Text;
using ShopElegance.Core.DTOs;
using ShopElegance.Core.Interfaces.Services;

namespace ShopElegance.Infrastructure.Email;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    // ─── Public methods ──────────────────────────────────────────────────
    public async Task SendOrderConfirmationAsync(string email, OrderDto order)
    {
        var subject = $"✦ Order Confirmed — {order.OrderNumber}";
        var body = BuildOrderConfirmationHtml(order);
        await SendAsync(email, subject, body);
    }

    public async Task SendOrderStatusUpdateAsync(string email, OrderDto order)
    {
        var subject = $"Your order {order.OrderNumber} is now {order.Status}";
        var body = BuildOrderStatusHtml(order);
        await SendAsync(email, subject, body);
    }

    public async Task SendWelcomeEmailAsync(string email, string name)
    {
        var subject = "Welcome to Practora Store ✦";
        var body = BuildWelcomeHtml(name);
        await SendAsync(email, subject, body);
    }

    public async Task SendPasswordResetAsync(string email, string resetLink)
    {
        var subject = "Reset your Practora Store password";
        var body = BuildPasswordResetHtml(resetLink);
        await SendAsync(email, subject, body);
    }

    // ─── Core send ───────────────────────────────────────────────────────
    private async Task SendAsync(string to, string subject, string htmlBody)
    {
        var smtp = _config.GetSection("Email:Smtp");
        var host = smtp["Host"] ?? "smtp.gmail.com";
        var port = int.Parse(smtp["Port"] ?? "587");
        var user = smtp["Username"] ?? throw new InvalidOperationException("SMTP username not configured");
        var pass = smtp["Password"] ?? throw new InvalidOperationException("SMTP password not configured");
        var fromAddr = smtp["From"] ?? user;
        var fromName = smtp["FromName"] ?? "Practora Store";

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(user, pass),
            Timeout = 15_000
        };

        using var message = new MailMessage
        {
            From = new MailAddress(fromAddr, fromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true,
            BodyEncoding = Encoding.UTF8,
            SubjectEncoding = Encoding.UTF8
        };
        message.To.Add(to);

        try
        {
            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}: {Subject}", to, subject);
            throw;
        }
    }

    // ─── HTML Templates ──────────────────────────────────────────────────
    private static string Wrap(string title, string body) => $@"
        <!DOCTYPE html>
        <html lang=""en"">
        <head>
          <meta charset=""UTF-8""/>
          <meta name=""viewport"" content=""width=device-width,initial-scale=1""/>
          <title>{title}</title>
          <style>
            body{{font-family:'Segoe UI',Arial,sans-serif;background:#f4f5f7;margin:0;padding:0}}
            .wrapper{{max-width:600px;margin:32px auto;background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 4px 16px rgba(0,0,0,.08)}}
            .header{{background:linear-gradient(135deg,#2c1f4a,#6c5ce7);padding:32px;text-align:center}}
            .header h1{{color:#d4a853;font-size:1.8rem;margin:0;letter-spacing:.05em}}
            .header p{{color:rgba(255,255,255,.7);margin:6px 0 0;font-size:.9rem}}
            .body{{padding:32px}}
            .btn{{display:inline-block;background:#6c5ce7;color:#fff;padding:14px 32px;border-radius:8px;text-decoration:none;font-weight:600;margin:20px 0}}
            .order-table{{width:100%;border-collapse:collapse;margin:20px 0}}
            .order-table th{{background:#f4f5f7;text-align:left;padding:10px 12px;font-size:.8rem;text-transform:uppercase;letter-spacing:.06em;color:#888}}
            .order-table td{{padding:12px;border-bottom:1px solid #f0f0f8;font-size:.9rem}}
            .total-row td{{font-weight:700;font-size:1rem;border-top:2px solid #e8e8f0}}
            .footer{{background:#f4f5f7;padding:20px;text-align:center;font-size:.78rem;color:#aaa}}
            .footer a{{color:#6c5ce7}}
          </style>
        </head>
        <body>
          <div class=""wrapper"">
            <div class=""header"">
              <h1>✦ Practora Store</h1>
              <p>Practical College Tools & Accessories</p>
            </div>
            <div class=""body"">{body}</div>
            <div class=""footer"">
              © {DateTime.Now.Year} Practora Store. All rights reserved.<br/>
              <a href=""https://practora.store/Home/Privacy"">Privacy Policy</a>
            </div>
          </div>
        </body>
        </html>
        ";

    private static string BuildOrderConfirmationHtml(OrderDto order)
    {
        var rows = string.Join("", order.Items.Select(i => $"""
            <tr>
              <td>{i.ProductName}</td>
              <td style="text-align:center">{i.Quantity}</td>
              <td style="text-align:right">{i.TotalPrice:C}</td>
            </tr>
            """));

        var body = $"""
            <h2 style="color:#2c1f4a;margin-top:0">Order Confirmed! 🎉</h2>
            <p>Hi {order.ShippingAddress.FirstName}, your order has been received and is being processed.</p>
            <p><strong>Order Number:</strong> <span style="color:#6c5ce7;font-size:1.1rem">{order.OrderNumber}</span></p>

            <table class="order-table">
              <thead><tr><th>Product</th><th style="text-align:center">Qty</th><th style="text-align:right">Total</th></tr></thead>
              <tbody>
                {rows}
                <tr><td colspan="2" style="text-align:right;font-size:.85rem;color:#888">Subtotal</td><td style="text-align:right">{order.SubTotal:C}</td></tr>
                <tr><td colspan="2" style="text-align:right;font-size:.85rem;color:#888">Shipping</td><td style="text-align:right">{order.ShippingCost:C}</td></tr>
                <tr><td colspan="2" style="text-align:right;font-size:.85rem;color:#888">Tax</td><td style="text-align:right">{order.TaxAmount:C}</td></tr>
                <tr class="total-row"><td colspan="2" style="text-align:right">Total</td><td style="text-align:right;color:#6c5ce7">{order.Total:C}</td></tr>
              </tbody>
            </table>

            <p><strong>Shipping to:</strong><br/>
            {order.ShippingAddress.FirstName} {order.ShippingAddress.LastName}<br/>
            {order.ShippingAddress.AddressLine1}<br/>
            {order.ShippingAddress.City}, {order.ShippingAddress.State} {order.ShippingAddress.PostalCode}</p>

            <a href="https://practora.store/Account/OrderDetail/{order.OrderNumber}" class="btn">Track Your Order →</a>
            """;

        return Wrap($"Order Confirmed — {order.OrderNumber}", body);
    }

    private static string BuildOrderStatusHtml(OrderDto order)
    {
        var body = $"""
            <h2 style="color:#2c1f4a;margin-top:0">Order Update</h2>
            <p>Hi {order.ShippingAddress.FirstName}, your order status has been updated.</p>
            <p><strong>Order:</strong> {order.OrderNumber}<br/>
               <strong>New Status:</strong> <span style="color:#6c5ce7;font-weight:700">{order.Status}</span></p>
            {(string.IsNullOrEmpty(order.TrackingNumber) ? "" : $"<p><strong>Tracking Number:</strong> {order.TrackingNumber}</p>")}
            <a href="https://practora.store/Account/OrderDetail/{order.OrderNumber}" class="btn">View Order →</a>
            """;
        return Wrap("Order Status Update", body);
    }

    private static string BuildWelcomeHtml(string name) => Wrap("Welcome to Practora Store", $"""
        <h2 style="color:#2c1f4a;margin-top:0">Welcome, {name}! 👋</h2>
        <p>We're thrilled to have you as part of the Practora Store community. Your account is ready.</p>
        <p>Explore our practical gear and tools designed for your college & professional needs.</p>
        <p><strong>Use code <span style="color:#6c5ce7">WELCOME10</span> for 10% off your first order.</strong></p>
        <a href="https://practora.store/Shop" class="btn">Start Shopping →</a>
        """);

    private static string BuildPasswordResetHtml(string resetLink) => Wrap("Reset Your Password", $"""
        <h2 style="color:#2c1f4a;margin-top:0">Reset Your Password</h2>
        <p>We received a request to reset the password for your Practora Store account.</p>
        <p>Click the button below to choose a new password. This link expires in <strong>24 hours</strong>.</p>
        <a href="{resetLink}" class="btn">Reset Password →</a>
        <p style="margin-top:24px;color:#888;font-size:.85rem">If you didn't request a password reset, please ignore this email. Your password will not change.</p>
        """);
}