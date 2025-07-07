using System.Net;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using PadelCourts.Core.Contracts;
using PadelCourts.Infrastructure.BookingProviders;
using PadelCourts.Infrastructure.BookingProviders.RezerwujKort;
using WebApplication1.BackgroundServices;
using WebApplication1.Endpoints;
using WebApplication1.Extensions;
using WebApplication1.Resolvers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<CourtBookingAvailabilitiesSyncOptions>(builder.Configuration.GetSection("CourtBookingAvailabilitiesSync"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddInfrastructure(builder.Configuration, builder.Logging, builder.Environment);
builder.Services.AddBookingProviders(builder.Configuration);
builder.Services.AddHostedService<CourtBookingAvailabilitiesSyncingService>();

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

var frontendUrl = builder.Configuration["FrontendUrl"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularSpa", policy =>
    {
        policy.WithOrigins(frontendUrl).AllowAnyHeader().AllowAnyMethod();
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