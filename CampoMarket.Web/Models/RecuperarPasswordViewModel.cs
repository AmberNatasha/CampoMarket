using System.ComponentModel.DataAnnotations;

namespace CampoMarket.Web.Models;

public sealed class RecuperarPasswordViewModel
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo valido.")]
    public string Correo { get; set; } = "";
}
