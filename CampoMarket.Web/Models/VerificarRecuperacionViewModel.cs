using System.ComponentModel.DataAnnotations;

namespace CampoMarket.Web.Models;

public sealed class VerificarRecuperacionViewModel
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo válido.")]
    public string Correo { get; set; } = "";

    [Required(ErrorMessage = "Ingresa la clave recibida por correo.")]
    [RegularExpression("^[A-Za-z0-9]{8}$", ErrorMessage = "La clave debe tener 8 caracteres.")]
    [Display(Name = "Clave de recuperación")]
    public string Codigo { get; set; } = "";
}
