using StoreMetrics.Repositories;
using StoreMetrics.Services;
using QuestPDF.Infrastructure;
using QuestPDF.Fluent;

var builder = WebApplication.CreateBuilder(args);

// ✔ QuestPDF License
// ============================================================================
QuestPDF.Settings.License = LicenseType.Community;

// ---------- MVC ----------
builder.Services.AddControllersWithViews();

// ---------- SESSION ----------
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromHours(8);
});

// ---------- MONGO SETTINGS ----------
builder.Services.Configure<MongoSettings>(
    builder.Configuration.GetSection("MongoSettings"));

builder.Services.AddSingleton<MongoDbService>();

// ---------- EMAIL SENDER ----------
builder.Services.AddSingleton<EmailSender>();

// ---------- REPOSITORIES ----------
builder.Services.AddSingleton<FileEvaluationRepository>();
builder.Services.AddScoped<IStoreRepository, StoreRepository>();
builder.Services.AddScoped<IEvaluationRepository, FileEvaluationRepository>();
builder.Services.AddScoped<MongoEvaluationRepository>();

// ---------- BUILD ----------
var app = builder.Build();

// ---------- MIDDLEWARE ----------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

// ---------- ROUTING ----------
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();
