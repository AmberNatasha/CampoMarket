param(
    [int]$Port = 5088
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "CampoMarket.Web\CampoMarket.Web.csproj"
$dll = Join-Path $root "CampoMarket.Web\bin\Debug\net10.0\CampoMarket.Web.dll"
$contentRoot = Join-Path $root "CampoMarket.Web"

dotnet build $project
dotnet $dll --urls "http://localhost:$Port" --contentRoot $contentRoot
