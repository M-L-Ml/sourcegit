using System.Text.Json.Serialization;

namespace SourceGit
{
    namespace Models
    {
        public class Null
        {
        }
    }

    namespace Models
    {
        [JsonSourceGenerationOptions(
         WriteIndented = true,
         IgnoreReadOnlyFields = true,
         IgnoreReadOnlyProperties = true)]
        //Converters = [
        //    typeof(ColorConverter),
        //       typeof(GridLengthConverter),
        //]

        //   [JsonSerializable(typeof(Models.ExternalToolPaths))]
        //  [JsonSerializable(typeof(Models.InteractiveRebaseJobCollection))]
        // [JsonSerializable(typeof(Models.JetBrainsState))]
        [JsonSerializable(typeof(Models.RepositorySettings))]

        public partial class JsonCodeGen : JsonSerializerContext { }
    }
}
