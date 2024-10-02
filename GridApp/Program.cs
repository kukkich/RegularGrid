using System.Text.Json;
using GridApp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using SharpMath.FEM.Core;
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

Grid<Point2D, IElement> TaskArea()
{
    List<int> xStepsCounts = [3, 5, 8, 5, 3];
    List<int> yStepsCounts = [7, 4];
   
    var gridBuilder = new RegularGridBuilder();
    var def = new RegularGridDefinition(
        ControlPoints: new Point2D[,]
        {
            { new(1, 1), new (10, 1), new(14, 1), new(16, 1), new(20, 1), new(29, 1) },
            { new(1, 12), new (10, 12), new(14, 10), new(16, 10), new(20, 12), new(29, 12) },
            { new(1, 16), new (10, 16), new(14, 16), new(16, 16), new(20, 16), new(29, 16) },
        },
        xStepsCounts.Select(x => new UniformSplitter(x)).ToArray<ICurveSplitter>(),
        yStepsCounts.Select(x => new UniformSplitter(x)).ToArray<ICurveSplitter>(),
        [
            new AreaDefinition(0, 5, 0, 1),
            new AreaDefinition(0, 1, 1, 2),
            new AreaDefinition(1, 4, 1, 2),
            new AreaDefinition(4, 5, 1, 2),
        ], 
        []);

    var grid = gridBuilder.Build(def);
    return grid;
}

string SerializeGrid(Grid<Point2D, IElement> grid)
{
    var json = JsonSerializer.Serialize(grid, new JsonSerializerOptions
    {
        WriteIndented = true,
        Converters = { new PointsCollectionJsonConverter() }
    });

    Console.WriteLine(json);
    return json;
}

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

var grid = TaskArea();

var str = SerializeGrid(grid);

Console.WriteLine();
