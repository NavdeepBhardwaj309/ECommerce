var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
//YARP(yet another reverse proxy)  implemenataion
// A reverse proxy gives clients one stable entry point instead of requiring them to know 
// each service’s address. It can also centralize concerns such as authentication, TLS,
//  rate limiting, request logging, and routing. It doesn’t implement business logic or 
//  automatically make the services available; it forwards requests to them.

//Routes match paths like /api/products/{**catch-all} and select a cluster;
//  each cluster names the service destination URL.
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
