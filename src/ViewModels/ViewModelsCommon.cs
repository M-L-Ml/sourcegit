using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SourceGit.ViewModels
{
    //[JsonSerializable(typeof(Models.InteractiveRebaseJobCollection))]
    [JsonSerializable(typeof(ViewModels.Preferences))]

    internal partial class JsonCodeGen : JsonSerializerContext { }
}
