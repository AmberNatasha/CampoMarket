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

        return connection.Query<ProductoResponseModel>(
            "sp_Producto_ObtenerTodos",
            commandType: CommandType.StoredProcedure);
    }

    public ProductoResponseModel? ObtenerProductoPorId(int id)
    {
        using var connection = new SqlConnection(_connectionString);

        var parameters = new DynamicParameters();
        parameters.Add("@Id", id);

        return connection.QueryFirstOrDefault<ProductoResponseModel>(
            "sp_Producto_ObtenerPorId",
            parameters,
            commandType: CommandType.StoredProcedure);
    }

    public int? ObtenerStock(int id)
    {
        using var connection = new SqlConnection(_connectionString);

        var parameters = new DynamicParameters();
        parameters.Add("@Id", id);

        return connection.QueryFirstOrDefault<int?>(
            "sp_Producto_ObtenerStock",
            parameters,
            commandType: CommandType.StoredProcedure);
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

        var parameters = new DynamicParameters();
        parameters.Add("@Id", idProducto);

        var stock = connection.QueryFirstOrDefault<int?>(
            "sp_Producto_ObtenerStock",
            parameters,
            commandType: CommandType.StoredProcedure);

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