using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using HinQesSignDemo.Contracts;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace HinQesSignDemo;

public interface ICertifactionClient
{
    Task<Stream?> Sign(Stream file, string fileName, string hinSigningToken, string signatureLanguage,
        CancellationToken cancellation = default);

    Task<Uri?> Prepare(Stream file, string hinSigningToken, string signatureLanguage,
        CancellationToken cancellation = default);

    Task<Uri?> QesSignFiles(string hinSigningToken, string signatureLanguage, string email, FileToSign[] files,
        Uri signCallbackUrl,
        CancellationToken cancellation = default);

    Task<SignStatus?> CheckStatus(Uri requestUri, string hinSigningToken, CancellationToken cancellation = default);

    Task<Uri?> QesSignFile(Stream file, string fileName, string hinSigningToken, string signatureLanguage,
        string email, Uri signCallbackUrl, CancellationToken cancellation = default);

    Task<Stream?> DownloadFile(Uri requestUri, string hinSigningToken, CancellationToken cancellation = default);
}

public class CertifactionClient(HttpClient httpClient, ILogger<CertifactionClient> logger) : ICertifactionClient
{
    public async Task<Stream?> Sign(Stream file, string fileName, string hinSigningToken, string signatureLanguage,
        CancellationToken cancellation = default)
    {
        try
        {
            SetupHeaders(hinSigningToken, signatureLanguage);

            using var requestBody = new StreamContent(file);
            HttpResponseMessage response = await httpClient.PostAsync(
                new Uri($"sign?filename={fileName}", UriKind.Relative),
                requestBody, cancellation);
            if (response.IsSuccessStatusCode) return await response.Content.ReadAsStreamAsync(cancellation);

            var error = await response.Content.ReadAsStringAsync(cancellation);
            logger.LogError(error);

            return null;
        }
        catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or TaskCanceledException)
        {
            logger.LogError(ex, "Something went wrong");
            return null;
        }
    }

    public async Task<Uri?> Prepare(Stream file, string hinSigningToken, string signatureLanguage,
        CancellationToken cancellation = default)
    {
        try
        {
            SetupHeaders(hinSigningToken, signatureLanguage);

            using var requestBody = new StreamContent(file);
            HttpResponseMessage response = await httpClient.PostAsync(
                new Uri("prepare?scope=sign&upload=true", UriKind.Relative),
                requestBody,
                cancellation);

            if (response.IsSuccessStatusCode || response.Headers.Location != null) return response.Headers.Location!;

            return null;
        }
        catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or TaskCanceledException)
        {
            logger.LogError(ex, "Something went wrong");
            return null;
        }
    }

    public async Task<Uri?> QesSignFile(Stream file, string fileName, string hinSigningToken, string signatureLanguage,
        string email, Uri signCallbackUrl, CancellationToken cancellation = default)
    {
        try
        {
            SetupHeaders(hinSigningToken, signatureLanguage);

            var parameters = new Dictionary<string, string>
            {
                { "email", email },
                { "legal-weight", "QES" },
                { "jurisdiction", "eIDAS" },
                { "force-identification", "true" },
                { "filename", fileName },
                { "webhook-url", signCallbackUrl.ToString() }
            };
            var url = QueryHelpers.AddQueryString("request/create", parameters!);

            using var requestBody = new StreamContent(file);
            requestBody.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            
            var response = await httpClient.PostAsync(
                new Uri(url, UriKind.Relative),
                requestBody, cancellation);
            response.EnsureSuccessStatusCode();
            
            var jsonResponseContent = await response.Content.ReadAsStringAsync(cancellation);
            var responseResult =
                JsonSerializer.Deserialize<HinSignRequestResponse>(jsonResponseContent);

            return responseResult?.SignSessionUrl;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
    }


    public async Task<Uri?> QesSignFiles(string hinSigningToken, string signatureLanguage, string email,
        FileToSign[] files, Uri signCallbackUrl,
        CancellationToken cancellation = default)
    {
        try
        {
            SetupHeaders(hinSigningToken, signatureLanguage);

            var parameters = new Dictionary<string, string>
            {
                { "email", email },
                { "legal-weight", "QES" },
                { "jurisdiction", "eIDAS" },
                { "force-identification", "true" },
                { "webhook-url", signCallbackUrl.ToString() }
            };
            var url = QueryHelpers.AddQueryString("request/create", parameters!);

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(new
                    {
                        files
                    }
                )
            };

            var json = await ((JsonContent)request.Content).ReadAsStringAsync(cancellation);

            Console.WriteLine(json);

            var response = await httpClient.SendAsync(request, cancellation);

            response.EnsureSuccessStatusCode();

            var jsonResponseContent = await response.Content.ReadAsStringAsync(cancellation);
            var responseResult =
                JsonSerializer.Deserialize<HinSignRequestResponse>(jsonResponseContent);

            return responseResult?.SignSessionUrl;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
    }

    public async Task<SignStatus?> CheckStatus(Uri requestUri, string hinSigningToken,
        CancellationToken cancellation = default)
    {
        try
        {
            SetupHeaders(hinSigningToken, "en");

            var encode = UrlEncoder.Default.Encode(requestUri.ToString());
            var response =
                await httpClient.GetAsync(
                    $"request/status?request_url={encode}", cancellation);
            response.EnsureSuccessStatusCode();

            var status = await response.Content.ReadFromJsonAsync<SignStatus>(cancellation);

            Console.WriteLine(status);

            return status;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
    }

    public async Task<Stream?> DownloadFile(Uri requestUri, string hinSigningToken, CancellationToken cancellation = default)
    {
        try
        {
            SetupHeaders(hinSigningToken);

            var encode = UrlEncoder.Default.Encode(requestUri.ToString());
            var response = await httpClient.GetAsync(
                new Uri($"download?file={encode}", UriKind.Relative), cancellation);
            if (response.IsSuccessStatusCode) return await response.Content.ReadAsStreamAsync(cancellation);

            var error = await response.Content.ReadAsStringAsync(cancellation);
            logger.LogError(error);

            return null;
        }
        catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or TaskCanceledException)
        {
            logger.LogError(ex, "Something went wrong");
            return null;
        }
    }

    private void SetupHeaders(string token, string? language = null)
    {
        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        httpClient.DefaultRequestHeaders.AcceptLanguage.Clear();
        if (language is not null)
        {
            httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue(language));
        }
    }
}

public record HinSignRequestResponse(
    [property: JsonPropertyName("request_url")]
    Uri SignSessionUrl);