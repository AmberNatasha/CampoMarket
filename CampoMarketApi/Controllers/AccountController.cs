using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CampoMarketApi.Models;
using CampoMarketApi.Repositories;
using CampoMarketApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampoMarketApi.Controllers;

[ApiController, Route("api/account")]
public sealed class AccountController(StoreRepository store) : ControllerBase
{
    [AllowAnonymous, HttpPost("registro")]
    public IActionResult Registro(RegistroRequest request)
    {
        if(store.Usuarios(correo:request.Correo).Count>0) return Conflict(new { ok=false,message="Ese correo ya está registrado." });
        var id=store.CrearUsuario(request,PasswordUtility.Hash(request.Password));
        if(!string.IsNullOrWhiteSpace(request.Direccion)) store.GuardarDireccion(id,new(0,"Casa","Sin provincia","Sin cantón","Sin distrito",request.Direccion,true));
        return Ok(new { ok=true,message="Cuenta creada. Ya puedes iniciar sesión.",user=Safe(store.Usuarios(id:id).Single()) });
    }

    [Authorize, HttpPut("perfil")]
    public IActionResult Perfil(PerfilRequest request)
    { store.ActualizarPerfil(UserId(),request);return Ok(new{ok=true,message="Perfil actualizado."}); }

    [Authorize, HttpPut("password")]
    public IActionResult Password(CambioPasswordRequest request)
    {
        var user=store.Usuarios(id:UserId()).Single();
        if(!PasswordUtility.Verify(request.Actual,user.PasswordHash)) return BadRequest(new{ok=false,message="La contraseña actual no coincide."});
        store.ActualizarPassword(user.Id,PasswordUtility.Hash(request.Nuevo));return Ok(new{ok=true,message="Contraseña actualizada."});
    }

    [AllowAnonymous, HttpPost("recuperacion")]
    public IActionResult Recuperacion(CorreoRequest request)
    {
        var user=store.Usuarios(correo:request.Correo).FirstOrDefault();
        if(user is null)return Ok(new{ok=true,message="Si el correo existe, recibirás una clave temporal.",token=(string?)null});
        const string chars="ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var code=string.Concat(Enumerable.Range(0,8).Select(_=>chars[RandomNumberGenerator.GetInt32(chars.Length)]));
        store.CrearToken(user.Id,TokenHash(code),DateTime.Now.AddHours(1));
        return Ok(new{ok=true,message="Si el correo existe, recibirás una clave temporal.",token=code});
    }

    [AllowAnonymous, HttpPost("recuperacion/validar")]
    public IActionResult Validar(ValidarCodigoRequest request)
    {
        var user=store.Usuarios(correo:request.Correo).FirstOrDefault();var token=store.ObtenerToken(TokenHash(request.Codigo));
        var ok=user is not null&&token is not null&&token.UsuarioId==user.Id&&!token.Usado&&token.ExpiraUtc>DateTime.Now;
        return ok?Ok(new{ok=true,message="Clave verificada."}):BadRequest(new{ok=false,message="La clave no es válida o ya expiró."});
    }

    [AllowAnonymous, HttpPost("recuperacion/restablecer")]
    public IActionResult Restablecer(RestablecerRequest request)
    {
        var hash=TokenHash(request.Token);var token=store.ObtenerToken(hash);
        if(token is null||token.Usado||token.ExpiraUtc<=DateTime.Now)return BadRequest(new{ok=false,message="El token no existe o ya expiró."});
        store.ActualizarPassword(token.UsuarioId,PasswordUtility.Hash(request.Nuevo));store.UsarToken(hash);
        return Ok(new{ok=true,message="Contraseña restablecida. Ya puedes iniciar sesión."});
    }
    private int UserId()=>int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static string TokenHash(string value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim().ToUpperInvariant()))).ToLowerInvariant();
    private static object Safe(UsuarioDto u)=>new{u.Id,u.Nombre,u.Correo,u.Telefono,u.Direccion,u.Rol};
}
