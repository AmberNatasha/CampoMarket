using CampoMarket.Web.Models;
using Microsoft.Data.SqlClient;

namespace CampoMarket.Web.Services;

public interface IUserRepository
{
    IReadOnlyList<Usuario> GetClients();
    Usuario? FindById(int id);
    Usuario? FindByEmail(string correo);
    int CreateUser(Usuario user);
    void UpdateLoginState(int userId, int intentosFallidos, DateTime? bloqueadoHastaUtc);
    void UpdateProfile(int userId, string nombre, string telefono);
    void UpdatePassword(int userId, string passwordHash);
    IReadOnlyList<DireccionCliente> GetAddresses(int userId);
    DireccionCliente? FindAddress(int userId, int id);
    int CreateAddress(DireccionCliente address);
    void UpdateAddress(DireccionCliente address);
    void DeleteAddress(int id);
    void ClearDefaultAddresses(int userId);
    IReadOnlyList<AuditLogItem> GetAuditLogs();
    void AddAuditLog(AuditLogItem item);
    IReadOnlyList<LogErrorItem> GetErrorLogs();
    void AddErrorLog(LogErrorItem item);
    void AddPasswordResetToken(PasswordResetToken token);
    PasswordResetToken? FindPasswordResetToken(string token);
    void MarkPasswordResetTokenUsed(string token);
}

public sealed class SqlUserRepository(IConfiguration configuration) : IUserRepository
{
    private readonly string _connectionString = configuration.GetConnectionString("CampoMarket")
        ?? throw new InvalidOperationException("Falta ConnectionStrings:CampoMarket.");

    public IReadOnlyList<Usuario> GetClients()
    {
        using var connection = OpenConnection();
        using var command = new SqlCommand(UserSelectSql + " WHERE u.rol = @rol AND u.activo = 1 ORDER BY u.nombre;", connection);
        command.Parameters.AddWithValue("@rol", RolesCampo.Cliente);
        return ReadUsers(command);
    }

    public Usuario? FindById(int id)
    {
        using var connection = OpenConnection();
        using var command = new SqlCommand(UserSelectSql + " WHERE u.id_usuario = @id AND u.activo = 1;", connection);
        command.Parameters.AddWithValue("@id", id);
        return ReadUsers(command).FirstOrDefault();
    }

    public Usuario? FindByEmail(string correo)
    {
        using var connection = OpenConnection();
        using var command = new SqlCommand(UserSelectSql + " WHERE u.correo = @correo AND u.activo = 1;", connection);
        command.Parameters.AddWithValue("@correo", correo.Trim().ToLowerInvariant());
        return ReadUsers(command).FirstOrDefault();
    }

    public int CreateUser(Usuario user)
    {
        using var connection = OpenConnection();
        using var command = new SqlCommand("""
            INSERT INTO dbo.Usuario (nombre, correo, contrasena_hash, telefono, rol, intentos_fallidos, bloqueado_hasta, activo)
            OUTPUT INSERTED.id_usuario
            VALUES (@nombre, @correo, @passwordHash, @telefono, @rol, @intentos, @bloqueadoHasta, 1);
            """, connection);
        command.Parameters.AddWithValue("@nombre", user.Nombre);
        command.Parameters.AddWithValue("@correo", user.Correo);
        command.Parameters.AddWithValue("@passwordHash", user.PasswordHash);
        command.Parameters.AddWithValue("@telefono", string.IsNullOrWhiteSpace(user.Telefono) ? DBNull.Value : user.Telefono);
        command.Parameters.AddWithValue("@rol", user.Rol);
        command.Parameters.AddWithValue("@intentos", user.IntentosFallidos);
        command.Parameters.AddWithValue("@bloqueadoHasta", user.BloqueadoHastaUtc is null ? DBNull.Value : user.BloqueadoHastaUtc.Value.ToLocalTime());
        return (int)command.ExecuteScalar()!;
    }

    public void UpdateLoginState(int userId, int intentosFallidos, DateTime? bloqueadoHastaUtc)
    {
        using var connection = OpenConnection();
        Execute(connection, """
            UPDATE dbo.Usuario
            SET intentos_fallidos = @intentos,
                bloqueado_hasta = @bloqueadoHasta
            WHERE id_usuario = @id;
            """,
            ("@id", userId),
            ("@intentos", intentosFallidos),
            ("@bloqueadoHasta", bloqueadoHastaUtc is null ? DBNull.Value : bloqueadoHastaUtc.Value.ToLocalTime()));
    }

    public void UpdateProfile(int userId, string nombre, string telefono)
    {
        using var connection = OpenConnection();
        Execute(connection, """
            UPDATE dbo.Usuario
            SET nombre = @nombre,
                telefono = @telefono
            WHERE id_usuario = @id;
            """,
            ("@id", userId),
            ("@nombre", nombre),
            ("@telefono", string.IsNullOrWhiteSpace(telefono) ? DBNull.Value : telefono));
    }

