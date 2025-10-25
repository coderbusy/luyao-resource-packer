# LuYao.ResourcePacker Example Project

This is a console application demonstrating how to use the LuYao.ResourcePacker library.

## What This Example Demonstrates

1. **Resource File Setup**: Place resource files in a `Resources` folder with the `.res.` pattern in the filename (e.g., `message.res.json`, `config.res.json`, `template.res.html`)

2. **Automatic Build-Time Packing**: During build, all `.res.*` files are automatically packed into a single `.dat` file

3. **Runtime Access**: Access packed resources at runtime using the `ResourcePackageReader` class

## Project Structure

```
ExampleProject/
├── ExampleProject.csproj    # Project configuration with ResourcePacker integration
├── Program.cs               # Main application demonstrating resource access
├── Resources/               # Directory containing resource files
│   ├── message.res.json     # JSON resource with greeting and features
│   ├── config.res.json      # JSON resource with configuration
│   └── template.res.html    # HTML template resource
└── README.md               # This file
```

## How to Build and Run

1. Build the project:
   ```bash
   dotnet build
   ```

2. Run the example:
   ```bash
   dotnet run
   ```

   Or run from the solution root:
   ```bash
   dotnet run --project examples/ExampleProject/ExampleProject.csproj
   ```

## What Happens During Build

1. The MSBuild integration scans for files matching the pattern `*.res.*`
2. All matching files are packed into `ExampleProject.dat`
3. The `.dat` file is copied to the output directory
4. At runtime, the application reads resources from the `.dat` file

## Key Features Demonstrated

- **String Resources**: Read JSON and HTML resources as strings
- **Binary Resources**: Read resources as byte arrays
- **Resource Enumeration**: List all available resources
- **Async Support**: All read operations support async/await

## Expected Output

When you run the example, you should see:

```
=== LuYao.ResourcePacker Example ===

Resource package loaded: [path]/ExampleProject.dat

Available resources:
  - config
  - message
  - template

--- Reading 'message' resource as string ---
{
  "greeting": "Hello from LuYao.ResourcePacker!",
  ...
}

--- Reading 'config' resource as string ---
{
  "appName": "Example Project",
  ...
}

--- Reading 'template' resource as bytes ---
Template size: 401 bytes
Template content (first 200 characters):
<!DOCTYPE html>
<html>
...

=== Example completed successfully ===
```

## Configuration

The example project uses the following MSBuild properties in `ExampleProject.csproj`:

```xml
<PropertyGroup>
  <!-- Enable resource packing -->
  <ResourcePackerEnabled>true</ResourcePackerEnabled>
</PropertyGroup>
```

You can also customize:
- `ResourcePackerPattern` - Custom file pattern (default: `*.res.*`)
- `ResourcePackerOutputFileName` - Custom output filename (default: `$(AssemblyName).dat`)

## Learn More

- See the main [README.md](../../README.md) for more information about LuYao.ResourcePacker
- Check the [test project](../../LuYao.ResourcePacker.Tests) for more usage examples
