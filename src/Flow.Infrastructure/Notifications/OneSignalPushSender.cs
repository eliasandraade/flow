using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Flow.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Flow.Infrastructure.Notifications;

/// <summary>
/// Sends push notifications through OneSignal.
///
/// Users are addressed by external id, which the mobile client sets to the Flow user id
/// after login. E-mail is never used as an identifier: it is neither stable nor an
/// authorisation claim.
///
/// The REST API key lives only here, on the server. It is never shipped to the app, and it
/// never reaches a log line.
/// </summary>
public sealed class OneSignalPushSender : IPushNotificationSender
{
    public const string HttpClientName = "onesignal";

    private readonly HttpClient _httpClient;
    private readonly OneSignalOptions _options;
    private readonly ILogger<OneSignalPushSender> _logger;

    public OneSignalPushSender(
        HttpClient httpClient,
        IOptions<OneSignalOptions> options,
        ILogger<OneSignalPushSender> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.AppId) && !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<PushDeliveryResult> SendAsync(
        PushNotificationRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured) return PushDeliveryResult.NotConfigured();

        var payload = new OneSignalRequest
        {
            AppId = _options.AppId,
            TargetChannel = "push",
            IncludeAliases = new AliasTargets { ExternalId = [request.UserId.ToString()] },
            Headings = new Dictionary<string, string> { ["en"] = request.Title },
            Contents = new Dictionary<string, string> { ["en"] = request.Body },
            Data = request.DeepLink is null
                ? null
                : new Dictionary<string, string> { ["deepLink"] = request.DeepLink },

            // The provider requires an RFC 9562 UUID here and keeps it for 30 days, so the
            // outbox message id is used verbatim: it is already a Guid, and it is the same
            // value on every retry of the same message. Our own DedupeKey is not a UUID and
            // does not belong in this field.
            IdempotencyKey = request.DeliveryId.ToString()
        };

        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, "notifications")
            {
                Content = JsonContent.Create(payload)
            };

            // OneSignal's current scheme is "Key <api key>", not Basic.
            message.Headers.Authorization = new AuthenticationHeaderValue("Key", _options.ApiKey);

            using var response = await _httpClient.SendAsync(message, cancellationToken);

            if (response.IsSuccessStatusCode)
                return await InterpretSuccessAsync(response, request, cancellationToken);

            var detail = await SafeReadAsync(response, cancellationToken);

            // 401/403 mean the credentials are wrong; retrying will not fix that, and a
            // 4xx body is a request problem rather than a blip.
            return IsTransient(response.StatusCode)
                ? PushDeliveryResult.Transient($"HTTP {(int)response.StatusCode}: {detail}")
                : PushDeliveryResult.Permanent($"HTTP {(int)response.StatusCode}: {detail}");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return PushDeliveryResult.Transient("Timed out waiting for the push provider.");
        }
        catch (HttpRequestException ex)
        {
            return PushDeliveryResult.Transient(ex.Message);
        }
        catch (Exception ex)
        {
            // Logged without the request, which carries the Authorization header.
            _logger.LogError(ex, "Unexpected failure dispatching a push notification.");
            return PushDeliveryResult.Transient("Unexpected provider failure.");
        }
    }

    /// <summary>
    /// A 2xx from this endpoint does not mean a notification exists.
    ///
    /// The provider answers 200 for any request it accepted, and only returns an id when a
    /// message was actually created; no id means it was not, typically because nobody in
    /// the target audience has a live subscription. Treating every 2xx as delivered is how
    /// an outbox ends up full of messages marked as sent that nobody ever received, which
    /// is worse than a queue that is honestly stuck.
    /// </summary>
    private async Task<PushDeliveryResult> InterpretSuccessAsync(
        HttpResponseMessage response,
        PushNotificationRequest request,
        CancellationToken cancellationToken)
    {
        OneSignalResponse? body;

        try
        {
            body = await response.Content.ReadFromJsonAsync<OneSignalResponse>(cancellationToken);
        }
        catch (Exception ex) when (ex is System.Text.Json.JsonException or InvalidOperationException
            or HttpRequestException)
        {
            // Accepted, but the answer is unreadable, so there is no evidence a message
            // was created. Retrying is safe because the delivery carries a stable
            // idempotency key.
            return PushDeliveryResult.Transient(
                "The push provider answered with a body that could not be read.");
        }

        if (!string.IsNullOrWhiteSpace(body?.Id))
            return PushDeliveryResult.Delivered(body!.Id);

        // The provider states why it created nothing. That is a fact about this recipient,
        // not a blip, so retrying the same message would just repeat the same answer.
        if (body?.Errors is { Count: > 0 })
        {
            _logger.LogWarning(
                "Push provider accepted the request for outbox message {DeliveryId} but created "
                + "no message: {Reason}",
                request.DeliveryId, string.Join("; ", body.Errors));

            return PushDeliveryResult.Permanent(
                $"Provider created no message: {string.Join("; ", body.Errors)}");
        }

        // Accepted, no id, no reason. Ambiguous, and one more attempt is safe precisely
        // because the delivery carries a stable idempotency key: if a message was in fact
        // created, the retry returns the original result instead of duplicating it.
        return PushDeliveryResult.Transient(
            "Provider returned success without a message id and without a reason.");
    }

    private static bool IsTransient(HttpStatusCode status) =>
        status is HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;

    private static async Task<string> SafeReadAsync(
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return content.Length > 500 ? content[..500] : content;
        }
        catch
        {
            return "<unreadable response body>";
        }
    }

    private sealed class OneSignalRequest
    {
        [JsonPropertyName("app_id")] public string AppId { get; init; } = string.Empty;
        [JsonPropertyName("target_channel")] public string TargetChannel { get; init; } = "push";
        [JsonPropertyName("include_aliases")] public AliasTargets IncludeAliases { get; init; } = new();
        [JsonPropertyName("headings")] public Dictionary<string, string> Headings { get; init; } = [];
        [JsonPropertyName("contents")] public Dictionary<string, string> Contents { get; init; } = [];

        [JsonPropertyName("data")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, string>? Data { get; init; }

        [JsonPropertyName("idempotency_key")] public string IdempotencyKey { get; init; } = string.Empty;
    }

    private sealed class AliasTargets
    {
        [JsonPropertyName("external_id")] public List<string> ExternalId { get; init; } = [];
    }

    private sealed class OneSignalResponse
    {
        [JsonPropertyName("id")] public string? Id { get; init; }
        [JsonPropertyName("errors")] public List<string>? Errors { get; init; }
    }
}
