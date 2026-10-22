"use strict";

(() => {
    const { esc, icono, cantidad, fechaHora, api, toast, abrir, cerrar, limpiarErrores, leerFormulario, enviar, boton, llenarSelect, debounce, Listado } = Spin;

    const tabla = document.getElementById("tabla");
    const puedeAlmacen = tabla.dataset.puedeAlmacen === "true";
    const filtros = document.getElementById("filtros");
    const parametros = new URLSearchParams(location.search);

    const ENTRADAS = new Set([1, 2, 5]);
    const REQUIEREN_MOTIVO = new Set([4, 5, 6, 7]);

    // ------------------------------------------------------------ listado
    const listado = new Listado({
        url: "/api/movimientos",
        tabla,
        filtros,
        estadoInicial: "",
        tamanoPagina: 20,
        pie: document.getElementById("pie"),
        textoVacio: "Aún no hay movimientos. Empiece registrando el stock inicial de cada insumo.",
        columnas: [
            { titulo: "N°", clase: "mono cel-tenue", render: (m) => esc(m.id) },
            { titulo: "Fecha", render: (m) => esc(fechaHora(m.fecha)) },
            {
                titulo: "Tipo",
                render: (m) => `<span class="mov-tipo ${m.signo > 0 ? "entrada" : "salida"}"><i></i>${esc(m.tipoNombre)}</span>` +
                    (m.revertido ? `<span class="cel-sub">Revertido</span>` : "") +
                    (m.referencia ? `<span class="cel-sub">${esc(m.referencia)}</span>` : "")
            },
            {
                titulo: "Insumo",
                render: (m) => `<span class="cel-principal">${esc(m.insumoNombre)}</span><span class="cel-sub">${esc(m.insumoCodigo)}</span>`
            },
            { titulo: "Almacén", clase: "cel-codigo", render: (m) => esc(m.almacenCodigo) },
            {
                titulo: "Cantidad",
                clase: "num",
                render: (m) => `<span class="cant-mov ${m.signo > 0 ? "entrada" : "salida"}">${m.signo > 0 ? "+" : "−"}${cantidad(m.cantidad)}</span> <span class="cel-tenue">${esc(m.unidadConsumoCodigo)}</span>`
            },
            { titulo: "Saldo", clase: "num", render: (m) => cantidad(m.saldoAlmacen) },
            {
                titulo: "Documento / motivo",
                render: (m) => [m.documento ? `<span>${esc(m.documento)}</span>` : "", m.motivo ? `<span class="cel-sub">${esc(m.motivo)}</span>` : ""].join("") || `<span class="cel-tenue">—</span>`
            },
            { titulo: "Usuario", render: (m) => `<span class="cel-tenue">${esc(m.usuario)}</span>` }
        ],
        acciones: puedeAlmacen
            ? (m) => boton("revertir", "deshacer", m.reversible ? "Revertir movimiento" : "No se puede revertir", { deshabilitado: !m.reversible })
            : null,
        alAccion: (accion, m) => { if (accion === "revertir") abrirRevertir(m); },
        alCargar: (d) => {
            tabla.tBodies[0].querySelectorAll("tr[data-id]").forEach((tr) => {
                const m = d.items.find((x) => String(x.id) === tr.dataset.id);
                tr.classList.toggle("is-revertido", Boolean(m?.revertido));
            });
        }
    });

    // ------------------------------------------------------------ registrar movimiento
    const dlg = document.getElementById("dlgMovimiento");
    const form = dlg.querySelector("form");
    const selAlmacen = document.getElementById("mAlmacen");
    const selInsumo = document.getElementById("mInsumo");
    const inCantidad = document.getElementById("mCantidad");
    const campoUnidad = document.getElementById("campoUnidad");
    const previsto = document.getElementById("previsto");
    let insumos = [];          // opciones con unidades y factor
    let stockActual = null;    // stock del insumo en el almacén elegido

    const insumoElegido = () => insumos.find((i) => i.insumoId === Number(selInsumo.value));
    const tipoElegido = () => Number(form.querySelector('input[name="tipo"]:checked')?.value ?? 0);
    const enCompra = () => form.querySelector('input[name="unidadIngreso"]:checked')?.value === "compra";

    async function cargarInsumos() {
        if (insumos.length) return;
        // El endpoint de stock trae unidades y factor de todos los insumos activos.
        const r = await api("/api/stock?tamanoPagina=100&pagina=1");
        if (!r.ok) { toast(r.message, "error"); return; }
        insumos = r.data.items;
        let pagina = 2;
        while (insumos.length < r.data.total && pagina <= r.data.totalPaginas) {
            const mas = await api(`/api/stock?tamanoPagina=100&pagina=${pagina++}`);
            if (!mas.ok) break;
            insumos = insumos.concat(mas.data.items);
        }
        selInsumo.innerHTML = `<option value="">Seleccione…</option>` +
            insumos.map((i) => `<option value="${i.insumoId}">${esc(i.codigo)} · ${esc(i.nombre)}</option>`).join("");
    }

    function actualizarUnidad() {
        const i = insumoElegido();
        const distinta = i && i.unidadCompraCodigo !== i.unidadConsumoCodigo && i.factorConversion !== 1;
        campoUnidad.hidden = !distinta;
        if (!distinta) form.querySelector('input[name="unidadIngreso"][value="consumo"]').checked = true;
        if (i) {
            form.querySelector("[data-u-consumo]").textContent = i.unidadConsumoCodigo;
            form.querySelector("[data-u-compra]").textContent = `${i.unidadCompraCodigo} (× ${cantidad(i.factorConversion)})`;
        }
    }

    function actualizarMotivo() {
        const requerido = REQUIEREN_MOTIVO.has(tipoElegido());
        document.getElementById("motivoReq").hidden = !requerido;
        form.elements.motivo.placeholder = requerido ? "Obligatorio: queda en el kardex" : "Opcional";
    }

    const consultarStock = debounce(async () => {
        stockActual = null;
        const i = insumoElegido();
        if (i && selAlmacen.value) {
            const r = await api(`/api/stock/cantidad?insumoId=${i.insumoId}&almacenId=${selAlmacen.value}`);
            if (r.ok) stockActual = Number(r.data);
        }
        actualizarPrevisto();
    }, 150);

    function actualizarPrevisto() {
        const i = insumoElegido();
        const tipo = tipoElegido();
        if (!i || !selAlmacen.value || stockActual === null) { previsto.hidden = true; return; }
        const almacen = selAlmacen.selectedOptions[0]?.textContent.split(" · ")[0] ?? "";
        const ingresado = Number(inCantidad.value) || 0;
        const enConsumo = enCompra() ? ingresado * i.factorConversion : ingresado;
        const signo = tipo ? (ENTRADAS.has(tipo) ? 1 : -1) : 0;
        const despues = stockActual + signo * enConsumo;
        const u = esc(i.unidadConsumoCodigo);

        previsto.hidden = false;
        previsto.classList.toggle("is-error", despues < 0);
        let html = `<span>Stock actual en ${esc(almacen)}: <b>${cantidad(stockActual)}</b> ${u}</span>`;
        if (tipo && ingresado > 0) {
            html += `<span>${icono("flecha")} después: <b>${cantidad(Math.round(despues * 1e6) / 1e6)}</b> ${u}</span>`;
            if (enCompra()) html += `<span class="cel-tenue">(${cantidad(ingresado)} ${esc(i.unidadCompraCodigo)} = ${cantidad(enConsumo)} ${u})</span>`;
            if (despues < 0) html += `<span>No alcanza el stock para esta salida.</span>`;
        }
        if (tipo === 1 && stockActual !== 0) html += `<span>Si ya tiene movimientos, use una entrada o un ajuste.</span>`;
        previsto.innerHTML = html;
    }

    form.addEventListener("change", (e) => {
        if (e.target.name === "insumoId") { actualizarUnidad(); consultarStock(); }
        else if (e.target.name === "almacenId") consultarStock();
        else if (e.target.name === "tipo") { actualizarMotivo(); actualizarPrevisto(); document.getElementById("tiposMov").classList.remove("is-invalid"); }
        else if (e.target.name === "unidadIngreso") actualizarPrevisto();
    });
    inCantidad.addEventListener("input", actualizarPrevisto);

    async function abrirRegistro(insumoId) {
        await Promise.all([
            cargarInsumos(),
            llenarSelect(selAlmacen, "/api/almacenes/opciones?soloActivos=true&soloInsumos=true", { vacio: "Seleccione…" })
        ]);
        form.reset();
        limpiarErrores(form);
        stockActual = null;
        previsto.hidden = true;
        if (selAlmacen.options.length === 2) selAlmacen.selectedIndex = 1;
        if (insumoId) selInsumo.value = String(insumoId);
        actualizarUnidad();
        actualizarMotivo();
        consultarStock();
        abrir(dlg);
        form.querySelector('input[name="tipo"]').focus();
    }

    form.addEventListener("submit", async (e) => {
        e.preventDefault();
        const datos = leerFormulario(form);
        const cuerpo = {
            tipo: datos.tipo ?? 0,
            almacenId: datos.almacenId,
            insumoId: datos.insumoId,
            cantidad: datos.cantidad,
            enUnidadCompra: enCompra(),
            documento: datos.documento,
            motivo: datos.motivo
        };
        const r = await enviar(form, "/api/movimientos", "POST", cuerpo);
        if (!r) return;
        cerrar(dlg);
        toast(r.message);
        listado.cargar();
    });

    // ------------------------------------------------------------ revertir
    const dlgRev = document.getElementById("dlgRevertir");
    const formRev = dlgRev.querySelector("form");
    let revirtiendo = null;

    function abrirRevertir(m) {
        revirtiendo = m;
        formRev.reset();
        limpiarErrores(formRev);
        const inverso = m.signo > 0 ? "saldrán" : "volverán a entrar";
        dlgRev.querySelector("[data-resumen]").innerHTML =
            `Movimiento N° ${esc(m.id)}: <b>${esc(m.tipoNombre)}</b> de ${cantidad(m.cantidad)} ${esc(m.unidadConsumoCodigo)} de ${esc(m.insumoNombre)} en ${esc(m.almacenCodigo)}. ` +
            `Se registrará un movimiento inverso: ${cantidad(m.cantidad)} ${esc(m.unidadConsumoCodigo)} ${inverso} del stock.`;
        abrir(dlgRev);
        formRev.elements.motivo.focus();
    }

    formRev.addEventListener("submit", async (e) => {
        e.preventDefault();
        const r = await enviar(formRev, `/api/movimientos/${revirtiendo.id}/revertir`, "POST");
        if (!r) return;
        cerrar(dlgRev);
        toast(r.message);
        listado.cargar();
    });

    // ------------------------------------------------------------ inicio
    document.getElementById("btnNuevo")?.addEventListener("click", () => abrirRegistro());
    llenarSelect(document.getElementById("fAlmacen"), "/api/almacenes/opciones", { vacio: "Todos" });
    listado.cargar();
    if (puedeAlmacen && parametros.get("nuevo") === "1") abrirRegistro(Number(parametros.get("insumo")) || null);
})();
