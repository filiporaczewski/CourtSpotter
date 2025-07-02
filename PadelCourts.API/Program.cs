using System.Net;
using PadelCourts.Core.Contracts;
using PadelCourts.Infrastructure.BookingProviders;
using PadelCourts.Infrastructure.BookingProviders.RezerwujKort;
using WebApplication1.BackgroundServices;
using WebApplication1.Endpoints;
using WebApplication1.Extensions;
using WebApplication1.Resolvers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.Configure<CourtBookingAvailabilitiesSyncOptions>(builder.Configuration.GetSection("CourtBookingAvailabilitiesSync"));

builder.Services.AddHostedService<CourtBookingAvailabilitiesSyncingService>();
builder.Services.AddSingleton<PlaytomicBookingProvider>();
builder.Services.AddSingleton<KlubyOrgCourtBookingProvider>();
builder.Services.AddSingleton<CourtBookingMeProvider>();
builder.Services.AddSingleton<RezerwujKortBookingProvider>();
builder.Services.AddSingleton<ICourtBookingProviderResolver, CourtBookingProviderResolver>();

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

builder.Services.AddHttpClient("PlaytomicClient", client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", 
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient("RezerwujKortClient", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularSpa", policy =>
    {
        policy.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddSingleton<CookieContainer>();

builder.Services.AddSingleton<HttpClientHandler>(sp =>
{
    var container = sp.GetRequiredService<CookieContainer>();
    return new HttpClientHandler
    {
        CookieContainer = container,
        UseCookies = true
    };
});

builder.Services.AddHttpClient("KlubyOrgClient", client =>
{
    client.BaseAddress = new Uri("https://kluby.org/");
    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
    client.Timeout = TimeSpan.FromSeconds(30);
}).ConfigurePrimaryHttpMessageHandler(sp => sp.GetRequiredService<HttpClientHandler>());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAngularSpa");

app.MapCourtAvailabilitiesEndpoints();
app.MapPadelClubsEndpoints();
app.MapPlaytomicCourtsEndpoint();

app.Run();