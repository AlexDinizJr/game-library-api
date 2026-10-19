using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http.Json;
using GameLibraryApi.Entities;
using GameLibraryApi.Exceptions;
using GameLibraryApi.Interfaces;
using GameLibraryApi.Options;
using Microsoft.Extensions.Options;

namespace GameLibraryApi.Services;

public sealed class IgdbClient(IHttpClientFactory httpClientFactory, IOptions<IgdbOptions> options) : IIgdbClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("igdb");
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _tokenExpiresAt;

    public async Task<IReadOnlyList<Game>> SearchGamesAsync(
        string query,
        int limit,
        CancellationToken cancellationToken)
    {
        var escapedQuery = query.Trim().Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
        var apiQuery = $"fields id,name,summary,first_release_date,genres.name,platforms.name,involved_companies.company.name,involved_companies.developer,involved_companies.publisher,cover.image_id; search \"{escapedQuery}\"; limit {limit};";
        var games = await SendGamesRequestAsync(apiQuery, cancellationToken);
        return games.Select(MapGame).ToArray();
    }

    public async Task<Game?> GetGameAsync(long externalId, CancellationToken cancellationToken)
    {
        var apiQuery = $"fields id,name,summary,first_release_date,genres.name,platforms.name,involved_companies.company.name,involved_companies.developer,involved_companies.publisher,cover.image_id; where id = {externalId}; limit 1;";
        var games = await SendGamesRequestAsync(apiQuery, cancellationToken);
        return games.Count == 0 ? null : MapGame(games[0]);
    }

    private async Task<IReadOnlyList<IgdbGameResponse>> SendGamesRequestAsync(
        string apiQuery,
        CancellationToken cancellationToken)
    {
        var credentials = options.Value;
        EnsureCredentialsConfigured(credentials);
        var accessToken = await GetAccessTokenAsync(credentials, cancellationToken);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri(new Uri(credentials.ApiBaseUrl, UriKind.Absolute), "games"));
        request.Headers.TryAddWithoutValidation("Client-ID", credentials.ClientId);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = new StringContent(apiQuery, Encoding.UTF8, "text/plain");

        using var response = await SendProviderRequestAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new IgdbApiException($"IGDB returned status code {(int)response.StatusCode}.");
        }

        try
        {
            var games = await response.Content.ReadFromJsonAsync<List<IgdbGameResponse>>(
                JsonOptions,
                cancellationToken);
            return games ?? [];
        }
        catch (JsonException exception)
        {
            throw new IgdbApiException("IGDB returned an invalid game response.", exception);
        }
    }

    private async Task<string> GetAccessTokenAsync(IgdbOptions credentials, CancellationToken cancellationToken)
    {
        if (_accessToken is not null && DateTimeOffset.UtcNow < _tokenExpiresAt.AddSeconds(-30))
        {
            return _accessToken;
        }

        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            if (_accessToken is not null && DateTimeOffset.UtcNow < _tokenExpiresAt.AddSeconds(-30))
            {
                return _accessToken;
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, credentials.OAuthUrl)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = credentials.ClientId,
                    ["client_secret"] = credentials.ClientSecret,
                    ["grant_type"] = "client_credentials"
                })
            };

            using var response = await SendProviderRequestAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new IgdbApiException($"Twitch OAuth returned status code {(int)response.StatusCode}.");
            }

            try
            {
                var token = await response.Content.ReadFromJsonAsync<OAuthTokenResponse>(
                    JsonOptions,
                    cancellationToken);
                if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
                {
                    throw new IgdbApiException("Twitch OAuth returned an empty access token.");
                }

                _accessToken = token.AccessToken;
                _tokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn);
                return _accessToken;
            }
            catch (JsonException exception)
            {
                throw new IgdbApiException("Twitch OAuth returned an invalid token response.", exception);
            }
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private async Task<HttpResponseMessage> SendProviderRequestAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new IgdbApiException("Could not connect to the IGDB/Twitch API.", exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new IgdbApiException("The IGDB/Twitch API request timed out.", exception);
        }
    }

    private static void EnsureCredentialsConfigured(IgdbOptions credentials)
    {
        if (string.IsNullOrWhiteSpace(credentials.ClientId)
            || string.IsNullOrWhiteSpace(credentials.ClientSecret)
            || credentials.ClientId.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase)
            || credentials.ClientSecret.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase))
        {
            throw new IgdbConfigurationException(
                "Set Igdb:ClientId and Igdb:ClientSecret using environment variables before using IGDB endpoints.");
        }
    }

    private static Game MapGame(IgdbGameResponse source)
    {
        var companies = source.InvolvedCompanies ?? [];
        return new Game
        {
            ExternalId = source.Id,
            Title = source.Name ?? string.Empty,
            Description = source.Summary,
            ReleaseDate = source.FirstReleaseDate is long timestamp
                ? DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime
                : null,
            Genres = source.Genres?.Select(item => item.Name).Where(name => name is not null).Cast<string>().ToList() ?? [],
            Platforms = source.Platforms?.Select(item => item.Name).Where(name => name is not null).Cast<string>().ToList() ?? [],
            Developers = companies
                .Where(company => company.Developer && company.Company?.Name is not null)
                .Select(company => company.Company!.Name!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Publishers = companies
                .Where(company => company.Publisher && company.Company?.Name is not null)
                .Select(company => company.Company!.Name!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            CoverImageUrl = string.IsNullOrWhiteSpace(source.Cover?.ImageId)
                ? null
                : $"https://images.igdb.com/igdb/image/upload/t_cover_big/{source.Cover.ImageId}.jpg"
        };
    }

    private sealed class OAuthTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; init; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; init; }
    }

    private sealed class IgdbGameResponse
    {
        public long Id { get; init; }

        public string? Name { get; init; }

        public string? Summary { get; init; }

        [JsonPropertyName("first_release_date")]
        public long? FirstReleaseDate { get; init; }

        public List<NameReference>? Genres { get; init; }

        public List<NameReference>? Platforms { get; init; }

        [JsonPropertyName("involved_companies")]
        public List<CompanyInvolvement>? InvolvedCompanies { get; init; }

        public CoverReference? Cover { get; init; }
    }

    private sealed class NameReference
    {
        public string? Name { get; init; }
    }

    private sealed class CoverReference
    {
        [JsonPropertyName("image_id")]
        public string? ImageId { get; init; }
    }

    private sealed class CompanyInvolvement
    {
        public CompanyReference? Company { get; init; }

        public bool Developer { get; init; }

        public bool Publisher { get; init; }
    }

    private sealed class CompanyReference
    {
        public string? Name { get; init; }
    }
}
