param(
    [string] $BrowserPath = 'C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe'
)

# Rebuild the Play icon and localized feature art from the app's own resources.
# Requires Windows, PowerShell 7, and Microsoft Edge (or a Chromium --BrowserPath).
# No screen capture, generated UI, external assets, or image-generation service is used.
$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$resources = Join-Path $repoRoot 'src\UI\Belot.UI\Resources'
$output = Join-Path $repoRoot 'store\google-play\graphics'
$work = Join-Path $repoRoot ('artifacts\store-graphics\' + [guid]::NewGuid().ToString('N'))
if (-not (Test-Path -LiteralPath $BrowserPath)) {
    throw "Chromium browser was not found: $BrowserPath"
}
[IO.Directory]::CreateDirectory($output) | Out-Null
[IO.Directory]::CreateDirectory($work) | Out-Null
Add-Type -AssemblyName System.Drawing

function ConvertTo-DataUrl([string] $Path, [string] $MediaType) {
    return 'data:' + $MediaType + ';base64,' + [Convert]::ToBase64String([IO.File]::ReadAllBytes($Path))
}

function Get-SvgBody([string] $Path) {
    $svg = [IO.File]::ReadAllText($Path)
    return [regex]::Match($svg, '(?s)<svg\b[^>]*>(.*)</svg>').Groups[1].Value
}

function Write-Graphic([string] $Name, [string] $Html, [int] $Width, [int] $Height) {
    $htmlPath = Join-Path $work ($Name + '.html')
    $rawPath = Join-Path $work ($Name + '.png')
    $pngPath = Join-Path $output ($Name + '.png')
    [IO.File]::WriteAllText($htmlPath, $Html, [Text.UTF8Encoding]::new($false))
    $arguments = @(
        '--headless', '--disable-gpu', '--hide-scrollbars', '--no-first-run',
        '--disable-extensions', '--force-device-scale-factor=1', '--run-all-compositor-stages-before-draw',
        '--virtual-time-budget=2000', "--window-size=$Width,$Height",
        ('--user-data-dir="' + (Join-Path $work ($Name + '-profile')) + '"'),
        ('--screenshot="' + $rawPath + '"'),
        ('"' + ([uri]$htmlPath).AbsoluteUri + '"')
    )
    $process = Start-Process -FilePath $BrowserPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
    if (-not $process.WaitForExit(30000)) {
        $process.Kill()
        throw "Rendering timed out: $Name"
    }
    if ($process.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $rawPath)) {
        throw "Rendering failed: $Name (exit $($process.ExitCode))"
    }

    # Convert Chromium's opaque raster to 24-bit RGB without scaling or resampling.
    $source = [Drawing.Bitmap]::new($rawPath)
    try {
        if ($source.Width -ne $Width -or $source.Height -ne $Height) {
            throw "Unexpected raster size: $($source.Width) x $($source.Height)"
        }
        $rgb = [Drawing.Bitmap]::new($Width, $Height, [Drawing.Imaging.PixelFormat]::Format24bppRgb)
        try {
            $canvas = [Drawing.Graphics]::FromImage($rgb)
            try {
                $canvas.Clear([Drawing.Color]::White)
                $canvas.DrawImageUnscaled($source, 0, 0)
            }
            finally { $canvas.Dispose() }
            $rgb.Save($pngPath, [Drawing.Imaging.ImageFormat]::Png)
        }
        finally { $rgb.Dispose() }
    }
    finally { $source.Dispose() }
    $bytes = [IO.File]::ReadAllBytes($pngPath)
    if ($bytes[24] -ne 8 -or $bytes[25] -ne 2) {
        throw "Output must be an 8-bit RGB PNG: $pngPath"
    }
    [pscustomobject]@{
        File = [IO.Path]::GetRelativePath($repoRoot, $pngPath).Replace('\', '/')
        Width = $Width
        Height = $Height
        Format = '24-bit RGB PNG, opaque'
        Sha256 = (Get-FileHash -LiteralPath $pngPath -Algorithm SHA256).Hash.ToLowerInvariant()
        Bytes = $bytes.Length
    }
}

$background = Get-SvgBody (Join-Path $resources 'AppIcon\appicon.svg')
$foreground = Get-SvgBody (Join-Path $resources 'AppIcon\appiconfg.svg')
$icon = @"
<!doctype html><html><head><meta charset="utf-8"><style>
html,body{margin:0;width:512px;height:512px;overflow:hidden;background:#073A1D}svg{display:block}
</style></head><body><svg xmlns="http://www.w3.org/2000/svg" width="512" height="512" viewBox="0 0 456 456">$background$foreground</svg></body></html>
"@

$regular = ConvertTo-DataUrl (Join-Path $resources 'Fonts\OpenSans-Regular.ttf') 'font/ttf'
$semibold = ConvertTo-DataUrl (Join-Path $resources 'Fonts\OpenSans-Semibold.ttf') 'font/ttf'
$king = ConvertTo-DataUrl (Join-Path $resources 'Images\card_kingspade.png') 'image/png'
$queen = ConvertTo-DataUrl (Join-Path $resources 'Images\card_queenspade.png') 'image/png'
$jack = ConvertTo-DataUrl (Join-Path $resources 'Images\card_jackspade.png') 'image/png'
$locales = @(
    @{ Name = 'feature-bg'; Title = 'БЕЛОТ'; Eyebrow = 'КЛАСИЧЕСКА ИГРА НА КАРТИ'; Subtitle = 'Твоят ход.'; Features = '5 нива · Подсказки · Офлайн'; Language = 'bg' },
    @{ Name = 'feature-en-US'; Title = 'BELOT'; Eyebrow = 'THE BULGARIAN CARD GAME'; Subtitle = 'Your move.'; Features = '5 levels · Hints · Play offline'; Language = 'en' }
)
$records = [Collections.Generic.List[object]]::new()
$records.Add((Write-Graphic 'icon' $icon 512 512))
foreach ($locale in $locales) {
    $html = @"
<!doctype html><html lang="$($locale.Language)"><head><meta charset="utf-8"><style>
@font-face{font-family:OpenSans;src:url('$regular')}@font-face{font-family:OpenSans;src:url('$semibold');font-weight:600}
*{box-sizing:border-box}html,body{margin:0;width:1024px;height:500px;overflow:hidden}
body{font-family:OpenSans,sans-serif;color:#fff;background:#0C2823;position:relative;
background-image:radial-gradient(ellipse at 79% 39%,#21664E 0%,#124137 44%,#0C2823 100%)}
.ring{position:absolute;border:1px solid rgba(226,184,100,.18);border-radius:50%;width:830px;height:830px;left:461px;top:-168px}
.ring.inner{width:700px;height:700px;left:526px;top:-103px;border-color:rgba(226,184,100,.1)}
.copy{position:absolute;left:72px;top:92px;width:485px;z-index:2}
.eyebrow{font-size:17px;font-weight:600;letter-spacing:2px;color:#E2B864;white-space:nowrap}
h1{font-size:104px;line-height:1.15;letter-spacing:2px;font-weight:600;margin:13px 0 5px}
.subtitle{font-size:32px;line-height:1.4;color:#E3EDE7;margin:0}
.features{position:absolute;left:74px;top:369px;font-size:21px;letter-spacing:.15px;white-space:nowrap;color:#E3EDE7}
.rule{width:44px;height:3px;background:#E2B864;margin-bottom:18px}
.cards{position:absolute;left:643px;top:101px;width:208px;height:290px}
.card{position:absolute;width:208px;height:290px;filter:drop-shadow(0 12px 12px rgba(0,0,0,.35));transform-origin:50% 96%}
.king{transform:translate(-38px,13px) rotate(-17deg)}
.queen{transform:translate(8px,-5px) rotate(-1deg)}
.jack{transform:translate(51px,10px) rotate(17deg)}
</style></head><body><div class="ring"></div><div class="ring inner"></div>
<div class="copy"><div class="eyebrow">$($locale.Eyebrow)</div><h1>$($locale.Title)</h1><p class="subtitle">$($locale.Subtitle)</p></div>
<div class="features"><div class="rule"></div>$($locale.Features)</div>
<div class="cards"><img class="card king" src="$king" alt=""><img class="card queen" src="$queen" alt=""><img class="card jack" src="$jack" alt=""></div>
</body></html>
"@
    $records.Add((Write-Graphic $locale.Name $html 1024 500))
}
$manifest = [ordered]@{
    Generator = 'tools/StoreScreenshots/render_graphics.ps1'
    BrowserVersion = (Get-Item -LiteralPath $BrowserPath).VersionInfo.FileVersion
    Source = 'App icon vectors, Open Sans fonts, and native playing-card artwork from src/UI/Belot.UI/Resources. Feature artwork is promotional composition, not a gameplay screenshot.'
    Assets = $records.ToArray()
}
[IO.File]::WriteAllText((Join-Path $output 'manifest.json'), ($manifest | ConvertTo-Json -Depth 5) + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
$records | Format-Table File, Width, Height, Bytes
