using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Serilog.Events;
using Sushi.Diagnostics.Exceptions;

namespace Sushi.Commands.Options;

/// <summary>
/// Contains the options passed to the compiler through command line arguments.
/// </summary>
internal sealed class CompilerOptions
{
    /// <summary>
    /// The path of the project, solution, or folder containing the project.
    /// </summary>
    public string Path { get; private set; } = string.Empty;

    /// <summary>
    /// Creates a new <see cref="LanguageServerOptions" /> from an <see cref="ICommandContext"/>.
    /// </summary>
    /// <param name="context">
    /// The context used to create the options.
    /// </param>
    /// <returns></returns>
    public static Task<CompilerOptions> FromContext([NotNull] ICommandContext context)
    {
        CompilerOptions options = new();

        if (context.Flags.Contains("debug"))
        {
            context.LevelSwitch.MinimumLevel = LogEventLevel.Debug;
        }

        if (!context.Arguments.TryGetValue("path", out string? value))
        {
            throw new SushiProgramException(SushiProgramError.InvalidParameterSyntax, "A path must be specified when compiling. Use the --path \"Path\\To\\File\\OrFolder.susproj\" parameter.");
        }

        options.Path = value;

        bool isFilePath = File.Exists(options.Path);

        bool isFolderPath = Directory.Exists(options.Path);

        IReadOnlyList<string> allowedExtensions = new ReadOnlyCollection<string>([.. context.DocumentProvider.DocumentTypes.Where(doc => doc.IsValidCompilationRoot).Select(doc => doc.Extension)]);

        if (!isFilePath && !isFolderPath)
        {
            throw new SushiProgramException(SushiProgramError.InvalidPath, $"File or Folder \"{options.Path}\" does not exist on disk. Check your inputs.");
        }

        if (isFilePath && !IsValidCompilationFileExtension(options.Path, allowedExtensions))
        {
            throw new SushiProgramException(SushiProgramError.InvalidPath, $"File \"{options.Path}\" is not a valid .susproj or .susln file.");
        }

        if (isFolderPath)
        {
            // No need to prefer .susln over .susproj in this step, because we will check that later downstream.
            string? projectFile = Directory.EnumerateFiles(options.Path, "*.*", SearchOption.TopDirectoryOnly)
                .FirstOrDefault(x => IsValidCompilationFileExtension(x, allowedExtensions));

            if (string.IsNullOrWhiteSpace(projectFile))
            {
                throw new SushiProgramException(SushiProgramError.InvalidPath, $"Folder \"{options.Path}\" does not contain a valid .susproj or .susln file. Make sure it is in the top directory and not a sub directory.");
            }
        }

        return Task.FromResult(options);
    }

    /// <summary>
    /// Checks if the file path is a valid compilation file based on its extension.
    /// </summary>
    /// <param name="file">
    /// The file path to check.
    /// </param>
    /// <param name="allowedExtensions">
    /// The list of allowed file extensions.
    /// </param>
    /// <returns>
    /// True if the file path has a .susproj or .susln extension. False otherwise.
    /// </returns>
    private static bool IsValidCompilationFileExtension(string file, IReadOnlyList<string> allowedExtensions)
    {
        string extension = System.IO.Path.GetExtension(file).Replace(".", string.Empty, StringComparison.OrdinalIgnoreCase);

        return allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }
}
