using System;
using System.IO;
using System.Text;

namespace LuYao.ResourcePacker
{
    /// <summary>
    /// Helper class for converting file names to valid C# resource keys.
    /// </summary>
    public static class ResourceKeyHelper
    {
        /// <summary>
        /// Converts a file path to a valid C# identifier for use as a resource key.
        /// </summary>
        /// <param name="filePath">The file path to convert.</param>
        /// <returns>A valid C# identifier based on the filename without extension.</returns>
        public static string GetResourceKey(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            // Get filename without extension
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("File path does not contain a valid filename.", nameof(filePath));

            return MakeSafeIdentifier(fileName);
        }

        /// <summary>
        /// Converts a string to a valid C# identifier by replacing invalid characters
        /// with underscores and adding a prefix underscore if it starts with an invalid character.
        /// </summary>
        /// <param name="name">The name to convert.</param>
        /// <returns>A valid C# identifier.</returns>
        public static string MakeSafeIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            var sb = new StringBuilder();
            
            for (int i = 0; i < name.Length; i++)
            {
                var c = name[i];
                // Only allow letters, digits, and underscores
                if (char.IsLetterOrDigit(c) || c == '_')
                {
                    sb.Append(c);
                }
                else
                {
                    // Replace invalid characters with underscore
                    sb.Append('_');
                }
            }
            
            var result = sb.ToString();
            
            // Ensure it starts with a letter or underscore (not a digit)
            if (result.Length > 0 && !char.IsLetter(result[0]) && result[0] != '_')
            {
                result = "_" + result;
            }
            
            // Edge case: if result is empty after sanitization, use a default
            if (string.IsNullOrEmpty(result))
            {
                result = "_resource";
            }
            
            return result;
        }
    }
}
