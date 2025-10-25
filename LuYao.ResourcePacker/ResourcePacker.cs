using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LuYao.ResourcePacker
{
    public class ResourcePacker
    {
        private readonly string _sourceDirectory;
        private readonly string _pattern;

        public ResourcePacker(string sourceDirectory, string pattern)
        {
            _sourceDirectory = sourceDirectory ?? throw new ArgumentNullException(nameof(sourceDirectory));
            _pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
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
            var resourceList = resources.ToList();
            
            using var fs = new FileStream(outputFilePath, FileMode.Create);
            using var writer = new BinaryWriter(fs);

            // Write header
            writer.Write(resourceList.Count);

            // Calculate offsets
            var currentOffset = CalculateHeaderSize(resourceList);

            // Write index
            foreach (var resource in resourceList)
            {
                writer.Write(resource.Key);
                writer.Write(currentOffset);
                writer.Write(resource.Content.Length);
                currentOffset += resource.Content.Length;
            }

            // Write content
            foreach (var resource in resourceList)
            {
                writer.Write(resource.Content);
            }
        }

        private int CalculateHeaderSize(List<ResourceFile> resources)
        {
            // Format: Count (int) + foreach resource (string length + string + long offset + int length)
            int size = sizeof(int);
            foreach (var resource in resources)
            {
                size += sizeof(int) + Encoding.UTF8.GetByteCount(resource.Key) + sizeof(long) + sizeof(int);
            }
            return size;
        }

        private class ResourceFile
        {
            public string FullPath { get; set; }
            public string Key { get; set; }
            public byte[] Content { get; set; }
        }
    }
}