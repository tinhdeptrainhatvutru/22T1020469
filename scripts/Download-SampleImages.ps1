param(
    [int]$Count = 20
)

$ErrorActionPreference = "Stop"

$solutionRoot = Split-Path -Parent $PSScriptRoot
$productsDir = Join-Path -Path $solutionRoot -ChildPath "SV22T1020605.Admin/wwwroot/images/products"

if (!(Test-Path $productsDir)) {
    New-Item -ItemType Directory -Path $productsDir -Force | Out-Null
}

for ($i = 1; $i -le $Count; $i++) {
    $fileName = "product_$i.jpg"
    $targetPath = Join-Path -Path $productsDir -ChildPath $fileName

    $url = "https://picsum.photos/600/600?random=$i"
    Write-Host "Downloading $url => $targetPath"

    Invoke-WebRequest -Uri $url -OutFile $targetPath -UseBasicParsing
}

Write-Host "Done!"

