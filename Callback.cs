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

    public static Results<NoContent, BadRequest> Handle(HttpContext context, [FromServices] ICertifactionClient client,
        [FromServices] IOptions<AppOptions> options)
    {
        var appOptions = options.Value;

        Console.WriteLine("Callback get called!!!!!");

        IQueryCollection query = context.Request.Query;

        if (query.ContainsKey("token")) Console.WriteLine($"Callback Url get call with parameter: {query["token"]}");

        Console.WriteLine("Check the state of signature");

        var result = client.CheckStatus(RequestUri!, appOptions.Certifaction.UserToken);

        return TypedResults.NoContent();
    }
}