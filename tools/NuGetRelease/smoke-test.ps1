param(
    [Parameter(Mandatory = $true)][string]$PackageDirectory,
    [Parameter(Mandatory = $true)][string]$Version
)

$ErrorActionPreference = 'Stop'
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw 'Expected a stable semantic version.' }
$packagePath = Join-Path (Resolve-Path -LiteralPath $PackageDirectory).Path "BelotGameEngine.$Version.nupkg"
$scratchPath = Join-Path $PSScriptRoot '../../artifacts/nuget-smoke'
New-Item -ItemType Directory -Path $scratchPath -Force | Out-Null
$scratchPath = (Resolve-Path -LiteralPath $scratchPath).Path

$archive = [System.IO.Compression.ZipFile]::OpenRead($packagePath)
try {
    $reader = [System.IO.StreamReader]::new($archive.GetEntry('README.md').Open())
    try { $readme = $reader.ReadToEnd() } finally { $reader.Dispose() }
} finally { $archive.Dispose() }
$sample = [regex]::Match($readme, '(?s)```csharp\r?\n(.*?)```')
if (-not $sample.Success) { throw 'The packaged README has no C# example.' }
$program = $sample.Groups[1].Value + @'

if (view.Hand.Count != 5 || match.ToMove == seat)
{
    throw new InvalidOperationException("The packaged engine did not deal five cards and advance the bid.");
}

foreach (var reference in typeof(BelotMatch).Assembly.GetReferencedAssemblies())
{
    if (reference.Name.StartsWith("Belot.AI", StringComparison.Ordinal))
    {
        throw new InvalidOperationException("Unexpected bot assembly reference.");
    }
}

Console.WriteLine("PASS: packaged README example; five-card seat view and accepted bid; no bot references.");
'@
[System.IO.File]::WriteAllText((Join-Path $scratchPath 'Program.cs'), $program)
$project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  <ItemGroup><PackageReference Include="BelotGameEngine" Version="[$Version]" /></ItemGroup>
</Project>
"@
$projectPath = Join-Path $scratchPath 'Smoke.csproj'
[System.IO.File]::WriteAllText($projectPath, $project)
$feed = [System.Security.SecurityElement]::Escape((Split-Path -Parent $packagePath))
$configPath = Join-Path $scratchPath 'NuGet.Config'
[System.IO.File]::WriteAllText($configPath, @"
<configuration>
  <packageSources><clear /><add key="release" value="$feed" /></packageSources>
  <packageSourceMapping><packageSource key="release"><package pattern="BelotGameEngine" /></packageSource></packageSourceMapping>
</configuration>
"@)
dotnet restore $projectPath --configfile $configPath --packages (Join-Path $scratchPath 'packages') --force --no-cache
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet run --project $projectPath -c Release --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
