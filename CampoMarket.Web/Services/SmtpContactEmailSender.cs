using System.Net;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using CampoMarket.Web.Models;

namespace CampoMarket.Web.Services;

public interface IContactEmailSender
{
    Task SendAsync(ContactoViewModel contact, CancellationToken cancellationToken = default);
}

public sealed class SmtpContactEmailSender(
    IOptions<SmtpOptions> options,
    IWebHostEnvironment environment) : IContactEmailSender
{
    private readonly SmtpOptions _options = options.Value;
    private readonly IWebHostEnvironment _environment = environment;

    public async Task SendAsync(ContactoViewModel contact, CancellationToken cancellationToken = default)
    {
        ValidateConfiguration();

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
        message.To.Add(MailboxAddress.Parse(_options.ContactRecipient));
        message.ReplyTo.Add(new MailboxAddress(contact.Nombre.Trim(), contact.Correo.Trim()));
        message.Subject = "Nueva consulta desde Campo Market";

        var safeName = WebUtility.HtmlEncode(contact.Nombre.Trim());
        var safeEmail = WebUtility.HtmlEncode(contact.Correo.Trim());
        var safePhone = WebUtility.HtmlEncode(
            string.IsNullOrWhiteSpace(contact.Telefono)
                ? "No indicado"
                : contact.Telefono.Trim());

        var safeMessage = WebUtility.HtmlEncode(contact.Mensaje.Trim())
            .Replace("\r\n", "<br>")
            .Replace("\n", "<br>");

        var templatePath = Path.Combine(
            AppContext.BaseDirectory,
            "EmailTemplates",
            "Contacto.html");

        var htmlBody = await EmailTemplateLoader.LoadAsync(
            templatePath,
            new Dictionary<string, string>
            {
                ["Nombre"] = safeName,
                ["Correo"] = safeEmail,
                ["Telefono"] = safePhone,
                ["Mensaje"] = safeMessage
            },
            cancellationToken);

        var textBody = await EmailTemplateLoader.LoadAsync(
            Path.Combine(AppContext.BaseDirectory, "EmailTemplates", "Contacto.txt"),
            new Dictionary<string, string>
            {
                ["Nombre"] = contact.Nombre.Trim(),
                ["Correo"] = contact.Correo.Trim(),
                ["Telefono"] = string.IsNullOrWhiteSpace(contact.Telefono) ? "No indicado" : contact.Telefono.Trim(),
                ["Mensaje"] = contact.Mensaje.Trim()
            },
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
            || string.IsNullOrWhiteSpace(_options.FromEmail)
            || string.IsNullOrWhiteSpace(_options.ContactRecipient))
        {
            throw new InvalidOperationException(
                "La configuración SMTP para contacto está incompleta.");
        }
    }
}
