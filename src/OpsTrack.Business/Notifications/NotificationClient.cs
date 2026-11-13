using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OpsTrack.Business.Notifications;

public class NotificationOptions
{
    /// <summary>Webhook that forwards SLA alerts to the department head (config key Notification:WebhookUrl).</summary>
    public string WebhookUrl { get; set; }
}

/// <summary>Payload sent to the webhook when a work request breaches its SLA.</summary>
public class SlaBreachNotification
{
    public string Number { get; set; }

    public string Title { get; set; }

    public string LineCode { get; set; }

    public string EquipmentCode { get; set; }

    public string Priority { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime DueAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public string ClosedBy { get; set; }

    /// <summary>Who should receive the alert. The webhook maps this to a person.</summary>
    public string Recipient { get; set; } = "department-head";
}

public interface INotificationClient
{
    Task SendSlaBreachAsync(SlaBreachNotification notification, CancellationToken ct = default);
}

/// <summary>
/// Posts JSON to the plant notification webhook. Registered as a typed HttpClient in Program.cs.
/// This calls a real external system: tests must replace the HTTP handler with a stub.
/// </summary>
public class NotificationClient : INotificationClient
{
    private readonly HttpClient _http;
    private readonly NotificationOptions _options;
    private readonly ILogger<NotificationClient> _logger;

    public NotificationClient(HttpClient http, IOptions<NotificationOptions> options, ILogger<NotificationClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendSlaBreachAsync(SlaBreachNotification notification, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.WebhookUrl))
        {
            _logger.LogInformation(
                "Notification:WebhookUrl is not set, SLA breach notification for {Number} skipped",
                notification.Number);
            return;
        }

        using var response = await _http.PostAsJsonAsync(_options.WebhookUrl, notification, ct);
        response.EnsureSuccessStatusCode();

        _logger.LogInformation("SLA breach notification sent for {Number}", notification.Number);
    }
}
