using System.Net;
using System.Net.Mail;

namespace Api.Services;

public interface ISmtpEmailSender
{
    Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken);
}

public sealed class SmtpEmailSender : ISmtpEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken)
    {
        var host = Setting("Host", "MAIL_HOST") ?? throw new InvalidOperationException("SMTP host is not configured.");
        var port = int.TryParse(Setting("Port", "MAIL_PORT"), out var configuredPort) ? configuredPort : 587;
        var username = Setting("Username", "MAIL_USERNAME");
        var password = Setting("Password", "MAIL_PASSWORD");
        var fromAddress = Setting("FromAddress", "MAIL_FROM_ADDRESS") ?? username
            ?? throw new InvalidOperationException("SMTP from address is not configured.");
        var fromName = Setting("FromName", "MAIL_FROM_NAME") ?? "KSB Loyalty";
        var encryption = Setting("Encryption", "MAIL_ENCRYPTION");
        var enableSsl = !string.Equals(encryption, "none", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(encryption, "false", StringComparison.OrdinalIgnoreCase)
            && port != 25 && port != 1025;

        _logger.LogInformation(
            "SMTP send starting. Host={Host} Port={Port} Encryption={Encryption} SSL={EnableSsl} UsernameConfigured={UsernameConfigured} From={From} To={To}",
            host, port, encryption ?? "default", enableSsl, !string.IsNullOrWhiteSpace(username), fromAddress, recipient);

        using var message = new MailMessage
        {
            From = new MailAddress(fromAddress, fromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };
        message.To.Add(recipient);

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = string.IsNullOrWhiteSpace(username)
        };
        if (!string.IsNullOrWhiteSpace(username)) client.Credentials = new NetworkCredential(username, password);

        using var smtpTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        smtpTimeout.CancelAfter(TimeSpan.FromSeconds(30));
        try
        {
            await client.SendMailAsync(message, smtpTimeout.Token);
            _logger.LogInformation("SMTP send completed successfully. To={To}", recipient);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError("SMTP server timed out after 30 seconds. Host={Host} Port={Port}", host, port);
            throw new TimeoutException("The SMTP server did not respond within 30 seconds.");
        }
        catch (SmtpException exception)
        {
            _logger.LogError(exception, "SMTP send failed. StatusCode={StatusCode} Host={Host} Port={Port}", exception.StatusCode, host, port);
            throw;
        }
    }

    private string? Setting(string key, string environmentName) =>
        Environment.GetEnvironmentVariable(environmentName)
        ?? Environment.GetEnvironmentVariable($"Mail__{key}")
        ?? _configuration[$"Mail:{key}"];
}
