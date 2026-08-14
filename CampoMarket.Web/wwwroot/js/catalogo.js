
(() => {
    const form = document.getElementById('catalogFilters');
    const results = document.getElementById('catalogResults');
    const token = document.querySelector('#catalogAntiForgery input[name="__RequestVerificationToken"]')?.value ?? '';
    let timer;

    const render = (items) => {
        results.innerHTML = items.map(producto => {
            const buy = producto.puedeComprar
                ? `<form method="post" action="/carrito/agregar" class="flex-fill">
                               <input name="__RequestVerificationToken" type="hidden" value="${token}" />
                               <input type="hidden" name="productoId" value="${producto.id}" />
                               <input type="hidden" name="cantidad" value="1" />
                               <button class="btn btn-primary w-100" type="submit"><i class="bi bi-basket"></i> Agregar</button>
                           </form>`
                : '';
            return `<div class="col-md-6 col-xl-4">
                        <article class="card h-100 product-card">
                            <img class="card-img-top" src="${producto.imagenUrl}" alt="${producto.nombre}" />
                            <div class="card-body d-flex flex-column">
                                <div class="d-flex justify-content-between gap-2">
                                    <h2 class="h5">${producto.nombre}</h2>
                                    <span class="price">${producto.precio}</span>
                                </div>
                                <p class="text-muted">${producto.descripcion}</p>
                                <div class="small mb-3">Stock: ${producto.stock}</div>
                                <div class="mt-auto d-flex gap-2">
                                    <a class="btn btn-outline-primary flex-fill" href="/catalogo/producto/${producto.id}">Ver</a>
                                    ${buy}
                                </div>
                            </div>
                        </article>
                    </div>`;
        }).join('');
    };

    const search = () => {
        const data = new URLSearchParams(new FormData(form));
        fetch(`/catalogo/buscar-json?${data.toString()}`)
            .then(response => response.json())
            .then(payload => render(payload.productos));
    };

    form.addEventListener('input', () => {
        clearTimeout(timer);
        timer = setTimeout(search, 250);
    });
    form.addEventListener('change', search);
})();
