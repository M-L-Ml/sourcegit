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


            public static RaiseExceptionDelegate RaiseExceptionD
            {
                get; set;
            } = RaiseExceptionDefault;
            public static void RaiseException(string context, string message)
            {
                RaiseExceptionD.Invoke(context, message);
            }
            public static void RaiseExceptionDefault(string context, string message)
            {
                Debug.Assert(context != null);
                Debug.WriteLine(context + ": " + message);
                throw new NotImplementedException();
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
