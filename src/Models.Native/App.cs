using System;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace SourceGit
{
    namespace Models
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
    }
    namespace ModelsN
    {
        [JsonSourceGenerationOptions(
         WriteIndented = true,
         IgnoreReadOnlyFields = true,
         IgnoreReadOnlyProperties = true)]
        //Converters = [
        //    typeof(ColorConverter),
        //       typeof(GridLengthConverter),
        //]

        [JsonSerializable(typeof(Models.ExternalToolPaths))]
        [JsonSerializable(typeof(Models.InteractiveRebaseJobCollection))]
        [JsonSerializable(typeof(Models.JetBrainsState))]
        //[JsonSerializable(typeof(Models.RepositorySettings))]
        
        public partial class JsonCodeGen : JsonSerializerContext { }
    }
}
