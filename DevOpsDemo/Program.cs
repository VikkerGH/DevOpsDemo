using DevOpsDemo.Middleware;
using DevOpsDemo.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddHealthChecks();

// Singleton deler samme lager mellem HTTP-kald. Data forsvinder ved genstart!
builder.Services.AddSingleton<IBookService, BookService>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Frontend og API ligger på samme server, så vi ikke har brug for CORS.
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// WebApplicationFactory bruger denne type til at starte API'et i integrationstests.
public partial class Program { }
