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
        private readonly string _pattern;

        public ResourcePacker(string sourceDirectory, string pattern)
        {
            if (string.IsNullOrEmpty(sourceDirectory))
                throw new ArgumentNullException(nameof(sourceDirectory));
            if (string.IsNullOrEmpty(pattern))
                throw new ArgumentNullException(nameof(pattern));

            _sourceDirectory = sourceDirectory;
            _pattern = pattern;
        }

        public void PackResources(string outputFilePath)
        {
            var resources = CollectResources();
            WriteResourcePackage(outputFilePath, resources);
        }

        private IEnumerable<ResourceFile> CollectResources()
        {
            return Directory.GetFiles(_sourceDirectory, _pattern, SearchOption.AllDirectories)
                .Select(file => new ResourceFile
                {
                    FullPath = file,
                    Key = GetResourceKey(file),
                    Content = File.ReadAllBytes(file)
                });
        }

        private string GetResourceKey(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            var firstDot = fileName.IndexOf('.');
            return firstDot > 0 ? fileName.Substring(0, firstDot) : fileName;
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