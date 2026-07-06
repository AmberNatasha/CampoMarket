using CampoMarket.Web.Models;

namespace CampoMarket.Web.Services;

public sealed class CampoMarketStore
{
    private readonly object _sync = new();
    private readonly List<Usuario> _usuarios = [];
    private readonly List<DireccionCliente> _direcciones = [];
    private readonly List<Categoria> _categorias = [];
    private readonly List<Producto> _productos = [];
    private readonly List<Pedido> _pedidos = [];
    private readonly List<MovimientoInventario> _movimientos = [];
    private readonly List<AuditLogItem> _auditLogs = [];
    private readonly List<LogErrorItem> _errorLogs = [];
    private readonly List<PasswordResetToken> _resetTokens = [];
    private readonly Dictionary<int, List<CarritoItem>> _carritos = [];
    private int _usuarioId = 1;
    private int _direccionId = 1;
    private int _categoriaId = 1;
    private int _productoId = 1;
    private int _pedidoId = 1;

    public CampoMarketStore()
    {
        Seed();
    }

    public IReadOnlyList<Categoria> Categorias
    {
        get { lock (_sync) return _categorias.ToList(); }
    }

    public IReadOnlyList<Producto> Productos
    {
        get { lock (_sync) return _productos.Select(Clone).ToList(); }
    }

    public IReadOnlyList<Pedido> Pedidos
    {
        get { lock (_sync) return _pedidos.Select(Clone).ToList(); }
    }

    public IReadOnlyList<MovimientoInventario> Movimientos
    {
        get { lock (_sync) return _movimientos.ToList(); }
    }

    public IReadOnlyList<AuditLogItem> AuditLogs
    {
        get { lock (_sync) return _auditLogs.ToList(); }
    }

    public IReadOnlyList<LogErrorItem> ErrorLogs
    {
        get { lock (_sync) return _errorLogs.ToList(); }
    }

    public Usuario? FindUserByEmail(string correo)
    {
        lock (_sync) return _usuarios.FirstOrDefault(u => u.Correo.Equals(correo, StringComparison.OrdinalIgnoreCase));
    }

    public Usuario? FindUser(int id)
    {
        lock (_sync) return _usuarios.FirstOrDefault(u => u.Id == id);
    }

    public (bool Ok, string Message, Usuario? User) Register(string nombre, string correo, string password, string telefono, string direccion)
    {
        lock (_sync)
        {
            if (_usuarios.Any(u => u.Correo.Equals(correo, StringComparison.OrdinalIgnoreCase)))
            {
                return (false, "Ese correo ya esta registrado.", null);
            }

            var user = new Usuario
            {
                Id = _usuarioId++,
                Nombre = nombre.Trim(),
                Correo = correo.Trim().ToLowerInvariant(),
                Telefono = telefono.Trim(),
                Direccion = direccion.Trim(),
                Rol = RolesCampo.Cliente,
                PasswordHash = PasswordService.Hash(password)
            };
            _usuarios.Add(user);
            if (!string.IsNullOrWhiteSpace(direccion))
            {
                _direcciones.Add(new DireccionCliente { Id = _direccionId++, UsuarioId = user.Id, Alias = "Casa", Detalle = direccion, Predeterminada = true });
            }

            return (true, "Cuenta creada. Ya puedes iniciar sesion.", user);
        }
    }

    public (bool Ok, string Message, Usuario? User) Login(string correo, string password, string ip = "")
    {
        lock (_sync)
        {
            var user = FindUserByEmail(correo);
            if (user is null)
            {
                _auditLogs.Add(new AuditLogItem { Correo = correo, Ip = ip, Evento = "Login fallido: correo no registrado" });
                return (false, "Correo o contrasena incorrectos.", null);
            }

            if (user.BloqueadoHastaUtc > DateTime.UtcNow)
            {
                _auditLogs.Add(new AuditLogItem { Correo = correo, Ip = ip, Evento = "Login bloqueado temporalmente" });
                return (false, "La cuenta esta bloqueada temporalmente por intentos fallidos.", null);
            }

            if (!PasswordService.Verify(password, user.PasswordHash))
            {
                user.IntentosFallidos++;
                _auditLogs.Add(new AuditLogItem { Correo = correo, Ip = ip, Evento = $"Login fallido #{user.IntentosFallidos}" });
                if (user.IntentosFallidos >= 5)
                {
                    user.BloqueadoHastaUtc = DateTime.UtcNow.AddMinutes(15);
                }

                return (false, "Correo o contrasena incorrectos.", null);
            }

            user.IntentosFallidos = 0;
            user.BloqueadoHastaUtc = null;
            _auditLogs.Add(new AuditLogItem { Correo = correo, Ip = ip, Evento = "Login exitoso" });
            return (true, "Sesion iniciada.", user);
        }
    }