    public void UpdatePassword(int userId, string passwordHash)
    {
        using var connection = OpenConnection();
        Execute(connection, """
            UPDATE dbo.Usuario
            SET contrasena_hash = @passwordHash
            WHERE id_usuario = @id;
            """,
            ("@id", userId),
            ("@passwordHash", passwordHash));
    }

    public IReadOnlyList<DireccionCliente> GetAddresses(int userId)
    {
        using var connection = OpenConnection();
        using var command = new SqlCommand("""
            SELECT id_direccion, id_usuario, provincia, canton, distrito, senas_exactas, predeterminada
            FROM dbo.Direccion
            WHERE id_usuario = @userId
            ORDER BY predeterminada DESC, id_direccion;
            """, connection);
        command.Parameters.AddWithValue("@userId", userId);
        return ReadAddresses(command);
    }

    public DireccionCliente? FindAddress(int userId, int id)
    {
        using var connection = OpenConnection();
        using var command = new SqlCommand("""
            SELECT id_direccion, id_usuario, provincia, canton, distrito, senas_exactas, predeterminada
            FROM dbo.Direccion
            WHERE id_usuario = @userId AND id_direccion = @id;
            """, connection);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@id", id);
        return ReadAddresses(command).FirstOrDefault();
    }

    public int CreateAddress(DireccionCliente address)
    {
        using var connection = OpenConnection();
        using var command = new SqlCommand("""
            INSERT INTO dbo.Direccion (id_usuario, provincia, canton, distrito, senas_exactas, predeterminada)
            OUTPUT INSERTED.id_direccion
            VALUES (@userId, @provincia, @canton, @distrito, @senas, @predeterminada);
            """, connection);
        AddAddressParameters(command, address);
        return (int)command.ExecuteScalar()!;
    }

    public void UpdateAddress(DireccionCliente address)
    {
        using var connection = OpenConnection();
        using var command = new SqlCommand("""
            UPDATE dbo.Direccion
            SET provincia = @provincia,
                canton = @canton,
                distrito = @distrito,
                senas_exactas = @senas,
                predeterminada = @predeterminada
            WHERE id_direccion = @id AND id_usuario = @userId;
            """, connection);
        command.Parameters.AddWithValue("@id", address.Id);
        AddAddressParameters(command, address);
        command.ExecuteNonQuery();
    }

    public void DeleteAddress(int id)
    {
        using var connection = OpenConnection();
        Execute(connection, "DELETE FROM dbo.Direccion WHERE id_direccion = @id;", ("@id", id));
    }

    public void ClearDefaultAddresses(int userId)
    {
        using var connection = OpenConnection();
        Execute(connection, "UPDATE dbo.Direccion SET predeterminada = 0 WHERE id_usuario = @userId;", ("@userId", userId));
    }

    public IReadOnlyList<AuditLogItem> GetAuditLogs()
    {
        using var connection = OpenConnection();
        using var command = new SqlCommand("""
            SELECT correo, evento, ISNULL(ip, ''), fecha_evento
            FROM dbo.Audit_Log
            ORDER BY fecha_evento DESC;
            """, connection);
        using var reader = command.ExecuteReader();
        var items = new List<AuditLogItem>();
        while (reader.Read())
        {
            items.Add(new AuditLogItem
            {
                Correo = reader.GetString(0),
                Evento = reader.GetString(1),
                Ip = reader.GetString(2),
                FechaUtc = DateTime.SpecifyKind(reader.GetDateTime(3), DateTimeKind.Local).ToUniversalTime()
            });
        }

        return items;
    }

    public void AddAuditLog(AuditLogItem item)
    {
        using var connection = OpenConnection();
        Execute(connection, """
            INSERT INTO dbo.Audit_Log (correo, ip, evento, fecha_evento)
            VALUES (@correo, @ip, @evento, @fecha);
            """,
            ("@correo", item.Correo),
            ("@ip", string.IsNullOrWhiteSpace(item.Ip) ? DBNull.Value : item.Ip),
            ("@evento", item.Evento),
            ("@fecha", item.FechaUtc.ToLocalTime()));
    }

    public IReadOnlyList<LogErrorItem> GetErrorLogs()
    {
        using var connection = OpenConnection();
        using var command = new SqlCommand("""
            SELECT ISNULL(ruta, ''), mensaje, fecha_error
            FROM dbo.Log_Error
            ORDER BY fecha_error DESC;
            """, connection);
        using var reader = command.ExecuteReader();
        var items = new List<LogErrorItem>();
        while (reader.Read())
        {
            items.Add(new LogErrorItem
            {
                Ruta = reader.GetString(0),
                Mensaje = reader.GetString(1),
                FechaUtc = DateTime.SpecifyKind(reader.GetDateTime(2), DateTimeKind.Local).ToUniversalTime()
            });
        }

        return items;
    }

