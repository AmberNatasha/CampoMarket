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

INSERT INTO dbo.Usuario (nombre, correo, contrasena_hash, telefono, rol)
VALUES
('Cliente Demo', 'cliente@campomarket.test', '$2a$12$wH0Iq2bN4YdYz1QF5cX5/OxB8eZ7I6w7O1rMuKM3F8SgC7m3fT6qK', '8888-8888', 'Cliente'),
('Admin Campo', 'admin@campomarket.test', '$2a$12$wH0Iq2bN4YdYz1QF5cX5/OxB8eZ7I6w7O1rMuKM3F8SgC7m3fT6qK', '8999-9999', 'Admin');

INSERT INTO dbo.Direccion (id_usuario, provincia, canton, distrito, senas_exactas, predeterminada)
VALUES
(1, 'San Jose', 'Central', 'Carmen', 'Barrio Fresco #123', 1);

INSERT INTO dbo.Metodo_Entrega (tipo, costo_adicional)
VALUES
('Express', 2.50),
('Recoleccion', 0.00);

INSERT INTO dbo.Categoria (nombre_categoria, descripcion)
VALUES
('Frutas', 'Frutas frescas y de temporada'),
('Verduras', 'Verduras y hortalizas seleccionadas'),
('Lacteos', 'Productos lacteos frescos'),
('Carnes', 'Cortes frescos para cocina diaria'),
('Despensa', 'Basicos naturales para el hogar');

INSERT INTO dbo.Producto (nombre_producto, descripcion, precio, stock, imagen_url, id_categoria, stock_minimo)
VALUES
('Manzana roja organica', 'Fruta crujiente seleccionada para loncheras y postres.', 1.25, 45, '/Images/Banner.jpg', 1, 10),
('Banano de finca', 'Dulce, fresco y listo para batidos o desayuno.', 0.55, 70, '/Images/Banner.jpg', 1, 15),
('Lechuga romana', 'Hojas verdes lavadas y empacadas para ensaladas.', 1.80, 20, '/Images/Banner.jpg', 2, 8),
('Tomate artesanal', 'Tomate de temporada con buen cuerpo y sabor.', 1.10, 12, '/Images/Banner.jpg', 2, 10),
('Queso fresco', 'Queso suave de produccion local.', 4.75, 16, '/Images/Banner.jpg', 3, 5),
('Pechuga campesina', 'Corte fresco para preparaciones familiares.', 6.90, 9, '/Images/Banner.jpg', 4, 6),
('Miel natural', 'Miel clara para bebidas, panes y marinados.', 5.40, 22, '/Images/Banner.jpg', 5, 5);

INSERT INTO dbo.Carrito (id_usuario) VALUES (1);
GO

PRINT 'Script inicial CampoMarket ejecutado correctamente.';
