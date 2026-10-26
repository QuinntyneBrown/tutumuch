using TutuMuch.Api.Auth;
using TutuMuch.Api.Privacy;
using TutuMuch.Application;
using TutuMuch.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddTutuMuchAuth(builder.Configuration);
builder.Services.Configure<StrictlyNecessaryCookieOptions>(builder.Configuration.GetSection(StrictlyNecessaryCookieOptions.Section));

var app = builder.Build();

app.UseMiddleware<CookieAllowListMiddleware>();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

/// <summary>Entry point, exposed to WebApplicationFactory in TutuMuch.Api.Tests.</summary>
public partial class Program;
