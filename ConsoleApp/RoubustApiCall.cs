
using System.Net;
using System.Net.Http;
using System.Text.Json;

namespace RobustnessDotnetConsoleApp;

internal class Program
{
    private static async Task Main()
    {
        using var cts = new CancellationTokenSource();

        // با Ctrl+C درخواست‌های در حال اجرا را لغو می‌کنیم.
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        // Timeout مانع انتظار نامحدود برای پاسخ سرور می‌شود.
        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        // آدرس نمونه برای دریافت اطلاعات JSON.
        var apiClient = new RobustHttpClient(httpClient);

        Console.WriteLine("HTTP Robustness Challenge");
        Console.WriteLine("Press Ctrl+C to cancel the request.\n");

        try
        {
            var posts = await apiClient.GetAsync<List<Post>>(
                "https://jsonplaceholder.typicode.com/posts",
                cts.Token);

            if (posts is null || posts.Count == 0)
            {
                Console.WriteLine("No data was returned.");
                return;
            }

            Console.WriteLine($"Successfully received {posts.Count} posts.");

            foreach (var post in posts.Take(5))
            {
                Console.WriteLine(
                    $"ID: {post.Id}, Title: {post.Title}");
            }
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            Console.WriteLine("\nRequest cancelled by the user.");
        }
        catch (Exception ex)
        {
            // آخرین لایه محافظ برای خطاهای پیش‌بینی‌نشده است.
            Console.WriteLine($"Unexpected application error: {ex.Message}");
        }
    }
}

public sealed class RobustHttpClient
{
    private readonly HttpClient _httpClient;
    private const int MaxAttempts = 3;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public RobustHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<T?> GetAsync<T>(
        string url,
        CancellationToken cancellationToken = default)
    {
        // ورودی نامعتبر را پیش از ارسال درخواست شناسایی می‌کنیم.
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp &&
             uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException(
                "A valid absolute HTTP/HTTPS URL is required.",
                nameof(url));
        }

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var response = await _httpClient.GetAsync(
                    uri,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

                // تمام پاسخ‌های ناموفق در این بخش بررسی می‌شوند.
                if (!response.IsSuccessStatusCode)
                {
                    var statusCode = response.StatusCode;

                    Console.WriteLine(
                        $"HTTP {(int)statusCode} ({statusCode}), " +
                        $"attempt {attempt}/{MaxAttempts}");

                    // فقط خطاهای موقت ارزش تلاش مجدد دارند.
                    if (IsTransientStatusCode(statusCode) &&
                        attempt < MaxAttempts)
                    {
                        var delay = GetRetryDelay(response, attempt);

                        await Task.Delay(delay, cancellationToken);
                        continue;
                    }

                    throw new HttpRequestException(
                        $"HTTP request failed with status " +
                        $"{(int)statusCode} ({statusCode}).",
                        null,
                        statusCode);
                }

                // فقط پاسخ موفق را به مدل موردنظر تبدیل می‌کنیم.
                try
                {
                    await using var stream =
                        await response.Content.ReadAsStreamAsync(
                            cancellationToken);

                    var result = await JsonSerializer.DeserializeAsync<T>(
                        stream,
                        JsonOptions,
                        cancellationToken);

                    if (result is null)
                    {
                        throw new InvalidDataException(
                            "The response body was empty or contained JSON null.");
                    }

                    return result;
                }
                catch (JsonException ex)
                {
                    // JSON نامعتبر معمولاً با تکرار درخواست اصلاح نمی‌شود.
                    throw new InvalidDataException(
                        "The server returned invalid JSON.", ex);
                }
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                // لغو توسط کاربر نباید باعث Retry شود.
                throw;
            }
            catch (OperationCanceledException ex)
            {
                // Timeout داخلی HttpClient یا لغو مستقل از توکن کاربر.
                Console.WriteLine(
                    $"Request timed out or was cancelled: {ex.Message}");

                if (attempt == MaxAttempts)
                {
                    throw new TimeoutException(
                        "The HTTP request timed out after all attempts.",
                        ex);
                }

                await Task.Delay(
                    GetBackoffDelay(attempt),
                    cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                // خطاهای شبکه مانند DNS، اتصال و TLS ممکن است موقت باشند.
                Console.WriteLine($"HTTP/network error: {ex.Message}");

                if (!IsRetryableNetworkError(ex) ||
                    attempt == MaxAttempts)
                {
                    throw;
                }

                await Task.Delay(
                    GetBackoffDelay(attempt),
                    cancellationToken);
            }
        }

        throw new InvalidOperationException(
            "The request ended without producing a result.");
    }

    private static bool IsTransientStatusCode(HttpStatusCode statusCode)
    {
        // خطاهای موقتی شناخته‌شده؛ همه 4xxها قابل Retry نیستند.
        return statusCode == HttpStatusCode.RequestTimeout       // 408
            || (int)statusCode == 429                             // Too Many Requests
            || statusCode == HttpStatusCode.InternalServerError  // 500
            || statusCode == HttpStatusCode.BadGateway            // 502
            || statusCode == HttpStatusCode.ServiceUnavailable   // 503
            || statusCode == HttpStatusCode.GatewayTimeout;      // 504
    }

    private static bool IsRetryableNetworkError(
        HttpRequestException exception)
    {
        // خطاهای دارای StatusCode در مسیر پاسخ HTTP بررسی شده‌اند.
        // خطاهای بدون StatusCode معمولاً مربوط به شبکه یا اتصال‌اند.
        return exception.StatusCode is null;
    }

    private static TimeSpan GetRetryDelay(
        HttpResponseMessage response,
        int attempt)
    {
        // اگر سرور Retry-After داده باشد، آن را رعایت می‌کنیم.
        var retryAfter = response.Headers.RetryAfter;

        if (retryAfter?.Delta is TimeSpan delta && delta > TimeSpan.Zero)
        {
            return delta > TimeSpan.FromSeconds(30)
                ? TimeSpan.FromSeconds(30)
                : delta;
        }

        if (retryAfter?.Date is DateTimeOffset date)
        {
            var remaining = date - DateTimeOffset.UtcNow;

            if (remaining > TimeSpan.Zero)
            {
                return remaining > TimeSpan.FromSeconds(30)
                    ? TimeSpan.FromSeconds(30)
                    : remaining;
            }
        }

        return GetBackoffDelay(attempt);
    }

    private static TimeSpan GetBackoffDelay(int attempt)
    {
        // Exponential backoff: حدود 1، سپس 2 ثانیه.
        var seconds = Math.Pow(2, attempt - 1);

        return TimeSpan.FromSeconds(Math.Min(seconds, 10));
    }
}

public sealed class Post
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;
}
