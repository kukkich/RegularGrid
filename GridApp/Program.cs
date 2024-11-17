using System.Text.Json;
using GridApp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharpMath.FEM.Core;
using SharpMath.FEM.Geometry;
using SharpMath.FEM.Geometry._2D.Quad;
using SharpMath.FEM.Geometry._2D.Quad.IO;
using SharpMath.FiniteElement.Assembling;
using SharpMath.Geometry._2D;

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

    return json;
}

void Show(IEdgeResolver edgeResolver)
{
    Console.WriteLine($"Ребро между вершинами 39 и 51: {edgeResolver.GetEdge(39, 51)}");
    Console.WriteLine($"Ребро между вершинами 40 и 53 существует? - {edgeResolver.TryGetEdge(40, 53, out _)}");
    Console.WriteLine("Элементы с ребром 120:");
    foreach (var element in edgeResolver.GetElementsByEdge(120))
    {
        Console.WriteLine($"\t{element}");
    }
    Console.WriteLine("Элемент 9 имеет рёбра:");
    foreach (var edge in edgeResolver.GetElementEdges(9))
    {
        Console.WriteLine($"\t{edge}");
    }

    Console.WriteLine($"Вершины ребра 54: {edgeResolver.GetNodesByEdge(54)}");
}

var services = new ServiceCollection();
ConfigureServices(services);
var provider = services.BuildServiceProvider();

var gridReader = provider.GetRequiredService<IGridDefinitionProvider<RegularGridDefinition>>();
var gridBuilder = provider.GetRequiredService<RegularGridBuilder>();

var gridDefinition = gridReader.Get();
var grid = gridBuilder.Build(gridDefinition);
// SerializeGrid(grid);
var elementEdgeResolver = new QuadElementEdgeResolver();
var portraitBuilder = new EdgesPortraitBuilder(elementEdgeResolver);
var portrait = portraitBuilder.Build(grid.Elements, grid.Nodes.TotalPoints);
var edgeResolver = new EdgeResolver(portrait, grid.Elements, elementEdgeResolver);
Show(edgeResolver);

// var edges = new Dictionary<Edge, int>();
// for (var i = 0; i < grid.Nodes.TotalPoints; i++)
// {
//     for (var j = 0; j < grid.Nodes.TotalPoints; j++)
//     {
//         var edge = new Edge(i, j);
//         if (i == j || edges.ContainsKey(edge))
//         {
//             continue;
//         }
//         edgeResolver.TryGetEdge(i, j, out var edgeId);
//
//         if (edgeId is null)
//         {
//             continue;
//         }
//         
//         edges.Add(edge, (int) edgeId);
//     }
// }
//
//
//
// foreach (var (edgeNodes, id) in edges)
// {
//     Console.WriteLine($"{edgeNodes.Begin} {edgeNodes.End} {id}");
// }
//
// Console.WriteLine();

class Edge
{
    public int Begin { get; }
    public int End { get; }

    public Edge(int begin, int end)
    {
        Begin = Math.Min(begin, end);
        End = Math.Max(begin, end);
    }
    
}
