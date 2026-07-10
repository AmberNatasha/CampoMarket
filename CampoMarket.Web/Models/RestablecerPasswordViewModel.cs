using System.ComponentModel.DataAnnotations;

namespace CampoMarket.Web.Models;

public sealed class RestablecerPasswordViewModel
{
    [Required]
    public string Token { get; set; } = "";

    [Required(ErrorMessage = "Ingresa la nueva contraseña.")]
    [MinLength(8, ErrorMessage = "La nueva contraseña debe tener al menos 8 caracteres.")]
    public string PasswordNuevo { get; set; } = "";
}