    public void LogError(string ruta, string mensaje)
    {
        lock (_sync)
        {
            _errorLogs.Add(new LogErrorItem { Ruta = ruta, Mensaje = mensaje });
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

            var user = _usuarios.FirstOrDefault(u => u.Id == userId);
            if (user is null) return (false, "Usuario no encontrado.");
            user.Nombre = nombre.Trim();
            user.Telefono = telefono.Trim();
            user.Direccion = direccion.Trim();
            return (true, "Perfil actualizado.");
        }
    }

    public (bool Ok, string Message) ChangePassword(int userId, string actual, string nuevo)
    {
        lock (_sync)
        {
            var user = _usuarios.FirstOrDefault(u => u.Id == userId);
            if (user is null) return (false, "Usuario no encontrado.");
            if (!PasswordService.Verify(actual, user.PasswordHash))
            {
                return (false, "La contrasena actual no coincide.");
            }

            user.PasswordHash = PasswordService.Hash(nuevo);
            return (true, "Contrasena actualizada.");
        }
    }

    public (bool Ok, string Message, string? Token) RequestPasswordReset(string correo)
    {
        lock (_sync)
        {
            var user = _usuarios.FirstOrDefault(u => u.Correo.Equals(correo, StringComparison.OrdinalIgnoreCase));
            if (user is null)
            {
                return (true, "Si el correo existe, se genero un enlace temporal.", null);
            }

            var token = Convert.ToHexString(Guid.NewGuid().ToByteArray()).ToLowerInvariant();
            _resetTokens.Add(new PasswordResetToken
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
            var reset = _resetTokens.LastOrDefault(t => t.Token == token && !t.Usado);
            if (reset is null || reset.ExpiraUtc < DateTime.UtcNow)
            {
                return (false, "El token no existe o ya expiro.");
            }

            var user = _usuarios.FirstOrDefault(u => u.Id == reset.UsuarioId);
            if (user is null) return (false, "Usuario no encontrado.");
            user.PasswordHash = PasswordService.Hash(nuevo);
            reset.Usado = true;
            return (true, "Contrasena restablecida. Ya puedes iniciar sesion.");
        }
    }

    public IEnumerable<Producto> BuscarProductos(string? categoria, string? buscar, string? orden)
    {
        lock (_sync)
        {
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
        lock (_sync) return _productos.FirstOrDefault(p => p.Id == id) is { } p ? Clone(p) : null;
    }

    public void SaveProduct(ProductoFormViewModel form)
    {
        lock (_sync)
        {
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
                    Activo = true,
                    ActualizadoUtc = DateTime.UtcNow
                });
                return;
            }

            var product = _productos.First(p => p.Id == form.Id);
            product.Nombre = form.Nombre;
            product.Descripcion = form.Descripcion;
            product.Precio = form.Precio;
            product.Stock = form.Stock;
            product.StockMinimo = form.StockMinimo;
            product.CategoriaId = form.CategoriaId;
            product.ImagenUrl = string.IsNullOrWhiteSpace(form.ImagenUrl) ? product.ImagenUrl : form.ImagenUrl;
            product.ActualizadoUtc = DateTime.UtcNow;
        }
    }

    public (bool Ok, string Message) DeactivateProduct(int id)
    {
        lock (_sync)
        {
            var hasActiveOrders = _pedidos.Any(p => p.Estado is not EstadosPedido.Entregado and not EstadosPedido.Cancelado && p.Detalles.Any(d => d.ProductoId == id));
            if (hasActiveOrders)
            {
                return (false, "No se puede desactivar: el producto esta en pedidos activos.");
            }

            var product = _productos.FirstOrDefault(p => p.Id == id);
            if (product is null) return (false, "Producto no encontrado.");
            product.Activo = false;
            return (true, "Producto desactivado.");
        }
    }

    public (bool Ok, string Message) AdjustStock(int id, int cantidad, string motivo)
    {
        lock (_sync)
        {
            if (cantidad <= 0) return (false, "El ajuste debe ser positivo.");
            var product = _productos.FirstOrDefault(p => p.Id == id);
            if (product is null) return (false, "Producto no encontrado.");
            product.Stock += cantidad;
            product.ActualizadoUtc = DateTime.UtcNow;
            _movimientos.Add(new MovimientoInventario { ProductoId = id, ProductoNombre = product.Nombre, Tipo = "Ajuste manual", Cantidad = cantidad, Motivo = motivo });
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
        lock (_sync) return _direcciones.Where(d => d.UsuarioId == userId).OrderByDescending(d => d.Predeterminada).ToList();
    }

    public DireccionCliente? FindAddress(int userId, int id)
    {
        lock (_sync) return _direcciones.FirstOrDefault(d => d.UsuarioId == userId && d.Id == id);
    }

    public (bool Ok, string Message) SaveAddress(int userId, DireccionFormViewModel form)
    {
        lock (_sync)
        {
            if (form.Predeterminada)
            {
                foreach (var address in _direcciones.Where(d => d.UsuarioId == userId))
                {
                    address.Predeterminada = false;
                }
            }

            var detalle = $"{form.Provincia}, {form.Canton}, {form.Distrito}. {form.SenasExactas}";
            if (form.Id == 0)
            {
                var hasAddress = _direcciones.Any(d => d.UsuarioId == userId);
                _direcciones.Add(new DireccionCliente
                {
                    Id = _direccionId++,
                    UsuarioId = userId,
                    Alias = form.Alias.Trim(),
                    Provincia = form.Provincia.Trim(),
                    Canton = form.Canton.Trim(),
                    Distrito = form.Distrito.Trim(),
                    SenasExactas = form.SenasExactas.Trim(),
                    Detalle = detalle,
                    Predeterminada = form.Predeterminada || !hasAddress
                });
                return (true, "Direccion agregada.");
            }

            var existing = _direcciones.FirstOrDefault(d => d.UsuarioId == userId && d.Id == form.Id);
            if (existing is null) return (false, "Direccion no encontrada.");
            existing.Alias = form.Alias.Trim();
            existing.Provincia = form.Provincia.Trim();
            existing.Canton = form.Canton.Trim();
            existing.Distrito = form.Distrito.Trim();
            existing.SenasExactas = form.SenasExactas.Trim();
            existing.Detalle = detalle;
            existing.Predeterminada = form.Predeterminada;
            return (true, "Direccion actualizada.");
        }
    }

    public (bool Ok, string Message) DeleteAddress(int userId, int id)
    {
        lock (_sync)
        {
            var address = _direcciones.FirstOrDefault(d => d.UsuarioId == userId && d.Id == id);
            if (address is null) return (false, "Direccion no encontrada.");
            var hasPendingOrder = _pedidos.Any(p => p.UsuarioId == userId && p.Estado is not EstadosPedido.Entregado and not EstadosPedido.Cancelado && p.DireccionEntrega == address.Detalle);
            if (hasPendingOrder) return (false, "No se puede eliminar una direccion usada por pedidos activos.");
            _direcciones.Remove(address);
            if (address.Predeterminada && _direcciones.FirstOrDefault(d => d.UsuarioId == userId) is { } next)
            {
                next.Predeterminada = true;
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
                query = query.Where(p => p.Numero.Contains(text, StringComparison.OrdinalIgnoreCase) || (_usuarios.FirstOrDefault(u => u.Id == p.UsuarioId)?.Nombre.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            return query.OrderByDescending(p => p.FechaUtc).Select(Clone).ToList();
        }
    }

    public IEnumerable<ProductoVendidoViewModel> ProductosMasVendidos(DateTime? desde, DateTime? hasta, int? categoriaId)
    {
        lock (_sync)
        {
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
            if (_categorias.Any(c => c.Id != form.Id && c.Nombre.Equals(form.Nombre.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                return (false, "Ya existe una categoria con ese nombre.");
            }

            if (form.Id == 0)
            {
                _categorias.Add(new Categoria { Id = _categoriaId++, Nombre = form.Nombre.Trim(), Descripcion = form.Descripcion.Trim(), Activa = true });
                return (true, "Categoria creada.");
            }

            var category = _categorias.FirstOrDefault(c => c.Id == form.Id);
            if (category is null) return (false, "Categoria no encontrada.");
            category.Nombre = form.Nombre.Trim();
            category.Descripcion = form.Descripcion.Trim();
            category.Activa = true;
            return (true, "Categoria actualizada.");
        }
    }

    public (bool Ok, string Message) DeleteCategory(int id)
    {
        lock (_sync)
        {
            if (_productos.Any(p => p.CategoriaId == id && p.Activo))
            {
                return (false, "No se puede eliminar una categoria con productos activos.");
            }

            var category = _categorias.FirstOrDefault(c => c.Id == id);
            if (category is null) return (false, "Categoria no encontrada.");
            category.Activa = false;
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
        _usuarios.Add(new Usuario { Id = _usuarioId++, Nombre = "Cliente Demo", Correo = "cliente@campomarket.test", Telefono = "88888888", Direccion = "Barrio Fresco #123", Rol = RolesCampo.Cliente, PasswordHash = PasswordService.Hash("Cliente123!") });
        _usuarios.Add(new Usuario { Id = _usuarioId++, Nombre = "Admin Campo", Correo = "admin@campomarket.test", Telefono = "89999999", Rol = RolesCampo.Admin, PasswordHash = PasswordService.Hash("Admin123!") });
        _direcciones.Add(new DireccionCliente
        {
            Id = _direccionId++,
            UsuarioId = 1,
            Alias = "Casa",
            Provincia = "San Jose",
            Canton = "Central",
            Distrito = "Carmen",
            SenasExactas = "Barrio Fresco #123",
            Detalle = "San Jose, Central, Carmen. Barrio Fresco #123",
            Predeterminada = true
        });

        foreach (var name in new[] { "Frutas", "Verduras", "Lacteos", "Carnes", "Despensa" })
        {
            _categorias.Add(new Categoria { Id = _categoriaId++, Nombre = name, Descripcion = $"Productos de categoria {name.ToLowerInvariant()}" });
        }

        AddSeedProduct("Manzana roja organica", "Fruta crujiente seleccionada para loncheras y postres.", 1, 1.25m, 45, 10, "/Images/Banner.jpg");
        AddSeedProduct("Banano de finca", "Dulce, fresco y listo para batidos o desayuno.", 1, 0.55m, 70, 15, "/Images/Banner.jpg");
        AddSeedProduct("Lechuga romana", "Hojas verdes lavadas y empacadas para ensaladas.", 2, 1.80m, 20, 8, "/Images/Banner.jpg");
        AddSeedProduct("Tomate artesanal", "Tomate de temporada con buen cuerpo y sabor.", 2, 1.10m, 12, 10, "/Images/Banner.jpg");
        AddSeedProduct("Queso fresco", "Queso suave de produccion local.", 3, 4.75m, 16, 5, "/Images/Banner.jpg");
        AddSeedProduct("Pechuga campesina", "Corte fresco para preparaciones familiares.", 4, 6.90m, 9, 6, "/Images/Banner.jpg");
        AddSeedProduct("Miel natural", "Miel clara para bebidas, panes y marinados.", 5, 5.40m, 22, 5, "/Images/Banner.jpg");
    }

    private void AddSeedProduct(string nombre, string descripcion, int categoriaId, decimal precio, int stock, int minimo, string imagen)
    {
        _productos.Add(new Producto { Id = _productoId++, Nombre = nombre, Descripcion = descripcion, CategoriaId = categoriaId, Precio = precio, Stock = stock, StockMinimo = minimo, ImagenUrl = imagen });
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

    private static bool IsValidPhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return true;
        var trimmed = phone.Trim();
        return trimmed.Length is >= 7 and <= 20 && trimmed.All(c => char.IsDigit(c) || c is '+' or '-' or ' ');
    }
}
