$dotnet = "C:\Program Files\JetBrains\JetBrains Rider 2026.1.0.1\lib\ReSharperHost\windows-x64\dotnet\dotnet.exe"
$project = "$PSScriptRoot\Lisovanie\Lisovanie.csproj"
$output  = "$PSScriptRoot\publish\Lisovanie"

Write-Host "Publishujem do: $output"

& $dotnet publish $project `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishTrimmed=false `
    --output $output

if ($LASTEXITCODE -ne 0) {
    Write-Error "Publish zlyhal (exit code $LASTEXITCODE)"
    exit $LASTEXITCODE
}

# Overenie pritomnosti nativnych DLL
$required = @("XatCOP_VCI3-64.dll", "XatCOP60-64.dll")
foreach ($dll in $required) {
    $path = Join-Path $output $dll
    if (Test-Path $path) {
        Write-Host "OK  $dll"
    } else {
        Write-Warning "CHYBA: $dll sa nenasiel v $output"
    }
}

Write-Host ""
Write-Host "Hotovo. Obsah adresara pre prenos:"
Get-ChildItem $output -File | Select-Object Name, @{N="Velkost (kB)"; E={[math]::Round($_.Length/1KB,1)}} | Format-Table -AutoSize
