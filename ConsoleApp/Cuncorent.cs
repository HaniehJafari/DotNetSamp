using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;

var app = WebApplication.CreateBuilder(args).Build();


ConcurrentBag<int> numbers = new ConcurrentBag<int>();

app.MapPost("/add", async (HttpContext context) =>
{
    var form = await context.Request.ReadFormAsync();
    
  
    if (form.TryGetValue("number", out var numberStr) && int.TryParse(numberStr, out var number))
    {
       
        numbers.Add(number);
        await context.Response.WriteAsync("Added");
    }
    else
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync("Invalid number");
    }
});


app.MapGet("/sum", () => numbers.Sum());

app.Run();





//Second way

long totalSum = 0;

app.MapPost("/add", async (HttpContext context) =>
{
    var form = await context.Request.ReadFormAsync();
    if (form.TryGetValue("number", out var numberStr) && int.TryParse(numberStr, out var number))
    {
     
        Interlocked.Add(ref totalSum, number);
        await context.Response.WriteAsync("Added");
    }
   
});

app.MapGet("/sum", () => Interlocked.Read(ref totalSum));
