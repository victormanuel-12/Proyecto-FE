"use strict";

(() => {
    const { esc, icono, numero, cantidad, fecha, hoyIso, api, toast, abrir, cerrar, limpiarErrores, leerFormulario, enviar, boton, llenarSelect, debounce, Listado } = Spin;

    const tabla = document.getElementById("tabla");
    const puedeVentas = tabla.dataset.puedeVentas === "true";
    const filtros = document.getElementById("filtros");
    const parametros = new URLSearchParams(location.search);
    let productos = [];   // opciones con botellas por paquete

    const paquetes = (n) => cantidad(Math.round(n * 100) / 100);

    // ------------------------------------------------------------ listado
    const listado = new Listado({
        url: "/api/ventas",
        tabla,
        filtros,
        estadoInicial: "",
        tamanoPagina: 20,
        pie: document.getElementById("pie"),
        textoVacio: "Aún no hay ventas. Registre una venta o importe el historial desde Excel.",
        columnas: [
            { titulo: "N°", clase: "mono cel-tenue", render: (v) => esc(v.id) },
            { titulo: "Fecha", render: (v) => esc(fecha(v.fecha)) },
            {
                titulo: "Producto",
                render: (v) => `<span class="cel-principal">${esc(v.productoNombre)}</span><span class="cel-sub">${esc(v.productoCodigo)}</span>`
            },
            { titulo: "Canal", render: (v) => esc(v.canalNombre) },
            {
                titulo: "Paquetes",
                clase: "num",
                render: (v) => `<b>${paquetes(v.paquetes)}</b><span class="cel-sub">${numero(Math.round(v.botellas))} botellas</span>`
            },
            {
                titulo: "Stock",
                render: (v) => v.anulada
                    ? `<span class="badge badge-off">${icono("deshacer")}Anulada</span><span class="cel-sub">${esc(v.motivoAnulacion)}</span>`
                    : v.afectaStock
                        ? `<span class="badge badge-ok">${icono("caja")}Descontó de ${esc(v.almacenCodigo)}</span>`
                        : `<span class="cel-tenue">Solo historial${v.origen === 2 ? " · importada" : ""}</span>`
            },
            { titulo: "Documento", render: (v) => v.documento ? esc(v.documento) : `<span class="cel-tenue">—</span>` },
            { titulo: "Registró", render: (v) => `<span class="cel-tenue">${esc(v.creadoPor)}</span>` }
        ],
        acciones: puedeVentas
            ? (v) => boton("anular", "deshacer", v.anulada ? "Venta anulada" : "Anular venta", { deshabilitado: v.anulada })
            : null,
        alAccion: (accion, v) => { if (accion === "anular") abrirAnular(v); },
        alCargar: (d) => {
            tabla.tBodies[0].querySelectorAll("tr[data-id]").forEach((tr) => {
                const v = d.items.find((x) => String(x.id) === tr.dataset.id);
                tr.classList.toggle("is-revertido", Boolean(v?.anulada));
            });
            const r = d.resumen;
            document.getElementById("resumen").innerHTML = d.total
                ? `En el periodo filtrado: <b>${paquetes(r.paquetes)}</b> paquetes · <b>${numero(Math.round(r.botellas))}</b> botellas · ${numero(r.productos)} producto(s) · ${numero(r.dias)} día(s) con ventas · ${numero(r.registros)} registro(s)`
                : "";
        }
    });

    // ------------------------------------------------------------ gráfico de demanda semanal
    const grafico = document.getElementById("grafico");
    const gProducto = document.getElementById("gProducto");
    const nota = document.querySelector("#historialNota span");

    function semanasElegidas() {
        return Number(document.querySelector('input[name="gSemanas"]:checked')?.value ?? 26);
    }

    async function cargarGrafico() {
        grafico.innerHTML = `<p class="grafico-vacio">Cargando…</p>`;
        const url = `/api/ventas/semanal?semanas=${semanasElegidas()}${gProducto.value ? `&productoId=${gProducto.value}` : ""}`;
        const r = await api(url);
        if (!r.ok) { grafico.innerHTML = `<p class="grafico-vacio">${esc(r.message)}</p>`; return; }
        dibujar(r.data);
    }

    function dibujar(d) {
        const semanas = d.semanas;
        const max = Math.max(...semanas.map((s) => s.paquetes), 0);
        document.getElementById("demandaSub").textContent = `${d.titulo} · paquetes vendidos por semana (lunes a domingo).`;

        if (d.semanasConHistorial >= 52) {
            nota.textContent = `Historial desde el ${fecha(d.primeraVenta)} (${numero(d.semanasConHistorial)} semanas): suficiente para el pronóstico con estacionalidad.`;
        } else if (d.semanasConHistorial > 0) {
            nota.textContent = `Historial desde el ${fecha(d.primeraVenta)} (${numero(d.semanasConHistorial)} semanas). Para captar la estacionalidad (verano, fiestas) el pronóstico necesita al menos 52 semanas.`;
        } else {
            nota.textContent = "Sin historial aún. Importe al menos un año de ventas para que el pronóstico tenga base.";
        }

        if (max <= 0) {
            grafico.innerHTML = `<p class="grafico-vacio">${icono("vacio")}No hay ventas en este periodo.</p>`;
            return;
        }

        const W = 900, H = 220, mi = 52, md = 8, ms = 12, mb = 26;
        const ancho = (W - mi - md) / semanas.length;
        const escala = (v) => ((H - ms - mb) * v) / max;
        const paso = niceStep(max);
        const lineas = [];
        for (let v = 0; v <= max; v += paso) {
            const y = H - mb - escala(v);
            lineas.push(`<line x1="${mi}" x2="${W - md}" y1="${y}" y2="${y}" class="g-grid"/><text x="${mi - 6}" y="${y + 4}" class="g-eje" text-anchor="end">${numero(v)}</text>`);
        }
        const cadaEtiqueta = Math.ceil(semanas.length / 13);
        const barras = semanas.map((s, i) => {
            const h = escala(s.paquetes);
            const x = mi + i * ancho + ancho * 0.15;
            const actual = i === semanas.length - 1;
            const etiqueta = i % cadaEtiqueta === 0
                ? `<text x="${x + ancho * 0.35}" y="${H - 8}" class="g-eje" text-anchor="middle">${fecha(s.semana).slice(0, 5)}</text>` : "";
            return `<g><rect x="${x.toFixed(1)}" y="${(H - mb - h).toFixed(1)}" width="${(ancho * 0.7).toFixed(1)}" height="${Math.max(h, s.paquetes > 0 ? 1 : 0).toFixed(1)}" class="g-barra${actual ? " actual" : ""}"><title>Semana del ${fecha(s.semana)}${actual ? " (en curso)" : ""}: ${paquetes(s.paquetes)} paquetes</title></rect>${etiqueta}</g>`;
        }).join("");

        const total = semanas.reduce((a, s) => a + s.paquetes, 0);
        const completas = semanas.slice(0, -1);
        const promedio = completas.length ? completas.reduce((a, s) => a + s.paquetes, 0) / completas.length : 0;
        grafico.innerHTML = `
            <svg viewBox="0 0 ${W} ${H}" role="img" aria-label="Paquetes vendidos por semana" preserveAspectRatio="none">${lineas.join("")}${barras}</svg>
            <p class="grafico-pie"><span>Total del periodo: <b>${paquetes(total)}</b> paquetes</span><span>Promedio por semana completa: <b>${paquetes(promedio)}</b></span><span class="g-leyenda"><i class="actual"></i>Semana en curso (incompleta)</span></p>`;
    }

    function niceStep(max) {
        const bruto = max / 4;
        const pot = 10 ** Math.floor(Math.log10(bruto));
        const n = bruto / pot;
        return (n <= 1 ? 1 : n <= 2 ? 2 : n <= 5 ? 5 : 10) * pot;
    }

    gProducto.addEventListener("change", cargarGrafico);
    document.querySelectorAll('input[name="gSemanas"]').forEach((r) => r.addEventListener("change", cargarGrafico));

    // ------------------------------------------------------------ registrar venta
    const dlg = document.getElementById("dlgVenta");
    const form = dlg?.querySelector("form");
    const selProducto = document.getElementById("vProducto");
    const selAlmacen = document.getElementById("vAlmacen");
    const inCantidad = document.getElementById("vCantidad");
    const chkAfecta = document.getElementById("vAfecta");
    const previsto = document.getElementById("previsto");
    const equivalencia = document.getElementById("equivalencia");
    let stockActual = null;

    const productoElegido = () => productos.find((p) => p.id === Number(selProducto?.value));
    const enBotellas = () => form.querySelector('input[name="unidad"]:checked')?.value === "botellas";
    const paquetesIngresados = () => {
        const p = productoElegido();
        const n = Number(inCantidad.value) || 0;
        return p && enBotellas() ? n / p.unidadesPorPaquete : n;
    };

    function actualizarEquivalencia() {
        const p = productoElegido();
        const n = Number(inCantidad.value) || 0;
        if (!p || n <= 0) { equivalencia.hidden = true; return; }
        equivalencia.hidden = false;
        equivalencia.textContent = enBotellas()
            ? `${numero(n)} botellas = ${paquetes(n / p.unidadesPorPaquete)} paquetes de ${p.unidadesPorPaquete}`
            : `${paquetes(n)} paquetes = ${numero(Math.round(n * p.unidadesPorPaquete))} botellas`;
    }

    const consultarStock = debounce(async () => {
        stockActual = null;
        const p = productoElegido();
        if (p && chkAfecta.checked && selAlmacen.value) {
            const r = await api(`/api/producto-terminado/cantidad?productoId=${p.id}&almacenId=${selAlmacen.value}`);
            if (r.ok) stockActual = Number(r.data);
        }
        actualizarPrevisto();
    }, 150);

    function actualizarPrevisto() {
        if (!chkAfecta.checked || stockActual === null) { previsto.hidden = true; return; }
        const salida = paquetesIngresados();
        const despues = Math.round((stockActual - salida) * 10000) / 10000;
        previsto.hidden = false;
        previsto.classList.toggle("is-error", despues < 0);
        let html = `<span>Stock actual: <b>${paquetes(stockActual)}</b> paquetes</span>`;
        if (salida > 0) html += `<span>${icono("flecha")} después: <b>${paquetes(despues)}</b></span>`;
        if (despues < 0) html += `<span>No alcanza el stock para este despacho.</span>`;
        previsto.innerHTML = html;
    }

    function actualizarAfecta() {
        document.getElementById("campoAlmacen").hidden = !chkAfecta.checked;
        selAlmacen.required = chkAfecta.checked;
        consultarStock();
    }

    if (form) {
        form.addEventListener("change", (e) => {
            if (e.target.name === "productoId") { actualizarEquivalencia(); consultarStock(); }
            else if (e.target.name === "almacenId") consultarStock();
            else if (e.target.name === "unidad") { actualizarEquivalencia(); actualizarPrevisto(); }
            else if (e.target.name === "afectaStock") actualizarAfecta();
            else if (e.target.name === "fecha") sugerirAfecta();
        });
        inCantidad.addEventListener("input", () => { actualizarEquivalencia(); actualizarPrevisto(); });

        form.addEventListener("submit", async (e) => {
            e.preventDefault();
            const d = leerFormulario(form);
            const cuerpo = {
                fecha: d.fecha ?? hoyIso(),
                productoId: d.productoId,
                canal: d.canal,
                cantidad: d.cantidad,
                enBotellas: enBotellas(),
                documento: d.documento,
                afectaStock: d.afectaStock,
                almacenId: d.afectaStock ? d.almacenId : null
            };
            const r = await enviar(form, "/api/ventas", "POST", cuerpo);
            if (!r) return;
            cerrar(dlg);
            toast(r.message);
            listado.cargar();
            cargarGrafico();
        });
    }

    /** Una venta de hoy suele ser un despacho real; una pasada, historial. Solo se sugiere al cambiar la fecha. */
    function sugerirAfecta() {
        const hoy = form.elements.fecha.value === hoyIso();
        if (chkAfecta.checked !== hoy && selAlmacen.options.length > 1) {
            chkAfecta.checked = hoy;
            actualizarAfecta();
        }
    }

    async function abrirRegistro() {
        await llenarSelect(selAlmacen, "/api/almacenes/opciones?soloActivos=true&soloProductoTerminado=true", { vacio: "Seleccione…" });
        selProducto.innerHTML = `<option value="">Seleccione…</option>` +
            productos.filter((p) => p.activo).map((p) => `<option value="${p.id}">${esc(p.nombre)}</option>`).join("");
        form.reset();
        limpiarErrores(form);
        form.elements.fecha.value = hoyIso();
        form.elements.fecha.max = hoyIso();
        stockActual = null;
        if (selAlmacen.options.length === 2) selAlmacen.selectedIndex = 1;
        const hayAlmacen = selAlmacen.options.length > 1;
        chkAfecta.checked = hayAlmacen;
        chkAfecta.disabled = !hayAlmacen;
        document.getElementById("campoAfecta").title = hayAlmacen ? "" : "No hay almacenes de producto terminado activos.";
        actualizarAfecta();
        actualizarEquivalencia();
        abrir(dlg);
        selProducto.focus();
    }

    // ------------------------------------------------------------ anular
    const dlgAnular = document.getElementById("dlgAnular");
    const formAnular = dlgAnular?.querySelector("form");
    let anulando = null;

    function abrirAnular(v) {
        anulando = v;
        formAnular.reset();
        limpiarErrores(formAnular);
        dlgAnular.querySelector("[data-resumen]").innerHTML =
            `Venta N° ${esc(v.id)} del ${esc(fecha(v.fecha))}: ${paquetes(v.paquetes)} paquetes de ${esc(v.productoNombre)} (${esc(v.canalNombre)}). ` +
            (v.afectaStock
                ? `Los paquetes volverán al stock de ${esc(v.almacenCodigo)}.`
                : "Dejará de contar en el historial de demanda.");
        abrir(dlgAnular);
        formAnular.elements.motivo.focus();
    }

    formAnular?.addEventListener("submit", async (e) => {
        e.preventDefault();
        const r = await enviar(formAnular, `/api/ventas/${anulando.id}/anular`, "POST");
        if (!r) return;
        cerrar(dlgAnular);
        toast(r.message);
        listado.cargar();
        cargarGrafico();
    });

    // ------------------------------------------------------------ inicio
    (async () => {
        const r = await api("/api/productos/opciones?soloActivos=false");
        productos = r.ok ? r.data : [];
        const opciones = productos.map((p) => `<option value="${p.id}">${esc(p.nombre)}${p.activo ? "" : " (inactivo)"}</option>`).join("");
        document.getElementById("fProducto").insertAdjacentHTML("beforeend", opciones);
        gProducto.insertAdjacentHTML("beforeend", opciones);
        if (parametros.get("producto")) {
            document.getElementById("fProducto").value = parametros.get("producto");
            gProducto.value = parametros.get("producto");
        }
        listado.cargar();
        cargarGrafico();
        if (puedeVentas && parametros.get("nuevo") === "1") abrirRegistro();
    })();
    document.getElementById("btnNuevo")?.addEventListener("click", abrirRegistro);
})();
