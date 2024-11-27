using System.Text.Json;
using GridApp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SharpMath;
using SharpMath.EquationsSystem.Preconditions;
using SharpMath.EquationsSystem.Preconditions.Diagonal;
using SharpMath.EquationsSystem.Solver;
using SharpMath.FEM.Core;
using SharpMath.FEM.Geometry;
using SharpMath.FEM.Geometry._2D.Quad;
using SharpMath.FEM.Geometry._2D.Quad.IO;
using SharpMath.FiniteElement._2D.Assembling;
using SharpMath.FiniteElement._2D.BasisFunctions;
using SharpMath.FiniteElement.Assembling;
using SharpMath.FiniteElement.Assembling.Boundary.RegularGrid;
using SharpMath.FiniteElement.Core.Assembling;
using SharpMath.FiniteElement.Core.Assembling.Boundary.First;
using SharpMath.FiniteElement.Core.Assembling.Boundary.Second;
using SharpMath.FiniteElement.Materials.LambdaGamma;
using SharpMath.FiniteElement.Materials.Providers;
using SharpMath.FiniteElement.Providers.Density;
using SharpMath.Geometry._2D;
using SharpMath.Integration;
using SharpMath.Matrices;
using SharpMath.Matrices.Sparse;
using SharpMath.Vectors;

void ShowGridIndexes(Grid<Point2D, IElement> grid1, EdgeResolver edgeResolver1)
{
    var edges = new Dictionary<Edge, int>();
    for (var i = 0; i < grid1.Nodes.TotalPoints; i++)
    {
        for (var j = 0; j < grid1.Nodes.TotalPoints; j++)
        {
            var edge = new Edge(i, j);
            if (i == j || edges.ContainsKey(edge))
            {
                continue;
            }

            edgeResolver1.TryGetEdge(i, j, out var edgeId);

            if (edgeId is null)
            {
                continue;
            }

            edges.Add(edge, (int) edgeId);
        }
    }

    foreach (var (edgeNodes, id) in edges)
    {
        Console.WriteLine($"{edgeNodes.Begin} {edgeNodes.End} {id}");
    }

    Console.WriteLine();
}

void ConfigureServices(IServiceCollection services)
{
    IConfiguration configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .Build();
    services.AddSingleton<IConfiguration>(configuration.GetSection("GridApp"));
    services.AddSingleton<ILogger, NullLogger>(_ => NullLogger.Instance);

    services.AddSingleton<LocalOptimalSchemeConfig>(provider =>
    {
        provider.GetService<IConfiguration>();
        var losConfig = configuration
            .GetSection("GridApp")
            .GetSection("LOS")
            .Get<LocalOptimalSchemeConfig>();

        return losConfig!;
    });
    services.AddTransient<LocalOptimalScheme>();
    services.AddTransient<LUPreconditioner>();
    services.AddTransient<SparsePartialLUResolver>();

    services.AddSingleton<IGridDefinitionProvider<RegularGridDefinition>, GridDefinitionReader>();
    services.AddSingleton<SymmetricMatrixPortraitBuilder>();
    services.AddSingleton<QuadElementEdgeResolver>();
    services.AddSingleton<EdgesPortraitBuilder>();
    services.AddSingleton<IElementEdgeResolver, QuadElementEdgeResolver>();

    services.AddSingleton<RegularBoundaryReader>();
    services.AddSingleton<RegularGridBuilder>();

    services.AddSingleton<IFirstBoundaryApplier<SymmetricSparseMatrix>, GaussExcluderSymmetricSparse>();
    services
        .AddSingleton<IRegularBoundaryApplier<SymmetricSparseMatrix>, RegularBoundaryApplier<SymmetricSparseMatrix>>();
    services.AddSingleton<IStackInserter<SymmetricSparseMatrix>, SymmetricInserter>();

    services.AddSingleton<QuadLinearNonScaledFunctions2DProvider>();

    services.AddSingleton<IIntegrator2D, Gauss2D>();
    services.AddSingleton(GaussConfig.Gauss2(8));
}

string SerializeGrid(Grid<Point2D, IElement> grid)
{
    var json = JsonSerializer.Serialize(grid, new JsonSerializerOptions
    {
        WriteIndented = true,
        Converters = {new PointsCollectionJsonConverter()}
    });

    return json;
}

