using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory; // تغییر: برای استفاده از In-Memory Cache
using System.Collections.Generic;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache(); // تغییر: ثبت IMemoryCache برای Cache کردن پاسخ‌های تکراری

var app = builder.Build();

var data = new List<Item>();

for (int i = 0; i < 10000; i++)
{
    // تغییر: Task.Delay(1) حذف شد؛ چون در کد قبلی await نشده بود
    // و بنابراین هیچ تأخیری ایجاد نمی‌کرد و فقط overhead ایجاد می‌کرد.

    data.Add(new Item { Id = i, Name = $"Item {i}", Value = i % 100 });
}

// تغییر: داده‌ها یک بار در زمان Startup مرتب می‌شوند.
// در کد قبلی OrderByDescending در هر Request اجرا می‌شد.
// با این کار عملیات Sort تکراری از مسیر Request حذف می‌شود.
data = data
    .OrderByDescending(x => x.Value)
    .ThenBy(x => x.Id)
    .ToList();

app.MapGet("/items", async (
    HttpRequest request,
    IMemoryCache cache) => // تغییر: IMemoryCache برای استفاده از Cache به Endpoint تزریق شد
{
    var query = request.Query;

    var filter = query["filter"].ToString();

    // تغییر: دریافت شماره صفحه از Query String
    // اگر مقدار نامعتبر یا کمتر از 1 باشد، صفحه 1 استفاده می‌شود.
    var page = int.TryParse(query["page"], out var pageNumber) && pageNumber > 0
        ? pageNumber
        : 1;

    // تغییر: دریافت تعداد آیتم‌های هر صفحه
    // حداکثر 100 آیتم اجازه داده می‌شود تا Client نتواند حجم بسیار بزرگی درخواست کند.
    var pageSize = int.TryParse(query["pageSize"], out var size) && size > 0
        ? Math.Min(size, 100)
        : 20;

    // تغییر: ایجاد Cache Key بر اساس پارامترهای درخواست.
    // بنابراین درخواست‌های یکسان می‌توانند از Cache استفاده کنند.
    var cacheKey = $"items:{filter}:{page}:{pageSize}";

    // تغییر: ابتدا Cache بررسی می‌شود.
    // اگر پاسخ قبلاً Cache شده باشد، دیگر Filter و Pagination اجرا نمی‌شوند.
    if (cache.TryGetValue(cacheKey, out object? cachedResult))
    {
        return Results.Ok(cachedResult);
    }

    // تغییر: Endpoint به صورت async تعریف شده و پردازش را await می‌کند.
    // این بخش برای پوشش Async Processing در Challenge اضافه شده است.
    var result = await Task.Run(() =>
    {
        // تغییر: Filter فقط در صورت ارسال filter انجام می‌شود.
        // StringComparison.OrdinalIgnoreCase نیز باعث می‌شود
        // جستجو نسبت به حروف بزرگ و کوچک حساس نباشد.
        var filtered = string.IsNullOrWhiteSpace(filter)
            ? data
            : data
                .Where(x => x.Name.Contains(
                    filter,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

        // تغییر: تعداد کل رکوردهای Filter شده محاسبه می‌شود
        // تا Client بتواند تعداد صفحات را محاسبه کند.
        var totalCount = filtered.Count;

        // تغییر: Pagination اضافه شد.
        // به جای ارسال کل داده‌ها فقط آیتم‌های صفحه موردنظر برگردانده می‌شوند.
        var items = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // تغییر: اطلاعات Pagination به همراه داده‌ها برگردانده می‌شود.
        return new
        {
            page,
            pageSize,
            totalCount,

            // تغییر: محاسبه تعداد کل صفحات
            totalPages = (int)Math.Ceiling(
                totalCount / (double)pageSize),

            items
        };
    });

    // تغییر: نتیجه برای درخواست‌های بعدی در Memory Cache ذخیره می‌شود.
    // بعد از یک دقیقه Cache منقضی خواهد شد.
    cache.Set(
        cacheKey,
        result,
        TimeSpan.FromMinutes(1));

    return Results.Ok(result);
});

app.Run();

class Item
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Value { get; set; }
}






////Second answer
//using Microsoft.AspNetCore.Builder;
//using Microsoft.AspNetCore.Http;
//using Microsoft.Extensions.Caching.Memory;
//using System.Collections.Generic;
//using System.Linq;

//var builder = WebApplication.CreateBuilder(args);

//// تغییر: ثبت MemoryCache برای Cache کردن پاسخ درخواست‌های تکراری
//// دلیل: درخواست‌های یکسان نباید هر بار عملیات Filter و Pagination را دوباره انجام دهند.
//builder.Services.AddMemoryCache(option
//    => new MemoryCacheEntryOptions()
//    {

//        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),

//        // تغییر: اگر حافظه تحت فشار قرار گرفت، این Cache قابل حذف است.
//        Priority = CacheItemPriority.Normal
//    });

//var app = builder.Build();

//var data = new List<Item>();

//// تغییر: به جای List قابل تغییر، بعد از ساخت داده‌ها آن را به آرایه تبدیل می‌کنیم.
//// دلیل: داده‌ها در این مثال ثابت هستند و آرایه برای خواندن مکرر مناسب‌تر است.
//for (int i = 0; i < 10000; i++)
//{
//    data.Add(new Item
//    {
//        Id = i,
//        Name = $"Item {i}",
//        Value = i % 100
//    });
//}


//// تغییر: داده‌ها یک بار بر اساس Value مرتب می‌شوند.
//// دلیل: در کد اولیه OrderByDescending در هر Request اجرا می‌شد.
//// با مرتب‌سازی یک‌باره، هزینه Sort از مسیر Request خارج می‌شود.
//var sortedData = data
//    .OrderByDescending(x => x.Value)
//    .ToArray();


//// تغییر: ایجاد Index برای Name.
//// دلیل: برای جستجوی Exact Name می‌توانیم به جای پیمایش کل لیست، مستقیماً
//// از Dictionary استفاده کنیم. این همان مفهوم Index در داده‌های in-memory است.
//var nameIndex = data
//    .ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);


//// تغییر: تبدیل داده‌ها به ساختار ReadOnly.
//// دلیل: داده‌ها پس از startup تغییر نمی‌کنند و فقط برای خواندن استفاده می‌شوند.
//var readOnlyData = sortedData;


//// تغییر: حذف lock.
//// دلیل: داده‌ها پس از startup فقط خوانده می‌شوند و هیچ عملیات Write همزمانی نداریم.
//// بنابراین lock در مسیر Request ضرورتی ندارد.
//var cacheOptions = new MemoryCacheEntryOptions
//{
//    // تغییر: تعیین مدت اعتبار Cache.
//    // دلیل: از باقی ماندن Cache برای همیشه جلوگیری می‌کند.
//    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),

//    // تغییر: اگر حافظه تحت فشار قرار گرفت، این Cache قابل حذف است.
//    Priority = CacheItemPriority.Normal
//};


//app.MapGet(
//    "/items",
//    async (
//        HttpRequest request,
//        IMemoryCache cache) =>
//    {
//        // تغییر: دریافت پارامتر filter
//        var filter = request.Query["filter"].ToString();

//        // تغییر: اضافه شدن Pagination
//        // دلیل: جلوگیری از بازگرداندن هزاران رکورد در یک Response.
//        var page = int.TryParse(request.Query["page"], out var parsedPage)
//            ? parsedPage
//            : 1;

//        // تغییر: تعیین pageSize پیش‌فرض
//        var pageSize = int.TryParse(request.Query["pageSize"], out var parsedPageSize)
//            ? parsedPageSize
//            : 50;


//        // تغییر: محدود کردن مقادیر Pagination.
//        // دلیل: جلوگیری از درخواست pageSize بسیار بزرگ و مصرف بیش از حد حافظه.
//        page = Math.Max(page, 1);
//        pageSize = Math.Clamp(pageSize, 1, 100);


//        // تغییر: ساخت Cache Key بر اساس تمام پارامترهای مؤثر در نتیجه.
//        // دلیل: نتیجه filter/page/pageSize متفاوت است و نباید با هم اشتباه شوند.
//        var cacheKey = $"items:{filter.ToLowerInvariant()}:{page}:{pageSize}";


//        // تغییر: استفاده از GetOrCreateAsync برای Cache.
//        // دلیل: مسیر Endpoint به شکل async اجرا می‌شود و برای Cacheهای
//        // آینده که ممکن است عملیات asynchronous داشته باشند نیز مناسب است.
//        var result = await cache.GetOrCreateAsync(
//            cacheKey,
//            async entry =>
//            {
//                entry.SetOptions(cacheOptions);

//                IEnumerable<Item> filteredItems;


//                if (string.IsNullOrWhiteSpace(filter))
//                {
//                    // تغییر: اگر Filter وجود نداشته باشد، مستقیماً از داده‌های
//                    // از قبل Sort شده استفاده می‌کنیم.
//                    // دلیل: دیگر OrderBy در هر Request اجرا نمی‌شود.
//                    filteredItems = readOnlyData;
//                }
//                else if (nameIndex.TryGetValue(filter, out var exactItem))
//                {
//                    // تغییر: استفاده از Index برای Exact Match.
//                    // دلیل: در این حالت O(1) به آیتم موردنظر دسترسی پیدا می‌کنیم
//                    // و نیازی به پیمایش کل داده‌ها نداریم.
//                    filteredItems = new[] { exactItem };
//                }
//                else
//                {
//                    // تغییر: برای Contains همچنان باید داده‌ها پیمایش شوند.
//                    // دلیل: Dictionary برای Contains مناسب نیست و فقط Exact Match
//                    // را می‌تواند به صورت مستقیم بهینه کند.
//                    filteredItems = readOnlyData.Where(
//                        x => x.Name.Contains(
//                            filter,
//                            StringComparison.OrdinalIgnoreCase));
//                }


//                // تغییر: Pagination قبل از ساخت Response انجام می‌شود.
//                // دلیل: فقط داده‌های موردنیاز Client انتخاب و Materialize می‌شوند.
//                var items = filteredItems
//                    .Skip((page - 1) * pageSize)
//                    .Take(pageSize)
//                    .ToArray();


//                // تغییر: خروجی را داخل یک مدل مشخص قرار می‌دهیم.
//                // دلیل: Client علاوه بر داده‌ها، اطلاعات Pagination را نیز دریافت می‌کند.
//                var response = new ItemsResponse
//                {
//                    Page = page,
//                    PageSize = pageSize,
//                    Items = items
//                };


//                // تغییر: استفاده از Task.FromResult برای تکمیل مسیر async.
//                // دلیل: عملیات فعلی کاملاً in-memory است و I/O asynchronous واقعی ندارد؛
//                // بنابراین Task.Run کردن آن توصیه نمی‌شود.
//                return await Task.FromResult(response);
//            });


//        return Results.Ok(result);
//    });


//app.Run();


//class Item
//{
//    public int Id { get; set; }
//    public string Name { get; set; }
//    public int Value { get; set; }
//}


//// تغییر: اضافه شدن DTO مربوط به Pagination.
//// دلیل: Response علاوه بر Items، اطلاعات صفحه جاری و اندازه صفحه را نیز برمی‌گرداند.
//class ItemsResponse
//{
//    public int Page { get; set; }

//    public int PageSize { get; set; }

//    public Item[] Items { get; set; } = [];
//}