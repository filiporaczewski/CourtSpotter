using CourtSpotter.AspNetCore.ExceptionHandlers;
using CourtSpotter.BackgroundServices;
using CourtSpotter.Endpoints;
using CourtSpotter.Endpoints.CourtAvailabilities;
using CourtSpotter.Endpoints.PadelClubs;
using CourtSpotter.Extensions;
using CourtSpotter.Infrastructure.BookingProviders.Playtomic;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<CourtBookingAvailabilitiesSyncOptions>(builder.Configuration.GetSection("CourtBookingAvailabilitiesSync"));
builder.Services.Configure<PlaytomicProviderOptions>(builder.Configuration.GetSection("PlaytomicProviderOptions"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddInfrastructure(builder.Configuration, builder.Logging, builder.Environment);
builder.Services.AddBookingProviders(builder.Configuration);
builder.Services.AddHostedService<CourtBookingAvailabilitiesSyncingService>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

var frontendUrls = builder.Configuration.GetSection("FrontendUrls").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularSpa", policy =>
    {
        policy.WithOrigins(frontendUrls).AllowAnyHeader().AllowAnyMethod();
    });
});

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