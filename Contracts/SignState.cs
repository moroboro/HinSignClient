// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);

using System.Text.Json.Serialization;

namespace HinQesSignDemo.Contracts;

public record EnvelopeItem(
    [property: JsonPropertyName("comments")]
    IReadOnlyList<object> Comments,
    [property: JsonPropertyName("file_id")]
    string FileId,
    [property: JsonPropertyName("file_url")]
    string FileUrl,
    [property: JsonPropertyName("jurisdiction")]
    string Jurisdiction,
    [property: JsonPropertyName("legal_weight")]
    string LegalWeight,
    [property: JsonPropertyName("signed_at")]
    DateTime SignedAt,
    [property: JsonPropertyName("status")] string Status
);

public record SignStatus(
    [property: JsonPropertyName("cancelled")]
    bool Cancelled,
    [property: JsonPropertyName("created_at")]
    DateTime CreatedAt,
    [property: JsonPropertyName("envelope_items")]
    IReadOnlyList<EnvelopeItem> EnvelopeItems,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("signer")] Signer Signer,
    [property: JsonPropertyName("url")] string Url
);

public record Signer(
    [property: JsonPropertyName("email")] string Email
);