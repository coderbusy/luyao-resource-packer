# Test Scenario: Non-Transitive Dependency

This test scenario demonstrates that the MSBuild and Source Generator components of LuYao.ResourcePacker.MSBuild are NOT transitively referenced.

## Project Structure

```
LibA (references LuYao.ResourcePacker.MSBuild via NuGet)
  └── Contains test.res.txt resource file
  └── Generates LibA.dat during build

LibB (references LibA)
  └── Does NOT get MSBuild/SG functionality
  └── Does receive LibA.dat in output

App2 (references LibB)
  └── Does NOT get MSBuild/SG functionality
  └── Does receive LibA.dat in output
```

## Expected Behavior

1. **LibA**: MSBuild targets execute, generates `LibA.dat`
2. **LibB**: MSBuild targets do NOT execute (no transitive import), but `LibA.dat` is copied to output
3. **App2**: MSBuild targets do NOT execute (no transitive import), but `LibA.dat` is copied to output

## Testing

```bash
# Build the entire chain
cd test-scenario
dotnet build App2/App2.csproj

# Verify LibA has LibA.dat
ls LibA/bin/Debug/net8.0/LibA.dat

# Verify LibB has LibA.dat but NOT LibB.dat
ls LibB/bin/Debug/net8.0/LibA.dat
ls LibB/bin/Debug/net8.0/LibB.dat  # Should not exist

# Verify App2 has LibA.dat
ls App2/bin/Debug/net8.0/LibA.dat
```

## Key Points

- The `LuYao.ResourcePacker.MSBuild` package only imports build assets for **direct** references
- The runtime dependency (`LuYao.ResourcePacker`) **IS** transitively passed
- Generated `.dat` files are copied to all consuming projects via the `CopyToOutputDirectory` setting
