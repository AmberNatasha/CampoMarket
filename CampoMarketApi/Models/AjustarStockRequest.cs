using CampoMarketApi.Models;

namespace CampoMarketApi.Models;

public sealed record AjustarStockRequest(
    int Cantidad,
    string Motivo);