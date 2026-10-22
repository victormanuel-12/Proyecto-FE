"use strict";

(() => {
    const { esc, icono, numero, cantidad, fechaHora, api, toast, abrir, cerrar, limpiarErrores, leerFormulario, enviar, llenarSelect, debounce, Listado } = Spin;

    const tabla = document.getElementById("tabla");
    const puedeAlmacen = tabla.dataset.puedeAlmacen === "true";
    const paquetes = (n) => cantidad(Math.round(n * 100) / 100);
    const ENTRADAS = new Set([1, 2, 4]);

    function cobertura(p) {
        if (p.paquetes <= 0) return `<span class="badge badge-crit">${icono("error")}Sin stock</span>`;
        if (p.diasDeVenta === null) return `<span class="cel-tenue">Sin ventas recientes</span>`;
        const dias = `${cantidad(p.diasDeVenta)} días`;
        if (p.diasDeVenta < 3) return `<span class="badge badge-warn">${icono("alerta")}${dias}</span>`;
        return `<span class="badge badge-ok">${icono("check")}${dias}</span>`;
    }

    // ------------------------------------------------------------ stock
    const listado = new Listado({
        url: "/api/producto-terminado",
        tabla,
        filtros: document.getElementById("filtros"),
        segmentos: document.getElementById("segmentos"),
        estadoInicial: "",
        pie: document.getElementById("pie"),
        textoVacio: "No hay productos activos. Regístrelos en Productos y recetas.",
        columnas: [
            { titulo: "SKU", clase: "cel-codigo", render: (p) => esc(p.codigo) },
            { titulo: "Producto", render: (p) => `<span class="cel-principal">${esc(p.nombre)}</span><span class="cel-sub">${esc(p.formatoNombre)} · ${p.unidadesPorPaquete} por paquete</span>` },
            {
                titulo: "Stock",
                clase: "num cel-stock",
                render: (p) => `<b>${paquetes(p.paquetes)}</b><span class="u">paq.</span>` +
                    (p.paquetes > 0 ? `<span class="cel-sub">${numero(Math.round(p.botellas))} botellas · ${cantidad(p.pallets)} pallets</span>` : "")
            },
            {
                titulo: "Por almacén",
                render: (p) => p.almacenes.length
                    ? `<span class="chips">${p.almacenes.map((a) => `<span class="chip" title="${esc(a.almacenNombre)}">${esc(a.almacenCodigo)}: ${paquetes(a.cantidad)}</span>`).join("")}</span>`
                    : `<span class="cel-tenue">—</span>`
            },
            { titulo: "Venta diaria", clase: "num", render: (p) => p.ventaDiariaPromedio > 0 ? `${paquetes(p.ventaDiariaPromedio)} <span class="cel-tenue">paq./día</span>` : `<span class="cel-tenue">—</span>` },
            { titulo: "Cobertura", render: cobertura },
            { titulo: "Último movimiento", render: (p) => p.ultimoMovimiento ? fechaHora(p.ultimoMovimiento) : `<span class="cel-tenue">Sin movimientos</span>` }
        ],
        acciones: (p) =>
            `<button type="button" class="icon-btn" data-accion="movimientos" title="Ver movimientos" aria-label="Ver movimientos">${icono("libro")}</button>` +
            `<a class="icon-btn" href="/ventas?producto=${p.productoId}" title="Ver ventas" aria-label="Ver ventas">${icono("carrito")}</a>` +
            (puedeAlmacen ? `<button type="button" class="icon-btn" data-accion="registrar" title="Registrar movimiento" aria-label="Registrar movimiento">${icono("flechas")}</button>` : ""),
        alAccion: (accion, p) => {
            if (accion === "movimientos") {
                fmProducto.value = String(p.productoId);
                movimientos.estado.pagina = 1;
                movimientos.cargar();
                document.getElementById("seccionMovimientos").scrollIntoView({ behavior: "smooth", block: "start" });
            } else if (accion === "registrar") abrirRegistro(p.productoId);
        }
    });

    // ------------------------------------------------------------ movimientos
    const fmProducto = document.getElementById("fmProducto");
    const movimientos = new Listado({
        url: "/api/producto-terminado/movimientos",
        tabla: document.getElementById("tablaMov"),
        filtros: document.getElementById("filtrosMov"),
        estadoInicial: "",
        tamanoPagina: 15,
        pie: document.getElementById("pieMov"),
        textoVacio: "Aún no hay movimientos de producto terminado.",
        columnas: [
            { titulo: "N°", clase: "mono cel-tenue", render: (m) => esc(m.id) },
            { titulo: "Fecha", render: (m) => esc(fechaHora(m.fecha)) },
            {
                titulo: "Tipo",
                render: (m) => `<span class="mov-tipo ${m.signo > 0 ? "entrada" : "salida"}"><i></i>${esc(m.tipoNombre)}</span>` +
                    (m.revertido ? `<span class="cel-sub">Revertido</span>` : "") +
                    (m.referencia ? `<span class="cel-sub">${esc(m.referencia)}</span>` : "")
            },
            { titulo: "Producto", render: (m) => `<span class="cel-principal">${esc(m.productoNombre)}</span><span class="cel-sub">${esc(m.productoCodigo)}</span>` },
            { titulo: "Almacén", clase: "cel-codigo", render: (m) => esc(m.almacenCodigo) },
            { titulo: "Paquetes", clase: "num", render: (m) => `<span class="cant-mov ${m.signo > 0 ? "entrada" : "salida"}">${m.signo > 0 ? "+" : "−"}${paquetes(m.paquetes)}</span>` },
            { titulo: "Saldo", clase: "num", render: (m) => paquetes(m.saldoAlmacen) },
            {
                titulo: "Documento / motivo",
                render: (m) => [m.documento ? `<span>${esc(m.documento)}</span>` : "", m.motivo ? `<span class="cel-sub">${esc(m.motivo)}</span>` : ""].join("") || `<span class="cel-tenue">—</span>`
            },
            { titulo: "Usuario", render: (m) => `<span class="cel-tenue">${esc(m.usuario)}</span>` }
        ]
    });

    // ------------------------------------------------------------ registrar
    const dlg = document.getElementById("dlgMovimiento");
    const form = dlg?.querySelector("form");
    const selAlmacen = document.getElementById("mAlmacen");
    const selProducto = document.getElementById("mProducto");
    const inPaquetes = document.getElementById("mPaquetes");
    const previsto = document.getElementById("previsto");
    let stockActual = null;

    const tipoElegido = () => Number(form.querySelector('input[name="tipo"]:checked')?.value ?? 0);

    const consultarStock = debounce(async () => {
        stockActual = null;
        if (selProducto.value && selAlmacen.value) {
            const r = await api(`/api/producto-terminado/cantidad?productoId=${selProducto.value}&almacenId=${selAlmacen.value}`);
            if (r.ok) stockActual = Number(r.data);
        }
        actualizarPrevisto();
    }, 150);

    function actualizarPrevisto() {
        if (stockActual === null) { previsto.hidden = true; return; }
        const tipo = tipoElegido();
        const n = Number(inPaquetes.value) || 0;
        const despues = Math.round((stockActual + (tipo ? (ENTRADAS.has(tipo) ? 1 : -1) : 0) * n) * 10000) / 10000;
        previsto.hidden = false;
        previsto.classList.toggle("is-error", despues < 0);
        let html = `<span>Stock actual: <b>${paquetes(stockActual)}</b> paquetes</span>`;
        if (tipo && n > 0) html += `<span>${icono("flecha")} después: <b>${paquetes(despues)}</b></span>`;
        if (despues < 0) html += `<span>No alcanza el stock para esta salida.</span>`;
        if (tipo === 1 && stockActual !== 0) html += `<span>Si ya tiene movimientos, use un ajuste.</span>`;
        previsto.innerHTML = html;
    }

    function actualizarMotivo() {
        const requerido = tipoElegido() > 1;
        document.getElementById("motivoReq").hidden = !requerido;
        form.elements.motivo.placeholder = requerido ? "Obligatorio: queda en el historial" : "Opcional";
    }

    async function abrirRegistro(productoId) {
        await llenarSelect(selAlmacen, "/api/almacenes/opciones?soloActivos=true&soloProductoTerminado=true", { vacio: "Seleccione…" });
        if (selAlmacen.options.length < 2) {
            toast("No hay almacenes de producto terminado activos. Regístrelos en Almacenes.", "error");
            return;
        }
        await llenarSelect(selProducto, "/api/productos/opciones?soloActivos=true", { vacio: "Seleccione…" });
        form.reset();
        limpiarErrores(form);
        stockActual = null;
        previsto.hidden = true;
        if (selAlmacen.options.length === 2) selAlmacen.selectedIndex = 1;
        if (productoId) selProducto.value = String(productoId);
        actualizarMotivo();
        consultarStock();
        abrir(dlg);
        form.querySelector('input[name="tipo"]').focus();
    }

    if (form) {
        form.addEventListener("change", (e) => {
            if (e.target.name === "productoId" || e.target.name === "almacenId") consultarStock();
            else if (e.target.name === "tipo") { actualizarMotivo(); actualizarPrevisto(); }
        });
        inPaquetes.addEventListener("input", actualizarPrevisto);
        form.addEventListener("submit", async (e) => {
            e.preventDefault();
            const d = leerFormulario(form);
            const r = await enviar(form, "/api/producto-terminado/movimientos", "POST", {
                tipo: d.tipo ?? 0, almacenId: d.almacenId, productoId: d.productoId, paquetes: d.paquetes, documento: d.documento, motivo: d.motivo
            });
            if (!r) return;
            cerrar(dlg);
            toast(r.message);
            listado.cargar();
            movimientos.cargar();
        });
    }

    // ------------------------------------------------------------ inicio
    document.getElementById("btnNuevo")?.addEventListener("click", () => abrirRegistro());
    llenarSelect(document.getElementById("fAlmacen"), "/api/almacenes/opciones?soloProductoTerminado=true", { vacio: "Todos" });
    llenarSelect(fmProducto, "/api/productos/opciones?soloActivos=false", { vacio: "Todos" }).then(() => movimientos.cargar());
    listado.cargar();
})();
