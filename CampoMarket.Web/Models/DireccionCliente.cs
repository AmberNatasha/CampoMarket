namespace CampoMarket.Web.Models;

public sealed class DireccionCliente
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string Alias { get; set; } = "";
    public string Provincia { get; set; } = "";
    public string Canton { get; set; } = "";
    public string Distrito { get; set; } = "";
    public string SenasExactas { get; set; } = "";
    public string Detalle { get; set; } = "";
    public bool Predeterminada { get; set; }
}
