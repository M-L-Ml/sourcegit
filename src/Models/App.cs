using System;
using System.Diagnostics;
using System.Drawing;
using System.Text.Json.Serialization;

namespace SourceGit
{
    public class App
    {
        public delegate void RaiseExceptionDelegate(string context, string message);


        public static RaiseExceptionDelegate RaiseException
        {
            get; set;
        } = RaiseExceptionDefault;

        public static void RaiseExceptionDefault(string context, string message)
        {
            Debug.Assert(context != null);
            Debug.WriteLine(context + ": " + message);
        }
    }

    [JsonSourceGenerationOptions(
     WriteIndented = true,
     IgnoreReadOnlyFields = true,
     IgnoreReadOnlyProperties = true)]
    //Converters = [
    //    typeof(ColorConverter),
    //       typeof(GridLengthConverter),
    //]

    [JsonSerializable(typeof(Models.ExternalToolPaths))]
    //[JsonSerializable(typeof(Models.InteractiveRebaseJobCollection))]
    [JsonSerializable(typeof(Models.JetBrainsState))]
    internal partial class JsonCodeGen : JsonSerializerContext { }

}
