/*
  Limpia todos los registros de la base de datos CampoMarket.
  Mantiene la estructura de tablas, relaciones, constraints, indices y procedimientos.
  Ejecutar en SQL Server Management Studio o Azure Data Studio.
*/

USE CampoMarket;
GO

SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DELETE FROM dbo.Token_Restablecimiento;
    DELETE FROM dbo.Log_Error;
    DELETE FROM dbo.Audit_Log;
    DELETE FROM dbo.Movimiento_Inventario;
    DELETE FROM dbo.Historial_Estado_Pedido;
    DELETE FROM dbo.Detalle_Pedido;
    DELETE FROM dbo.Pedido;
    DELETE FROM dbo.Detalle_Carrito;
    DELETE FROM dbo.Carrito;
    DELETE FROM dbo.Producto;
    DELETE FROM dbo.Categoria;
    DELETE FROM dbo.Metodo_Entrega;
    DELETE FROM dbo.Direccion;
    DELETE FROM dbo.Usuario;

    DBCC CHECKIDENT ('dbo.Token_Restablecimiento', RESEED, 0);
    DBCC CHECKIDENT ('dbo.Log_Error', RESEED, 0);
    DBCC CHECKIDENT ('dbo.Audit_Log', RESEED, 0);
    DBCC CHECKIDENT ('dbo.Movimiento_Inventario', RESEED, 0);
    DBCC CHECKIDENT ('dbo.Historial_Estado_Pedido', RESEED, 0);
    DBCC CHECKIDENT ('dbo.Detalle_Pedido', RESEED, 0);
    DBCC CHECKIDENT ('dbo.Pedido', RESEED, 0);
    DBCC CHECKIDENT ('dbo.Detalle_Carrito', RESEED, 0);
    DBCC CHECKIDENT ('dbo.Carrito', RESEED, 0);
    DBCC CHECKIDENT ('dbo.Producto', RESEED, 0);
    DBCC CHECKIDENT ('dbo.Categoria', RESEED, 0);
    DBCC CHECKIDENT ('dbo.Metodo_Entrega', RESEED, 0);
    DBCC CHECKIDENT ('dbo.Direccion', RESEED, 0);
    DBCC CHECKIDENT ('dbo.Usuario', RESEED, 0);

    IF OBJECT_ID('dbo.SQ_NumeroPedido', 'SO') IS NOT NULL
    BEGIN
        ALTER SEQUENCE dbo.SQ_NumeroPedido RESTART WITH 1;
    END;

    COMMIT TRANSACTION;
    PRINT 'Base CampoMarket limpiada correctamente.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;

    THROW;
END CATCH;
GO
