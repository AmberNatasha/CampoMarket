using System.ComponentModel.DataAnnotations;

namespace CampoMarket.Web.Models;

public sealed class ContactoViewModel
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
    [Display(Name = "Nombre completo")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo válido.")]
    [StringLength(254)]
    [Display(Name = "Correo electrónico")]
    public string Correo { get; set; } = "";

    [Phone(ErrorMessage = "Ingresa un teléfono válido.")]
    [StringLength(25)]
    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }

    [Required(ErrorMessage = "El mensaje es obligatorio.")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "El mensaje debe tener entre 10 y 2000 caracteres.")]
    public string Mensaje { get; set; } = "";
}
