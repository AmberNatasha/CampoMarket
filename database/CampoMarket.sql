/*
  Campo Market - Script inicial oficial
  Compatible con SQL Server 2025 / SSMS 22.

  Base del modelo oficial:
  Usuario, Direccion, Metodo_Entrega, Categoria, Producto, Carrito,
  Detalle_Carrito, Pedido y Detalle_Pedido.

  Extensiones justificadas por las HU:
  - Usuario: intentos_fallidos, bloqueado_hasta y activo para seguridad.
  - Direccion: predeterminada para varias direcciones de entrega.
  - Producto: activo, stock_minimo y fecha_actualizacion para baja logica e inventario.
  - Pedido: numero_pedido y fecha_cancelacion para confirmacion/cancelacion.
  - Historial_Estado_Pedido, Movimiento_Inventario, Audit_Log,
    Log_Error y Token_Restablecimiento para HU de administracion, auditoria y recuperacion.
*/

IF DB_ID('CampoMarket') IS NULL
BEGIN
    CREATE DATABASE CampoMarket;
END;
GO

USE CampoMarket;
GO

IF OBJECT_ID('dbo.Token_Restablecimiento', 'U') IS NOT NULL DROP TABLE dbo.Token_Restablecimiento;
IF OBJECT_ID('dbo.Log_Error', 'U') IS NOT NULL DROP TABLE dbo.Log_Error;
IF OBJECT_ID('dbo.Audit_Log', 'U') IS NOT NULL DROP TABLE dbo.Audit_Log;
IF OBJECT_ID('dbo.Movimiento_Inventario', 'U') IS NOT NULL DROP TABLE dbo.Movimiento_Inventario;
IF OBJECT_ID('dbo.Historial_Estado_Pedido', 'U') IS NOT NULL DROP TABLE dbo.Historial_Estado_Pedido;
IF OBJECT_ID('dbo.Detalle_Pedido', 'U') IS NOT NULL DROP TABLE dbo.Detalle_Pedido;
IF OBJECT_ID('dbo.Pedido', 'U') IS NOT NULL DROP TABLE dbo.Pedido;
IF OBJECT_ID('dbo.Detalle_Carrito', 'U') IS NOT NULL DROP TABLE dbo.Detalle_Carrito;
IF OBJECT_ID('dbo.Carrito', 'U') IS NOT NULL DROP TABLE dbo.Carrito;
IF OBJECT_ID('dbo.Producto', 'U') IS NOT NULL DROP TABLE dbo.Producto;
IF OBJECT_ID('dbo.Categoria', 'U') IS NOT NULL DROP TABLE dbo.Categoria;
IF OBJECT_ID('dbo.Metodo_Entrega', 'U') IS NOT NULL DROP TABLE dbo.Metodo_Entrega;
IF OBJECT_ID('dbo.Direccion', 'U') IS NOT NULL DROP TABLE dbo.Direccion;
IF OBJECT_ID('dbo.Usuario', 'U') IS NOT NULL DROP TABLE dbo.Usuario;
IF OBJECT_ID('dbo.SQ_NumeroPedido', 'SO') IS NOT NULL DROP SEQUENCE dbo.SQ_NumeroPedido;
GO

CREATE TABLE dbo.Usuario (
    id_usuario INT IDENTITY(1,1) NOT NULL,
    nombre VARCHAR(100) NOT NULL,
    correo VARCHAR(150) NOT NULL,
    contrasena_hash VARCHAR(255) NOT NULL,
    telefono VARCHAR(20) NULL,
    rol VARCHAR(50) NOT NULL,
    fecha_registro DATETIME NOT NULL CONSTRAINT DF_Usuario_fecha_registro DEFAULT GETDATE(),
    intentos_fallidos INT NOT NULL CONSTRAINT DF_Usuario_intentos_fallidos DEFAULT 0,
    bloqueado_hasta DATETIME NULL,
    activo BIT NOT NULL CONSTRAINT DF_Usuario_activo DEFAULT 1,
    CONSTRAINT PK_Usuario PRIMARY KEY (id_usuario),
    CONSTRAINT UQ_Usuario_correo UNIQUE (correo),
    CONSTRAINT CK_Usuario_rol CHECK (rol IN ('Cliente', 'Admin')),
    CONSTRAINT CK_Usuario_intentos CHECK (intentos_fallidos >= 0)
);

CREATE TABLE dbo.Direccion (
    id_direccion INT IDENTITY(1,1) NOT NULL,
    id_usuario INT NOT NULL,
    provincia VARCHAR(100) NOT NULL,
    canton VARCHAR(100) NOT NULL,
    distrito VARCHAR(100) NOT NULL,
    senas_exactas VARCHAR(255) NOT NULL,
    predeterminada BIT NOT NULL CONSTRAINT DF_Direccion_predeterminada DEFAULT 0,
    CONSTRAINT PK_Direccion PRIMARY KEY (id_direccion),
    CONSTRAINT FK_Direccion_Usuario FOREIGN KEY (id_usuario) REFERENCES dbo.Usuario(id_usuario)
);

CREATE TABLE dbo.Metodo_Entrega (
    id_metodo INT IDENTITY(1,1) NOT NULL,
    tipo VARCHAR(100) NOT NULL,
    costo_adicional DECIMAL(10,2) NOT NULL,
    CONSTRAINT PK_Metodo_Entrega PRIMARY KEY (id_metodo),
    CONSTRAINT UQ_Metodo_Entrega_tipo UNIQUE (tipo),
    CONSTRAINT CK_Metodo_Entrega_costo CHECK (costo_adicional >= 0)
);

CREATE TABLE dbo.Categoria (
    id_categoria INT IDENTITY(1,1) NOT NULL,
    nombre_categoria VARCHAR(100) NOT NULL,
    descripcion VARCHAR(255) NULL,
    activo BIT NOT NULL CONSTRAINT DF_Categoria_activo DEFAULT 1,
    CONSTRAINT PK_Categoria PRIMARY KEY (id_categoria),
    CONSTRAINT UQ_Categoria_nombre UNIQUE (nombre_categoria)
);

