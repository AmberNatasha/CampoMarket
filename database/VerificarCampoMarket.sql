USE CampoMarket;
GO

SELECT 'Usuario' AS tabla, COUNT(*) AS registros FROM dbo.Usuario
UNION ALL SELECT 'Direccion', COUNT(*) FROM dbo.Direccion
UNION ALL SELECT 'Metodo_Entrega', COUNT(*) FROM dbo.Metodo_Entrega
UNION ALL SELECT 'Categoria', COUNT(*) FROM dbo.Categoria
UNION ALL SELECT 'Producto', COUNT(*) FROM dbo.Producto
UNION ALL SELECT 'Carrito', COUNT(*) FROM dbo.Carrito
UNION ALL SELECT 'Pedido', COUNT(*) FROM dbo.Pedido
UNION ALL SELECT 'Detalle_Pedido', COUNT(*) FROM dbo.Detalle_Pedido;
GO

SELECT
    p.nombre_producto,
    c.nombre_categoria,
    p.precio,
    p.stock,
    p.stock_minimo,
    CASE WHEN p.stock <= p.stock_minimo THEN 'Stock bajo' ELSE 'OK' END AS alerta
FROM dbo.Producto p
INNER JOIN dbo.Categoria c ON c.id_categoria = p.id_categoria
ORDER BY p.nombre_producto;
GO
