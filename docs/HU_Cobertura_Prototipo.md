# Campo Market - Cobertura de HU del prototipo

Este documento resume las historias cubiertas en el prototipo MVC actual. La persistencia productiva queda preparada por `CampoMarket.Web/SCRIPTS/ScriptInicial.txt`; la app usa datos en memoria para probar rapido el flujo completo.

## Funcionales

| HU | Estado | Donde probar |
| --- | --- | --- |
| US-01 Registro | Implementada | `/registro` |
| US-02 Login por rol | Implementada | `/login` con cliente/admin demo |
| US-03 Logout | Implementada | Boton `Salir` |
| US-04 Recuperar contrasena | Implementada como prototipo | `/recuperar` y enlace generado |
| US-05 Rutas admin protegidas | Implementada | `/admin`, `/admin/productos`, `/admin/categorias` |
| US-06 Editar perfil | Implementada | `/perfil` |
| US-07 Cambiar contrasena | Implementada | `/perfil/contrasena` |
| US-08 Varias direcciones | Implementada | `/perfil/direcciones` |
| US-09 Catalogo activo con stock | Implementada | `/catalogo` |
| US-10 Filtro por categoria | Implementada con fetch | `/catalogo` |
| US-11 Busqueda por nombre | Implementada con fetch | `/catalogo` |
| US-12 Detalle producto | Implementada | `/catalogo/producto/{id}` |
| US-13 Orden por precio | Implementada | `/catalogo` |
| US-14 Agregar carrito | Implementada | `/catalogo` cliente |
| US-15 Ajustar cantidad | Implementada | `/carrito` |
| US-16 Eliminar producto | Implementada | `/carrito` |
| US-17 Resumen carrito | Implementada | `/carrito` |
| US-18 Vaciar carrito | Implementada | `/carrito` |
| US-19 Metodo entrega | Implementada | `/carrito` |
| US-20 Confirmar pedido y descontar stock | Implementada en memoria transaccional | `/carrito` |
| US-21 Historial cliente | Implementada | `/pedidos` |
| US-22 Detalle pedido cliente | Implementada | `/pedidos/{id}` |
| US-23 Cancelar pedido pendiente | Implementada | `/pedidos/{id}` |
| US-24 Pedidos activos admin | Implementada | `/admin/pedidos` |
| US-25 Cambiar estado pedido | Implementada | `/admin/pedidos/{id}` |
| US-26 Detalle pedido admin | Implementada | `/admin/pedidos/{id}` |
| US-27 Buscar pedido | Implementada | `/admin/pedidos` |
| US-28 Historial admin paginado | Implementada | `/admin/pedidos?historial=true` |
| US-29 Crear producto | Implementada | `/admin/productos/nuevo` |
| US-30 Editar producto | Implementada | `/admin/productos/{id}/editar` |
| US-31 Desactivar producto | Implementada | `/admin/productos` |
| US-32 Ajustar stock | Implementada | `/admin/productos` |
| US-33 CRUD categorias | Implementada | `/admin/categorias` |
| US-34 Alerta stock bajo | Implementada | `/admin` y `/admin/productos` |
| US-35 Dashboard del dia | Implementada | `/admin` |
| US-36 Productos mas vendidos | Implementada con filtros | `/admin/reportes` |
| US-37 Movimientos inventario | Implementada con filtros | `/admin/reportes` |

## No funcionales

| NF | Estado | Evidencia |
| --- | --- | --- |
| NF-01 Seguridad web | Preparada | HTTPS redirect, cookie auth, CORS local, CSP, X-Frame-Options |
| NF-02 Validacion y consultas seguras | Preparada | Validaciones server/client, script con constraints; futura BD via repositorios parametrizados |
| NF-03 Intentos fallidos | Implementada | Bloqueo tras 5 intentos y `/admin/auditoria` |
| NF-04 Rendimiento | Preparada | Indices SQL, paginacion catalogo/pedidos, filtros |
| NF-05 Transacciones | Preparada | Flujo de pedido/cancelacion atomico en memoria; script listo para transaccion SQL |
| NF-06 Concurrencia | Preparada | Store con lock y script `scripts/load-smoke.ps1` |
| NF-07 Responsive | Implementada | Bootstrap y estilos responsivos |
| NF-08 Marca | Implementada | Paleta, fuentes y navegacion Campo Market |
| NF-09 Mensajes claros | Implementada | TempData alerts y validaciones inline |
| NF-10 Tres capas | Preparada | Controllers, Services, Models; siguiente paso natural: Repositories SQL |
| NF-11 3FN y scripts versionados | Implementada | `CampoMarket.Web/SCRIPTS/ScriptInicial.txt` |
| NF-12 Errores globales | Implementada | `ErrorController`, vista amigable y `/admin/auditoria` |
| NF-13 Instalacion | Implementada | README y scripts locales |
