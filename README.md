# CampoMarket

Prototipo funcional MVC para Campo Market.

## Ejecutar la app

Desde PowerShell:

```powershell
.\scripts\run-local.ps1
```

Luego abrir `http://localhost:5088`.

Cuentas demo:

- Cliente: `cliente@campomarket.test` / `Cliente123!`
- Admin: `admin@campomarket.test` / `Admin123!`

## Verificar rutas

Con la app corriendo:

```powershell
.\scripts\verify-local.ps1
```

Para una prueba ligera de concurrencia de 20 usuarios:

```powershell
.\scripts\load-smoke.ps1
```

## Base de datos

El script inicial oficial esta en `CampoMarket.Web\SCRIPTS\ScriptInicial.txt` y tambien hay una copia en `database\CampoMarket.sql`. Se puede ejecutar desde SSMS 22 para crear `CampoMarket` con tablas, claves primarias, claves foraneas, restricciones, indices y datos demo. El prototipo web actual conserva datos en memoria para pruebas rapidas; el script deja lista la estructura oficial para conectar SQL Server.

Luego puedes ejecutar `database\VerificarCampoMarket.sql` para confirmar conteos basicos y alertas de stock.

## Cobertura de historias

La matriz de HU implementadas esta en `docs\HU_Cobertura_Prototipo.md`.
G3 
