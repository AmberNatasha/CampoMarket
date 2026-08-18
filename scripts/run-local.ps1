param(
    [int]$Port = 5088
)

$ErrorActionPreference = "Stop"
$env:ASPNETCORE_ENVIRONMENT = "Development"
$root = Split-Path -Parent $PSScriptRoot
$webProject = Join-Path $root "CampoMarket.Web\CampoMarket.Web.csproj"
$apiProject = Join-Path $root "CampoMarketApi\CampoMarketApi.csproj"
$dll = Join-Path $root "CampoMarket.Web\bin\Debug\net10.0\CampoMarket.Web.dll"
$apiDll = Join-Path $root "CampoMarketApi\bin\Debug\net10.0\CampoMarketApi.dll"
$contentRoot = Join-Path $root "CampoMarket.Web"
$apiContentRoot = Join-Path $root "CampoMarketApi"

dotnet build (Join-Path $root "CampoMarket.slnx")
$api = Start-Process dotnet -ArgumentList @($apiDll, "--urls", "http://localhost:5079", "--contentRoot", $apiContentRoot) -WindowStyle Hidden -PassThru
try {
    Start-Sleep -Seconds 1
    if ($api.HasExited) {
        throw "La API no pudo iniciar. Revisa su configuración de desarrollo."
    }
    dotnet $dll --urls "http://localhost:$Port" --contentRoot $contentRoot
}
finally {
    if (!$api.HasExited) {
        Stop-Process -Id $api.Id
    }
}
