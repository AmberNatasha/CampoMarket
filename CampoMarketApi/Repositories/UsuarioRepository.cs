using CampoMarketApi.Services;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;

namespace CampoMarketApi.Repositories;

public sealed class UsuarioRepository(IConfiguration configuration)
{
    private readonly string _connectionString =
        configuration.GetConnectionString("CampoMarket")
        ?? throw new InvalidOperationException(
            "Falta ConnectionStrings:CampoMarket.");

    public AuthenticatedUser? ValidateCredentials(
        string email,
        string password)
    {
        using var connection = new SqlConnection(_connectionString);

        const string sql = """
            SELECT
                id_usuario AS IdUsuario,
                nombre AS Nombre,
                correo AS Correo,
                rol AS Rol,
                contrasena_hash AS ContrasenaHash,
                bloqueado_hasta AS BloqueadoHasta
            FROM dbo.Usuario
            WHERE LOWER(LTRIM(RTRIM(correo))) = @correo
              AND activo = 1;
            """;

        var usuario = connection.QueryFirstOrDefault<UsuarioCredenciales>(
            sql,
            new
            {
                correo = email.Trim().ToLowerInvariant()
            });

        if (usuario is null)
            return null;

        // Verificar si la cuenta está temporalmente bloqueada.
        if (usuario.BloqueadoHasta.HasValue &&
            usuario.BloqueadoHasta.Value > DateTime.Now)
        {
            return null;
        }

        // Verificar contraseña.
        if (!VerifyPassword(password, usuario.ContrasenaHash))
        {
            return null;
        }

        return new AuthenticatedUser(
            usuario.IdUsuario,
            usuario.Nombre,
            usuario.Correo,
            usuario.Rol,
            usuario.ContrasenaHash);
    }

    private static bool VerifyPassword(
        string password,
        string storedHash)
    {
        if (string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

        var parts = storedHash.Split('$');

        if (parts.Length != 4)
            return false;

        if (!parts[0].Equals(
                "PBKDF2",
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!int.TryParse(parts[1], out var iterations))
            return false;

        if (iterations <= 0)
            return false;

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);

            var actual = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expected.Length);

            return CryptographicOperations.FixedTimeEquals(
                actual,
                expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private sealed class UsuarioCredenciales
    {
        public int IdUsuario { get; init; }

        public string Nombre { get; init; } = string.Empty;

        public string Correo { get; init; } = string.Empty;

        public string Rol { get; init; } = string.Empty;

        public string ContrasenaHash { get; init; } = string.Empty;

        public DateTime? BloqueadoHasta { get; init; }
    }
}