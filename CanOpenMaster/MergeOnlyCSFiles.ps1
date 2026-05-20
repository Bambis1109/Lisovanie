$outputFile = "MergeOnlyCSFiles.txt"

# Kontrola a vymazanie starého súboru
if (Test-Path $outputFile) {
    Write-Host "Našiel sa starý súbor '$outputFile'. Vymazávam..." -ForegroundColor Yellow
    Remove-Item $outputFile -Force
}

Write-Host "Začínam vyhľadávať a spájať IBA .cs súbory (ignorujem zložky bin a obj)..." -ForegroundColor Cyan

# Počítadlo spracovaných súborov
$counter = 0

# Získanie všetkých .cs súborov rekurzívne a filtrovanie vylúčených adresárov
Get-ChildItem -Filter *.cs -Recurse | Where-Object { 
    $_.FullName -notmatch '[\\/]bin[\\/]' -and 
    $_.FullName -notmatch '[\\/]obj[\\/]' 
} | ForEach-Object {
    # Získanie relatívnej cesty a odstránenie '.\' na začiatku
    $relativePath = Resolve-Path -Relative $_.FullName
    $relativePath = $relativePath -replace '^\.\\', '' 
    
    # Zobrazenie priebehu spracovania v konzole
    Write-Host "Pripájam: $relativePath" -ForegroundColor Green
    
    # Hlavička s názvom súboru
    $header = "`r`n// ==========================================`r`n"
    $header += "// Súbor: $relativePath`r`n"
    $header += "// ==========================================`r`n"
    
    # Zápis hlavičky do súboru (používame UTF-8 kódovanie)
    Add-Content -Path $outputFile -Value $header -Encoding UTF8
    
    # Prečítanie obsahu súboru a jeho pripojenie
    $content = Get-Content $_.FullName -Raw
    Add-Content -Path $outputFile -Value $content -Encoding UTF8
    
    $counter++
}

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Hotovo! Celkovo bolo spojených $counter .cs súborov do '$outputFile'." -ForegroundColor Cyan