CREATE TABLE dbo.Producto (
    id_producto INT IDENTITY(1,1) NOT NULL,
    nombre_producto VARCHAR(150) NOT NULL,
    descripcion VARCHAR(255) NULL,
    precio DECIMAL(10,2) NOT NULL,
    stock INT NOT NULL,
    imagen_url VARCHAR(500) NULL,
    id_categoria INT NOT NULL,
    activo BIT NOT NULL CONSTRAINT DF_Producto_activo DEFAULT 1,
    stock_minimo INT NOT NULL CONSTRAINT DF_Producto_stock_minimo DEFAULT 5,
    fecha_actualizacion DATETIME NOT NULL CONSTRAINT DF_Producto_fecha_actualizacion DEFAULT GETDATE(),
    CONSTRAINT PK_Producto PRIMARY KEY (id_producto),
    CONSTRAINT FK_Producto_Categoria FOREIGN KEY (id_categoria) REFERENCES dbo.Categoria(id_categoria),
    CONSTRAINT CK_Producto_precio CHECK (precio >= 0),
    CONSTRAINT CK_Producto_stock CHECK (stock >= 0),
    CONSTRAINT CK_Producto_stock_minimo CHECK (stock_minimo >= 0)
);

CREATE TABLE dbo.Carrito (
    id_carrito INT IDENTITY(1,1) NOT NULL,
    id_usuario INT NOT NULL,
    fecha_creacion DATETIME NOT NULL CONSTRAINT DF_Carrito_fecha_creacion DEFAULT GETDATE(),
    CONSTRAINT PK_Carrito PRIMARY KEY (id_carrito),
    CONSTRAINT FK_Carrito_Usuario FOREIGN KEY (id_usuario) REFERENCES dbo.Usuario(id_usuario)
);

CREATE TABLE dbo.Detalle_Carrito (
    id_detalle_carrito INT IDENTITY(1,1) NOT NULL,
    id_carrito INT NOT NULL,
    id_producto INT NOT NULL,
    cantidad INT NOT NULL,
    CONSTRAINT PK_Detalle_Carrito PRIMARY KEY (id_detalle_carrito),
    CONSTRAINT FK_Detalle_Carrito_Carrito FOREIGN KEY (id_carrito) REFERENCES dbo.Carrito(id_carrito),
    CONSTRAINT FK_Detalle_Carrito_Producto FOREIGN KEY (id_producto) REFERENCES dbo.Producto(id_producto),
    CONSTRAINT UQ_Detalle_Carrito_producto UNIQUE (id_carrito, id_producto),
    CONSTRAINT CK_Detalle_Carrito_cantidad CHECK (cantidad > 0)
);

CREATE TABLE dbo.Pedido (
    id_pedido INT IDENTITY(1,1) NOT NULL,
    id_usuario INT NOT NULL,
    id_direccion INT NOT NULL,
    id_metodo INT NOT NULL,
    numero_pedido VARCHAR(30) NOT NULL,
    fecha_pedido DATETIME NOT NULL CONSTRAINT DF_Pedido_fecha_pedido DEFAULT GETDATE(),
    estado VARCHAR(50) NOT NULL,
    total DECIMAL(10,2) NOT NULL,
    fecha_cancelacion DATETIME NULL,
    CONSTRAINT PK_Pedido PRIMARY KEY (id_pedido),
    CONSTRAINT UQ_Pedido_numero UNIQUE (numero_pedido),
    CONSTRAINT FK_Pedido_Usuario FOREIGN KEY (id_usuario) REFERENCES dbo.Usuario(id_usuario),
    CONSTRAINT FK_Pedido_Direccion FOREIGN KEY (id_direccion) REFERENCES dbo.Direccion(id_direccion),
    CONSTRAINT FK_Pedido_Metodo_Entrega FOREIGN KEY (id_metodo) REFERENCES dbo.Metodo_Entrega(id_metodo),
    CONSTRAINT CK_Pedido_estado CHECK (estado IN ('Pendiente', 'Preparando', 'Listo', 'Entregado', 'Cancelado')),
    CONSTRAINT CK_Pedido_total CHECK (total >= 0)
);

CREATE TABLE dbo.Detalle_Pedido (
    id_detalle_pedido INT IDENTITY(1,1) NOT NULL,
    id_pedido INT NOT NULL,
    id_producto INT NOT NULL,
    cantidad INT NOT NULL,
    precio_unitario DECIMAL(10,2) NOT NULL,
    CONSTRAINT PK_Detalle_Pedido PRIMARY KEY (id_detalle_pedido),
    CONSTRAINT FK_Detalle_Pedido_Pedido FOREIGN KEY (id_pedido) REFERENCES dbo.Pedido(id_pedido),
    CONSTRAINT FK_Detalle_Pedido_Producto FOREIGN KEY (id_producto) REFERENCES dbo.Producto(id_producto),
    CONSTRAINT CK_Detalle_Pedido_cantidad CHECK (cantidad > 0),
    CONSTRAINT CK_Detalle_Pedido_precio CHECK (precio_unitario >= 0)
);

CREATE TABLE dbo.Historial_Estado_Pedido (
    id_historial INT IDENTITY(1,1) NOT NULL,
    id_pedido INT NOT NULL,
    estado VARCHAR(50) NOT NULL,
    fecha_cambio DATETIME NOT NULL CONSTRAINT DF_Historial_Estado_fecha DEFAULT GETDATE(),
    CONSTRAINT PK_Historial_Estado_Pedido PRIMARY KEY (id_historial),
    CONSTRAINT FK_Historial_Estado_Pedido FOREIGN KEY (id_pedido) REFERENCES dbo.Pedido(id_pedido),
    CONSTRAINT CK_Historial_Estado CHECK (estado IN ('Pendiente', 'Preparando', 'Listo', 'Entregado', 'Cancelado'))
);

