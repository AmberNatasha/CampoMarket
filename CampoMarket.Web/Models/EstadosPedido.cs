namespace CampoMarket.Web.Models;

public static class EstadosPedido
{
    public const string Pendiente = "Pendiente";
    public const string Preparando = "Preparando";
    public const string Listo = "Listo";
    public const string Entregado = "Entregado";
    public const string Cancelado = "Cancelado";

    public static IReadOnlyList<OpcionSeleccion> Opciones { get; } =
    [
        new(Pendiente, Pendiente),
        new(Preparando, Preparando),
        new(Listo, Listo),
        new(Entregado, Entregado),
        new(Cancelado, Cancelado)
    ];
}
