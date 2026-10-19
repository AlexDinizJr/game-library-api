namespace GameLibraryApi.Options;

public sealed class IgdbOptions
{
    public const string SectionName = "Igdb";

    public string ClientId { get; init; } = string.Empty;

    public string ClientSecret { get; init; } = string.Empty;

    public string ApiBaseUrl { get; init; } = "https://api.igdb.com/v4/";

    public string OAuthUrl { get; init; } = "https://id.twitch.tv/oauth2/token";
}
