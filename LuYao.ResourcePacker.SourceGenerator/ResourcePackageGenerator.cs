using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LuYao.ResourcePacker.SourceGenerator
{
    /// <summary>
    /// Source generator that creates strongly-typed resource access code for resource files.
    /// </summary>
    [Generator]
    public class ResourcePackageGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Get all additional files - will be filtered by ResourcePackerPattern in Execute
            var resourceFiles = context.AdditionalTextsProvider.Collect();

            // Get analyzer config options
            var analyzerConfigOptions = context.AnalyzerConfigOptionsProvider;

            // Combine compilation, files, and config
            var compilationAndFiles = context.CompilationProvider
                .Combine(resourceFiles)
                .Combine(analyzerConfigOptions);

            // Register source output
            context.RegisterSourceOutput(compilationAndFiles, (spc, source) => 
                Execute(spc, source.Left.Left, source.Left.Right, source.Right));
        }

        private static void Execute(SourceProductionContext context, Compilation compilation, 
            System.Collections.Immutable.ImmutableArray<AdditionalText> resourceFiles,
            AnalyzerConfigOptionsProvider configOptions)
        {
            // Get the resource directory from analyzer config (default to "Resources")
            var resourceDirectory = GetResourceDirectory(configOptions);
            
            // Filter additional files to only include those in the resource directory
            var filteredResourceFiles = resourceFiles
                .Where(f => IsInResourceDirectory(f.Path, resourceDirectory))
                .ToList();
            
            if (filteredResourceFiles.Count == 0)
            {
                return; // No resource files, nothing to generate
            }

            // Get assembly name
            var assemblyName = compilation.AssemblyName ?? "Assembly";
            
            // Fixed class name to "R" (like Android)
            var className = "R";
            
            // Get root namespace from compilation options or default to assembly name
            var rootNamespace = GetRootNamespace(compilation, configOptions);
            
            // Visibility is always internal
            var visibility = "internal";
            
            // Get output filename from analyzer config (default to {AssemblyName}.dat)
            var outputFileName = GetOutputFileName(configOptions, assemblyName);
            
            // Extract resource keys using ResourceKeyHelper
            var resourceKeys = filteredResourceFiles
                .Select(f => global::LuYao.ResourcePacker.ResourceKeyHelper.GetResourceKey(f.Path))
                .Where(k => !string.IsNullOrEmpty(k))
                .OrderBy(k => k)
                .Distinct()
                .ToList();

            // Generate source code
            var source = GenerateSourceCode(assemblyName, className, rootNamespace, visibility, outputFileName, resourceKeys);

            // Add source to compilation
            context.AddSource($"{className}.g.cs", SourceText.From(source, Encoding.UTF8));
        }

        private static string GetRootNamespace(Compilation compilation, AnalyzerConfigOptionsProvider configOptions)
        {
            // Try to get RootNamespace from analyzer config (MSBuild property)
            if (configOptions.GlobalOptions.TryGetValue("build_property.RootNamespace", out var rootNamespace))
            {
                if (!string.IsNullOrWhiteSpace(rootNamespace))
                {
                    return rootNamespace;
                }
            }
            
            // Default to assembly name if no explicit root namespace is found
            return compilation.AssemblyName ?? "Assembly";
        }

        private static string GetResourceDirectory(AnalyzerConfigOptionsProvider configOptions)
        {
            // Try to get the resource directory from global analyzer config
            if (configOptions.GlobalOptions.TryGetValue("build_property.ResourcePackerDirectory", out var directory))
            {
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    return directory;
                }
            }
            
            // Default to "Resources"
            return "Resources";
        }

        private static bool IsInResourceDirectory(string filePath, string resourceDirectory)
        {
            if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(resourceDirectory))
            {
                return false;
            }

            // Normalize paths for comparison
            var normalizedPath = filePath.Replace('\\', '/');
            var normalizedDir = resourceDirectory.Replace('\\', '/');
            
            // Check if the file path contains the resource directory
            // Handle both "Resources" and "Resources/" formats
            return normalizedPath.Contains($"/{normalizedDir}/") ||
                   normalizedPath.EndsWith($"/{normalizedDir}");
        }

        private static string GetOutputFileName(AnalyzerConfigOptionsProvider configOptions, string assemblyName)
        {
            // Try to get the output filename from global analyzer config
            if (configOptions.GlobalOptions.TryGetValue("build_property.ResourcePackerOutputFileName", out var outputFileName))
            {
                if (!string.IsNullOrWhiteSpace(outputFileName))
                {
                    return outputFileName;
                }
            }
            
            // Default to {AssemblyName}.dat
            return $"{assemblyName}.dat";
        }

        private static string GenerateSourceCode(string assemblyName, string className, string rootNamespace, string visibility, string outputFileName, List<string> resourceKeys)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("// <auto-generated />");
            sb.AppendLine("#nullable enable");
            sb.AppendLine();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.IO;");
            sb.AppendLine("using LuYao.ResourcePacker;");
            sb.AppendLine();
            
            // Add namespace
            sb.AppendLine($"namespace {rootNamespace}");
            sb.AppendLine("{");
            
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// Provides strongly-typed access to resources in {assemblyName}.");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    {visibility} static class {className}");
            sb.AppendLine("    {");
            
            // Add resource key constants
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Resource keys available in this package.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public static class Keys");
            sb.AppendLine("        {");
            
            foreach (var key in resourceKeys)
            {
                var safeKey = key; // Already safe from ResourceKeyHelper
                sb.AppendLine($"            /// <summary>Resource key for '{key}'</summary>");
                sb.AppendLine($"            public const string {safeKey} = \"{key}\";");
            }
            
            sb.AppendLine("        }");
            sb.AppendLine();
            
            // Add lazy-initialized reader instance
            sb.AppendLine("        private static readonly Lazy<ResourcePackageReader> _reader = new Lazy<ResourcePackageReader>(() =>");
            sb.AppendLine("        {");
            sb.AppendLine($"            var datFilePath = Path.Combine(AppContext.BaseDirectory, \"{outputFileName}\");");
            sb.AppendLine("            if (!File.Exists(datFilePath))");
            sb.AppendLine("            {");
            sb.AppendLine($"                throw new FileNotFoundException($\"Resource package file not found: {{datFilePath}}\");");
            sb.AppendLine("            }");
            sb.AppendLine("            return new ResourcePackageReader(datFilePath);");
            sb.AppendLine("        });");
            sb.AppendLine();
            
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Gets the ResourcePackageReader instance for accessing resources.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public static ResourcePackageReader Reader => _reader.Value;");
            sb.AppendLine();
            
            // Add ContainsKey wrapper method
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Determines whether the package contains a resource with the specified key.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        /// <param name=\"resourceKey\">The key to check for.</param>");
            sb.AppendLine("        /// <returns>true if the package contains a resource with the specified key; otherwise, false.</returns>");
            sb.AppendLine("        public static bool ContainsKey(string resourceKey)");
            sb.AppendLine("        {");
            sb.AppendLine("            return Reader.ContainsKey(resourceKey);");
            sb.AppendLine("        }");
            sb.AppendLine();
            
            // Add helper methods for each resource
            foreach (var key in resourceKeys)
            {
                var safeKey = key; // Already safe from ResourceKeyHelper
                var methodName = $"Read{Capitalize(safeKey)}";
                var asyncMethodName = $"Read{Capitalize(safeKey)}Async";
                
                // Async byte array method
                sb.AppendLine($"        /// <summary>");
                sb.AppendLine($"        /// Reads the '{key}' resource as a byte array asynchronously.");
                sb.AppendLine($"        /// </summary>");
                sb.AppendLine($"        public static System.Threading.Tasks.Task<byte[]> {asyncMethodName}()");
                sb.AppendLine($"        {{");
                sb.AppendLine($"            return Reader.ReadResourceAsync(Keys.{safeKey});");
                sb.AppendLine($"        }}");
                sb.AppendLine();
                
                // Sync byte array method
                sb.AppendLine($"        /// <summary>");
                sb.AppendLine($"        /// Reads the '{key}' resource as a byte array synchronously.");
                sb.AppendLine($"        /// </summary>");
                sb.AppendLine($"        public static byte[] {methodName}()");
                sb.AppendLine($"        {{");
                sb.AppendLine($"            return Reader.ReadResource(Keys.{safeKey});");
                sb.AppendLine($"        }}");
                sb.AppendLine();
                
                // Async string method (UTF-8)
                sb.AppendLine($"        /// <summary>");
                sb.AppendLine($"        /// Reads the '{key}' resource as a string asynchronously using UTF-8 encoding.");
                sb.AppendLine($"        /// </summary>");
                sb.AppendLine($"        public static System.Threading.Tasks.Task<string> Read{Capitalize(safeKey)}AsStringAsync()");
                sb.AppendLine($"        {{");
                sb.AppendLine($"            return Reader.ReadResourceAsStringAsync(Keys.{safeKey});");
                sb.AppendLine($"        }}");
                sb.AppendLine();
                
                // Async string method (with encoding)
                sb.AppendLine($"        /// <summary>");
                sb.AppendLine($"        /// Reads the '{key}' resource as a string asynchronously using the specified encoding.");
                sb.AppendLine($"        /// </summary>");
                sb.AppendLine($"        /// <param name=\"encoding\">The encoding to use when converting bytes to string.</param>");
                sb.AppendLine($"        public static System.Threading.Tasks.Task<string> Read{Capitalize(safeKey)}AsStringAsync(System.Text.Encoding encoding)");
                sb.AppendLine($"        {{");
                sb.AppendLine($"            return Reader.ReadResourceAsStringAsync(Keys.{safeKey}, encoding);");
                sb.AppendLine($"        }}");
                sb.AppendLine();
                
                // Sync string method (UTF-8)
                sb.AppendLine($"        /// <summary>");
                sb.AppendLine($"        /// Reads the '{key}' resource as a string synchronously using UTF-8 encoding.");
                sb.AppendLine($"        /// </summary>");
                sb.AppendLine($"        public static string {methodName}AsString()");
                sb.AppendLine($"        {{");
                sb.AppendLine($"            return Reader.ReadResourceAsString(Keys.{safeKey});");
                sb.AppendLine($"        }}");
                sb.AppendLine();
                
                // Sync string method (with encoding)
                sb.AppendLine($"        /// <summary>");
                sb.AppendLine($"        /// Reads the '{key}' resource as a string synchronously using the specified encoding.");
                sb.AppendLine($"        /// </summary>");
                sb.AppendLine($"        /// <param name=\"encoding\">The encoding to use when converting bytes to string.</param>");
                sb.AppendLine($"        public static string {methodName}AsString(System.Text.Encoding encoding)");
                sb.AppendLine($"        {{");
                sb.AppendLine($"            return Reader.ReadResourceAsString(Keys.{safeKey}, encoding);");
                sb.AppendLine($"        }}");
                sb.AppendLine();
                
                // GetStream method
                sb.AppendLine($"        /// <summary>");
                sb.AppendLine($"        /// Gets a read-only stream for the '{key}' resource.");
                sb.AppendLine($"        /// </summary>");
                sb.AppendLine($"        public static System.IO.Stream Get{Capitalize(safeKey)}Stream()");
                sb.AppendLine($"        {{");
                sb.AppendLine($"            return Reader.GetStream(Keys.{safeKey});");
                sb.AppendLine($"        }}");
                sb.AppendLine();
            }
            
            sb.AppendLine("    }"); // Close class
            sb.AppendLine("}"); // Close namespace
            
            return sb.ToString();
        }

        private static string Capitalize(string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;
            
            return char.ToUpperInvariant(s[0]) + s.Substring(1);
        }
    }
}
