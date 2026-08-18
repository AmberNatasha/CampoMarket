namespace CampoMarket.Web.Models;

public static class TiposEntrega
{
    public const string Express = "Express";
    public const string Recoleccion = "Recoleccion";

    public static IReadOnlyList<OpcionSeleccion> Opciones { get; } =
    [
        new(Express, "Express a domicilio"),
        new(Recoleccion, "Recolección en tienda")
    ];
}
