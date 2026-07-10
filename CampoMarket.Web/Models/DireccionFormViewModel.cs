using System.ComponentModel.DataAnnotations;

namespace CampoMarket.Web.Models;

public sealed class DireccionFormViewModel
{
    public int Id { get; set; }
    [Required] public string Alias { get; set; } = "";
    [Required] public string Provincia { get; set; } = "";
    [Required] public string Canton { get; set; } = "";
    [Required] public string Distrito { get; set; } = "";
    [Required] public string SenasExactas { get; set; } = "";
    public bool Predeterminada { get; set; }
}
