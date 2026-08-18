using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

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
    public string ContactRecipient { get; set; } = "";
    public bool EnableSsl { get; set; } = true;
}

public interface IPasswordResetEmailSender
{
    Task SendAsync(string recipientEmail, string resetCode, CancellationToken cancellationToken = default);
}

public sealed class SmtpPasswordResetEmailSender(
    IOptions<SmtpOptions> options,
    IWebHostEnvironment environment) : IPasswordResetEmailSender
{
    private readonly SmtpOptions _options = options.Value;
    private readonly IWebHostEnvironment _environment = environment;

    public async Task SendAsync(string recipientEmail, string resetCode, CancellationToken cancellationToken = default)
    {
        ValidateConfiguration();

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
        message.To.Add(MailboxAddress.Parse(recipientEmail));
        message.Subject = "Restablece tu contraseña de Campo Market";

        var templatePath = Path.Combine(
            AppContext.BaseDirectory,
            "EmailTemplates",
            "RecuperarContrasena.html");

        var htmlBody = await EmailTemplateLoader.LoadAsync(
            templatePath,
            new Dictionary<string, string>
            {
                ["Codigo"] = resetCode
            },
            cancellationToken);

        var textBody = await EmailTemplateLoader.LoadAsync(
            Path.Combine(AppContext.BaseDirectory, "EmailTemplates", "RecuperarContrasena.txt"),
            new Dictionary<string, string> { ["Codigo"] = resetCode },
            cancellationToken);

        var bodyBuilder = new BodyBuilder
        {
            TextBody = textBody,
            HtmlBody = htmlBody
        };

        var logoPath = Path.Combine(_environment.WebRootPath, "Images", "Logo.png");

        if (File.Exists(logoPath))
        {
            var logo = bodyBuilder.LinkedResources.Add(logoPath);
            logo.ContentId = "campomarket-logo";
        }

        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();

        var socketOptions = !_options.EnableSsl
            ? SecureSocketOptions.None
            : _options.Port == 465
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls;

        await client.ConnectAsync(_options.Host, _options.Port, socketOptions, cancellationToken);
        await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
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
