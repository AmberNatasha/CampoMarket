param(
    [string]$BaseUrl = "http://localhost:5088"
)

$ErrorActionPreference = "Stop"
$paths = @(
    "/",
    "/catalogo",
    "/login",
    "/registro",
    "/recuperar",
    "/carrito",
    "/pedidos",
    "/perfil",
    "/perfil/direcciones",
    "/admin",
    "/admin/productos",
    "/admin/categorias",
    "/admin/reportes",
    "/admin/auditoria"
)

foreach ($path in $paths) {
    try {
        $response = Invoke-WebRequest -Uri "$BaseUrl$path" -UseBasicParsing -MaximumRedirection 0 -ErrorAction Stop
        "{0,-12} -> {1}" -f $path, [int]$response.StatusCode
    }
    catch {
        $status = $_.Exception.Response.StatusCode.value__
        if ($status -in 302, 401, 403) {
            "{0,-12} -> {1} (esperado si requiere sesion)" -f $path, $status
        }
        else {
            "{0,-12} -> ERROR {1}" -f $path, $_.Exception.Message
        }
    }
}
