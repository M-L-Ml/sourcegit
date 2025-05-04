using System;
using System.Text.Json;
using System.Text.Json.Serialization;

using Avalonia.Controls;

namespace SourceGit.Converters
{
    public class GridLengthConverter : JsonConverter<GridLength>
    {
        public override GridLength Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var size = reader.GetDouble();
            return new GridLength(size, GridUnitType.Pixel);
        }

        public override void Write(Utf8JsonWriter writer, GridLength value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value.Value);
        }
    }
}
