using CampoMarket.Web.Models;

namespace CampoMarket.Web.Services;

public sealed class ApiCatalogService(ApiRequestClient api) : ICatalogService
{
    public IReadOnlyList<Categoria> Categorias => api.Get<List<Categoria>>("api/store/categorias");
    public IReadOnlyList<Producto> Productos => api.Get<List<Producto>>("api/store/productos");

    public IEnumerable<Producto> BuscarProductos(string? categoria, string? buscar, string? orden)
    {
        var query = Productos.Where(p => p.Activo && p.Stock > 0);
        if (int.TryParse(categoria, out var categoryId) && categoryId > 0) query = query.Where(p => p.CategoriaId == categoryId);
        if (!string.IsNullOrWhiteSpace(buscar)) query = query.Where(p => p.Nombre.Contains(buscar.Trim(), StringComparison.OrdinalIgnoreCase));
        return orden switch { "precio_desc" => query.OrderByDescending(p => p.Precio), "precio_asc" => query.OrderBy(p => p.Precio), _ => query.OrderBy(p => p.Nombre) };
    }

    public Producto? FindProduct(int id) => Productos.FirstOrDefault(p => p.Id == id);

    public (bool Ok, string Message) SaveProduct(ProductoFormViewModel form)
    {
        api.Post<object>("api/productos", new { IdProducto=form.Id, IdCategoria=form.CategoriaId, NombreProducto=form.Nombre, form.Descripcion, form.Precio, form.Stock, form.StockMinimo, form.ImagenUrl, form.Activo });
        return (true, form.Id == 0 ? "Producto creado." : "Producto actualizado.");
    }

    public (bool Ok, string Message) DeactivateProduct(int id)
    { api.Delete($"api/productos/{id}"); return (true, "Producto desactivado."); }

    public (bool Ok, string Message) AdjustStock(int id, int cantidad, string motivo)
    { api.Put<object>($"api/productos/{id}/stock", new { cantidad, motivo }); return (true, "Stock actualizado."); }

    public (bool Ok, string Message) SaveCategory(CategoriaFormViewModel form)
    { api.Post<object>("api/store/categorias", new { form.Id, form.Nombre, form.Descripcion }); return (true, form.Id == 0 ? "Categoría creada." : "Categoría actualizada."); }

    public (bool Ok, string Message) DeleteCategory(int id)
    { api.Delete($"api/store/categorias/{id}"); return (true, "Categoría desactivada."); }
}
