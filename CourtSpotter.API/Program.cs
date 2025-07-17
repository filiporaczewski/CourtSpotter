using System.IdentityModel.Tokens.Jwt;
using CourtSpotter.AspNetCore.ExceptionHandlers;
using CourtSpotter.BackgroundServices;
using CourtSpotter.BackgroundServices.CourtBookingAvailabilitiesSync;
using CourtSpotter.Core.Options;
using CourtSpotter.Endpoints.CourtAvailabilities;
using CourtSpotter.Endpoints.PadelClubs;
using CourtSpotter.Extensions;
using CourtSpotter.Extensions.DI;
using CourtSpotter.Infrastructure.BookingProviders.Playtomic;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy("CreatePadelClubs", policy => policy.RequireClaim("scp","padelclubs.create"));
});

builder.Services.Configure<CourtBookingAvailabilitiesSyncOptions>(builder.Configuration.GetSection("CourtBookingAvailabilitiesSync"));

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

app.UseAuthentication();
app.UseAuthorization();

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

app.Run();