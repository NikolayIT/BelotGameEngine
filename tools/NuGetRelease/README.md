# Engine NuGet releases

`BelotGameEngine` packages only `src/Belot.Engine`. The AI players, trained
weights, MAUI app and training tools are separate repository projects.

## Trusted publishing

NuGet owner: `Nikolay.IT`. The policy must use:

| Setting | Value |
| --- | --- |
| Repository owner | `NikolayIT` |
| Repository | `BelotGameEngine` |
| Workflow file | `publish.yml` |
| Environment | `release` |
| Scope | Push only new package versions |
| Package pattern | `BelotGameEngine` (exact, no wildcard) |

The GitHub `release` environment allows only tags matching `*.*.*`. The workflow
also requires a stable numeric tag that exactly matches the engine's `Version`,
for example `2.0.0`. It runs when a GitHub release is published, or through a
manual dispatch **on that tag**. Branch pushes do not publish packages.

The test/pack job has read-only repository access. Only the publish job requests
an OIDC token, after all checks pass. `NuGet/login` exchanges that token for a
short-lived NuGet key immediately before the push. No long-lived NuGet secret
is stored. Actions are pinned to commit hashes.

## Prepare and verify

1. Update the engine project version, release notes and packaged README.
2. Run the engine, ClaudePlayer and UI test projects in Release with warnings
   treated as errors. The latter projects check consumers; their assemblies are
   not packaged.
3. Commit the release source, then build and pack from that clean commit:

```powershell
dotnet build src/Belot.Engine/Belot.Engine.csproj -c Release -p:ContinuousIntegrationBuild=true -warnaserror
dotnet pack src/Belot.Engine/Belot.Engine.csproj -c Release --no-build -p:ContinuousIntegrationBuild=true -warnaserror -o artifacts/nuget
python tools/NuGetRelease/verify-package.py artifacts/nuget --version 2.0.0 --commit (git rev-parse HEAD)
python -m unittest discover -s tools/NuGetRelease -p 'test_*.py'
./tools/NuGetRelease/smoke-test.ps1 -PackageDirectory artifacts/nuget -Version 2.0.0
```

The verifier rejects extra assemblies or weights, runtime package dependencies,
wrong metadata and a source commit that differs from the release. The smoke
check compiles and runs the README example against the actual local package
with a separate package cache and no project reference.

4. Push the committed source and matching tag, then publish its GitHub release.
   Follow `.github/workflows/publish.yml` to completion. Its `engine-nuget`
   artifact preserves the package that was submitted.
5. Confirm the version appears on nuget.org, download it and compare its DLL and
   metadata with the workflow artifact. NuGet adds a repository signature, so
   the downloaded ZIP hash may differ. NuGet scanning/indexing can finish after
   the push succeeds.

An existing NuGet version cannot be replaced. A failed run before a successful
push can be retried on the same tag. If the push succeeded, inspect NuGet before
retrying; this workflow deliberately reports duplicate versions as an error.

References: [NuGet trusted publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing),
[NuGet/login](https://github.com/NuGet/login).