CREATE TABLE dbo.Movimiento_Inventario (
    id_movimiento INT IDENTITY(1,1) NOT NULL,
    id_producto INT NOT NULL,
    tipo VARCHAR(50) NOT NULL,
    cantidad INT NOT NULL,
    motivo VARCHAR(255) NOT NULL,
    fecha_movimiento DATETIME NOT NULL CONSTRAINT DF_Movimiento_fecha DEFAULT GETDATE(),
    CONSTRAINT PK_Movimiento_Inventario PRIMARY KEY (id_movimiento),
    CONSTRAINT FK_Movimiento_Inventario_Producto FOREIGN KEY (id_producto) REFERENCES dbo.Producto(id_producto),
    CONSTRAINT CK_Movimiento_tipo CHECK (tipo IN ('Venta automatica', 'Ajuste manual', 'Reintegro por cancelacion'))
);

CREATE TABLE dbo.Audit_Log (
    id_audit_log INT IDENTITY(1,1) NOT NULL,
    correo VARCHAR(150) NOT NULL,
    ip VARCHAR(64) NULL,
    evento VARCHAR(100) NOT NULL,
    fecha_evento DATETIME NOT NULL CONSTRAINT DF_Audit_Log_fecha DEFAULT GETDATE(),
    CONSTRAINT PK_Audit_Log PRIMARY KEY (id_audit_log)
);

CREATE TABLE dbo.Log_Error (
    id_log_error INT IDENTITY(1,1) NOT NULL,
    ruta VARCHAR(255) NULL,
    mensaje VARCHAR(1000) NOT NULL,
    detalle VARCHAR(MAX) NULL,
    fecha_error DATETIME NOT NULL CONSTRAINT DF_Log_Error_fecha DEFAULT GETDATE(),
    CONSTRAINT PK_Log_Error PRIMARY KEY (id_log_error)
);

CREATE TABLE dbo.Token_Restablecimiento (
    id_token INT IDENTITY(1,1) NOT NULL,
    id_usuario INT NOT NULL,
    token_hash VARCHAR(255) NOT NULL,
    fecha_creacion DATETIME NOT NULL CONSTRAINT DF_Token_fecha_creacion DEFAULT GETDATE(),
    fecha_expiracion DATETIME NOT NULL,
    usado BIT NOT NULL CONSTRAINT DF_Token_usado DEFAULT 0,
    CONSTRAINT PK_Token_Restablecimiento PRIMARY KEY (id_token),
    CONSTRAINT FK_Token_Restablecimiento_Usuario FOREIGN KEY (id_usuario) REFERENCES dbo.Usuario(id_usuario)
);
GO

CREATE INDEX IX_Direccion_usuario ON dbo.Direccion(id_usuario);
CREATE INDEX IX_Producto_categoria ON dbo.Producto(id_categoria, activo, stock);
CREATE INDEX IX_Producto_nombre ON dbo.Producto(nombre_producto);
CREATE INDEX IX_Carrito_usuario ON dbo.Carrito(id_usuario);
CREATE INDEX IX_Pedido_usuario_fecha ON dbo.Pedido(id_usuario, fecha_pedido DESC);
CREATE INDEX IX_Pedido_estado_metodo ON dbo.Pedido(estado, id_metodo);
CREATE INDEX IX_Detalle_Pedido_pedido ON dbo.Detalle_Pedido(id_pedido);
CREATE INDEX IX_Movimiento_producto_fecha ON dbo.Movimiento_Inventario(id_producto, fecha_movimiento DESC);
GO

CREATE SEQUENCE dbo.SQ_NumeroPedido
    AS INT
    START WITH 1
    INCREMENT BY 1;
GO

INSERT INTO dbo.Metodo_Entrega (tipo, costo_adicional)
VALUES
('Express', 2.50),
('Recoleccion', 0.00);
GO

