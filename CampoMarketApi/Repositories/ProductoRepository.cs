using CampoMarketApi.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CampoMarketApi.Repositories;

public sealed class ProductoRepository(IConfiguration configuration)
{
    private readonly string _connectionString = configuration.GetConnectionString("CampoMarket")
        ?? throw new InvalidOperationException("Falta ConnectionStrings:CampoMarket.");

    public IEnumerable<ProductoResponseModel> ObtenerProductos()
    {
        using var connection = new SqlConnection(_connectionString);

        return connection.Query<ProductoResponseModel>("""
        SELECT
            id_producto AS Id_Producto,
            nombre_producto AS Nombre_Producto,
            descripcion,
            precio,
            stock,
            imagen_url AS Imagen_Url,
            id_categoria AS Id_Categoria
        FROM dbo.Producto
        WHERE activo = 1
        ORDER BY nombre_producto;
        """);
    }

    public ProductoResponseModel? ObtenerProductoPorId(int id)
    {
        using var connection = new SqlConnection(_connectionString);

        return connection.QueryFirstOrDefault<ProductoResponseModel>("""
        SELECT
            id_producto AS Id_Producto,
            nombre_producto AS Nombre_Producto,
            descripcion,
            precio,
            stock,
            imagen_url AS Imagen_Url,
            id_categoria AS Id_Categoria
        FROM dbo.Producto
        WHERE id_producto = @Id
          AND activo = 1;
        """, new { Id = id });
    }

    public int? ObtenerStock(int id)
    {
        using var connection = new SqlConnection(_connectionString);

        return connection.QueryFirstOrDefault<int?>("""
        SELECT stock
        FROM dbo.Producto
        WHERE id_producto = @Id
          AND activo = 1;
        """, new { Id = id });
    }

    public int GuardarProducto(ProductoRequestModel model)
    {
        using var connection = new SqlConnection(_connectionString);

        var parameters = new DynamicParameters();
        parameters.Add("@id_producto", model.IdProducto == 0 ? null : model.IdProducto);
        parameters.Add("@id_categoria", model.IdCategoria);
        parameters.Add("@nombre_producto", model.NombreProducto);
        parameters.Add("@descripcion", model.Descripcion);
        parameters.Add("@precio", model.Precio);
        parameters.Add("@stock", model.Stock);
        parameters.Add("@stock_minimo", model.StockMinimo);
        parameters.Add("@imagen_url", model.ImagenUrl);
        parameters.Add("@activo", model.Activo);

        return connection.Execute(
            "sp_Producto_Guardar",
            parameters,
            commandType: System.Data.CommandType.StoredProcedure);
    }

    public int DesactivarProducto(int id)
    {
        using var connection = new SqlConnection(_connectionString);

        var parameters = new DynamicParameters();
        parameters.Add("@id_producto", id);

        return connection.Execute(
            "sp_Producto_Desactivar",
            parameters,
            commandType: CommandType.StoredProcedure);
    }

    public bool VerificarStock(int idProducto, int cantidad)
    {
        using var connection = new SqlConnection(_connectionString);

        var stock = connection.QueryFirstOrDefault<int?>("""
        SELECT stock
        FROM dbo.Producto
        WHERE id_producto = @Id
          AND activo = 1;
        """, new { Id = idProducto });

        if (stock is null)
            return false;

        return stock >= cantidad;
    }

    public int AjustarStock(int idProducto, int cantidad, string motivo)
    {
        using var connection = new SqlConnection(_connectionString);

        var parameters = new DynamicParameters();
        parameters.Add("@id_producto", idProducto);
        parameters.Add("@cantidad", cantidad);
        parameters.Add("@motivo", motivo);

        return connection.Execute(
            "sp_Producto_AjustarStock",
            parameters,
            commandType: CommandType.StoredProcedure);
    }
}