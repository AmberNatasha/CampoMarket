using CampoMarketApi.Services;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;

namespace CampoMarketApi.Repositories;

public sealed class UsuarioRepository(IConfiguration configuration)
{
    private readonly string _connectionString = configuration.GetConnectionString("CampoMarket")
        ?? throw new InvalidOperationException("Falta ConnectionStrings:CampoMarket.");

    public AuthenticatedUser? ValidateCredentials(string email, string password)
    {
        using var connection = new SqlConnection(_connectionString);

        var parameters = new DynamicParameters();
        parameters.Add("@correo", email.Trim().ToLowerInvariant());

        var usuario = connection.QueryFirstOrDefault<UsuarioCredenciales>(
            "sp_Usuario_ValidarCredenciales",
            parameters,
            commandType: System.Data.CommandType.StoredProcedure);

        if (usuario is null)
            return null;

        if (usuario.BloqueadoHasta > DateTime.Now ||
            !VerifyPassword(password, usuario.ContrasenaHash))
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
