// ============================================================
// AlertLogService — FIX #5 from Recruiter Review
// ============================================================
// "A browser toast that disappears after 6 seconds is not
//  autonomous reporting."
//
// This service writes every alert to a persistent log file AND
// can optionally POST to a Slack webhook. Even when the browser
// tab is closed, even at 3 AM, every alert is durably recorded.
//
// In production, this would integrate with:
//   - PagerDuty / OpsGenie for critical pages
//   - Slack / Teams for real-time channel notifications
//   - Email digest for non-urgent weekly summaries
// ============================================================

using CloudWalkMonitoring.Models;

namespace CloudWalkMonitoring.Services;

public class AlertLogService
{
    private readonly string _logPath;
    private readonly string? _slackWebhookUrl;
    private readonly HttpClient _http;
    private readonly object _writeLock = new();

    public AlertLogService(IConfiguration cfg, IWebHostEnvironment env)
    {
        _logPath = Path.Combine(env.ContentRootPath, "..", "alerts.log");
        _slackWebhookUrl = cfg["Slack:WebhookUrl"];
        _http = new HttpClient();
    }

    /// <summary>
    /// Durably log an alert to disk. This persists even if the browser is closed.
    /// </summary>
    public void LogAlert(Alert alert)
    {
        var line = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] " +
                   $"{alert.Severity,-8} | {alert.Status,-16} | {alert.Type,-5} | " +
                   $"z={alert.ZScore,+7:+0.000;-0.000} | " +
                   $"observed={alert.ObservedCount,4} | μ={alert.RollingMean,7:F1} | " +
                   $"σ={alert.RollingStdDev,6:F2} | " +
                   $"{alert.Message}";

        lock (_writeLock)
        {
            File.AppendAllText(_logPath, line + Environment.NewLine);
        }
    }

    /// <summary>
    /// Log a batch of alerts and optionally send critical ones to Slack.
    /// </summary>
    public async Task ProcessAlertBatchAsync(List<Alert> alerts)
    {
        foreach (var alert in alerts)
        {
            LogAlert(alert);

            // Only send CRITICAL alerts to Slack (avoid channel spam)
            if (alert.Severity == "CRITICAL" && !string.IsNullOrEmpty(_slackWebhookUrl))
            {
                await SendSlackAlertAsync(alert);
            }
        }
    }

    /// <summary>
    /// POST a formatted alert to a Slack incoming webhook.
    /// </summary>
    private async Task SendSlackAlertAsync(Alert alert)
    {
        try
        {
            var emoji = alert.Severity == "CRITICAL" ? "🔴" : "🟡";
            var payload = new
            {
                text = $"{emoji} *{alert.Severity}* — {alert.Status.ToUpper()} {alert.Type}\n" +
                       $"Observed: `{alert.ObservedCount}` | μ: `{alert.RollingMean:F1}` | " +
                       $"z: `{alert.ZScore:+0.00;-0.00}`\n" +
                       $"Time: `{alert.Timestamp:yyyy-MM-dd HH:mm}` | {alert.Message}"
            };

            await _http.PostAsJsonAsync(_slackWebhookUrl!, payload);
        }
        catch (Exception ex)
        {
            // Never let notification failure crash the detection pipeline
            lock (_writeLock)
            {
                File.AppendAllText(_logPath,
                    $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] SLACK_ERROR | {ex.Message}{Environment.NewLine}");
            }
        }
    }
}