CREATE OR ALTER TRIGGER dbo.TR_Usuario_CrearCarritoCliente
ON dbo.Usuario
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Carrito (id_usuario)
    SELECT i.id_usuario
    FROM inserted i
    WHERE i.rol = 'Cliente'
      AND NOT EXISTS (
          SELECT 1
          FROM dbo.Carrito c
          WHERE c.id_usuario = i.id_usuario
      );
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_Producto_FechaActualizacion
ON dbo.Producto
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF UPDATE(fecha_actualizacion)
    BEGIN
        RETURN;
    END;

    UPDATE p
    SET fecha_actualizacion = GETDATE()
    FROM dbo.Producto p
    INNER JOIN inserted i ON i.id_producto = p.id_producto;
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_Pedido_HistorialEstado
ON dbo.Pedido
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Historial_Estado_Pedido (id_pedido, estado)
    SELECT i.id_pedido, i.estado
    FROM inserted i
    LEFT JOIN deleted d ON d.id_pedido = i.id_pedido
    WHERE d.id_pedido IS NULL
       OR ISNULL(d.estado, '') <> ISNULL(i.estado, '');
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Usuario_Registrar
    @nombre VARCHAR(100),
    @correo VARCHAR(150),
    @contrasena_hash VARCHAR(255),
    @telefono VARCHAR(20) = NULL,
    @rol VARCHAR(50) = 'Cliente',
    @id_usuario INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.Usuario WHERE correo = @correo)
    BEGIN
        THROW 51000, 'Ya existe un usuario con ese correo.', 1;
    END;

    INSERT INTO dbo.Usuario (nombre, correo, contrasena_hash, telefono, rol)
    VALUES (@nombre, LOWER(@correo), @contrasena_hash, @telefono, @rol);

    SET @id_usuario = SCOPE_IDENTITY();
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Categoria_Guardar
    @id_categoria INT = NULL OUTPUT,
    @nombre_categoria VARCHAR(100),
    @descripcion VARCHAR(255) = NULL,
    @activo BIT = 1
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM dbo.Categoria
        WHERE nombre_categoria = @nombre_categoria
          AND (@id_categoria IS NULL OR id_categoria <> @id_categoria)
    )
    BEGIN
        THROW 51001, 'Ya existe una categoria con ese nombre.', 1;
    END;

    IF @id_categoria IS NULL OR @id_categoria = 0
    BEGIN
        INSERT INTO dbo.Categoria (nombre_categoria, descripcion, activo)
        VALUES (@nombre_categoria, @descripcion, @activo);

        SET @id_categoria = SCOPE_IDENTITY();
        RETURN;
    END;

    UPDATE dbo.Categoria
    SET nombre_categoria = @nombre_categoria,
        descripcion = @descripcion,
        activo = @activo
    WHERE id_categoria = @id_categoria;

    IF @@ROWCOUNT = 0
    BEGIN
        THROW 51002, 'Categoria no encontrada.', 1;
    END;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Categoria_Desactivar
    @id_categoria INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM dbo.Producto
        WHERE id_categoria = @id_categoria
          AND activo = 1
    )
    BEGIN
        THROW 51003, 'No se puede desactivar una categoria con productos activos.', 1;
    END;

    UPDATE dbo.Categoria
    SET activo = 0
    WHERE id_categoria = @id_categoria;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Producto_Guardar
    @id_producto INT = NULL OUTPUT,
    @id_categoria INT,
    @nombre_producto VARCHAR(150),
    @descripcion VARCHAR(255) = NULL,
    @precio DECIMAL(10,2),
    @stock INT,
    @stock_minimo INT = 5,
    @imagen_url VARCHAR(500) = NULL,
    @activo BIT = 1
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.Categoria
        WHERE id_categoria = @id_categoria
          AND activo = 1
    )
    BEGIN
        THROW 51004, 'La categoria seleccionada no existe o esta inactiva.', 1;
    END;

    IF @id_producto IS NULL OR @id_producto = 0
    BEGIN
        INSERT INTO dbo.Producto (
            id_categoria,
            nombre_producto,
            descripcion,
            precio,
            stock,
            stock_minimo,
            imagen_url,
            activo
        )
        VALUES (
            @id_categoria,
            @nombre_producto,
            @descripcion,
            @precio,
            @stock,
            @stock_minimo,
            @imagen_url,
            @activo
        );

        SET @id_producto = SCOPE_IDENTITY();
        RETURN;
    END;

    UPDATE dbo.Producto
    SET id_categoria = @id_categoria,
        nombre_producto = @nombre_producto,
        descripcion = @descripcion,
        precio = @precio,
        stock = @stock,
        stock_minimo = @stock_minimo,
        imagen_url = @imagen_url,
        activo = @activo
    WHERE id_producto = @id_producto;

    IF @@ROWCOUNT = 0
    BEGIN
        THROW 51005, 'Producto no encontrado.', 1;
    END;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Producto_Desactivar
    @id_producto INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM dbo.Pedido p
        INNER JOIN dbo.Detalle_Pedido dp ON dp.id_pedido = p.id_pedido
        WHERE dp.id_producto = @id_producto
          AND p.estado NOT IN ('Entregado', 'Cancelado')
    )
    BEGIN
        THROW 51006, 'No se puede desactivar un producto usado en pedidos activos.', 1;
    END;

    UPDATE dbo.Producto
    SET activo = 0
    WHERE id_producto = @id_producto;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Producto_AjustarStock
    @id_producto INT,
    @cantidad INT,
    @motivo VARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @cantidad = 0
    BEGIN
        THROW 51007, 'El ajuste de stock no puede ser cero.', 1;
    END;

    BEGIN TRANSACTION;

    UPDATE dbo.Producto
    SET stock = stock + @cantidad
    WHERE id_producto = @id_producto
      AND stock + @cantidad >= 0;

    IF @@ROWCOUNT = 0
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 51008, 'Producto no encontrado o ajuste deja stock negativo.', 1;
    END;

    INSERT INTO dbo.Movimiento_Inventario (id_producto, tipo, cantidad, motivo)
    VALUES (@id_producto, 'Ajuste manual', @cantidad, @motivo);

    COMMIT TRANSACTION;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Carrito_AgregarProducto
    @id_usuario INT,
    @id_producto INT,
    @cantidad INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @id_carrito INT;
    DECLARE @stock INT;

    IF @cantidad <= 0
    BEGIN
        THROW 51009, 'La cantidad debe ser mayor a cero.', 1;
    END;

    SELECT @stock = stock
    FROM dbo.Producto
    WHERE id_producto = @id_producto
      AND activo = 1;

    IF @stock IS NULL
    BEGIN
        THROW 51010, 'Producto no encontrado o inactivo.', 1;
    END;

    BEGIN TRANSACTION;

    SELECT @id_carrito = id_carrito
    FROM dbo.Carrito
    WHERE id_usuario = @id_usuario;

    IF @id_carrito IS NULL
    BEGIN
        INSERT INTO dbo.Carrito (id_usuario) VALUES (@id_usuario);
        SET @id_carrito = SCOPE_IDENTITY();
    END;

    IF EXISTS (
        SELECT 1
        FROM dbo.Detalle_Carrito
        WHERE id_carrito = @id_carrito
          AND id_producto = @id_producto
    )
    BEGIN
        UPDATE dbo.Detalle_Carrito
        SET cantidad = cantidad + @cantidad
        WHERE id_carrito = @id_carrito
          AND id_producto = @id_producto;
    END;
    ELSE
    BEGIN
        INSERT INTO dbo.Detalle_Carrito (id_carrito, id_producto, cantidad)
        VALUES (@id_carrito, @id_producto, @cantidad);
    END;

    IF EXISTS (
        SELECT 1
        FROM dbo.Detalle_Carrito
        WHERE id_carrito = @id_carrito
          AND id_producto = @id_producto
          AND cantidad > @stock
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 51011, 'No hay stock suficiente para esa cantidad.', 1;
    END;

    COMMIT TRANSACTION;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Pedido_CrearDesdeCarrito
    @id_usuario INT,
    @id_direccion INT,
    @id_metodo INT,
    @numero_pedido VARCHAR(30) OUTPUT,
    @id_pedido INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @id_carrito INT;
    DECLARE @id_producto INT;
    DECLARE @cantidad INT;
    DECLARE @precio DECIMAL(10,2);
    DECLARE @total DECIMAL(10,2) = 0;

    SELECT @id_carrito = id_carrito
    FROM dbo.Carrito
    WHERE id_usuario = @id_usuario;

    IF @id_carrito IS NULL OR NOT EXISTS (SELECT 1 FROM dbo.Detalle_Carrito WHERE id_carrito = @id_carrito)
    BEGIN
        THROW 51012, 'El carrito esta vacio.', 1;
    END;

    BEGIN TRANSACTION;

    DECLARE carrito_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT dc.id_producto, dc.cantidad, p.precio
        FROM dbo.Detalle_Carrito dc
        INNER JOIN dbo.Producto p ON p.id_producto = dc.id_producto
        WHERE dc.id_carrito = @id_carrito
          AND p.activo = 1;

    OPEN carrito_cursor;
    FETCH NEXT FROM carrito_cursor INTO @id_producto, @cantidad, @precio;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF NOT EXISTS (
            SELECT 1
            FROM dbo.Producto WITH (UPDLOCK, ROWLOCK)
            WHERE id_producto = @id_producto
              AND stock >= @cantidad
        )
        BEGIN
            CLOSE carrito_cursor;
            DEALLOCATE carrito_cursor;
            ROLLBACK TRANSACTION;
            THROW 51013, 'Stock insuficiente para uno o mas productos.', 1;
        END;

        SET @total = @total + (@precio * @cantidad);
        FETCH NEXT FROM carrito_cursor INTO @id_producto, @cantidad, @precio;
    END;

    CLOSE carrito_cursor;
    DEALLOCATE carrito_cursor;

    SET @numero_pedido = CONCAT('CM-', FORMAT(GETDATE(), 'yyyyMMdd'), '-', RIGHT(CONCAT('0000', NEXT VALUE FOR dbo.SQ_NumeroPedido), 4));

    INSERT INTO dbo.Pedido (id_usuario, id_direccion, id_metodo, numero_pedido, estado, total)
    VALUES (@id_usuario, @id_direccion, @id_metodo, @numero_pedido, 'Pendiente', @total);

    SET @id_pedido = SCOPE_IDENTITY();

    DECLARE detalle_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT dc.id_producto, dc.cantidad, p.precio
        FROM dbo.Detalle_Carrito dc
        INNER JOIN dbo.Producto p ON p.id_producto = dc.id_producto
        WHERE dc.id_carrito = @id_carrito;

    OPEN detalle_cursor;
    FETCH NEXT FROM detalle_cursor INTO @id_producto, @cantidad, @precio;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        INSERT INTO dbo.Detalle_Pedido (id_pedido, id_producto, cantidad, precio_unitario)
        VALUES (@id_pedido, @id_producto, @cantidad, @precio);

        UPDATE dbo.Producto
        SET stock = stock - @cantidad
        WHERE id_producto = @id_producto;

        INSERT INTO dbo.Movimiento_Inventario (id_producto, tipo, cantidad, motivo)
        VALUES (@id_producto, 'Venta automatica', -@cantidad, @numero_pedido);

        FETCH NEXT FROM detalle_cursor INTO @id_producto, @cantidad, @precio;
    END;

    CLOSE detalle_cursor;
    DEALLOCATE detalle_cursor;

    DELETE FROM dbo.Detalle_Carrito
    WHERE id_carrito = @id_carrito;

    COMMIT TRANSACTION;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Pedido_AvanzarEstado
    @id_pedido INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Pedido
    SET estado = CASE estado
        WHEN 'Pendiente' THEN 'Preparando'
        WHEN 'Preparando' THEN 'Listo'
        WHEN 'Listo' THEN 'Entregado'
        ELSE estado
    END
    WHERE id_pedido = @id_pedido
      AND estado IN ('Pendiente', 'Preparando', 'Listo');

    IF @@ROWCOUNT = 0
    BEGIN
        THROW 51014, 'El pedido no existe o ya no puede avanzar.', 1;
    END;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Pedido_Cancelar
    @id_pedido INT,
    @id_usuario INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @numero_pedido VARCHAR(30);
    DECLARE @id_producto INT;
    DECLARE @cantidad INT;

    SELECT @numero_pedido = numero_pedido
    FROM dbo.Pedido
    WHERE id_pedido = @id_pedido
      AND estado = 'Pendiente'
      AND (@id_usuario IS NULL OR id_usuario = @id_usuario);

    IF @numero_pedido IS NULL
    BEGIN
        THROW 51015, 'Solo se pueden cancelar pedidos pendientes.', 1;
    END;

    BEGIN TRANSACTION;

    UPDATE dbo.Pedido
    SET estado = 'Cancelado',
        fecha_cancelacion = GETDATE()
    WHERE id_pedido = @id_pedido;

    DECLARE reintegro_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT id_producto, cantidad
        FROM dbo.Detalle_Pedido
        WHERE id_pedido = @id_pedido;

    OPEN reintegro_cursor;
    FETCH NEXT FROM reintegro_cursor INTO @id_producto, @cantidad;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        UPDATE dbo.Producto
        SET stock = stock + @cantidad
        WHERE id_producto = @id_producto;

        INSERT INTO dbo.Movimiento_Inventario (id_producto, tipo, cantidad, motivo)
        VALUES (@id_producto, 'Reintegro por cancelacion', @cantidad, @numero_pedido);

        FETCH NEXT FROM reintegro_cursor INTO @id_producto, @cantidad;
    END;

    CLOSE reintegro_cursor;
    DEALLOCATE reintegro_cursor;

    COMMIT TRANSACTION;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Reporte_StockBajo
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @resultado TABLE (
        id_producto INT,
        producto VARCHAR(150),
        categoria VARCHAR(100),
        stock INT,
        stock_minimo INT
    );

    DECLARE @id_producto INT;
    DECLARE @producto VARCHAR(150);
    DECLARE @categoria VARCHAR(100);
    DECLARE @stock INT;
    DECLARE @stock_minimo INT;

    DECLARE stock_bajo_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT p.id_producto, p.nombre_producto, c.nombre_categoria, p.stock, p.stock_minimo
        FROM dbo.Producto p
        INNER JOIN dbo.Categoria c ON c.id_categoria = p.id_categoria
        WHERE p.activo = 1
        ORDER BY c.nombre_categoria, p.nombre_producto;

    OPEN stock_bajo_cursor;
    FETCH NEXT FROM stock_bajo_cursor INTO @id_producto, @producto, @categoria, @stock, @stock_minimo;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF @stock <= @stock_minimo
        BEGIN
            INSERT INTO @resultado (id_producto, producto, categoria, stock, stock_minimo)
            VALUES (@id_producto, @producto, @categoria, @stock, @stock_minimo);
        END;

        FETCH NEXT FROM stock_bajo_cursor INTO @id_producto, @producto, @categoria, @stock, @stock_minimo;
    END;

    CLOSE stock_bajo_cursor;
    DEALLOCATE stock_bajo_cursor;

    SELECT id_producto, producto, categoria, stock, stock_minimo
    FROM @resultado
    ORDER BY categoria, producto;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Producto_ObtenerTodos
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        id_producto AS Id_Producto,
        nombre_producto AS Nombre_Producto,
        descripcion,
        precio,
        stock,
        imagen_url AS Imagen_Url,
        id_categoria AS Id_Categoria
    FROM dbo.Producto
    WHERE activo = 1
    ORDER BY nombre_producto;
END;
GO


CREATE OR ALTER PROCEDURE dbo.sp_Producto_ObtenerPorId
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        id_producto AS Id_Producto,
        nombre_producto AS Nombre_Producto,
        descripcion,
        precio,
        stock,
        imagen_url AS Imagen_Url,
        id_categoria AS Id_Categoria
    FROM dbo.Producto
    WHERE id_producto = @Id
      AND activo = 1;
END;
GO


CREATE OR ALTER PROCEDURE dbo.sp_Producto_ObtenerStock
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT stock
    FROM dbo.Producto
    WHERE id_producto = @Id
      AND activo = 1;
END;
GO


CREATE OR ALTER PROCEDURE dbo.sp_Usuario_ValidarCredenciales
    @correo VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        id_usuario AS IdUsuario,
        nombre AS Nombre,
        correo AS Correo,
        rol AS Rol,
        contrasena_hash AS ContrasenaHash,
        bloqueado_hasta AS BloqueadoHasta
    FROM dbo.Usuario
    WHERE correo = @correo
      AND activo = 1;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Categoria_Listar AS
BEGIN
    SET NOCOUNT ON;
    SELECT id_categoria AS Id, nombre_categoria AS Nombre,
           ISNULL(descripcion, '') AS Descripcion, activo AS Activa
    FROM dbo.Categoria ORDER BY id_categoria;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Producto_ListarStore AS
BEGIN
    SET NOCOUNT ON;
    SELECT id_producto AS Id, id_categoria AS CategoriaId, nombre_producto AS Nombre,
           ISNULL(descripcion,'') AS Descripcion, precio AS Precio, stock AS Stock,
           stock_minimo AS StockMinimo, ISNULL(imagen_url,'') AS ImagenUrl,
           activo AS Activo, fecha_actualizacion AS ActualizadoUtc
    FROM dbo.Producto ORDER BY id_producto;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Usuario_Obtener
    @id_usuario INT = NULL, @correo VARCHAR(150) = NULL, @solo_clientes BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT u.id_usuario AS Id, u.nombre AS Nombre, u.correo AS Correo,
           ISNULL(u.telefono, '') AS Telefono,
           ISNULL((SELECT TOP 1 CONCAT(d.provincia, ', ', d.canton, ', ', d.distrito, '. ', d.senas_exactas)
                   FROM dbo.Direccion d WHERE d.id_usuario = u.id_usuario
                   ORDER BY d.predeterminada DESC, d.id_direccion), '') AS Direccion,
           u.rol AS Rol, u.contrasena_hash AS PasswordHash,
           u.intentos_fallidos AS IntentosFallidos, u.bloqueado_hasta AS BloqueadoHastaUtc
    FROM dbo.Usuario u
    WHERE u.activo = 1
      AND (@id_usuario IS NULL OR u.id_usuario = @id_usuario)
      AND (@correo IS NULL OR u.correo = LOWER(@correo))
      AND (@solo_clientes = 0 OR u.rol = 'Cliente')
    ORDER BY u.nombre;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Usuario_ActualizarPerfil
    @id_usuario INT, @nombre VARCHAR(100), @telefono VARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Usuario SET nombre = @nombre, telefono = @telefono
    WHERE id_usuario = @id_usuario AND activo = 1;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Usuario_ActualizarPassword
    @id_usuario INT, @contrasena_hash VARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Usuario SET contrasena_hash = @contrasena_hash WHERE id_usuario = @id_usuario;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Direccion_Listar @id_usuario INT AS
BEGIN
    SET NOCOUNT ON;
    SELECT id_direccion AS Id, id_usuario AS UsuarioId, '' AS Alias,
           provincia AS Provincia, canton AS Canton, distrito AS Distrito,
           senas_exactas AS SenasExactas,
           CONCAT(provincia, ', ', canton, ', ', distrito, '. ', senas_exactas) AS Detalle,
           predeterminada AS Predeterminada
    FROM dbo.Direccion WHERE id_usuario = @id_usuario
    ORDER BY predeterminada DESC, id_direccion;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Direccion_Guardar
    @id_direccion INT = NULL OUTPUT, @id_usuario INT, @provincia VARCHAR(100),
    @canton VARCHAR(100), @distrito VARCHAR(100), @senas_exactas VARCHAR(255), @predeterminada BIT
AS
BEGIN
    SET NOCOUNT ON;
    IF @predeterminada = 1 UPDATE dbo.Direccion SET predeterminada = 0 WHERE id_usuario = @id_usuario;
    IF @id_direccion IS NULL OR @id_direccion = 0
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.Direccion WHERE id_usuario = @id_usuario) SET @predeterminada = 1;
        INSERT dbo.Direccion(id_usuario, provincia, canton, distrito, senas_exactas, predeterminada)
        VALUES(@id_usuario, @provincia, @canton, @distrito, @senas_exactas, @predeterminada);
        SET @id_direccion = SCOPE_IDENTITY();
    END
    ELSE
        UPDATE dbo.Direccion SET provincia=@provincia, canton=@canton, distrito=@distrito,
            senas_exactas=@senas_exactas, predeterminada=@predeterminada
        WHERE id_direccion=@id_direccion AND id_usuario=@id_usuario;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Direccion_Eliminar @id_usuario INT, @id_direccion INT AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @era_predeterminada BIT = (SELECT predeterminada FROM dbo.Direccion WHERE id_direccion=@id_direccion AND id_usuario=@id_usuario);
    DELETE dbo.Direccion WHERE id_direccion=@id_direccion AND id_usuario=@id_usuario;
    IF @era_predeterminada = 1
        UPDATE dbo.Direccion SET predeterminada=1 WHERE id_direccion=(SELECT TOP 1 id_direccion FROM dbo.Direccion WHERE id_usuario=@id_usuario ORDER BY id_direccion);
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Carrito_Listar @id_usuario INT AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.id_producto AS Id, p.id_categoria AS CategoriaId, p.nombre_producto AS Nombre,
           ISNULL(p.descripcion,'') AS Descripcion, p.precio AS Precio, p.stock AS Stock,
           p.stock_minimo AS StockMinimo, ISNULL(p.imagen_url,'') AS ImagenUrl,
           p.activo AS Activo, p.fecha_actualizacion AS ActualizadoUtc, dc.cantidad AS Cantidad
    FROM dbo.Carrito c INNER JOIN dbo.Detalle_Carrito dc ON dc.id_carrito=c.id_carrito
    INNER JOIN dbo.Producto p ON p.id_producto=dc.id_producto
    WHERE c.id_usuario=@id_usuario ORDER BY p.nombre_producto;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Carrito_Actualizar @id_usuario INT, @id_producto INT, @cantidad INT AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @id_carrito INT=(SELECT id_carrito FROM dbo.Carrito WHERE id_usuario=@id_usuario);
    IF @id_carrito IS NULL BEGIN INSERT dbo.Carrito(id_usuario) VALUES(@id_usuario); SET @id_carrito=SCOPE_IDENTITY(); END;
    IF @cantidad <= 0 DELETE dbo.Detalle_Carrito WHERE id_carrito=@id_carrito AND id_producto=@id_producto;
    ELSE
    BEGIN
        DECLARE @stock INT=(SELECT stock FROM dbo.Producto WHERE id_producto=@id_producto AND activo=1);
        IF @stock IS NULL THROW 51100, 'Producto no encontrado.', 1;
        SET @cantidad=IIF(@cantidad>@stock,@stock,@cantidad);
        IF EXISTS(SELECT 1 FROM dbo.Detalle_Carrito WHERE id_carrito=@id_carrito AND id_producto=@id_producto)
            UPDATE dbo.Detalle_Carrito SET cantidad=@cantidad WHERE id_carrito=@id_carrito AND id_producto=@id_producto;
        ELSE INSERT dbo.Detalle_Carrito(id_carrito,id_producto,cantidad) VALUES(@id_carrito,@id_producto,@cantidad);
    END;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Carrito_Eliminar @id_usuario INT, @id_producto INT = NULL AS
