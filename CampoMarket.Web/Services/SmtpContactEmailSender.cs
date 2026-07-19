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
        var safePhone = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(contact.Telefono) ? "No indicado" : contact.Telefono.Trim());
        var safeMessage = WebUtility.HtmlEncode(contact.Mensaje.Trim()).Replace("\r\n", "<br>").Replace("\n", "<br>");
        var bodyBuilder = new BodyBuilder
        {
            TextBody = $"""
                Nueva consulta desde Campo Market

                Nombre: {contact.Nombre.Trim()}
                Correo: {contact.Correo.Trim()}
                Teléfono: {(string.IsNullOrWhiteSpace(contact.Telefono) ? "No indicado" : contact.Telefono.Trim())}

                Mensaje:
                {contact.Mensaje.Trim()}
                """,
            HtmlBody = $$"""
                <!doctype html>
                <html lang="es">
                <body style="margin:0;background:#f3f6f1;font-family:Arial,Helvetica,sans-serif;color:#243127;">
                  <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#f3f6f1;padding:32px 12px;">
                    <tr><td align="center">
                      <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:620px;background:#fff;border-radius:16px;overflow:hidden;box-shadow:0 8px 24px rgba(29,69,43,.10);">
                        <tr><td align="center" style="background:#edf6ec;padding:24px;"><img src="cid:campomarket-logo" alt="Campo Market" width="145" style="display:block;max-width:145px;height:auto;"></td></tr>
                        <tr><td style="padding:32px 38px;">
                          <h1 style="margin:0 0 22px;font-size:24px;color:#205b32;">Nueva consulta</h1>
                          <table role="presentation" width="100%" cellspacing="0" cellpadding="6" style="font-size:15px;line-height:1.5;">
                            <tr><td style="font-weight:bold;width:90px;">Nombre:</td><td>{{safeName}}</td></tr>
                            <tr><td style="font-weight:bold;">Correo:</td><td><a href="mailto:{{safeEmail}}" style="color:#2f7d43;">{{safeEmail}}</a></td></tr>
                            <tr><td style="font-weight:bold;">Teléfono:</td><td>{{safePhone}}</td></tr>
                          </table>
                          <div style="margin-top:22px;padding:18px;background:#f7faf6;border-left:4px solid #2f7d43;border-radius:6px;font-size:15px;line-height:1.65;">{{safeMessage}}</div>
                          <p style="margin:22px 0 0;font-size:13px;color:#718075;">Responde este correo para contestarle directamente al visitante.</p>
                        </td></tr>
                      </table>
                    </td></tr>
                  </table>
                </body>
                </html>
                """
        };

        var logoPath = Path.Combine(_environment.WebRootPath, "Images", "Logo.png");
        if (File.Exists(logoPath))
        {
            var logo = bodyBuilder.LinkedResources.Add(logoPath);
            logo.ContentId = "campomarket-logo";
        }

        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        var socketOptions = _options.Port == 465
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
            throw new InvalidOperationException("La configuración SMTP para contacto está incompleta.");
        }
    }
}
