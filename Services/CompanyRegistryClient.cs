using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace LLB.Services;

public interface ICompanyRegistryClient
{
    Task<CompanyRegistryResult> GetCompanyAsync(string companyNumber, CancellationToken cancellationToken = default);
}

public sealed class CompanyRegistryClient : ICompanyRegistryClient
{
    private const string TokenCacheKey = "company-registry:access-token";
    private static readonly SemaphoreSlim TokenLock = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly CompanyRegistryOptions _options;
    private readonly ILogger<CompanyRegistryClient> _logger;

    public CompanyRegistryClient(
        HttpClient httpClient,
        IMemoryCache cache,
        IOptions<CompanyRegistryOptions> options,
        ILogger<CompanyRegistryClient> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<CompanyRegistryResult> GetCompanyAsync(
        string companyNumber,
        CancellationToken cancellationToken = default)
    {
        var normalizedCompanyNumber = NormalizeCompanyNumber(companyNumber);
        var cacheKey = $"company-registry:company:{normalizedCompanyNumber}";

        if (_cache.TryGetValue(cacheKey, out CompanyRegistryResult? cachedCompany) && cachedCompany is not null)
        {
            return cachedCompany;
        }

        var token = await GetAccessTokenAsync(forceRefresh: false, cancellationToken);
        var response = await SendCompanyRequestAsync(normalizedCompanyNumber, token, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            response.Dispose();
            token = await GetAccessTokenAsync(forceRefresh: true, cancellationToken);
            response = await SendCompanyRequestAsync(normalizedCompanyNumber, token, cancellationToken);
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new CompanyRegistryException("No company was found with that company number.", response.StatusCode);
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Company registry lookup failed for {CompanyNumber} with HTTP {StatusCode}.",
                    normalizedCompanyNumber,
                    (int)response.StatusCode);
                throw new CompanyRegistryException("The company registry is currently unavailable.", response.StatusCode);
            }

            CompanyRegistryResponse? registryResponse;
            try
            {
                await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                registryResponse = await JsonSerializer.DeserializeAsync<CompanyRegistryResponse>(
                    responseStream,
                    JsonOptions,
                    cancellationToken);
            }
            catch (JsonException exception)
            {
                _logger.LogWarning(exception, "The company registry returned an invalid response.");
                throw new CompanyRegistryException("The company registry returned an invalid response.");
            }

            var company = registryResponse?.CompanyInfo?.Value;
            if (company is null || string.IsNullOrWhiteSpace(company.CompanyNumber))
            {
                throw new CompanyRegistryException("No company was found with that company number.", HttpStatusCode.NotFound);
            }

            var result = new CompanyRegistryResult
            {
                CompanyInfo = company,
                Directors = registryResponse?.Directors?.Value?
                    .Where(director => !string.IsNullOrWhiteSpace(director.Names)
                        || !string.IsNullOrWhiteSpace(director.Surname)
                        || !string.IsNullOrWhiteSpace(director.NationalId))
                    .ToArray() ?? Array.Empty<CompanyRegistryDirector>()
            };

            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(Math.Max(1, _options.CompanyCacheMinutes)));
            return result;
        }
    }

    private async Task<HttpResponseMessage> SendCompanyRequestAsync(
        string companyNumber,
        string token,
        CancellationToken cancellationToken)
    {
        var requestUri = $"api/v1/reports/getCompanyInfos?companyNum={Uri.EscapeDataString(companyNumber)}";
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    }

    private async Task<string> GetAccessTokenAsync(bool forceRefresh, CancellationToken cancellationToken)
    {
        if (!forceRefresh && _cache.TryGetValue(TokenCacheKey, out string? cachedToken) && !string.IsNullOrWhiteSpace(cachedToken))
        {
            return cachedToken;
        }

        await TokenLock.WaitAsync(cancellationToken);
        try
        {
            if (!forceRefresh && _cache.TryGetValue(TokenCacheKey, out cachedToken) && !string.IsNullOrWhiteSpace(cachedToken))
            {
                return cachedToken;
            }

            if (string.IsNullOrWhiteSpace(_options.UserName) || string.IsNullOrWhiteSpace(_options.Password))
            {
                throw new CompanyRegistryException("Company registry credentials have not been configured.");
            }

            using var response = await _httpClient.PostAsJsonAsync(
                "api/Account/login",
                new { _options.UserName, _options.Password },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Company registry login failed with HTTP {StatusCode}.", (int)response.StatusCode);
                throw new CompanyRegistryException("The company registry is currently unavailable.", response.StatusCode);
            }

            var login = await response.Content.ReadFromJsonAsync<CompanyRegistryLoginResponse>(JsonOptions, cancellationToken);
            if (string.IsNullOrWhiteSpace(login?.Token))
            {
                throw new CompanyRegistryException("The company registry did not return an access token.");
            }

            _cache.Set(TokenCacheKey, login.Token, TimeSpan.FromMinutes(Math.Max(1, _options.TokenCacheMinutes)));
            return login.Token;
        }
        finally
        {
            TokenLock.Release();
        }
    }

    private static string NormalizeCompanyNumber(string companyNumber)
    {
        var normalized = companyNumber?.Trim().ToUpperInvariant() ?? string.Empty;
        if (normalized.Length == 0)
        {
            throw new CompanyRegistryException("Enter a company number.", HttpStatusCode.BadRequest);
        }

        if (normalized.Length > 50)
        {
            throw new CompanyRegistryException("The company number is too long.", HttpStatusCode.BadRequest);
        }

        return normalized;
    }

    private sealed class CompanyRegistryResponse
    {
        [JsonPropertyName("companyinfo")]
        public RegistryValue<CompanyRegistryCompany>? CompanyInfo { get; init; }

        [JsonPropertyName("directors")]
        public RegistryValue<List<CompanyRegistryDirector>>? Directors { get; init; }
    }

    private sealed class RegistryValue<T>
    {
        [JsonPropertyName("value")]
        public T? Value { get; init; }
    }

    private sealed class CompanyRegistryLoginResponse
    {
        [JsonPropertyName("token")]
        public string? Token { get; init; }
    }
}

public sealed class CompanyRegistryOptions
{
    public const string SectionName = "CompanyRegistry";

    public string BaseUrl { get; set; } = "https://cipz.pfms.gov.zw:9090/";
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 15;
    public int TokenCacheMinutes { get; set; } = 30;
    public int CompanyCacheMinutes { get; set; } = 5;
}

public sealed class CompanyRegistryResult
{
    public CompanyRegistryCompany CompanyInfo { get; init; } = new();
    public IReadOnlyList<CompanyRegistryDirector> Directors { get; init; } = Array.Empty<CompanyRegistryDirector>();
}

public sealed class CompanyRegistryCompany
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? CompanyNumber { get; init; }
    public string? Type { get; init; }
    public string? PhysicalAddress { get; init; }
    public string? PostalAddress { get; init; }
    public string? City { get; init; }
    public string? TelNumber { get; init; }
    public string? MobileNumber { get; init; }
    public string? Email { get; init; }
}

public sealed class CompanyRegistryDirector
{
    public string? Names { get; init; }
    public string? Surname { get; init; }

    [JsonPropertyName("id_no")]
    public string? NationalId { get; init; }

    public string? Address { get; init; }
    public string? Email { get; init; }
}

public sealed class CompanyRegistryException : Exception
{
    public CompanyRegistryException(string message, HttpStatusCode? statusCode = null)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode? StatusCode { get; }
}
