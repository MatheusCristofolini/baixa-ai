Write-Host "Iniciando compilação do Baixa AI..." -ForegroundColor Cyan

$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) {
    Write-Error "Compilador do .NET Framework não encontrado em: $csc"
    exit 1
}

$iconFile = "app_icon.ico"
$argsList = @("/target:winexe", "/optimize+", "/out:BaixaAI.exe")

# Add icon if it exists
if (Test-Path $iconFile) {
    Write-Host "Ícone encontrado: $iconFile. Adicionando ao executável..." -ForegroundColor Green
    $argsList += "/win32icon:$iconFile"
} else {
    Write-Host "Ícone não encontrado. O executável usará o ícone padrão." -ForegroundColor Yellow
}

# Add standard references
$argsList += "/r:System.dll,System.Drawing.dll,System.Windows.Forms.dll"

# Add source file
$argsList += "BaixaAI.cs"

# Run compiler
Write-Host "Executando compilador C# para gerar BaixaAI.exe..." -ForegroundColor Cyan
& $csc $argsList

if ($LASTEXITCODE -ne 0) {
    Write-Error "Ocorreu um erro durante a compilação do Baixa AI."
    exit $LASTEXITCODE
}
Write-Host "BaixaAI.exe gerado com SUCESSO!`n" -ForegroundColor Green

# --- COMPILAÇÃO DO INSTALADOR (INNO SETUP) ---
Write-Host "Preparando empacotamento do instalador..." -ForegroundColor Cyan

# Caminhos possíveis do ISCC.exe
$isccPaths = @(
    "ISCC.exe", # se estiver no PATH
    "$env:LocalAppData\Programs\Inno Setup 6\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe"
)

$iscc = $null
foreach ($path in $isccPaths) {
    if (Get-Command $path -ErrorAction SilentlyContinue) {
        $iscc = $path
        break
    }
    if (Test-Path $path) {
        $iscc = $path
        break
    }
}

if ($null -eq $iscc) {
    Write-Warning "Inno Setup Compiler (ISCC.exe) não foi encontrado nos caminhos padrão."
    Write-Warning "O instalador Setup_BaixaAI.exe não pôde ser gerado."
    Write-Warning "Certifique-se de ter o Inno Setup instalado."
    exit 0
}

Write-Host "Compilador do Inno Setup encontrado em: $iscc" -ForegroundColor Green
Write-Host "Executando compilador de instalação..." -ForegroundColor Cyan

& $iscc installer.iss

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nInstalador compilado com SUCESSO! Arquivo Setup_BaixaAI.exe gerado na pasta Output." -ForegroundColor Green
    # Inno Setup standard output folder is under Output\ relative to script, unless customized.
    # In our installer.iss we did not change output dir, so it outputs to 'Output\' folder by default.
    # Let's move it to the root directory for ease of access if the user prefers, or keep it in Output.
    if (Test-Path "Output\Setup_BaixaAI.exe") {
        Move-Item "Output\Setup_BaixaAI.exe" ".\Setup_BaixaAI.exe" -Force
        Remove-Item "Output" -Recurse -ErrorAction SilentlyContinue
        Write-Host "Instalador movido para o diretório raiz: .\Setup_BaixaAI.exe" -ForegroundColor Green
    }
} else {
    Write-Error "Ocorreu um erro durante a compilação do instalador."
    exit $LASTEXITCODE
}
