using System.ComponentModel.DataAnnotations;

namespace CampoMarket.Web.Models;

public sealed class RegistroViewModel
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo valido.")]
    public string Correo { get; set; } = "";

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    public string Password { get; set; } = "";

    [Phone(ErrorMessage = "Ingresa un telefono valido.")]
    public string Telefono { get; set; } = "";

    public string Direccion { get; set; } = "";
}
