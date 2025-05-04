using System.Text.Json.Serialization;
using SourceGit.Converters;

namespace SourceGit
{

    [JsonSourceGenerationOptions(
        WriteIndented = true,
        IgnoreReadOnlyFields = true,
        IgnoreReadOnlyProperties = true,
        Converters = [
            typeof(ColorConverter),
            typeof(GridLengthConverter),
        ]
    )]
    [JsonSerializable(typeof(Models.ExternalToolPaths))]
   // [JsonSerializable(typeof(Models.InteractiveRebaseJobCollection))]
    [JsonSerializable(typeof(Models.JetBrainsState))]
    [JsonSerializable(typeof(Models.ThemeOverrides))]
    [JsonSerializable(typeof(Models.Version))]
    //[JsonSerializable(typeof(Models.RepositorySettings))]
//    [JsonSerializable(typeof(ViewModels.Preferences))]
    internal partial class JsonCodeGen : JsonSerializerContext { }
}