BEGIN
    SET NOCOUNT ON;
    DELETE dc FROM dbo.Detalle_Carrito dc INNER JOIN dbo.Carrito c ON c.id_carrito=dc.id_carrito
    WHERE c.id_usuario=@id_usuario AND (@id_producto IS NULL OR dc.id_producto=@id_producto);
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Pedido_Listar
    @id_pedido INT=NULL, @id_usuario INT=NULL, @estado VARCHAR(50)=NULL,
    @tipo VARCHAR(100)=NULL, @incluir_cerrados BIT=1, @buscar VARCHAR(150)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT pe.id_pedido AS Id, pe.numero_pedido AS Numero, pe.id_usuario AS UsuarioId,
           pe.estado AS Estado, me.tipo AS TipoEntrega,
           CONCAT(d.provincia, ', ', d.canton, ', ', d.distrito, '. ', d.senas_exactas) AS DireccionEntrega,
           pe.fecha_pedido AS FechaUtc, pe.fecha_cancelacion AS CanceladoUtc, pe.total AS Total
    FROM dbo.Pedido pe INNER JOIN dbo.Metodo_Entrega me ON me.id_metodo=pe.id_metodo
    INNER JOIN dbo.Direccion d ON d.id_direccion=pe.id_direccion
    INNER JOIN dbo.Usuario u ON u.id_usuario=pe.id_usuario
    WHERE (@id_pedido IS NULL OR pe.id_pedido=@id_pedido) AND (@id_usuario IS NULL OR pe.id_usuario=@id_usuario)
      AND (@estado IS NULL OR pe.estado=@estado) AND (@tipo IS NULL OR me.tipo=@tipo)
      AND (@incluir_cerrados=1 OR pe.estado NOT IN ('Entregado','Cancelado'))
      AND (@buscar IS NULL OR pe.numero_pedido LIKE '%'+@buscar+'%' OR u.nombre LIKE '%'+@buscar+'%')
    ORDER BY pe.fecha_pedido DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Pedido_Detalles @id_pedido INT AS
