using System.Collections.Generic;
using System;
using Microsoft.EntityFrameworkCore;


// این دو یپکیج را باید نصب کنیم
//"Microsoft.EntityFrameworkCore.Sqlite"
// "Microsoft.EntityFrameworkCore.Design"  این برای Migration است

var builder = WebApplication.CreateBuilder(args);


// ============================================================
// تغییر 1:
// خواندن مسیر دیتابیس از Environment Variable به نام DB_PATH
//
// در کد اولیه مسیر دیتابیس به صورت ثابت بود:
//     Data Source=app.db
//
// اکنون می‌توانیم هنگام اجرای برنامه مسیر دیتابیس را
// از طریق Environment Variable تعیین کنیم.
//
// مثال:
//     DB_PATH=/app/app.db
//
// اگر DB_PATH وجود نداشته باشد، مقدار "app.db" استفاده می‌شود.
// ============================================================

var dbPath = builder.Configuration["DB_PATH"] ?? "app.db";


// ============================================================
// تغییر 2:
// استفاده از مسیر دیتابیس که از Configuration دریافت کردیم.
//
// قبلاً:
//     options.UseSqlite("Data Source=app.db");
//
// مشکل آن کد این بود که مسیر دیتابیس Hard-code شده بود.
//
// اکنون مسیر به صورت Dynamic ساخته می‌شود و بنابراین
// می‌توان آن را در محیط‌های مختلف Override کرد.
// ============================================================

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));


var app = builder.Build();


// ============================================================
// تغییر 3:
// اجرای خودکار EF Core Migration هنگام Startup
//
// در کد اولیه هیچ Migrationای هنگام اجرای برنامه انجام
// نمی‌شد.
//
// با CreateScope یک Scope برای دریافت AppDbContext ایجاد
// می‌کنیم، چون AppDbContext به صورت Scoped ثبت شده است.
// ============================================================

using (var scope = app.Services.CreateScope())
{
    // ========================================================
    // تغییر 4:
    // دریافت AppDbContext از Dependency Injection
    //
    // به جای اینکه خودمان AppDbContext را new کنیم،
    // آن را از DI Container دریافت می‌کنیم.
    // ========================================================

    var dbContext = scope.ServiceProvider
        .GetRequiredService<AppDbContext>();


    // ========================================================
    // تغییر 5:
    // اجرای Migrationهای pending به صورت خودکار
    //
    // اگر دیتابیس وجود نداشته باشد، ایجاد می‌شود.
    //
    // اگر Migration جدیدی وجود داشته باشد، اجرا می‌شود.
    //
    // اگر تمام Migrationها قبلاً اجرا شده باشند،
    // هیچ کاری انجام نمی‌شود.
    // ========================================================

    dbContext.Database.Migrate();
}


app.MapGet("/", () => "Hello World");


// ============================================================
// این Endpoint برای تست اختیاری است.
//
// جزء اصلی Challenge نیست، اما می‌توانیم با آن بررسی کنیم
// که دیتابیس و جدول Items به درستی ایجاد شده‌اند.
// ============================================================

app.MapGet("/items", async (AppDbContext db) =>
{
    return await db.Items.ToListAsync();
});


app.Run();


public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
    : base(options)
    {
    }

    public DbSet<Item> Items { get; set; }
}


// ============================================================
// Entity
//
// این قسمت نیز در کد اولیه وجود داشت.
//
// فقط string را مقداردهی اولیه کرده‌ایم تا در صورت فعال بودن
// Nullable Reference Types با Warning مواجه نشویم.
// ============================================================

public class Item
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}





//Dcker
//# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

//# This stage is used when running from VS in fast mode (Default for Debug configuration)
//FROM mcr.microsoft.com / dotnet / aspnet:9.0 AS base
//USER $APP_UID
//WORKDIR /app
//EXPOSE 8080


//# ============================================================
//# مرحله Build
//#
//# از SDK استفاده می‌کنیم چون در این مرحله باید پروژه را
//# Restore و Build/Publish کنیم.
//# ============================================================
//FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
//ARG BUILD_CONFIGURATION=Release
//WORKDIR /src




//# ============================================================
//# فقط فایل csproj را ابتدا Copy می‌کنیم.
//#
//# این کار باعث می‌شود Docker بتواند Layer مربوط به Restore
//# را Cache کند و در Buildهای بعدی سریع‌تر باشد.
//# ============================================================
//COPY ["InstallabilityAnswer/InstallabilityAnswer.csproj", "InstallabilityAnswer/"]

//# Restore کردن dependencyهای پروژه
//RUN dotnet restore "./InstallabilityAnswer/InstallabilityAnswer.csproj"
//# Copy کردن باقی فایل‌های پروژه
//COPY . .
//WORKDIR "/src/InstallabilityAnswer"
//RUN dotnet build "./InstallabilityAnswer.csproj" -c $BUILD_CONFIGURATION -o /app/build

//# Publish کردن برنامه
//FROM build AS publish
//ARG BUILD_CONFIGURATION=Release
//RUN dotnet publish "./InstallabilityAnswer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost = false




//# ============================================================
//# مرحله Runtime
//#
//# برای اجرای برنامه دیگر به SDK احتیاج نداریم.
//# بنابراین از ASP.NET Core Runtime image استفاده می‌کنیم.
//# ============================================================
//FROM base AS final
//WORKDIR /app
//COPY --from=publish /app/publish .





//# ============================================================
//# تغییر مهم:
//# تعیین مقدار پیش‌فرض برای DB_PATH در Docker
//#
//# اگر هنگام docker run مقدار دیگری ندهیم،
//# برنامه از /app/app.db استفاده خواهد کرد.
//#
//# این مقدار قابل Override است.
//# ============================================================

//ENV DB_PATH=/app/app.db



//# ============================================================
//# تغییر مهم:
//# مشخص کردن پورتی که ASP.NET Core داخل Container روی آن
//# Listen می‌کند.
//# ============================================================

//ENV ASPNETCORE_HTTP_PORTS=8080


//# ============================================================
//# اعلام اینکه Container از پورت 8080 استفاده می‌کند.
//# ============================================================

//EXPOSE 8080


//# ============================================================
//# اجرای برنامه
//# ============================================================

//ENTRYPOINT ["dotnet", "InstallabilityAnswer.dll"]








