using System.Text.Encodings.Web;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/echo", async (HttpContext context) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var requestBody = await reader.ReadToEndAsync();

  
    if (requestBody.Length > 500)
    {
        return Results.BadRequest("Message too long.");
    }

  
    var safeInput = HtmlEncoder.Default.Encode(requestBody);

    var responseMessage = $"You entered: {safeInput}";

   
    return Results.Text(responseMessage, contentType: "text/plain; charset=utf-8");
});

app.Run();