BEGIN SET NOCOUNT ON;
    SELECT dp.id_producto AS ProductoId,p.nombre_producto AS ProductoNombre,dp.cantidad AS Cantidad,dp.precio_unitario AS PrecioUnitario
    FROM dbo.Detalle_Pedido dp INNER JOIN dbo.Producto p ON p.id_producto=dp.id_producto
    WHERE dp.id_pedido=@id_pedido ORDER BY p.nombre_producto;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Pedido_CrearWeb
    @id_usuario INT, @tipo VARCHAR(100), @direccion VARCHAR(500),
    @numero_pedido VARCHAR(30) OUTPUT, @id_pedido INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @tipo_normalizado VARCHAR(100)=IIF(@tipo='Recoleccion','Recoleccion','Express');
    DECLARE @id_metodo INT=(SELECT id_metodo FROM dbo.Metodo_Entrega WHERE tipo=@tipo_normalizado);
    DECLARE @id_direccion INT;
    IF @id_metodo IS NULL
    BEGIN
        INSERT dbo.Metodo_Entrega(tipo,costo_adicional) VALUES(@tipo_normalizado,IIF(@tipo_normalizado='Recoleccion',0,2.50));
        SET @id_metodo=SCOPE_IDENTITY();
    END;
    IF @tipo_normalizado='Recoleccion'
        SELECT TOP 1 @id_direccion=id_direccion FROM dbo.Direccion WHERE id_usuario=@id_usuario ORDER BY predeterminada DESC,id_direccion;
    ELSE
        SELECT TOP 1 @id_direccion=id_direccion FROM dbo.Direccion WHERE id_usuario=@id_usuario
          AND CONCAT(provincia, ', ', canton, ', ', distrito, '. ', senas_exactas)=@direccion
        ORDER BY predeterminada DESC,id_direccion;
    IF @id_direccion IS NULL SELECT TOP 1 @id_direccion=id_direccion FROM dbo.Direccion WHERE id_usuario=@id_usuario ORDER BY predeterminada DESC,id_direccion;
    IF @id_direccion IS NULL
    BEGIN
        INSERT dbo.Direccion(id_usuario,provincia,canton,distrito,senas_exactas,predeterminada)
        VALUES(@id_usuario,'Sin provincia','Sin canton','Sin distrito',ISNULL(NULLIF(@direccion,''),'Dirección no especificada'),1);
        SET @id_direccion=SCOPE_IDENTITY();
    END;
    EXEC dbo.sp_Pedido_CrearDesdeCarrito @id_usuario,@id_direccion,@id_metodo,@numero_pedido OUTPUT,@id_pedido OUTPUT;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Pedido_Historial @id_pedido INT AS
