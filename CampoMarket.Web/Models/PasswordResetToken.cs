namespace CampoMarket.Web.Models;

public sealed class PasswordResetToken
{
    public int UsuarioId { get; set; }
    public string Token { get; set; } = "";
    public DateTime ExpiraUtc { get; set; }
    public bool Usado { get; set; }
}
