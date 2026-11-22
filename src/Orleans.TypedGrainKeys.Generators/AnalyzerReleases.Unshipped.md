; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
TGK001 | Orleans.TypedGrainKeys | Error | Grain key must be partial
TGK002 | Orleans.TypedGrainKeys | Error | Grain key must be a record
TGK003 | Orleans.TypedGrainKeys | Error | Grain key must have positional parameters
TGK004 | Orleans.TypedGrainKeys | Error | Unsupported grain key declaration
TGK005 | Orleans.TypedGrainKeys | Error | Unsupported grain key part type
TGK006 | Orleans.TypedGrainKeys | Info | Make the grain partial to generate its Key property
TGK007 | Orleans.TypedGrainKeys | Warning | Grain already has a Key member
TGK008 | Orleans.TypedGrainKeys | Warning | Grain implements interfaces with different typed keys
TGK009 | Orleans.TypedGrainKeys | Error | Invalid grain key name
TGK010 | Orleans.TypedGrainKeys | Error | Grain key name is used by another key
TGK011 | Orleans.TypedGrainKeys | Warning | Use the typed key to get a grain with a typed key
