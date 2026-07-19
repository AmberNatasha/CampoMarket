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

        var bodyBuilder = new BodyBuilder
        {
            TextBody = $"""
                Hola:

                Recibimos una solicitud para restablecer la contraseña de tu cuenta de Campo Market.

                Ingresa esta clave en Campo Market para crear una nueva contraseña:
                {resetCode}

                El enlace vence en una hora y solo puede utilizarse una vez. Si no solicitaste este cambio, ignora este mensaje.
                """,
            HtmlBody = $$"""
                <!doctype html>
                <html lang="es">
                <body style="margin:0;background:#f3f6f1;font-family:Arial,Helvetica,sans-serif;color:#243127;">
                  <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#f3f6f1;padding:32px 12px;">
                    <tr>
                      <td align="center">
                        <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:600px;background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 8px 24px rgba(29,69,43,.10);">
                          <tr>
                            <td align="center" style="background:#edf6ec;padding:28px 24px 20px;">
                              <img src="cid:campomarket-logo" alt="Campo Market" width="150" style="display:block;max-width:150px;height:auto;">
                            </td>
                          </tr>
                          <tr>
                            <td style="padding:34px 38px;">
                              <h1 style="margin:0 0 18px;font-size:25px;line-height:1.25;color:#205b32;">Restablece tu contraseña</h1>
                              <p style="margin:0 0 14px;font-size:16px;line-height:1.6;">Hola:</p>
                              <p style="margin:0 0 18px;font-size:16px;line-height:1.6;">Recibimos una solicitud para restablecer la contraseña de tu cuenta de Campo Market.</p>
                              <p style="margin:0 0 12px;font-size:14px;line-height:1.6;color:#536057;text-align:center;">Ingresa esta clave en la pantalla de recuperación:</p>
                              <div style="margin:0 auto 26px;padding:16px 20px;max-width:280px;background:#edf6ec;border:1px solid #c9dec9;border-radius:10px;text-align:center;font-family:Consolas,monospace;font-size:30px;font-weight:bold;letter-spacing:6px;color:#205b32;">{{resetCode}}</div>
                              <p style="margin:0 0 12px;font-size:14px;line-height:1.6;color:#536057;">La clave vence en una hora y solo puede utilizarse una vez.</p>
                              <p style="margin:0;font-size:14px;line-height:1.6;color:#536057;">Si no solicitaste este cambio, puedes ignorar este mensaje.</p>
                            </td>
                          </tr>
                          <tr>
                            <td align="center" style="border-top:1px solid #e4ebe3;padding:18px 24px;font-size:12px;color:#718075;">
                              Este es un mensaje automático de Campo Market.
                            </td>
                          </tr>
                        </table>
                      </td>
                    </tr>
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
            || string.IsNullOrWhiteSpace(_options.FromEmail))
        {
            throw new InvalidOperationException(
                "La configuración SMTP está incompleta. Configura Smtp:Password y Smtp:FromEmail mediante secretos o variables de entorno.");
        }
    }
}
