using System.Security.Cryptography;
using System.Text;
using CampoMarket.Web.Models;

namespace CampoMarket.Web.Services;

public sealed class SqlAccountService(IUserRepository users) :
    IUserService,
    IPasswordResetService,
    IAddressService,
    IAuditService
{
    public IReadOnlyList<Usuario> Clientes => users.GetClients();
    public IReadOnlyList<AuditLogItem> AuditLogs => users.GetAuditLogs();
    public IReadOnlyList<LogErrorItem> ErrorLogs => users.GetErrorLogs();

    public Usuario? FindUser(int id) => users.FindById(id);

    public (bool Ok, string Message, Usuario? User) Register(string nombre, string correo, string password, string telefono, string direccion)
    {
        if (users.FindByEmail(correo) is not null)
        {
            return (false, "Ese correo ya esta registrado.", null);
        }

        var user = new Usuario
        {
            Nombre = nombre.Trim(),
            Correo = correo.Trim().ToLowerInvariant(),
            Telefono = telefono.Trim(),
            Rol = RolesCampo.Cliente,
            PasswordHash = PasswordService.Hash(password)
        };

        user.Id = users.CreateUser(user);

        if (!string.IsNullOrWhiteSpace(direccion))
        {
            users.CreateAddress(new DireccionCliente
            {
                UsuarioId = user.Id,
                Alias = "Casa",
                Provincia = "Sin provincia",
                Canton = "Sin canton",
                Distrito = "Sin distrito",
                SenasExactas = direccion.Trim(),
                Predeterminada = true
            });
        }

        return (true, "Cuenta creada. Ya puedes iniciar sesion.", user);
    }

    public (bool Ok, string Message, Usuario? User) Login(string correo, string password, string ip = "")
    {
        var user = users.FindByEmail(correo);
        if (user is null)
        {
            users.AddAuditLog(new AuditLogItem { Correo = correo, Ip = ip, Evento = "Login fallido: correo no registrado" });
            return (false, "Correo o contraseña incorrectos.", null);
        }

        if (user.BloqueadoHastaUtc > DateTime.UtcNow)
        {
            users.AddAuditLog(new AuditLogItem { Correo = correo, Ip = ip, Evento = "Login bloqueado temporalmente" });
            return (false, "La cuenta esta bloqueada temporalmente por intentos fallidos.", null);
        }

        if (!PasswordService.Verify(password, user.PasswordHash))
        {
            user.IntentosFallidos++;
            if (user.IntentosFallidos >= 5)
            {
                user.BloqueadoHastaUtc = DateTime.UtcNow.AddMinutes(15);
            }

            users.UpdateLoginState(user.Id, user.IntentosFallidos, user.BloqueadoHastaUtc);
            users.AddAuditLog(new AuditLogItem { Correo = correo, Ip = ip, Evento = $"Login fallido #{user.IntentosFallidos}" });
            return (false, "Correo o contraseña incorrectos.", null);
        }

        users.UpdateLoginState(user.Id, 0, null);
        users.AddAuditLog(new AuditLogItem { Correo = correo, Ip = ip, Evento = "Login exitoso" });
        user.IntentosFallidos = 0;
        user.BloqueadoHastaUtc = null;
        return (true, "Sesion iniciada.", user);
    }

    public (bool Ok, string Message) UpdateProfile(int userId, string nombre, string telefono, string direccion)
    {
        if (!IsValidPhone(telefono))
        {
            return (false, "El telefono debe tener entre 7 y 20 caracteres y usar solo digitos, espacios, guiones o prefijo.");
        }

        if (users.FindById(userId) is null)
        {
            return (false, "Usuario no encontrado.");
        }

        users.UpdateProfile(userId, nombre.Trim(), telefono.Trim());
        return (true, "Perfil actualizado.");
    }

    public (bool Ok, string Message) ChangePassword(int userId, string actual, string nuevo)
    {
        var user = users.FindById(userId);
        if (user is null) return (false, "Usuario no encontrado.");
        if (!PasswordService.Verify(actual, user.PasswordHash))
        {
            return (false, "La contraseña actual no coincide.");
        }

        users.UpdatePassword(userId, PasswordService.Hash(nuevo));
        return (true, "contraseña actualizada.");
    }

    public (bool Ok, string Message, string? Token) RequestPasswordReset(string correo)
    {
        var user = users.FindByEmail(correo);
        if (user is null)
        {
            return (true, "Si el correo existe, se genero un enlace temporal.", null);
        }

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        users.AddPasswordResetToken(new PasswordResetToken
        {
            UsuarioId = user.Id,
            Token = HashResetToken(token),
            ExpiraUtc = DateTime.UtcNow.AddHours(1)
        });

        return (true, "Si el correo existe, recibirás un enlace temporal.", token);
    }

    public (bool Ok, string Message) ResetPassword(string token, string nuevo)
    {
        var tokenHash = HashResetToken(token);
        var reset = users.FindPasswordResetToken(tokenHash);
        if (reset is null || reset.Usado || reset.ExpiraUtc < DateTime.UtcNow)
        {
            return (false, "El token no existe o ya expiro.");
        }

        if (users.FindById(reset.UsuarioId) is null)
        {
            return (false, "Usuario no encontrado.");
        }

        users.UpdatePassword(reset.UsuarioId, PasswordService.Hash(nuevo));
        users.MarkPasswordResetTokenUsed(tokenHash);
        return (true, "contraseña restablecida. Ya puedes iniciar sesion.");
    }

    private static string HashResetToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    public IEnumerable<DireccionCliente> GetAddresses(int userId) => users.GetAddresses(userId);

    public DireccionCliente? FindAddress(int userId, int id) => users.FindAddress(userId, id);

    public (bool Ok, string Message) SaveAddress(int userId, DireccionFormViewModel form)
    {
        if (form.Predeterminada)
        {
            users.ClearDefaultAddresses(userId);
        }

        var address = new DireccionCliente
        {
            Id = form.Id,
            UsuarioId = userId,
            Alias = form.Alias.Trim(),
            Provincia = form.Provincia.Trim(),
            Canton = form.Canton.Trim(),
            Distrito = form.Distrito.Trim(),
            SenasExactas = form.SenasExactas.Trim(),
            Predeterminada = form.Predeterminada
        };
        address.Detalle = $"{address.Provincia}, {address.Canton}, {address.Distrito}. {address.SenasExactas}";

        if (form.Id == 0)
        {
            var hasAddress = users.GetAddresses(userId).Any();
            address.Predeterminada = form.Predeterminada || !hasAddress;
            users.CreateAddress(address);
            return (true, "Direccion agregada.");
        }

        if (users.FindAddress(userId, form.Id) is null)
        {
            return (false, "Direccion no encontrada.");
        }

        users.UpdateAddress(address);
        return (true, "Direccion actualizada.");
    }

    public (bool Ok, string Message) DeleteAddress(int userId, int id)
    {
        var address = users.FindAddress(userId, id);
        if (address is null) return (false, "Direccion no encontrada.");

        users.DeleteAddress(id);

        var remaining = users.GetAddresses(userId).ToList();
        if (address.Predeterminada && remaining.Count > 0 && !remaining.Any(a => a.Predeterminada))
        {
            var next = remaining[0];
            next.Predeterminada = true;
            users.UpdateAddress(next);
        }

        return (true, "Direccion eliminada.");
    }

    public void LogError(string ruta, string mensaje) =>
        users.AddErrorLog(new LogErrorItem { Ruta = ruta, Mensaje = mensaje });

    private static bool IsValidPhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return true;
        var trimmed = phone.Trim();
        return trimmed.Length is >= 7 and <= 20 && trimmed.All(c => char.IsDigit(c) || c is '+' or '-' or ' ');
    }
}
