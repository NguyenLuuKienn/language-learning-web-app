
using LanguageLearning.Infrastructure;
using LanguageLearning.Application.Interfaces; 
using LanguageLearning.Application.Services;   

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Đăng ký tầng Infrastructure (EF Core MySQL DbContext)
builder.Services.AddInfrastructure(builder.Configuration);

// Issue #20: Register Deck service
builder.Services.AddScoped<IDeckService, DeckService>();

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
