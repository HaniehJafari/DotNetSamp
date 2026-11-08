using System.Globalization;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLocalization(options =>
{
    options.ResourcesPath = "Resources";
});

builder.Services.AddControllers();

var supportedCultures = new[]
{
    new CultureInfo("en-US"),
    new CultureInfo("fa-IR")
};

var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en-US"),

    SupportedCultures = supportedCultures,

    SupportedUICultures = supportedCultures
};

var app = builder.Build();

app.UseRequestLocalization(localizationOptions);

app.MapControllers();

app.Run();







//Contoller

    using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

[ApiController]
[Route("api/products")]
public class ProductController : ControllerBase
{
    private readonly IStringLocalizer<ProductController> _localizer;

    public ProductController(
        IStringLocalizer<ProductController> localizer)
    {
        _localizer = localizer;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            Message = _localizer["ProductsRetrievedSuccessfully"]
        });
    }
}



//Accept-Language: fa-IR