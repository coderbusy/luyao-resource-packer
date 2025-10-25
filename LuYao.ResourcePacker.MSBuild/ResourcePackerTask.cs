using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;

namespace LuYao.ResourcePacker.MSBuild
{
    public class ResourcePackerTask : Task
    {
        [Required]
        public string ProjectDir { get; set; }

        [Required]
        public string OutputPath { get; set; }

        [Required]
        public string AssemblyName { get; set; }

        public string ResourcePattern { get; set; } = "*.res.*";

        public string OutputFileName { get; set; }

        public override bool Execute()
        {
            try
            {
                var outputFileName = string.IsNullOrEmpty(OutputFileName)
                    ? $"{AssemblyName}.dat"
                    : OutputFileName;

                var outputFilePath = Path.Combine(OutputPath, outputFileName);

                // Create resource packer
                var packer = new global::LuYao.ResourcePacker.ResourcePacker(ProjectDir, ResourcePattern);

                // Pack resources
                packer.PackResources(outputFilePath);

                Log.LogMessage(MessageImportance.Normal, $"Resources packed successfully to: {outputFilePath}");
                return true;
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to pack resources: {ex.Message}");
                return false;
            }
        }
    }
}