<#
.SYNOPSIS
    Publishes and verifies a production-signed Belot Android App Bundle.
.DESCRIPTION
    Passwords are read by the signing tools from existing files, never command-line values.
    Create those files outside the repository, restrict access, and remove them afterward.
    No key, password file, device installation or Google Play upload is created here.
.EXAMPLE
    .\tools\PlayRelease\build-android.ps1 -KeystorePath C:\keys\upload.keystore -KeyAlias upload -StorePasswordFile C:\private\store.txt -KeyPasswordFile C:\private\key.txt
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $KeystorePath,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $KeyAlias,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $StorePasswordFile,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $KeyPasswordFile,

    [ValidateNotNullOrEmpty()]
    [string] $VersionName = '1.0',

    [ValidateRange(1, 2100000000)]
    [int] $VersionCode = 1,

    [string] $OutputDirectory
)

$ErrorActionPreference = 'Stop'
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$project = Join-Path $repoRoot 'src\UI\Belot.UI\Belot.UI.csproj'

function Require-NonemptyFile([string] $Path, [string] $Description) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "$Description file is missing."
    }

    $item = Get-Item -LiteralPath $Path
    if ($item.Length -eq 0) {
        throw "$Description file is empty."
    }

    return $item.FullName
}

$KeystorePath = Require-NonemptyFile $KeystorePath 'Keystore'
$StorePasswordFile = Require-NonemptyFile $StorePasswordFile 'Store password'
$KeyPasswordFile = Require-NonemptyFile $KeyPasswordFile 'Key password'
if ([string]::IsNullOrWhiteSpace($KeyAlias)) {
    throw 'The key alias must not be blank.'
}

foreach ($passwordFile in @($StorePasswordFile, $KeyPasswordFile)) {
    if ([string]::IsNullOrWhiteSpace([System.IO.File]::ReadAllText($passwordFile))) {
        throw 'A password file contains no password.'
    }
}

$dotnet = (Get-Command dotnet.exe -ErrorAction Stop).Source
$keytool = (Get-Command keytool.exe -ErrorAction Stop).Source
$jarsigner = (Get-Command jarsigner.exe -ErrorAction Stop).Source
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot "artifacts\play-release-$VersionName\publish"
}

$OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
$releaseDirectory = [System.IO.Directory]::GetParent($OutputDirectory).FullName
$null = [System.IO.Directory]::CreateDirectory($OutputDirectory)
$buildLog = Join-Path $releaseDirectory 'android-publish.log'
$certificateLog = Join-Path $releaseDirectory 'upload-certificate.log'
$signatureLog = Join-Path $releaseDirectory 'aab-signature.log'

# Check the selected publisher key before starting a potentially long build.
$certificate = & $keytool -list -v -keystore $KeystorePath -alias $KeyAlias -storepass:file $StorePasswordFile 2>&1
if ($LASTEXITCODE -ne 0) {
    throw 'The upload keystore or alias could not be opened with the supplied password file.'
}

$certificateText = $certificate -join [Environment]::NewLine
if ($certificateText -match 'CN\s*=\s*Android Debug') {
    throw 'An Android Debug certificate cannot be used for production.'
}

$fingerprint = [regex]::Match($certificateText, 'SHA256:\s*([A-Fa-f0-9:]+)').Groups[1].Value
if ($fingerprint.Length -ne 95) {
    throw 'The upload certificate SHA-256 fingerprint could not be read.'
}

[System.IO.File]::WriteAllText($certificateLog, $certificateText + [Environment]::NewLine)
$arguments = @(
    'publish', $project, '-f', 'net10.0-android', '-c', 'Release', '--verbosity', 'minimal',
    '-p:TreatWarningsAsErrors=true', '-p:AndroidPackageFormats=aab', '-p:AndroidKeyStore=true',
    "-p:ApplicationDisplayVersion=$VersionName", "-p:ApplicationVersion=$VersionCode",
    "-p:AndroidSigningKeyStore=$KeystorePath", "-p:AndroidSigningKeyAlias=$KeyAlias",
    "-p:AndroidSigningStorePass=file:$StorePasswordFile", "-p:AndroidSigningKeyPass=file:$KeyPasswordFile",
    "-p:PublishDir=$OutputDirectory$([System.IO.Path]::DirectorySeparatorChar)",
    '-consoleLoggerParameters:Summary'
)

