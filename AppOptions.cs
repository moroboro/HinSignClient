namespace HinQesSignDemo;

public class AppOptions
{
    public const string ConfigurationSectionName = "Application";

    public required string AppName { get; init; }

    public required int AppPort { get; init; }

    public required Uri CallbackUrl { get; init; }

    public required CertifactionOptions Certifaction { get; init; }
}

public class CertifactionOptions
{
    public required Uri ContainerHost { get; init; }

    public required string UserToken { get; init; }


    public required string UserEmail { get; init; }
}