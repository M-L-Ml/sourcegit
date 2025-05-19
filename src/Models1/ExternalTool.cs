using System;
using System.IO;

namespace Sausa
{
    /// <summary>
    /// Represents an external tool that can be used by the application
    /// </summary>

    /// <summary>
    /// Encapsulates information for any external tool (editor or otherwise).
    /// </summary>
    /// <param name="Name"></param>
    /// <param name="IconName"></param>
    /// <param name="ExecArgsGenerator"></param>
    public record class ExternalToolInfo(string Name, string? IconName, Func<string, string>? ExecArgsGenerator);

      public record class ExternalToolInfo2(
        string Name,
        string? IconName,
        Func<string, string>? ExecArgsGenerator,
         Func<string> LocationFinder
    ) : ExternalToolInfo(Name, IconName, ExecArgsGenerator);



    public record class ExternalTool
        //(string Name,
        //string? IconName,
        //Func<string, string>? ExecArgsGenerator, string Location, string Type) 
        : ExternalToolInfo //(Name, IconName, ExecArgsGenerator)
    {
        //ExternalToolInfo Info { get;init; }
        public ExternalTool(string name, string? iconName, string location, string type,
            Func<string, string>? ExecArgsGenerator = default) :
            base(name, iconName, ExecArgsGenerator)
        {
            Location = location;
        }
        public ExternalTool(ExternalToolInfo info, string location, string type
         ) :
            base(info)// name, iconName, ExecArgsGenerator)
        {
            Location = location;
        }
        //public ExternalTool(string name, string location, string icon, string type)
        //{
        //    Name = name;
        //    Location = location;
        // //   Icon = icon;
        //    Type = type;
        //}

        ///// <summary>
        ///// Name of the external tool
        ///// </summary>
        //public string Name { get; init; } = string.Empty;

        /// <summary>
        /// Path to the executable for the external tool
        /// </summary>
          public string Location { get; init; } = string.Empty;

        //  /// <summary>
        //  /// Icon identifier for the external tool
        //  /// </summary>
        ////  public string Icon { get; init; } = string.Empty;

        //  /// <summary>
        //  /// Type/category of the external tool
        //  /// </summary>
         public string Type { get; init; } = string.Empty;

        /// <summary>
        /// Gets a value indicating whether this external tool is valid
        /// </summary>
        public bool IsValid => !string.IsNullOrEmpty(Location) && File.Exists(Location);
    }
}