BEGIN SET NOCOUNT ON;
    SELECT estado AS Estado,fecha_cambio AS FechaUtc FROM dbo.Historial_Estado_Pedido
    WHERE id_pedido=@id_pedido ORDER BY fecha_cambio;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Reporte_ProductosVendidos @desde DATE=NULL,@hasta DATE=NULL,@id_categoria INT=NULL AS
BEGIN SET NOCOUNT ON;
    SELECT p.nombre_producto AS Producto,SUM(dp.cantidad) AS Cantidad,SUM(dp.cantidad*dp.precio_unitario) AS Total
    FROM dbo.Detalle_Pedido dp INNER JOIN dbo.Pedido pe ON pe.id_pedido=dp.id_pedido
    INNER JOIN dbo.Producto p ON p.id_producto=dp.id_producto
    WHERE pe.estado<>'Cancelado' AND (@desde IS NULL OR CAST(pe.fecha_pedido AS DATE)>=@desde)
      AND (@hasta IS NULL OR CAST(pe.fecha_pedido AS DATE)<=@hasta) AND (@id_categoria IS NULL OR p.id_categoria=@id_categoria)
    GROUP BY p.nombre_producto ORDER BY SUM(dp.cantidad) DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Reporte_Movimientos @desde DATE=NULL,@hasta DATE=NULL,@id_producto INT=NULL AS
