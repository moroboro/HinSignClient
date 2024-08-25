using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HinQesSignDemo;

public static class SignBatch
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
            hb.AppendHtml("<title>HIN batch signing demo</title>");
            hb.AppendHtml("</head>");
            hb.AppendHtml("<body>");
            hb.AppendHtml("<h2>HIN QES Batch signing</h2>");
            hb.AppendHtml("<h3>1. Upload files for signing</h3>");

            var files = new List<FileToSign>();

            await using var fileStream1 = new FileStream("FileToSign_1.pdf", FileMode.Open, FileAccess.Read);
            var file1Uri = await client.Prepare(fileStream1, appOptions.Certifaction.UserToken, "de");
            if (file1Uri is null)
            {
                Console.WriteLine("Upload file #1 failed.");
                return TypedResults.BadRequest("Upload file #1 failed.");
            }

            files.Add(new FileToSign(file1Uri.ToString(), "FileToSign_1.pdf"));

            hb.AppendHtml($"<p>The first file was uploaded successfully: {file1Uri}</p>");
            Console.WriteLine($"File #1 : {file1Uri}");

            await using var fileStream2 = new FileStream("FileToSign_2.pdf", FileMode.Open, FileAccess.Read);
            var file2Uri = await client.Prepare(fileStream2, appOptions.Certifaction.UserToken, "de");
            if (file2Uri is null)
            {
                Console.WriteLine("Upload file #2 failed.");
                return TypedResults.BadRequest("Upload file #2 failed.");
            }

            files.Add(new FileToSign(file2Uri.ToString(), "FileToSign_2.pdf"));

            hb.AppendHtml($"<p>The second file was uploaded successfully: {file2Uri}</p>");
            hb.AppendLine();
            Console.WriteLine($"File #2 : {file2Uri}");


            hb.AppendHtml("<h3>2. Request signature</h3>");

            var requestId = Guid.NewGuid().ToString("D");

            var callback = new UriBuilder(appOptions.CallbackUrl) { Query = $"?token={requestId}" };

            var url = await client.QesSignFiles(appOptions.Certifaction.UserToken, "de",
                appOptions.Certifaction.UserEmail,
                files.ToArray(), callback.Uri);

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