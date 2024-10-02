using System.Text.Json;
using System.Text.Json.Serialization;
using SharpMath.FEM.Geometry;
using SharpMath.Geometry._2D;

namespace GridApp;

public class PointsCollectionJsonConverter : JsonConverter<IPointsCollection<Point2D>>
{
    public override IPointsCollection<Point2D> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, IPointsCollection<Point2D> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        for (int i = 0; i < value.TotalPoints; i++)
        {
            JsonSerializer.Serialize(writer, value[i], options);
        }
        writer.WriteEndArray();
    }
}