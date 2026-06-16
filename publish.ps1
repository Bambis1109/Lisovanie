# Skript pre publikovanie aplikácie Lisovanie (win-x64, Self-Contained)

$dotnet = "dotnet"
$project = "$PSScriptRoot\Lisovanie\Lisovanie.csproj"
$output  = "$PSScriptRoot\publish\Lisovanie"

# 1. Vyčistenie starého buildu
if (Test-Path $output) {
    Write-Host "Čistím starý publish adresár..." -ForegroundColor Gray
    Remove-Item -Recurse -Force $output
}

Write-Host "Publishujem do: $output" -ForegroundColor Cyan

# 2. Samotný publish
# SingleFile vypnutý, pretože natívne DLL (Ixxat) sa pri ňom nesprávajú konzistentne
& $dotnet publish $project `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:PublishTrimmed=false `
    --output $output

if ($LASTEXITCODE -ne 0) {
    Write-Error "Publish zlyhal (exit code $LASTEXITCODE)"
    exit $LASTEXITCODE
}

# 3. Overenie prítomnosti natívnych DLL (Ixxat CANopen Master)
$required = @("XatCOP_VCI3-64.dll", "XatCOP60-64.dll")
Write-Host "`nKontrola natívnych knižníc:" -ForegroundColor Yellow
foreach ($dll in $required) {
    $path = Join-Path $output $dll
    if (Test-Path $path) {
        Write-Host "  [OK]  $dll" -ForegroundColor Green
    } else {
        Write-Warning "  [!!!] CHYBA: $dll sa nenašiel v $output"
    }
}

Write-Host "`nHotovo. Obsah adresára pre prenos:"
Get-ChildItem $output -File | Select-Object Name, @{N="Velkost (MB)"; E={[math]::Round($_.Length/1MB,2)}} | Format-Table -AutoSize