& $dotnet @arguments 2>&1 | Tee-Object -FilePath $buildLog
if ($LASTEXITCODE -ne 0) {
    throw "Android publish failed. See $buildLog"
}

$bundle = Join-Path $OutputDirectory 'com.nksolutions.belot-Signed.aab'
if (-not (Test-Path -LiteralPath $bundle -PathType Leaf)) {
    throw 'Publish did not produce the expected signed Belot bundle.'
}

$signature = & $jarsigner -verify -certs $bundle 2>&1
$signatureExit = $LASTEXITCODE
$signatureText = $signature -join [Environment]::NewLine
[System.IO.File]::WriteAllText($signatureLog, $signatureText + [Environment]::NewLine)
if ($signatureExit -ne 0 -or $signatureText -notmatch 'jar verified\.') {
    throw "The bundle signature could not be verified. See $signatureLog"
}

$bundleCertificate = & $keytool -printcert -jarfile $bundle 2>&1
if ($LASTEXITCODE -ne 0) {
    throw 'The bundle signing certificate could not be read.'
}

$bundleCertificateText = $bundleCertificate -join [Environment]::NewLine
if ($bundleCertificateText -match 'CN\s*=\s*Android Debug' -or $bundleCertificateText -notmatch [regex]::Escape($fingerprint)) {
    throw 'The bundle was not signed with the selected production upload key.'
}

$manifestPath = Join-Path $repoRoot 'src\UI\Belot.UI\obj\Release\net10.0-android\android\AndroidManifest.xml'
[xml] $manifest = Get-Content -LiteralPath $manifestPath -Raw
$androidNamespace = 'http://schemas.android.com/apk/res/android'
if ($manifest.manifest.package -ne 'com.nksolutions.belot' -or
    $manifest.manifest.GetAttribute('versionCode', $androidNamespace) -ne "$VersionCode" -or
    $manifest.manifest.GetAttribute('versionName', $androidNamespace) -ne $VersionName -or
    $manifest.manifest.'uses-sdk'.GetAttribute('targetSdkVersion', $androidNamespace) -ne '36') {
    throw 'The generated manifest does not match the requested Belot release and target API 36.'
}

$archive = [System.IO.Compression.ZipFile]::OpenRead($bundle)
try {
    $abis = @($archive.Entries.FullName | Where-Object { $_ -match '^base/lib/[^/]+/.*\.so$' } |
        ForEach-Object { ($_ -split '/')[2] } | Sort-Object -Unique)
    if (($abis -join ',') -ne 'arm64-v8a,x86_64') {
        throw 'The bundle does not contain both required arm64-v8a and x86_64 libraries.'
    }
}
finally {
    $archive.Dispose()
}

$hash = (Get-FileHash -LiteralPath $bundle -Algorithm SHA256).Hash.ToLowerInvariant()
$release = [ordered]@{
    packageId = 'com.nksolutions.belot'
    versionName = $VersionName
    versionCode = $VersionCode
    targetSdkVersion = 36
    abis = $abis
    file = $bundle
    sizeBytes = (Get-Item -LiteralPath $bundle).Length
    sha256 = $hash
    uploadCertificateSha256 = $fingerprint
    signatureVerified = $true
    createdAtUtc = [DateTime]::UtcNow.ToString('O')
}
$release | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath (Join-Path $releaseDirectory 'bundle-manifest.json') -Encoding utf8
Write-Host "Verified release bundle: $bundle"
Write-Host "SHA-256: $hash"
Write-Host "Upload certificate SHA-256: $fingerprint"
