using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HinQesSignDemo;

public static class SignSingle
{
    public static async Task<Results<ContentHttpResult, BadRequest<string>>> Handle(HttpContext context,
        [FromServices] ICertifactionClient client,
        [FromServices] IOptions<AppOptions> options)
    {
        AppOptions appOptions = options.Value;

        try
        {
            var hb = new HtmlContentBuilder();
            hb.AppendHtml("<!DOCTYPE html>");
            hb.AppendHtml("<html lang='en'>");
            hb.AppendHtml("<head>");
            hb.AppendHtml("<meta charset='UTF-8'>");
            hb.AppendHtml("<meta name='viewport' content='width=device-width, initial-scale=1.0'>");
            hb.AppendHtml("<title>HIN QES single file sign demo</title>");
            hb.AppendHtml("</head>");
            hb.AppendHtml("<body>");
            hb.AppendHtml("<h2>HIN QES single file sign demo</h2>");
            hb.AppendHtml("<h3>Request signature</h3>");


            await using var fileStream1 = new FileStream("FileToSign_1.pdf", FileMode.Open, FileAccess.Read);

            var requestId = Guid.NewGuid().ToString("D");
            var callback = new UriBuilder(appOptions.CallbackUrl) { Query = $"?token={requestId}" };

            var url = await client.QesSignFile(fileStream1, "single-file.pdf", appOptions.Certifaction.UserToken, "de",
                appOptions.Certifaction.UserEmail, callback.Uri);
            if (url is null)
            {
                Console.WriteLine("Failed to get signing request url");
                return TypedResults.BadRequest("Failed to get signing request url");
            }

            Callback.RequestUri = url;

            hb.AppendHtml($"<p>Signature request with {requestId} was registered.");
            hb.AppendHtml(
                $"<p>Please use the following link to sign documents: <a target=\"_blank\" href=\"{url}\">{url}</a></p>");

            hb.AppendHtml("</body>");
            hb.AppendHtml("</html>");

            await using var writer = new StringWriter();
            hb.WriteTo(writer, HtmlEncoder.Default);

            return TypedResults.Content(writer.ToString(), "text/html");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return TypedResults.BadRequest(ex.ToString());
        }
    }
}