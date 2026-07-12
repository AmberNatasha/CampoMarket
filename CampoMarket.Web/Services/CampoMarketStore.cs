using CampoMarket.Web.Models;

namespace CampoMarket.Web.Services;

public sealed class CampoMarketStore :
    ICatalogService
{
    private readonly object _sync = new();
    private readonly ICatalogRepository _catalogRepository;
    private readonly IUserRepository _userRepository;
    private readonly List<Categoria> _categorias = [];
    private readonly List<Producto> _productos = [];
    private readonly List<Pedido> _pedidos = [];
    private readonly List<MovimientoInventario> _movimientos = [];
    private readonly Dictionary<int, List<CarritoItem>> _carritos = [];
    private int _categoriaId = 1;
    private int _productoId = 1;
    private int _pedidoId = 1;

    public CampoMarketStore(ICatalogRepository catalogRepository, IUserRepository userRepository)
    {
        _catalogRepository = catalogRepository;
        _userRepository = userRepository;
        Seed();
    }

    public IReadOnlyList<Categoria> Categorias
    {
        get
        {
            lock (_sync)
            {
                LoadCatalogState();
                return _categorias.Select(Clone).ToList();
            }
        }
    }

    public IReadOnlyList<Producto> Productos
    {
        get
        {
            lock (_sync)
            {
                LoadCatalogState();
                return _productos.Select(Clone).ToList();
            }
        }
    }

    public IReadOnlyList<Pedido> Pedidos
    {
        get { lock (_sync) return _pedidos.Select(Clone).ToList(); }
    }

    public IReadOnlyList<MovimientoInventario> Movimientos
    {
        get
        {
            lock (_sync)
            {
                LoadCatalogState();
                return _movimientos.Select(Clone).ToList();
            }
        }
    }

    public IReadOnlyList<AuditLogItem> AuditLogs
    {
        get { lock (_sync) return _userRepository.GetAuditLogs(); }
    }

    public IReadOnlyList<LogErrorItem> ErrorLogs
    {
        get { lock (_sync) return _userRepository.GetErrorLogs(); }
    }

    public IReadOnlyList<Usuario> Clientes
    {
        get { lock (_sync) return _userRepository.GetClients(); }
    }

    public Usuario? FindUserByEmail(string correo)
    {
        lock (_sync) return _userRepository.FindByEmail(correo);
    }

    public Usuario? FindUser(int id)
    {
        lock (_sync) return _userRepository.FindById(id);
    }

    public (bool Ok, string Message, Usuario? User) Register(string nombre, string correo, string password, string telefono, string direccion)
    {
        lock (_sync)
        {
            if (_userRepository.FindByEmail(correo) is not null)
            {
                return (false, "Ese correo ya esta registrado.", null);
            }

            var user = new Usuario
            {
                Nombre = nombre.Trim(),
                Correo = correo.Trim().ToLowerInvariant(),
                Telefono = telefono.Trim(),
                Rol = RolesCampo.Cliente,
                PasswordHash = PasswordService.Hash(password)
            };

            user.Id = _userRepository.CreateUser(user);
            if (!string.IsNullOrWhiteSpace(direccion))
            {
                _userRepository.CreateAddress(new DireccionCliente
                {
                    UsuarioId = user.Id,
                    Alias = "Casa",
                    Provincia = "Sin provincia",
                    Canton = "Sin canton",
                    Distrito = "Sin distrito",
                    SenasExactas = direccion.Trim(),
                    Predeterminada = true
                });
            }

            return (true, "Cuenta creada. Ya puedes iniciar sesion.", user);
        }
    }

    public (bool Ok, string Message, Usuario? User) Login(string correo, string password, string ip = "")
    {
        lock (_sync)
        {
            var user = _userRepository.FindByEmail(correo);
            if (user is null)
            {
                _userRepository.AddAuditLog(new AuditLogItem { Correo = correo, Ip = ip, Evento = "Login fallido: correo no registrado" });
                return (false, "Correo o contraseña incorrectos.", null);
            }

            if (user.BloqueadoHastaUtc > DateTime.UtcNow)
            {
                _userRepository.AddAuditLog(new AuditLogItem { Correo = correo, Ip = ip, Evento = "Login bloqueado temporalmente" });
                return (false, "La cuenta esta bloqueada temporalmente por intentos fallidos.", null);
            }

            if (!PasswordService.Verify(password, user.PasswordHash))
            {
                user.IntentosFallidos++;
                if (user.IntentosFallidos >= 5)
                {
                    user.BloqueadoHastaUtc = DateTime.UtcNow.AddMinutes(15);
                }

                _userRepository.UpdateLoginState(user.Id, user.IntentosFallidos, user.BloqueadoHastaUtc);
                _userRepository.AddAuditLog(new AuditLogItem { Correo = correo, Ip = ip, Evento = $"Login fallido #{user.IntentosFallidos}" });
                return (false, "Correo o contraseña incorrectos.", null);
            }

            _userRepository.UpdateLoginState(user.Id, 0, null);
            _userRepository.AddAuditLog(new AuditLogItem { Correo = correo, Ip = ip, Evento = "Login exitoso" });
            user.IntentosFallidos = 0;
            user.BloqueadoHastaUtc = null;
            return (true, "Sesion iniciada.", user);
        }
    }

    public void LogError(string ruta, string mensaje)
    {
        lock (_sync)
        {
            _userRepository.AddErrorLog(new LogErrorItem { Ruta = ruta, Mensaje = mensaje });
        }
    }

    public (bool Ok, string Message) UpdateProfile(int userId, string nombre, string telefono, string direccion)
    {
        lock (_sync)
        {
            if (!IsValidPhone(telefono))
            {
                return (false, "El telefono debe tener entre 7 y 20 caracteres y usar solo digitos, espacios, guiones o prefijo.");
            }

            if (_userRepository.FindById(userId) is null) return (false, "Usuario no encontrado.");
            _userRepository.UpdateProfile(userId, nombre.Trim(), telefono.Trim());
            return (true, "Perfil actualizado.");
        }
    }

    public (bool Ok, string Message) ChangePassword(int userId, string actual, string nuevo)
    {
        lock (_sync)
        {
            var user = _userRepository.FindById(userId);
            if (user is null) return (false, "Usuario no encontrado.");
            if (!PasswordService.Verify(actual, user.PasswordHash))
            {
                return (false, "La contraseña actual no coincide.");
            }

            _userRepository.UpdatePassword(userId, PasswordService.Hash(nuevo));
            return (true, "contraseña actualizada.");
        }
    }

    public (bool Ok, string Message, string? Token) RequestPasswordReset(string correo)
    {
        lock (_sync)
        {
            var user = _userRepository.FindByEmail(correo);
            if (user is null)
            {
                return (true, "Si el correo existe, se genero un enlace temporal.", null);
            }

            var token = Convert.ToHexString(Guid.NewGuid().ToByteArray()).ToLowerInvariant();
            _userRepository.AddPasswordResetToken(new PasswordResetToken
            {
                UsuarioId = user.Id,
                Token = token,
                ExpiraUtc = DateTime.UtcNow.AddHours(1)
            });
            return (true, "Token temporal generado. En produccion se enviaria por correo.", token);
        }
    }

    public (bool Ok, string Message) ResetPassword(string token, string nuevo)
    {
        lock (_sync)
        {
            var reset = _userRepository.FindPasswordResetToken(token);
            if (reset is null || reset.Usado || reset.ExpiraUtc < DateTime.UtcNow)
            {
                return (false, "El token no existe o ya expiro.");
            }

            if (_userRepository.FindById(reset.UsuarioId) is null) return (false, "Usuario no encontrado.");
            _userRepository.UpdatePassword(reset.UsuarioId, PasswordService.Hash(nuevo));
            _userRepository.MarkPasswordResetTokenUsed(token);
            return (true, "contraseña restablecida. Ya puedes iniciar sesion.");
        }
    }
    public IEnumerable<Producto> BuscarProductos(string? categoria, string? buscar, string? orden)
    {
        lock (_sync)
        {
            LoadCatalogState();
            var query = _productos.Where(p => p.Activo && p.Stock > 0);
            if (int.TryParse(categoria, out var categoriaId) && categoriaId > 0)
            {
                query = query.Where(p => p.CategoriaId == categoriaId);
            }

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                query = query.Where(p => p.Nombre.Contains(buscar.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            query = orden switch
            {
                "precio_desc" => query.OrderByDescending(p => p.Precio),
                "precio_asc" => query.OrderBy(p => p.Precio),
                _ => query.OrderBy(p => p.Nombre)
            };

            return query.Select(Clone).ToList();
        }
    }

    public Producto? FindProduct(int id)
    {
        lock (_sync)
        {
            LoadCatalogState();
            return _productos.FirstOrDefault(p => p.Id == id) is { } p ? Clone(p) : null;
        }
    }

    public (bool Ok, string Message) SaveProduct(ProductoFormViewModel form)
    {
        lock (_sync)
        {
            LoadCatalogState();
            var productsSnapshot = _productos.Select(Clone).ToList();
            var productIdSnapshot = _productoId;

            if (!_categorias.Any(c => c.Id == form.CategoriaId && c.Activa))
            {
                return (false, "Selecciona una categoria activa para el producto.");
            }

            if (form.Id == 0)
            {
                _productos.Add(new Producto
                {
                    Id = _productoId++,
                    Nombre = form.Nombre,
                    Descripcion = form.Descripcion,
                    Precio = form.Precio,
                    Stock = form.Stock,
                    StockMinimo = form.StockMinimo,
                    CategoriaId = form.CategoriaId,
                    ImagenUrl = string.IsNullOrWhiteSpace(form.ImagenUrl) ? "/Images/Banner.jpg" : form.ImagenUrl,
                    Activo = form.Activo,
                    ActualizadoUtc = DateTime.UtcNow
                });
                try
                {
                    SaveCatalogState();
                }
                catch
                {
                    _productos.Clear();
                    _productos.AddRange(productsSnapshot);
                    _productoId = productIdSnapshot;
                    throw;
                }

                return (true, "Producto creado.");
            }

            var product = _productos.FirstOrDefault(p => p.Id == form.Id);
            if (product is null) return (false, "Producto no encontrado.");

            product.Nombre = form.Nombre;
            product.Descripcion = form.Descripcion;
            product.Precio = form.Precio;
            product.Stock = form.Stock;
            product.StockMinimo = form.StockMinimo;
            product.CategoriaId = form.CategoriaId;
            product.ImagenUrl = string.IsNullOrWhiteSpace(form.ImagenUrl) ? product.ImagenUrl : form.ImagenUrl;
            product.Activo = form.Activo;
            product.ActualizadoUtc = DateTime.UtcNow;
            try
            {
                SaveCatalogState();
            }
            catch
            {
                _productos.Clear();
                _productos.AddRange(productsSnapshot);
                _productoId = productIdSnapshot;
                throw;
            }

            return (true, "Producto actualizado.");
        }
    }

    public (bool Ok, string Message) DeactivateProduct(int id)
    {
        lock (_sync)
        {
            LoadCatalogState();
            var hasActiveOrders = _pedidos.Any(p => p.Estado is not EstadosPedido.Entregado and not EstadosPedido.Cancelado && p.Detalles.Any(d => d.ProductoId == id));
            if (hasActiveOrders)
            {
                return (false, "No se puede desactivar: el producto esta en pedidos activos.");
            }

            var product = _productos.FirstOrDefault(p => p.Id == id);
            if (product is null) return (false, "Producto no encontrado.");
            product.Activo = false;
            product.ActualizadoUtc = DateTime.UtcNow;
            SaveCatalogState();
            return (true, "Producto desactivado.");
        }
    }

    public (bool Ok, string Message) AdjustStock(int id, int cantidad, string motivo)
    {
        lock (_sync)
        {
            LoadCatalogState();
            if (cantidad == 0) return (false, "El ajuste no puede ser cero.");
            var product = _productos.FirstOrDefault(p => p.Id == id);
            if (product is null) return (false, "Producto no encontrado.");
            if (product.Stock + cantidad < 0) return (false, "El ajuste dejaria el stock en negativo.");
            product.Stock += cantidad;
            product.ActualizadoUtc = DateTime.UtcNow;
            _movimientos.Add(new MovimientoInventario { ProductoId = id, ProductoNombre = product.Nombre, Tipo = "Ajuste manual", Cantidad = cantidad, Motivo = motivo });
            SaveCatalogState();
            return (true, "Stock actualizado.");
        }
    }

    public IReadOnlyList<CarritoLineaViewModel> GetCart(int userId)
    {
        lock (_sync)
        {
            return GetCartItems(userId)
                .Select(item => new CarritoLineaViewModel { Producto = Clone(_productos.First(p => p.Id == item.ProductoId)), Cantidad = item.Cantidad })
                .ToList();
        }
    }

    public (bool Ok, string Message) AddToCart(int userId, int productId, int cantidad)
    {
        lock (_sync)
        {
            var product = _productos.FirstOrDefault(p => p.Id == productId && p.Activo);
            if (product is null) return (false, "Producto no encontrado.");
            if (cantidad <= 0) return (false, "La cantidad debe ser mayor a cero.");
            var cart = GetCartItems(userId);
            var item = cart.FirstOrDefault(x => x.ProductoId == productId);
            var newQuantity = (item?.Cantidad ?? 0) + cantidad;
            if (newQuantity > product.Stock) return (false, "No hay stock suficiente.");
            if (item is null) cart.Add(new CarritoItem { ProductoId = productId, Cantidad = cantidad });
            else item.Cantidad = newQuantity;
            return (true, "Producto agregado al carrito.");
        }
    }

    public void UpdateCart(int userId, int productId, int cantidad)
    {
        lock (_sync)
        {
            var cart = GetCartItems(userId);
            var item = cart.FirstOrDefault(x => x.ProductoId == productId);
            if (item is null) return;
            var stock = _productos.First(p => p.Id == productId).Stock;
            item.Cantidad = Math.Min(Math.Max(cantidad, 0), stock);
            if (item.Cantidad == 0) cart.Remove(item);
        }
    }

    public void RemoveFromCart(int userId, int productId)
    {
        lock (_sync) GetCartItems(userId).RemoveAll(x => x.ProductoId == productId);
    }

    public void ClearCart(int userId)
    {
        lock (_sync) GetCartItems(userId).Clear();
    }

    public IEnumerable<DireccionCliente> GetAddresses(int userId)
    {
        lock (_sync) return _userRepository.GetAddresses(userId);
    }

    public DireccionCliente? FindAddress(int userId, int id)
    {
        lock (_sync) return _userRepository.FindAddress(userId, id);
    }

    public (bool Ok, string Message) SaveAddress(int userId, DireccionFormViewModel form)
    {
        lock (_sync)
        {
            if (form.Predeterminada)
            {
                _userRepository.ClearDefaultAddresses(userId);
            }

            var address = new DireccionCliente
            {
                Id = form.Id,
                UsuarioId = userId,
                Alias = form.Alias.Trim(),
                Provincia = form.Provincia.Trim(),
                Canton = form.Canton.Trim(),
                Distrito = form.Distrito.Trim(),
                SenasExactas = form.SenasExactas.Trim(),
                Predeterminada = form.Predeterminada
            };
            address.Detalle = $"{address.Provincia}, {address.Canton}, {address.Distrito}. {address.SenasExactas}";

            if (form.Id == 0)
            {
                var hasAddress = _userRepository.GetAddresses(userId).Any();
                address.Predeterminada = form.Predeterminada || !hasAddress;
                _userRepository.CreateAddress(address);
                return (true, "Direccion agregada.");
            }

            if (_userRepository.FindAddress(userId, form.Id) is null) return (false, "Direccion no encontrada.");
            _userRepository.UpdateAddress(address);
            return (true, "Direccion actualizada.");
        }
    }

    public (bool Ok, string Message) DeleteAddress(int userId, int id)
    {
        lock (_sync)
        {
            var address = _userRepository.FindAddress(userId, id);
            if (address is null) return (false, "Direccion no encontrada.");
            var hasPendingOrder = _pedidos.Any(p => p.UsuarioId == userId && p.Estado is not EstadosPedido.Entregado and not EstadosPedido.Cancelado && p.DireccionEntrega == address.Detalle);
            if (hasPendingOrder) return (false, "No se puede eliminar una direccion usada por pedidos activos.");

            _userRepository.DeleteAddress(id);
            var remaining = _userRepository.GetAddresses(userId).ToList();
            if (address.Predeterminada && remaining.Count > 0 && !remaining.Any(d => d.Predeterminada))
            {
                var next = remaining[0];
                next.Predeterminada = true;
                _userRepository.UpdateAddress(next);
            }

            return (true, "Direccion eliminada.");
        }
    }
    public (bool Ok, string Message, Pedido? Pedido) CreateOrder(int userId, string tipoEntrega, string direccionEntrega)
    {
        lock (_sync)
        {
            var cart = GetCartItems(userId);
            if (cart.Count == 0) return (false, "Tu carrito esta vacio.", null);
            foreach (var item in cart)
            {
                var product = _productos.First(p => p.Id == item.ProductoId);
                if (item.Cantidad > product.Stock) return (false, $"Stock insuficiente para {product.Nombre}.", null);
            }

            var order = new Pedido
            {
                Id = _pedidoId++,
                Numero = $"CM-{DateTime.UtcNow:yyyyMMdd}-{_pedidoId:0000}",
                UsuarioId = userId,
                TipoEntrega = tipoEntrega,
                DireccionEntrega = tipoEntrega == TiposEntrega.Recoleccion ? "Campo Market Central, 8:00 a 20:00" : direccionEntrega,
                Estado = EstadosPedido.Pendiente,
                FechaUtc = DateTime.UtcNow
            };

            foreach (var item in cart)
            {
                var product = _productos.First(p => p.Id == item.ProductoId);
                product.Stock -= item.Cantidad;
                product.ActualizadoUtc = DateTime.UtcNow;
                order.Detalles.Add(new PedidoDetalle { ProductoId = product.Id, ProductoNombre = product.Nombre, Cantidad = item.Cantidad, PrecioUnitario = product.Precio });
                _movimientos.Add(new MovimientoInventario { ProductoId = product.Id, ProductoNombre = product.Nombre, Tipo = "Venta automatica", Cantidad = -item.Cantidad, Motivo = order.Numero });
            }

            order.Total = order.Detalles.Sum(x => x.Subtotal);
            order.Historial.Add(new HistorialEstado { Estado = EstadosPedido.Pendiente });
            _pedidos.Add(order);
            cart.Clear();
            SaveCatalogState();
            return (true, $"Pedido {order.Numero} generado.", Clone(order));
        }
    }

    public (bool Ok, string Message) CancelOrder(int userId, int orderId)
    {
        lock (_sync)
        {
            var order = _pedidos.FirstOrDefault(p => p.Id == orderId && p.UsuarioId == userId);
            if (order is null) return (false, "Pedido no encontrado.");
            if (order.Estado != EstadosPedido.Pendiente) return (false, "Solo puedes cancelar pedidos pendientes.");
            order.Estado = EstadosPedido.Cancelado;
            order.CanceladoUtc = DateTime.UtcNow;
            order.Historial.Add(new HistorialEstado { Estado = EstadosPedido.Cancelado });
            foreach (var detail in order.Detalles)
            {
                var product = _productos.First(p => p.Id == detail.ProductoId);
                product.Stock += detail.Cantidad;
                _movimientos.Add(new MovimientoInventario { ProductoId = product.Id, ProductoNombre = product.Nombre, Tipo = "Reintegro por cancelacion", Cantidad = detail.Cantidad, Motivo = order.Numero });
            }

            SaveCatalogState();
            return (true, "Pedido cancelado y stock reintegrado.");
        }
    }

    public (bool Ok, string Message) AdvanceOrder(int orderId)
    {
        lock (_sync)
        {
            var order = _pedidos.FirstOrDefault(p => p.Id == orderId);
            if (order is null) return (false, "Pedido no encontrado.");
            var next = order.Estado switch
            {
                EstadosPedido.Pendiente => EstadosPedido.Preparando,
                EstadosPedido.Preparando => EstadosPedido.Listo,
                EstadosPedido.Listo => EstadosPedido.Entregado,
                _ => null
            };
            if (next is null) return (false, "El pedido ya no puede avanzar.");
            order.Estado = next;
            order.Historial.Add(new HistorialEstado { Estado = next });
            return (true, $"Pedido actualizado a {next}.");
        }
    }

    public IEnumerable<Pedido> PedidosCliente(int userId)
    {
        lock (_sync) return _pedidos.Where(p => p.UsuarioId == userId).OrderByDescending(p => p.FechaUtc).Select(Clone).ToList();
    }

    public Pedido? FindOrder(int id)
    {
        lock (_sync) return _pedidos.FirstOrDefault(p => p.Id == id) is { } p ? Clone(p) : null;
    }

    public IEnumerable<Pedido> BuscarPedidosAdmin(string? estado, string? tipo, string? buscar, bool incluirCerrados = false)
    {
        lock (_sync)
        {
            var query = _pedidos.AsEnumerable();
            if (!incluirCerrados)
            {
                query = query.Where(p => p.Estado is not EstadosPedido.Entregado and not EstadosPedido.Cancelado);
            }

            if (!string.IsNullOrWhiteSpace(estado)) query = query.Where(p => p.Estado == estado);
            if (!string.IsNullOrWhiteSpace(tipo)) query = query.Where(p => p.TipoEntrega == tipo);
            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var text = buscar.Trim();
                query = query.Where(p => p.Numero.Contains(text, StringComparison.OrdinalIgnoreCase) || (_userRepository.FindById(p.UsuarioId)?.Nombre.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            return query.OrderByDescending(p => p.FechaUtc).Select(Clone).ToList();
        }
    }

    public IEnumerable<ProductoVendidoViewModel> ProductosMasVendidos(DateTime? desde, DateTime? hasta, int? categoriaId)
    {
        lock (_sync)
        {
            LoadCatalogState();
            var pedidos = _pedidos.Where(p => p.Estado != EstadosPedido.Cancelado);
            if (desde.HasValue) pedidos = pedidos.Where(p => p.FechaUtc.Date >= desde.Value.Date);
            if (hasta.HasValue) pedidos = pedidos.Where(p => p.FechaUtc.Date <= hasta.Value.Date);

            var query = pedidos.SelectMany(p => p.Detalles);
            if (categoriaId.HasValue && categoriaId.Value > 0)
            {
                query = query.Where(d => _productos.Any(p => p.Id == d.ProductoId && p.CategoriaId == categoriaId.Value));
            }

            return query
                .GroupBy(d => d.ProductoNombre)
                .Select(g => new ProductoVendidoViewModel
                {
                    Producto = g.Key,
                    Cantidad = g.Sum(x => x.Cantidad),
                    Total = g.Sum(x => x.Subtotal)
                })
                .OrderByDescending(x => x.Cantidad)
                .ToList();
        }
    }

    public IEnumerable<MovimientoInventario> FiltrarMovimientos(DateTime? desde, DateTime? hasta, int? productoId)
    {
        lock (_sync)
        {
            LoadCatalogState();
            var query = _movimientos.AsEnumerable();
            if (desde.HasValue) query = query.Where(m => m.FechaUtc.Date >= desde.Value.Date);
            if (hasta.HasValue) query = query.Where(m => m.FechaUtc.Date <= hasta.Value.Date);
            if (productoId.HasValue && productoId.Value > 0) query = query.Where(m => m.ProductoId == productoId.Value);
            return query.OrderByDescending(m => m.FechaUtc).ToList();
        }
    }

    public Usuario? UsuarioPedido(int pedidoUsuarioId) => FindUser(pedidoUsuarioId);

    public (bool Ok, string Message) SaveCategory(CategoriaFormViewModel form)
    {
        lock (_sync)
        {
            LoadCatalogState();
            var categoriesSnapshot = _categorias.Select(Clone).ToList();
            var categoryIdSnapshot = _categoriaId;

            if (_categorias.Any(c => c.Id != form.Id && c.Nombre.Equals(form.Nombre.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                return (false, "Ya existe una categoria con ese nombre.");
            }

            if (form.Id == 0)
            {
                _categorias.Add(new Categoria { Id = _categoriaId++, Nombre = form.Nombre.Trim(), Descripcion = form.Descripcion.Trim(), Activa = true });
                try
                {
                    SaveCatalogState();
                }
                catch
                {
                    _categorias.Clear();
                    _categorias.AddRange(categoriesSnapshot);
                    _categoriaId = categoryIdSnapshot;
                    throw;
                }

                return (true, "Categoria creada.");
            }

            var category = _categorias.FirstOrDefault(c => c.Id == form.Id);
            if (category is null) return (false, "Categoria no encontrada.");
            category.Nombre = form.Nombre.Trim();
            category.Descripcion = form.Descripcion.Trim();
            category.Activa = true;
            try
            {
                SaveCatalogState();
            }
            catch
            {
                _categorias.Clear();
                _categorias.AddRange(categoriesSnapshot);
                _categoriaId = categoryIdSnapshot;
                throw;
            }

            return (true, "Categoria actualizada.");
        }
    }

    public (bool Ok, string Message) DeleteCategory(int id)
    {
        lock (_sync)
        {
            LoadCatalogState();
            if (_productos.Any(p => p.CategoriaId == id && p.Activo))
            {
                return (false, "No se puede eliminar una categoria con productos activos.");
            }

            var category = _categorias.FirstOrDefault(c => c.Id == id);
            if (category is null) return (false, "Categoria no encontrada.");
            category.Activa = false;
            SaveCatalogState();
            return (true, "Categoria desactivada.");
        }
    }

    private List<CarritoItem> GetCartItems(int userId)
    {
        if (!_carritos.TryGetValue(userId, out var cart))
        {
            cart = [];
            _carritos[userId] = cart;
        }

        return cart;
    }

    private void Seed()
    {
        LoadCatalogState();
    }

    private void LoadCatalogState()
    {
        var state = _catalogRepository.Load();

        _categorias.Clear();
        _categorias.AddRange(state.Categorias);
        _productos.Clear();
        _productos.AddRange(state.Productos);
        _movimientos.Clear();
        _movimientos.AddRange(state.Movimientos);

        _categoriaId = _categorias.Count == 0 ? 1 : _categorias.Max(c => c.Id) + 1;
        _productoId = _productos.Count == 0 ? 1 : _productos.Max(p => p.Id) + 1;
    }

    private void SaveCatalogState()
    {
        _catalogRepository.Save(new CatalogState
        {
            Categorias = _categorias,
            Productos = _productos,
            Movimientos = _movimientos
        });
        LoadCatalogState();
    }

    private static Producto Clone(Producto p) => new()
    {
        Id = p.Id,
        CategoriaId = p.CategoriaId,
        Nombre = p.Nombre,
        Descripcion = p.Descripcion,
        Precio = p.Precio,
        Stock = p.Stock,
        StockMinimo = p.StockMinimo,
        ImagenUrl = p.ImagenUrl,
        Activo = p.Activo,
        ActualizadoUtc = p.ActualizadoUtc
    };

    private static Categoria Clone(Categoria c) => new()
    {
        Id = c.Id,
        Nombre = c.Nombre,
        Descripcion = c.Descripcion,
        Activa = c.Activa
    };

    private static Pedido Clone(Pedido p) => new()
    {
        Id = p.Id,
        Numero = p.Numero,
        UsuarioId = p.UsuarioId,
        Estado = p.Estado,
        TipoEntrega = p.TipoEntrega,
        DireccionEntrega = p.DireccionEntrega,
        FechaUtc = p.FechaUtc,
        CanceladoUtc = p.CanceladoUtc,
        Total = p.Total,
        Detalles = p.Detalles.Select(d => new PedidoDetalle { ProductoId = d.ProductoId, ProductoNombre = d.ProductoNombre, Cantidad = d.Cantidad, PrecioUnitario = d.PrecioUnitario }).ToList(),
        Historial = p.Historial.Select(h => new HistorialEstado { Estado = h.Estado, FechaUtc = h.FechaUtc }).ToList()
    };

    private static Usuario Clone(Usuario u) => new()
    {
        Id = u.Id,
        Nombre = u.Nombre,
        Correo = u.Correo,
        Telefono = u.Telefono,
        Direccion = u.Direccion,
        Rol = u.Rol,
        IntentosFallidos = u.IntentosFallidos,
        BloqueadoHastaUtc = u.BloqueadoHastaUtc
    };

    private static MovimientoInventario Clone(MovimientoInventario m) => new()
    {
        FechaUtc = m.FechaUtc,
        ProductoId = m.ProductoId,
        ProductoNombre = m.ProductoNombre,
        Tipo = m.Tipo,
        Cantidad = m.Cantidad,
        Motivo = m.Motivo
    };

    private static bool IsValidPhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return true;
        var trimmed = phone.Trim();
        return trimmed.Length is >= 7 and <= 20 && trimmed.All(c => char.IsDigit(c) || c is '+' or '-' or ' ');
    }
}