    public void AddErrorLog(LogErrorItem item)
    {
        using var connection = OpenConnection();
        Execute(connection, """
            INSERT INTO dbo.Log_Error (ruta, mensaje, fecha_error)
            VALUES (@ruta, @mensaje, @fecha);
            """,
            ("@ruta", string.IsNullOrWhiteSpace(item.Ruta) ? DBNull.Value : item.Ruta),
            ("@mensaje", item.Mensaje),
            ("@fecha", item.FechaUtc.ToLocalTime()));
    }

    public void AddPasswordResetToken(PasswordResetToken token)
    {
        using var connection = OpenConnection();
        Execute(connection, """
            INSERT INTO dbo.Token_Restablecimiento (id_usuario, token_hash, fecha_expiracion, usado)
            VALUES (@userId, @token, @expira, 0);
            """,
            ("@userId", token.UsuarioId),
            ("@token", token.Token),
            ("@expira", token.ExpiraUtc.ToLocalTime()));
    }

    public PasswordResetToken? FindPasswordResetToken(string token)
    {
        using var connection = OpenConnection();
        using var command = new SqlCommand("""
            SELECT TOP 1 id_usuario, token_hash, fecha_expiracion, usado
            FROM dbo.Token_Restablecimiento
            WHERE token_hash = @token
            ORDER BY id_token DESC;
            """, connection);
        command.Parameters.AddWithValue("@token", token);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;
        return new PasswordResetToken
        {
            UsuarioId = reader.GetInt32(0),
            Token = reader.GetString(1),
            ExpiraUtc = DateTime.SpecifyKind(reader.GetDateTime(2), DateTimeKind.Local).ToUniversalTime(),
            Usado = reader.GetBoolean(3)
        };
    }

    public void MarkPasswordResetTokenUsed(string token)
    {
        using var connection = OpenConnection();
        Execute(connection, "UPDATE dbo.Token_Restablecimiento SET usado = 1 WHERE token_hash = @token;", ("@token", token));
    }

    private static readonly string UserSelectSql = """
        SELECT u.id_usuario,
               u.nombre,
               u.correo,
               ISNULL(u.telefono, ''),
               u.rol,
               u.contrasena_hash,
               u.intentos_fallidos,
               u.bloqueado_hasta,
               ISNULL((
                   SELECT TOP 1 CONCAT(d.provincia, ', ', d.canton, ', ', d.distrito, '. ', d.senas_exactas)
                   FROM dbo.Direccion d
                   WHERE d.id_usuario = u.id_usuario
                   ORDER BY d.predeterminada DESC, d.id_direccion
               ), '') AS direccion
        FROM dbo.Usuario u
        """;

    private SqlConnection OpenConnection()
    {
        var connection = new SqlConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private static List<Usuario> ReadUsers(SqlCommand command)
    {
        using var reader = command.ExecuteReader();
        var users = new List<Usuario>();
        while (reader.Read())
        {
            users.Add(new Usuario
            {
                Id = reader.GetInt32(0),
                Nombre = reader.GetString(1),
                Correo = reader.GetString(2),
                Telefono = reader.GetString(3),
                Rol = reader.GetString(4),
                PasswordHash = reader.GetString(5),
                IntentosFallidos = reader.GetInt32(6),
                BloqueadoHastaUtc = reader.IsDBNull(7) ? null : DateTime.SpecifyKind(reader.GetDateTime(7), DateTimeKind.Local).ToUniversalTime(),
                Direccion = reader.GetString(8)
            });
        }

        return users;
    }

    private static List<DireccionCliente> ReadAddresses(SqlCommand command)
    {
        using var reader = command.ExecuteReader();
        var addresses = new List<DireccionCliente>();
        while (reader.Read())
        {
            var address = new DireccionCliente
            {
                Id = reader.GetInt32(0),
                UsuarioId = reader.GetInt32(1),
                Alias = "Direccion",
                Provincia = reader.GetString(2),
                Canton = reader.GetString(3),
                Distrito = reader.GetString(4),
                SenasExactas = reader.GetString(5),
                Predeterminada = reader.GetBoolean(6)
            };
            address.Detalle = $"{address.Provincia}, {address.Canton}, {address.Distrito}. {address.SenasExactas}";
            addresses.Add(address);
        }

        return addresses;
    }

    private static void AddAddressParameters(SqlCommand command, DireccionCliente address)
    {
        command.Parameters.AddWithValue("@userId", address.UsuarioId);
        command.Parameters.AddWithValue("@provincia", address.Provincia);
        command.Parameters.AddWithValue("@canton", address.Canton);
        command.Parameters.AddWithValue("@distrito", address.Distrito);
        command.Parameters.AddWithValue("@senas", address.SenasExactas);
        command.Parameters.AddWithValue("@predeterminada", address.Predeterminada);
    }

    private static void Execute(SqlConnection connection, string sql, params (string Name, object? Value)[] parameters)
    {
        using var command = new SqlCommand(sql, connection);
        foreach (var parameter in parameters)
        {
            command.Parameters.AddWithValue(parameter.Name, parameter.Value ?? DBNull.Value);
        }

        command.ExecuteNonQuery();
    }
}
