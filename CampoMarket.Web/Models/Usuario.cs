namespace CampoMarket.Web.Models;

public sealed class Usuario
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Correo { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string Direccion { get; set; } = "";
    public string Rol { get; set; } = RolesCampo.Cliente;
    public string PasswordHash { get; set; } = "";
    public int IntentosFallidos { get; set; }
    public DateTime? BloqueadoHastaUtc { get; set; }
}
