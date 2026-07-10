using System.ComponentModel.DataAnnotations;

namespace CampoMarket.Web.Models;

public sealed class PerfilViewModel
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    public string Nombre { get; set; } = "";

    [Phone(ErrorMessage = "Ingresa un telefono valido.")]
    [RegularExpression(@"^[0-9+\-\s]{7,20}$", ErrorMessage = "El telefono solo puede usar digitos, espacios, guiones o prefijo.")]
    public string Telefono { get; set; } = "";

    public string Direccion { get; set; } = "";
}
