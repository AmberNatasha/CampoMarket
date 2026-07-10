/*
  Script opcional para crear un administrador inicial.
  Ejecutalo solo cuando necesites habilitar acceso al panel admin.

  Credenciales temporales:
  correo: admin@campomarket.test
  clave:  Admin123!

  Cambia la clave desde la app despues del primer inicio de sesion.
*/

USE CampoMarket;
GO

IF NOT EXISTS (
    SELECT 1
    FROM dbo.Usuario
    WHERE correo = 'admin@campomarket.test'
)
BEGIN
    INSERT INTO dbo.Usuario (
        nombre,
        correo,
        contrasena_hash,
        telefono,
        rol,
        activo
    )
    VALUES (
        'Admin Campo',
        'admin@campomarket.test',
        'PBKDF2$120000$66nd6xpOYOp0dsIs6NweZw==$gpjXxLGWo/c9TeOiacGG97SeSIp+e1sus+2xemxIi6k=',
        '8999-9999',
        'Admin',
        1
    );
END;
GO
