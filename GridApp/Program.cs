using System.Text.Json;
using GridApp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using SharpMath.FEM.Core;
using SharpMath.FEM.Geometry;
using SharpMath.FEM.Geometry._2D.Quad;
using SharpMath.FEM.Geometry._2D.Quad.IO;
using SharpMath.Geometry._2D;
using SharpMath.Geometry.Splitting;

void ConfigureServices(IServiceCollection services)
{
    IConfiguration configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .Build();
    services.AddSingleton<IConfiguration>(configuration.GetSection("GridApp"));

    services.AddSingleton<IGridDefinitionProvider<RegularGridDefinition>, GridDefinitionReader>();
    services.AddSingleton<RegularGridBuilder>();
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

var services = new ServiceCollection();
ConfigureServices(services);
var provider = services.BuildServiceProvider();

var gridReader = provider.GetRequiredService<IGridDefinitionProvider<RegularGridDefinition>>();
var gridBuilder = provider.GetRequiredService<RegularGridBuilder>();

var gridDefinition = gridReader.Get();
var grid = gridBuilder.Build(gridDefinition);

SerializeGrid(grid);

Console.WriteLine();
