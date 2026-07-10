/*
  Consulta todos los registros de las tablas principales de CampoMarket.
  Ejecutar en SQL Server Management Studio o Azure Data Studio.
*/

USE CampoMarket;
GO

SELECT 'Usuario' AS Tabla, COUNT(*) AS Registros FROM dbo.Usuario;
SELECT * FROM dbo.Usuario ORDER BY id_usuario;

SELECT 'Direccion' AS Tabla, COUNT(*) AS Registros FROM dbo.Direccion;
SELECT * FROM dbo.Direccion ORDER BY id_direccion;

SELECT 'Metodo_Entrega' AS Tabla, COUNT(*) AS Registros FROM dbo.Metodo_Entrega;
SELECT * FROM dbo.Metodo_Entrega ORDER BY id_metodo;

SELECT 'Categoria' AS Tabla, COUNT(*) AS Registros FROM dbo.Categoria;
SELECT * FROM dbo.Categoria ORDER BY id_categoria;

SELECT 'Producto' AS Tabla, COUNT(*) AS Registros FROM dbo.Producto;
SELECT * FROM dbo.Producto ORDER BY id_producto;

SELECT 'Carrito' AS Tabla, COUNT(*) AS Registros FROM dbo.Carrito;
SELECT * FROM dbo.Carrito ORDER BY id_carrito;

SELECT 'Detalle_Carrito' AS Tabla, COUNT(*) AS Registros FROM dbo.Detalle_Carrito;
SELECT * FROM dbo.Detalle_Carrito ORDER BY id_detalle_carrito;

SELECT 'Pedido' AS Tabla, COUNT(*) AS Registros FROM dbo.Pedido;
SELECT * FROM dbo.Pedido ORDER BY id_pedido;

SELECT 'Detalle_Pedido' AS Tabla, COUNT(*) AS Registros FROM dbo.Detalle_Pedido;
SELECT * FROM dbo.Detalle_Pedido ORDER BY id_detalle_pedido;

SELECT 'Historial_Estado_Pedido' AS Tabla, COUNT(*) AS Registros FROM dbo.Historial_Estado_Pedido;
SELECT * FROM dbo.Historial_Estado_Pedido ORDER BY id_historial;

SELECT 'Movimiento_Inventario' AS Tabla, COUNT(*) AS Registros FROM dbo.Movimiento_Inventario;
SELECT * FROM dbo.Movimiento_Inventario ORDER BY id_movimiento;

SELECT 'Audit_Log' AS Tabla, COUNT(*) AS Registros FROM dbo.Audit_Log;
SELECT * FROM dbo.Audit_Log ORDER BY id_audit_log;

SELECT 'Log_Error' AS Tabla, COUNT(*) AS Registros FROM dbo.Log_Error;
SELECT * FROM dbo.Log_Error ORDER BY id_log_error;

SELECT 'Token_Restablecimiento' AS Tabla, COUNT(*) AS Registros FROM dbo.Token_Restablecimiento;
SELECT * FROM dbo.Token_Restablecimiento ORDER BY id_token;
GO
