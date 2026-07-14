using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace CampoMarket.Web.Services;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string FromEmail { get; set; } = "";
    public string FromName { get; set; } = "Campo Market";
    public bool EnableSsl { get; set; } = true;
}

public interface IPasswordResetEmailSender
{
    Task SendAsync(string recipientEmail, string resetUrl, CancellationToken cancellationToken = default);
}

public sealed class SmtpPasswordResetEmailSender(IOptions<SmtpOptions> options) : IPasswordResetEmailSender
{
    private readonly SmtpOptions _options = options.Value;

    public async Task SendAsync(string recipientEmail, string resetUrl, CancellationToken cancellationToken = default)
    {
        ValidateConfiguration();

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromEmail, _options.FromName),
            Subject = "Restablece tu contraseña de Campo Market",
            Body = $"""
                Hola:

                Recibimos una solicitud para restablecer la contraseña de tu cuenta de Campo Market.

                Abre este enlace para crear una nueva contraseña:
                {resetUrl}

                El enlace vence en una hora y solo puede utilizarse una vez. Si no solicitaste este cambio, ignora este mensaje.
                """,
            IsBodyHtml = false
        };
        message.To.Add(new MailAddress(recipientEmail));

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_options.Username, _options.Password),
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(message, cancellationToken);
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.Host)
            || string.IsNullOrWhiteSpace(_options.Username)
            || string.IsNullOrWhiteSpace(_options.Password)
            || string.IsNullOrWhiteSpace(_options.FromEmail))
        {
            throw new InvalidOperationException(
                "La configuración SMTP está incompleta. Configura Smtp:Password y Smtp:FromEmail mediante secretos o variables de entorno.");
        }
    }
}
