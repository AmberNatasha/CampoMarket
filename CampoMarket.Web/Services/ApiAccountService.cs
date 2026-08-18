using CampoMarket.Web.Models;

namespace CampoMarket.Web.Services;

public sealed class ApiAccountService(ApiRequestClient api) : IUserService,IPasswordResetService,IAddressService,IAuditService
{
    public IReadOnlyList<Usuario> Clientes=>api.Get<List<Usuario>>("api/store/clientes");
    public IReadOnlyList<AuditLogItem> AuditLogs=>api.Get<List<AuditLogItem>>("api/store/auditoria");
    public IReadOnlyList<LogErrorItem> ErrorLogs=>api.Get<List<LogErrorItem>>("api/store/errores");
    public Usuario? FindUser(int id){try{return api.Get<Usuario>($"api/store/usuarios/{id}");}catch{return null;}}
    public (bool Ok,string Message,Usuario? User) Register(string nombre,string correo,string password,string telefono,string direccion)
    { try{var r=api.Post<ApiUserResult>("api/account/registro",new{nombre,correo,password,telefono,direccion});return(r.Ok,r.Message,r.User);}catch(Exception e){return(false,Clean(e),null);} }
    public (bool Ok,string Message,Usuario? User) Login(string correo,string password,string ip="")=>(false,"El inicio de sesión se realiza mediante la API de autenticación.",null);
    public (bool Ok,string Message) UpdateProfile(int userId,string nombre,string telefono,string direccion)
    {try{var r=api.Put<ApiResult>("api/account/perfil",new{nombre,telefono,direccion});return(r.Ok,r.Message);}catch(Exception e){return(false,Clean(e));}}
    public (bool Ok,string Message) ChangePassword(int userId,string actual,string nuevo)
    {try{var r=api.Put<ApiResult>("api/account/password",new{actual,nuevo});return(r.Ok,r.Message);}catch(Exception e){return(false,Clean(e));}}
    public (bool Ok,string Message,string? Token) RequestPasswordReset(string correo)
    {try{var r=api.Post<ApiResult>("api/account/recuperacion",new{correo});return(r.Ok,r.Message,r.Token);}catch(Exception e){return(false,Clean(e),null);}}
    public (bool Ok,string Message) ValidatePasswordResetCode(string correo,string code)
    {try{var r=api.Post<ApiResult>("api/account/recuperacion/validar",new{correo,codigo=code});return(r.Ok,r.Message);}catch(Exception e){return(false,Clean(e));}}
    public (bool Ok,string Message) ResetPassword(string token,string nuevo)
    {try{var r=api.Post<ApiResult>("api/account/recuperacion/restablecer",new{token,nuevo});return(r.Ok,r.Message);}catch(Exception e){return(false,Clean(e));}}
    public IEnumerable<DireccionCliente> GetAddresses(int userId)=>api.Get<List<DireccionCliente>>("api/store/direcciones");
    public DireccionCliente? FindAddress(int userId,int id){try{return api.Get<DireccionCliente>($"api/store/direcciones/{id}");}catch{return null;}}
    public (bool Ok,string Message) SaveAddress(int userId,DireccionFormViewModel form)
    {try{var r=api.Post<ApiResult>("api/store/direcciones",new{form.Id,form.Alias,form.Provincia,form.Canton,form.Distrito,form.SenasExactas,form.Predeterminada});return(r.Ok,r.Message);}catch(Exception e){return(false,Clean(e));}}
    public (bool Ok,string Message) DeleteAddress(int userId,int id)
    {try{api.Delete($"api/store/direcciones/{id}");return(true,"Dirección eliminada.");}catch(Exception e){return(false,Clean(e));}}
    public void LogError(string ruta,string mensaje){try{api.Post<object>("api/store/errores",new{ruta,mensaje});}catch{/* no propagar errores del registro */}}
    private static string Clean(Exception e)=>e.Message.Length>500?e.Message[..500]:e.Message;
}

public sealed class ApiUserResult:ApiResult { public Usuario? User { get; set; } }
