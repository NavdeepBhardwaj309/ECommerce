var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services
	.AddReverseProxy()
	.LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapHealthChecks("/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
	Predicate = _ => false
});
app.MapHealthChecks("/ready");
app.MapReverseProxy();

app.Run();
