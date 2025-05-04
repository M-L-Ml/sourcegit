using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SourceGit.ViewModels
{
    //[JsonSerializable(typeof(Models.InteractiveRebaseJobCollection))]

    [JsonSourceGenerationOptions(
        WriteIndented = true,
        IgnoreReadOnlyFields = true,
        IgnoreReadOnlyProperties = true,
        Converters = [
            typeof(Converters.ColorConverter),
            typeof(Converters.GridLengthConverter),
        ]
    )]
    [JsonSerializable(typeof(Models.ExternalToolPaths))]
    // [JsonSerializable(typeof(Models.InteractiveRebaseJobCollection))]
    [JsonSerializable(typeof(Models.JetBrainsState))]
    [JsonSerializable(typeof(Models.ThemeOverrides))]
    [JsonSerializable(typeof(ViewModels.Preferences))]

    public partial class JsonCodeGen : JsonSerializerContext { }
}
