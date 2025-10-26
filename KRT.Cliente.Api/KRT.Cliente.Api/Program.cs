using Microsoft.EntityFrameworkCore;
using KRT.Cliente.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder
    .Services
    .AddDbContext<AppDbContext>(options =>
    options
    .UseSqlite(builder
    .Configuration
    .GetConnectionString("AppDbContext") ?? throw new InvalidOperationException("Connection string 'AppDbContext' not found.")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "KRTCliente:";
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
