param(
    [string]$BaseUrl = "http://localhost:5088",
    [int]$Users = 20
)

$ErrorActionPreference = "Stop"
$jobs = 1..$Users | ForEach-Object {
    Start-Job -ScriptBlock {
        param($BaseUrl)
        Invoke-WebRequest -Uri "$BaseUrl/catalogo" -UseBasicParsing | Out-Null
        Invoke-WebRequest -Uri "$BaseUrl/login" -UseBasicParsing | Out-Null
        "OK"
    } -ArgumentList $BaseUrl
}

$results = $jobs | Wait-Job | Receive-Job
$jobs | Remove-Job

"Solicitudes completadas: $($results.Count) / $Users"
