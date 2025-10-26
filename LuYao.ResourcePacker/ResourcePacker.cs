using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LuYao.ResourcePacker
{
    public class ResourcePacker
    {
        /// <summary>
        /// The current version of the resource package format.
        /// </summary>
        public const byte FormatVersion = 1;

        private readonly string _sourceDirectory;

        public ResourcePacker(string sourceDirectory)
        {
            if (string.IsNullOrEmpty(sourceDirectory))
                throw new ArgumentNullException(nameof(sourceDirectory));

            _sourceDirectory = sourceDirectory;
        }

        public void PackResources(string outputFilePath)
        {
            var resources = CollectResources();
            WriteResourcePackage(outputFilePath, resources);
        }

        private IEnumerable<ResourceFile> CollectResources()
        {
            if (!Directory.Exists(_sourceDirectory))
            {
                return Enumerable.Empty<ResourceFile>();
            }

            return Directory.GetFiles(_sourceDirectory, "*", SearchOption.AllDirectories)
                .Select(file => new ResourceFile
                {
                    FullPath = file,
                    Key = ResourceKeyHelper.GetResourceKey(file),
                    Content = File.ReadAllBytes(file)
                });
        }

        private void WriteResourcePackage(string outputFilePath, IEnumerable<ResourceFile> resources)
        {
            var resourceList = resources.OrderBy(r => r.Key).ToList();
            
            using var fs = new FileStream(outputFilePath, FileMode.Create);
            using var writer = new BinaryWriter(fs);

            // Write version number (requirement 1)
            writer.Write(FormatVersion);

            // Write resource count
            writer.Write(resourceList.Count);

            // Write index (requirement 3: only key and length)
            foreach (var resource in resourceList)
            {
                writer.Write(resource.Key);
                writer.Write(resource.Content.Length);
            }

            // Write content
            foreach (var resource in resourceList)
            {
                writer.Write(resource.Content);
            }
        }



        private class ResourceFile
        {
            public string FullPath { get; set; }
            public string Key { get; set; }
            public byte[] Content { get; set; }
        }
    }
}