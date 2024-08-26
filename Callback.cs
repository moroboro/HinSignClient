using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HinQesSignDemo;

public static class Callback
{
    public static Uri? RequestUri =
        new Uri(
            "https://sign-test.hin.ch/sign?s&signatureRequestID=r3cH21h0AfJ#W1tBcXa_r_3IPwfFqrakAWK1qu8IZja-YZJV_k75b_M=");

    public static async Task<NoContent> Handle(HttpContext context, [FromServices] ICertifactionClient client,
        [FromServices] IOptions<AppOptions> options)
    {
        var appOptions = options.Value;

        Console.WriteLine("Callback get called!!!!!");

        var query = context.Request.Query;

        if (query.ContainsKey("token")) Console.WriteLine($"Callback Url get call with parameter: {query["token"]}");

        Console.WriteLine("Check the state of signature");

        var signState = await client.CheckStatus(RequestUri!, appOptions.Certifaction.UserToken);

        if (signState is not null && !signState.Cancelled && signState.EnvelopeItems[0].Status == "signed")
        {
            await using var contentStream = await client.DownloadFile(RequestUri!, appOptions.Certifaction.UserToken);
            if (contentStream is null)
            {
                Console.WriteLine("Cannot download signed file");
                return TypedResults.NoContent();
            }

            string resultFileName = $"c:\\temp\\result_{Guid.NewGuid().ToString("D")[..8]}.pdf";

            await using var fileStream = new FileStream(resultFileName, FileMode.Create, FileAccess.Write,
                FileShare.None, 8192, true);

            await contentStream.CopyToAsync(fileStream);
            
            Console.WriteLine("File downloaded successfully. File name: {0}", resultFileName);
            
            return TypedResults.NoContent();
        }
        
        Console.WriteLine("Signing was canceled by the user.");
        return TypedResults.NoContent();
    }
}