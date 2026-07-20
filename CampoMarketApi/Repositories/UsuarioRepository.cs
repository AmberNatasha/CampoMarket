using System.Security.Cryptography;
using CampoMarketApi.Services;
using Microsoft.Data.SqlClient;

namespace CampoMarketApi.Repositories;

public sealed class UsuarioRepository(IConfiguration configuration)
{
    private readonly string _connectionString = configuration.GetConnectionString("CampoMarket")
        ?? throw new InvalidOperationException("Falta ConnectionStrings:CampoMarket.");

    public AuthenticatedUser? ValidateCredentials(string email, string password)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var command = new SqlCommand("""
            SELECT id_usuario, nombre, correo, rol, contrasena_hash, bloqueado_hasta
            FROM dbo.Usuario
            WHERE correo = @correo AND activo = 1;
            """, connection);
        command.Parameters.AddWithValue("@correo", email.Trim().ToLowerInvariant());
        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;

        var blockedUntil = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5);
        var passwordHash = reader.GetString(4);
        if (blockedUntil > DateTime.Now || !VerifyPassword(password, passwordHash)) return null;

        return new AuthenticatedUser(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            passwordHash);
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split('$');
        if (parts.Length != 4 || parts[0] != "PBKDF2" || !int.TryParse(parts[1], out var iterations))
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(
                password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
