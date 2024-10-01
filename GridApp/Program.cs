using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using SharpMath.FEM.Geometry._2D.Quad;
using SharpMath.Geometry._2D;
using SharpMath.Geometry.Splitting;

void ConfigureServices(IServiceCollection services)
{
    IConfiguration configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .Build();
    services.AddSingleton(configuration);

    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(configuration)
        .Enrich.FromLogContext()
        .CreateLogger();
    services.AddLogging(loggingBuilder =>
        loggingBuilder.AddSerilog(dispose: true));
}

// var services = new ServiceCollection();
// ConfigureServices(services);
// var provider = services.BuildServiceProvider();

var gridBuilder = new RegularGridBuilder();
var def = new RegularGridDefinition(
    ControlPoints: new Point2D[,]
    {
        { new(0, 0), new (4, 0) },
        { new(2, 2), new (4, 2) }
    },
    [new UniformSplitter(2)],
    [new UniformSplitter(2)],
    [new AreaDefinition(0, 1, 0, 1)],
    []);
var grid = gridBuilder.Build(def);

Console.WriteLine();
// var logger = provider.GetRequiredService<ILogger<Program>>();
// logger.LogInformation("GridApp, You're just a miserable copy of me!");
// logger.LogCritical("No, I'm the upgrade!");