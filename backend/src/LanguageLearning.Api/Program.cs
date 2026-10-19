
using LanguageLearning.Api.Middleware;
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Application.Services;
using LanguageLearning.Infrastructure;
using LanguageLearning.Api.Services;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IDeckService, DeckService>();
builder.Services.AddScoped<ICardService, CardService>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
