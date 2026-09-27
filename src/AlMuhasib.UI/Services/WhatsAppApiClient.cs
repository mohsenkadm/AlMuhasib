using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AlMuhasib.Core.Models.Ux;

namespace AlMuhasib.UI.Services;

/// <summary>إرسال رسائل وملفات PDF عبر مزوّدات واتساب API.</summary>
public sealed class WhatsAppApiClient
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(60) };

    public async Task SendDocumentAsync(
        WhatsAppApiSettings settings,
        string phoneDigits,
        string message,
        string pdfPath,
        CancellationToken cancellationToken = default)
    {
        if (!settings.IsConfigured)
            throw new InvalidOperationException("إعدادات واتساب API غير مكتملة. أكمل المفتاح ومعرّف المثيل من إعدادات الميزات.");

        if (string.IsNullOrWhiteSpace(pdfPath) || !File.Exists(pdfPath))
            throw new InvalidOperationException("ملف PDF غير موجود للإرسال.");

        switch (settings.Provider)
        {
            case WhatsAppApiProvider.UltraMsg:
                await SendUltraMsgAsync(settings, phoneDigits, message, pdfPath, cancellationToken);
                break;
            case WhatsAppApiProvider.MetaCloud:
                await SendMetaCloudAsync(settings, phoneDigits, message, pdfPath, cancellationToken);
                break;
            case WhatsAppApiProvider.CustomWebhook:
                await SendCustomWebhookAsync(settings, phoneDigits, message, pdfPath, cancellationToken);
                break;
            default:
                throw new InvalidOperationException("مزوّد واتساب غير مدعوم.");
        }
    }

    private static async Task SendUltraMsgAsync(
        WhatsAppApiSettings settings,
        string phoneDigits,
        string message,
        string pdfPath,
        CancellationToken cancellationToken)
    {
        var instance = settings.InstanceId!.Trim();
        var token = settings.ApiKey!.Trim();
        var baseUrl = string.IsNullOrWhiteSpace(settings.BaseUrl)
            ? $"https://api.ultramsg.com/{instance}"
            : settings.BaseUrl.Trim().TrimEnd('/');

        var fileBytes = await File.ReadAllBytesAsync(pdfPath, cancellationToken);
        var fileName = Path.GetFileName(pdfPath);
        var dataUri = $"data:application/pdf;base64,{Convert.ToBase64String(fileBytes)}";

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["token"] = token,
            ["to"] = phoneDigits,
            ["filename"] = fileName,
            ["document"] = dataUri,
            ["caption"] = message
        });

        using var response = await Http.PostAsync($"{baseUrl}/messages/document", content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"فشل إرسال UltraMsg: {(int)response.StatusCode} — {body}");
    }

    private static async Task SendMetaCloudAsync(
        WhatsAppApiSettings settings,
        string phoneDigits,
        string message,
        string pdfPath,
        CancellationToken cancellationToken)
    {
        var phoneNumberId = settings.InstanceId!.Trim();
        var token = settings.ApiKey!.Trim();
        var baseUrl = string.IsNullOrWhiteSpace(settings.BaseUrl)
            ? "https://graph.facebook.com/v19.0"
            : settings.BaseUrl.Trim().TrimEnd('/');

        // 1) Upload media
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("whatsapp"), "messaging_product");
        await using var stream = File.OpenRead(pdfPath);
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(fileContent, "file", Path.GetFileName(pdfPath));

        using var uploadRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/{phoneNumberId}/media");
        uploadRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        uploadRequest.Content = form;

        using var uploadResponse = await Http.SendAsync(uploadRequest, cancellationToken);
        var uploadBody = await uploadResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!uploadResponse.IsSuccessStatusCode)
            throw new InvalidOperationException($"فشل رفع ملف Meta: {(int)uploadResponse.StatusCode} — {uploadBody}");

        using var uploadJson = JsonDocument.Parse(uploadBody);
        if (!uploadJson.RootElement.TryGetProperty("id", out var mediaIdEl))
            throw new InvalidOperationException("استجابة Meta بدون معرّف ملف.");
        var mediaId = mediaIdEl.GetString();
        if (string.IsNullOrWhiteSpace(mediaId))
            throw new InvalidOperationException("معرّف ملف Meta فارغ.");

        // 2) Send document message
        var payload = new
        {
            messaging_product = "whatsapp",
            to = phoneDigits,
            type = "document",
            document = new
            {
                id = mediaId,
                caption = message,
                filename = Path.GetFileName(pdfPath)
            }
        };

        using var sendContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var sendRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/{phoneNumberId}/messages");
        sendRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        sendRequest.Content = sendContent;

        using var sendResponse = await Http.SendAsync(sendRequest, cancellationToken);
        var sendBody = await sendResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!sendResponse.IsSuccessStatusCode)
            throw new InvalidOperationException($"فشل إرسال Meta: {(int)sendResponse.StatusCode} — {sendBody}");
    }

    private static async Task SendCustomWebhookAsync(
        WhatsAppApiSettings settings,
        string phoneDigits,
        string message,
        string pdfPath,
        CancellationToken cancellationToken)
    {
        var url = settings.BaseUrl!.Trim();
        var fileBytes = await File.ReadAllBytesAsync(pdfPath, cancellationToken);
        var payload = new
        {
            phone = phoneDigits,
            message,
            fileName = Path.GetFileName(pdfPath),
            mimeType = "application/pdf",
            fileBase64 = Convert.ToBase64String(fileBytes),
            senderId = settings.SenderId
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        if (!string.IsNullOrWhiteSpace(settings.ApiKey))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey.Trim());
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await Http.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"فشل إرسال Webhook: {(int)response.StatusCode} — {body}");
    }
}
