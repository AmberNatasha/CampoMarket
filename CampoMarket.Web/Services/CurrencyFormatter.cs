using System.Globalization;

namespace CampoMarket.Web.Services;

public static class CurrencyFormatter
{
    private static readonly CultureInfo CostaRica = CultureInfo.GetCultureInfo("es-CR");

    public static string Colones(decimal amount) => string.Format(CostaRica, "₡{0:N0}", amount);
}
