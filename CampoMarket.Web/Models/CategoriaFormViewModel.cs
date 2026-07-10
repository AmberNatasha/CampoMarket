using System.ComponentModel.DataAnnotations;

namespace CampoMarket.Web.Models;

public sealed class CategoriaFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    public string Nombre { get; set; } = "";

    public string Descripcion { get; set; } = "";
}
