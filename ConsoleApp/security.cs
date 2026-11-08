using System.Text.Encodings.Web;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/echo", async (HttpContext context) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var requestBody = await reader.ReadToEndAsync();

    // 1. Validation (اعتبارسنجی)
    // بررسی طول ورودی با سینتکس صحیح (Length با L بزرگ)
    if (requestBody.Length > 500)
    {
        return Results.BadRequest("Message too long.");
    }

    // 2. Output Encoding (ایمن‌سازی در برابر Injection و XSS)
    // به جای Regex، از متد استاندارد دات‌نت برای خنثی‌سازی کاراکترهای مخرب HTML/JS استفاده می‌کنیم.
    // مثلاً <script> تبدیل می‌شود به &lt;script&gt; و بی‌خطر می‌گردد.
    var safeInput = HtmlEncoder.Default.Encode(requestBody);

    var responseMessage = $"You entered: {safeInput}";

    // 3. Safe Response (پاسخ امن)
    // استفاده از Results.Text به جای WriteAsync خام، تا Content-Type به صورت صریح روی text/plain تنظیم شود.
    // این کار جلوی تفسیر اشتباه مرورگر (MIME-Sniffing) را می‌گیرد.
    return Results.Text(responseMessage, contentType: "text/plain; charset=utf-8");
});

app.Run();