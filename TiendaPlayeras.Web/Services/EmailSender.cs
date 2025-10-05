using MailKit.Net.Smtp;
using MimeKit;


namespace TiendaPlayeras.Web.Services
{
/// <summary>
/// Envío de correos con MailKit vía SMTP (Gmail).
/// </summary>
public class EmailSender
{
private readonly IConfiguration _cfg;
public EmailSender(IConfiguration cfg) => _cfg = cfg;


/// <summary>Envía un correo simple en texto/HTML.</summary>
public async Task SendAsync(string to, string subject, string html)
{
var msg = new MimeMessage();
msg.From.Add(MailboxAddress.Parse(_cfg["Smtp:From"]));
msg.To.Add(MailboxAddress.Parse(to));
msg.Subject = subject;
msg.Body = new TextPart("html") { Text = html };


using var client = new SmtpClient();
await client.ConnectAsync(_cfg["Smtp:Host"], int.Parse(_cfg["Smtp:Port"]!), MailKit.Security.SecureSocketOptions.StartTls);
await client.AuthenticateAsync(_cfg["Smtp:User"], _cfg["Smtp:Password"]);
await client.SendAsync(msg);
await client.DisconnectAsync(true);
}
}
}