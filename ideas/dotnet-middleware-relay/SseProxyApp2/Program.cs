var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); 
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();

var proxyRoutes = builder.Configuration.GetSection("ProxySettings:Routes").Get<string[]>();
foreach (var route in proxyRoutes)
{
    app.MapWhen(
        context => context.Request.Path.StartsWithSegments(route), 
        appBuilder => { appBuilder.UseMiddleware<ProxyMiddleware>(); }
    );
}

app.Run();
