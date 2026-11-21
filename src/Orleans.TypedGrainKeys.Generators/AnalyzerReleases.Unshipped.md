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
