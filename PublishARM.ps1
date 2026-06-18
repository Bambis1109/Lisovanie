# Skript pre publikovanie aplikácie Lisovanie (win-arm64, Self-Contained)
# Pripraví spustiteľnú verziu pre Windows 10 na ARM.
#
# POZOR: Natívne Ixxat knižnice (XatCOP_VCI3-64.dll, XatCOP60-64.dll) sú
#        kompilované pre x64. Na Windows 10 ARM fungujú len cez x86/x64
#        emuláciu, ktorá nemusí byť dostupná pre 64-bit natívny kód.
#        Ak CANopen komunikácia na ARM nezbehne, je to očakávané obmedzenie
#        natívnych knižníc — nie chyba publishu.

# Projekt cieli na .NET 10 — systémový "dotnet" je .NET 9, preto hľadáme
# .NET 10 SDK z Ridera. Fallback na systémový dotnet, ak sa Rider nenájde.
$dotnet = "dotnet"
$riderDotnet = Get-ChildItem `
    "C:\Program Files\JetBrains\JetBrains Rider *\lib\ReSharperHost\windows-x64\dotnet\dotnet.exe" `
    -ErrorAction SilentlyContinue | Sort-Object FullName -Descending | Select-Object -First 1
if ($riderDotnet) {
    $dotnet = $riderDotnet.FullName
    Write-Host "Používam .NET SDK z Ridera: $dotnet" -ForegroundColor Gray
} else {
    Write-Warning "Rider .NET SDK sa nenašiel, používam systémový 'dotnet' (môže byť .NET 9)."
}

$project = "$PSScriptRoot\Lisovanie\Lisovanie.csproj"
$output  = "$PSScriptRoot\publish\Lisovanie-arm64"

# 1. Ukončenie visiacich procesov, ktoré držia projektové DLL.
#    Po zatvorení appky často zostáva visieť Avalonia XAML Previewer
#    (Avalonia.Designer.HostApp.dll v Rideri), ktorý drží CanOpenMaster.dll
#    a natívne Ixxat knižnice zamknuté -> publish/clean zlyhá.
$lockedDll = "$PSScriptRoot\Lisovanie\bin\Debug\net10.0\CanOpenMaster.dll"
$lockers = Get-Process | Where-Object {
    ($_.ProcessName -eq "Lisovanie") -or
    ($_.Modules.FileName -contains $lockedDll)
} 2>$null
if ($lockers) {
    Write-Host "Ukončujem visiace procesy držiace projektové DLL (PID: $($lockers.Id -join ', '))..." -ForegroundColor Yellow
    $lockers | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 500
}

# 2. Vyčistenie starého buildu
if (Test-Path $output) {
    Write-Host "Čistím starý publish adresár..." -ForegroundColor Gray
    Remove-Item -Recurse -Force $output
}

Write-Host "Publishujem do: $output" -ForegroundColor Cyan

# 3. Samotný publish
# SingleFile vypnutý, pretože natívne DLL (Ixxat) sa pri ňom nesprávajú konzistentne
& $dotnet publish $project `
    --configuration Release `
    --runtime win-arm64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:PublishTrimmed=false `
    --output $output

if ($LASTEXITCODE -ne 0) {
    Write-Error "Publish zlyhal (exit code $LASTEXITCODE)"
    exit $LASTEXITCODE
}

# 4. Overenie prítomnosti natívnych DLL (Ixxat CANopen Master)
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
