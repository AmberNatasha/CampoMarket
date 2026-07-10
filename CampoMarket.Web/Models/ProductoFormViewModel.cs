using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CampoMarket.Web.Models;

public sealed class ProductoFormViewModel
{
    public int Id { get; set; }
    [Required] public string Nombre { get; set; } = "";
    [Required] public string Descripcion { get; set; } = "";
    [Range(0.01, 100000)] public decimal Precio { get; set; }
    [Range(0, 100000)] public int Stock { get; set; }
    [Range(0, 100000)] public int StockMinimo { get; set; }
    [Required] public int CategoriaId { get; set; }
    public string? ImagenUrl { get; set; }
    public IFormFile? ImagenArchivo { get; set; }
    public bool Activo { get; set; } = true;

    [ValidateNever]
    public IEnumerable<Categoria> Categorias { get; set; } = [];
}
