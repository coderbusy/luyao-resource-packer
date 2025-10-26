# RootNamespace Test Sample

This sample project demonstrates and validates that the source generator correctly respects the `RootNamespace` property in the .csproj file.

## Purpose

When a project sets `<RootNamespace>Popcorn.Toolkit</RootNamespace>` in its .csproj file, the generated `R` class should be placed in the `Popcorn.Toolkit` namespace rather than defaulting to the assembly name.

## Project Configuration

The project is configured with:
- **AssemblyName**: RootNamespaceTest
- **RootNamespace**: Popcorn.Toolkit
- **Resources**: Contains sample.txt and config.json

## Expected Behavior

The source generator should:
1. Read the `RootNamespace` property from MSBuild
2. Generate the `R` class in the `Popcorn.Toolkit` namespace
3. The Program.cs can access `R` class directly since it's in the same namespace

## Running the Test

```bash
dotnet run --project samples/RootNamespaceTest/RootNamespaceTest.csproj
```

Expected output:
```
RootNamespace Test - Verifying generated R class namespace
=============================================================

R class is accessible in namespace: Popcorn.Toolkit
Expected namespace: Popcorn.Toolkit

Available resource keys:
  - R.Keys.sample: sample
  - R.Keys.config: config

Sample resource content: This is a sample resource file for testing RootNamespace property.
Config resource content: {
  "message": "Hello from RootNamespace test!",
  "version": "1.0"
}

✓ Test PASSED: R class is in the correct namespace (Popcorn.Toolkit)
```

## Verification

The test program:
1. Accesses the `R` class without qualification (proving it's in `Popcorn.Toolkit`)
2. Verifies the namespace using reflection
3. Reads resources to ensure functionality works correctly