BEGIN SET NOCOUNT ON;
    SELECT m.fecha_movimiento AS FechaUtc,m.id_producto AS ProductoId,p.nombre_producto AS ProductoNombre,
           m.tipo AS Tipo,m.cantidad AS Cantidad,m.motivo AS Motivo
    FROM dbo.Movimiento_Inventario m INNER JOIN dbo.Producto p ON p.id_producto=m.id_producto
    WHERE (@desde IS NULL OR CAST(m.fecha_movimiento AS DATE)>=@desde)
      AND (@hasta IS NULL OR CAST(m.fecha_movimiento AS DATE)<=@hasta) AND (@id_producto IS NULL OR m.id_producto=@id_producto)
    ORDER BY m.fecha_movimiento DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Auditoria_Listar AS
BEGIN SET NOCOUNT ON; SELECT fecha_evento AS FechaUtc,correo AS Correo,evento AS Evento,ISNULL(ip,'') AS Ip FROM dbo.Audit_Log ORDER BY fecha_evento DESC; END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Error_Listar AS
BEGIN SET NOCOUNT ON; SELECT fecha_error AS FechaUtc,ISNULL(ruta,'') AS Ruta,mensaje AS Mensaje FROM dbo.Log_Error ORDER BY fecha_error DESC; END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Error_Agregar @ruta VARCHAR(255),@mensaje VARCHAR(1000) AS
BEGIN SET NOCOUNT ON; INSERT dbo.Log_Error(ruta,mensaje) VALUES(@ruta,@mensaje); END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Auditoria_Agregar @correo VARCHAR(150),@ip VARCHAR(64),@evento VARCHAR(100) AS
BEGIN SET NOCOUNT ON; INSERT dbo.Audit_Log(correo,ip,evento) VALUES(@correo,@ip,@evento); END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Token_Crear @id_usuario INT,@token_hash VARCHAR(255),@fecha_expiracion DATETIME AS
BEGIN SET NOCOUNT ON; INSERT dbo.Token_Restablecimiento(id_usuario,token_hash,fecha_expiracion) VALUES(@id_usuario,@token_hash,@fecha_expiracion); END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Token_Obtener @token_hash VARCHAR(255) AS
BEGIN SET NOCOUNT ON;
 SELECT TOP 1 id_usuario AS UsuarioId,token_hash AS Token,fecha_expiracion AS ExpiraUtc,usado AS Usado
 FROM dbo.Token_Restablecimiento WHERE token_hash=@token_hash ORDER BY id_token DESC;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Token_MarcarUsado @token_hash VARCHAR(255) AS
BEGIN SET NOCOUNT ON; UPDATE dbo.Token_Restablecimiento SET usado=1 WHERE token_hash=@token_hash; END;
GO

PRINT 'Script inicial CampoMarket ejecutado correctamente.';
