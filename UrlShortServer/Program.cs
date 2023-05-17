using Microsoft.EntityFrameworkCore;
using UrlShortServer.Database;
using UrlShortServer.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<UrlDbContext>(options =>
{
    options.UseSqlite("Data Source=url.db");
});

builder.Services.AddScoped<IShortenerService, ShortenerService>();
builder.Services.AddScoped<IShortUrlService, RandomShortUrlService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
