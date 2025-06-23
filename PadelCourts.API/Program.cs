using PadelCourts.Core.Contracts;
using PadelCourts.Infrastructure.BookingProviders;
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
builder.Services.AddHostedService<CourtSyncingService>();
builder.Services.AddScoped<PlaytomicProvider>();
builder.Services.AddScoped<KlubyOrgProvider>();
builder.Services.AddScoped<CourtMeProvider>();
builder.Services.AddScoped<ICourtProviderResolver, CourtProviderResolver>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularSpa", policy =>
    {
        policy.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod();
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

app.Run();