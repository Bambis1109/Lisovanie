@echo off
setlocal

:: Nastavenie nazvu vystupneho suboru
set "VYSTUP=ai.txt"

:: 1. Kontrola a vymazanie existujuceho suboru ai.txt
if exist "%VYSTUP%" (
    del "%VYSTUP%"
)

:: 2. Prechod cez vsetky .cs subory v aktualnom adresari
for %%f in (*.cs) do (
    
    :: Zapis nazvu suboru s oddelovacom pre lepsiu prehladnost
    echo ======================================== >> "%VYSTUP%"
    echo Nazov suboru: %%f >> "%VYSTUP%"
    echo ======================================== >> "%VYSTUP%"
    
    :: 3. Pripojenie samotneho obsahu suboru
    type "%%f" >> "%VYSTUP%"
    
    :: Pridanie dvoch prazdnych riadkov na koniec pre lepsiu citatelnost dalsieho suboru
    echo. >> "%VYSTUP%"
    echo. >> "%VYSTUP%"
)

echo.
echo Hotovo! Vsetky .cs subory boli uspesne spojene do suboru %VYSTUP%.
pause