void Show(IEdgeResolver edgeResolver)
{
    Console.WriteLine($"Ребро между вершинами 39 и 51: {edgeResolver.GetEdgeId(39, 51)}");
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
var boundaryReader = provider.GetRequiredService<RegularBoundaryReader>();
var gridBuilder = provider.GetRequiredService<RegularGridBuilder>();
var matrixPortraitBuilder = provider.GetRequiredService<SymmetricMatrixPortraitBuilder>();
var elementEdgeResolver = provider.GetRequiredService<QuadElementEdgeResolver>();
var edgesPortraitBuilder = provider.GetRequiredService<EdgesPortraitBuilder>();
var integrator = provider.GetRequiredService<IIntegrator2D>();
var inserter = provider.GetRequiredService<IStackInserter<SymmetricSparseMatrix>>();
var basicFunctions = provider.GetRequiredService<QuadLinearNonScaledFunctions2DProvider>();
var slaeSolver = new ConjugateGradientSolver(
    new DiagonalPreconditionerFactory(),
    1e-13,
    4000
);

var materialProvider = new FromArrayMaterialProvider<Material>([new Material(1, 10000)]);

var (boundary, expressions) = boundaryReader.Get();
var gridDefinition = gridReader.Get();
var grid = gridBuilder.Build(gridDefinition);

var matrix = matrixPortraitBuilder.Build(grid.Elements, grid.Nodes.TotalPoints);
var edgesPortrait = edgesPortraitBuilder.Build(grid.Elements, grid.Nodes.TotalPoints);
var edgeResolver = new EdgeResolver(edgesPortrait, grid.Elements, elementEdgeResolver);


var localAssembler = new QuadLinearAssembler2D(
    grid.Nodes,
    new AreaProvider<AreaDefinition>(gridDefinition.Areas),
    integrator,
    materialProvider,
    basicFunctions,
    basicFunctions,
    new FuncDensity<Point2D, double>(grid.Nodes, p =>
    {
        var x = p.X;
        var y = p.Y;
        const double lambda = 1d;
        const double gamma = 10000;
        // return 0;
        return gamma * (2 * x - 5 * y);
        // return -lambda * (2d / 3 + 1) + gamma * (p.X * p.X / 3 + p.Y * p.Y - p.Y);
    }));
var conditionApplier = new RegularBoundaryApplier<SymmetricSparseMatrix>(
    provider.GetRequiredService<IFirstBoundaryApplier<SymmetricSparseMatrix>>(),
    new SecondBoundaryApplier<SymmetricSparseMatrix>(grid.Nodes, inserter),
    new ArrayExpressionProvider(expressions),
    grid.Nodes,
    gridDefinition
);

var equation = BuildEquation(
    grid,
    new Equation<SymmetricSparseMatrix>(
        matrix,
        Vector.Create(grid.Nodes.TotalPoints),
        Vector.Create(grid.Nodes.TotalPoints)
    ),
    boundary
);

var resultEquation = slaeSolver.Solve(equation);

Equation<SymmetricSparseMatrix> BuildEquation(
    Grid<Point2D, IElement> grid,
    Equation<SymmetricSparseMatrix> equation,
    IReadOnlyCollection<RegularBoundaryCondition> boundaryConditions
)
{
    var matrixMemory = new StackMatrix(stackalloc double[4 * 4], 4);
    Span<double> vector = stackalloc double[4];
    var indexes = new StackIndexPermutation(stackalloc int[4]);

    foreach (var element in grid.Elements)
    {
        localAssembler.AssembleMatrix(element, matrixMemory, indexes);
        var localMatrix = new StackLocalMatrix(matrixMemory, indexes);
        inserter.InsertMatrix(equation.Matrix, localMatrix);

        localAssembler.AssembleRightSide(element, vector, indexes);
        var localRightSide = new StackLocalVector(vector, indexes);
        inserter.InsertVector(equation.RightSide, localRightSide);
    }

    foreach (var conditionGroup in boundaryConditions
                 .GroupBy(x => x.Type)
                 .OrderByDescending(x => x.Key) // 3, 2, 1
            )
    {
        foreach (var condition in conditionGroup)
        {
            conditionApplier.Apply(equation, condition);
        }
    }

    return equation;
}

for (var i = 0; i < grid.Nodes.TotalPoints; i++)
{
    Console.WriteLine($"({grid.Nodes[i].X:F3}, {grid.Nodes[i].Y:F3}) : {resultEquation[i]}");
}

// ShowGridIndexes(grid, edgeResolver);
Console.WriteLine();
