"use strict";

(() => {
    const { esc, icono, cantidad, numero, fecha, fechaHora, hoyIso, api, toast, ocupado } = Spin;

    const form = document.getElementById("filtros");
    const reporte = document.getElementById("reporte");
    const btnExportar = document.getElementById("btnExportar");
    const btnImprimir = document.getElementById("btnImprimir");
    const empresa = reporte.dataset.empresa;
    const selInsumo = document.getElementById("kInsumo");
    const selAlmacen = document.getElementById("kAlmacen");
    let pagina = 1;
    let ultimaConsulta = null;

    // ------------------------------------------------------------ periodos
    const iso = (d) => `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
    function aplicarPreset(preset) {
        const hoy = new Date();
        let desde = new Date(hoy.getFullYear(), hoy.getMonth(), 1);
        let hasta = hoy;
        if (preset === "mes-anterior") {
            desde = new Date(hoy.getFullYear(), hoy.getMonth() - 1, 1);
            hasta = new Date(hoy.getFullYear(), hoy.getMonth(), 0);
        } else if (preset === "90") {
            desde = new Date(hoy.getFullYear(), hoy.getMonth(), hoy.getDate() - 89);
        } else if (preset === "anio") {
            desde = new Date(hoy.getFullYear(), 0, 1);
        }
        form.elements.desde.value = iso(desde);
        form.elements.hasta.value = iso(hasta);
    }

    form.addEventListener("click", (e) => {
        const p = e.target.closest("[data-preset]");
        if (!p) return;
        aplicarPreset(p.dataset.preset);
        if (selInsumo.value) consultar(1);
    });

    // ------------------------------------------------------------ consulta
    function parametros(pag) {
        const p = new URLSearchParams();
        p.set("insumoId", selInsumo.value);
        if (selAlmacen.value) p.set("almacenId", selAlmacen.value);
        if (form.elements.desde.value) p.set("desde", form.elements.desde.value);
        if (form.elements.hasta.value) p.set("hasta", form.elements.hasta.value);
        if (pag) { p.set("pagina", pag); p.set("tamanoPagina", 50); }
        return p;
    }

    async function consultar(pag = 1) {
        if (!selInsumo.value) {
            selInsumo.classList.add("is-invalid");
            selInsumo.focus();
            toast("Elija el insumo del que quiere ver el kardex.", "error");
            return;
        }
        selInsumo.classList.remove("is-invalid");
        if (form.elements.desde.value && form.elements.hasta.value && form.elements.desde.value > form.elements.hasta.value) {
            toast("La fecha «Desde» no puede ser posterior a «Hasta».", "error");
            form.elements.desde.focus();
            return;
        }

        const boton = form.querySelector('[type="submit"]');
        ocupado(boton, true);
        reporte.setAttribute("aria-busy", "true");
        const r = await api(`/api/kardex?${parametros(pag)}`);
        ocupado(boton, false);
        reporte.removeAttribute("aria-busy");

        if (!r.ok) {
            reporte.innerHTML = `<div class="kardex-vacio" role="alert">${icono("error")} ${esc(r.message)}</div>`;
            btnExportar.disabled = btnImprimir.disabled = true;
            return;
        }
        pagina = r.data.lineas.pagina;
        ultimaConsulta = parametros(null);
        history.replaceState(null, "", `/kardex?${new URLSearchParams({
            insumo: selInsumo.value, ...(selAlmacen.value ? { almacen: selAlmacen.value } : {}),
            desde: r.data.desde, hasta: r.data.hasta
        })}`);
        form.elements.desde.value = r.data.desde;
        form.elements.hasta.value = r.data.hasta;
        pintar(r.data);
        btnExportar.disabled = btnImprimir.disabled = false;
    }

    form.addEventListener("submit", (e) => { e.preventDefault(); consultar(1); });

    // ------------------------------------------------------------ reporte
    function pintar(k) {
        const u = esc(k.unidadConsumoCodigo);
        const lineas = k.lineas.items;
        const ultimaPagina = k.lineas.pagina === k.lineas.totalPaginas;

        const filas = lineas.map((l) => `
            <tr class="${l.revertido ? "is-revertido" : ""}">
                <td data-label="Fecha">${esc(fecha(l.fecha))}</td>
                <td data-label="Hora" class="mono">${esc(fechaHora(l.fecha).slice(11))}</td>
                <td data-label="N° mov." class="mono">${esc(l.id)}</td>
                <td data-label="Movimiento">${esc(l.tipoNombre)}${l.revertido ? ` <span class="badge badge-off">Revertido</span>` : ""}</td>
                <td data-label="Almacén" class="mono">${esc(l.almacenCodigo)}</td>
                <td data-label="Documento">${esc(l.documento ?? "")}</td>
                <td data-label="Detalle" class="detalle">${esc(l.detalle ?? "")}</td>
                <td data-label="Entrada" class="num entrada">${l.entrada != null ? cantidad(l.entrada) : ""}</td>
                <td data-label="Salida" class="num salida">${l.salida != null ? cantidad(l.salida) : ""}</td>
                <td data-label="Saldo" class="num col-saldo">${cantidad(l.saldo)}</td>
                <td data-label="Usuario" class="cel-tenue">${esc(l.usuario)}</td>
            </tr>`).join("");

        const filaInicial = k.lineas.pagina === 1 ? `
            <tr class="fila-saldo">
                <td data-label="Fecha">${esc(fecha(k.desde))}</td><td></td><td></td>
                <td data-label="Movimiento" colspan="4">Saldo inicial</td>
                <td></td><td></td>
                <td data-label="Saldo" class="num col-saldo">${cantidad(k.saldoInicial)}</td><td></td>
            </tr>` : "";

        const filaFinal = ultimaPagina ? `
            <tr class="fila-saldo">
                <td data-label="Fecha">${esc(fecha(k.hasta))}</td><td></td><td></td>
                <td data-label="Movimiento" colspan="4">Totales del periodo y saldo final</td>
                <td data-label="Entradas" class="num">${cantidad(k.totalEntradas)}</td>
                <td data-label="Salidas" class="num">${cantidad(k.totalSalidas)}</td>
                <td data-label="Saldo final" class="num col-saldo">${cantidad(k.saldoFinal)}</td><td></td>
            </tr>` : "";

        const sinMovimientos = !lineas.length
            ? `<tr><td colspan="11" class="kardex-vacio">No hubo movimientos en este periodo. El saldo se mantuvo en ${cantidad(k.saldoInicial)} ${u}.</td></tr>`
            : "";

        const paginador = k.lineas.totalPaginas > 1 ? `
            <nav class="pager no-imprimir" aria-label="Páginas del kardex">
                <button type="button" data-pagina="${k.lineas.pagina - 1}" aria-label="Página anterior" ${k.lineas.pagina <= 1 ? "disabled" : ""}>${icono("izq")}</button>
                <span class="cel-tenue">Página ${k.lineas.pagina} de ${k.lineas.totalPaginas}</span>
                <button type="button" data-pagina="${k.lineas.pagina + 1}" aria-label="Página siguiente" ${ultimaPagina ? "disabled" : ""}>${icono("der")}</button>
            </nav>` : "";

        reporte.innerHTML = `
            <header class="reporte-hd">
                <div>
                    <p class="solo-impresion meta">${esc(empresa)} · Kardex de insumo</p>
                    <span class="cel-codigo">${esc(k.insumoCodigo)}</span>
                    <h2>${esc(k.insumoNombre)}</h2>
                    <p class="meta">${esc(k.categoriaNombre)} · Unidad: ${u} · Stock mín. ${cantidad(k.stockMinimo)} – máx. ${cantidad(k.stockMaximo)}</p>
                </div>
                <dl class="reporte-meta">
                    <dt>Almacén</dt><dd>${esc(k.almacenNombre)}</dd>
                    <dt>Periodo</dt><dd>${esc(fecha(k.desde))} al ${esc(fecha(k.hasta))}</dd>
                    <dt>Movimientos</dt><dd>${numero(k.movimientos)}</dd>
                    <dt>Generado</dt><dd>${esc(fechaHora(k.generadoEn))} · ${esc(k.generadoPor)}</dd>
                </dl>
            </header>
            <div class="balance" aria-label="Resumen del periodo">
                <div class="balance-item"><span>Saldo inicial</span><b>${cantidad(k.saldoInicial)}</b></div>
                <span class="balance-op" aria-hidden="true">+</span>
                <div class="balance-item entrada"><span>Entradas</span><b>${cantidad(k.totalEntradas)}</b></div>
                <span class="balance-op" aria-hidden="true">−</span>
                <div class="balance-item salida"><span>Salidas</span><b>${cantidad(k.totalSalidas)}</b></div>
                <span class="balance-op" aria-hidden="true">=</span>
                <div class="balance-item final"><span>Saldo final (${u})</span><b>${cantidad(k.saldoFinal)}</b></div>
            </div>
            <div class="tbl-scroll">
                <table class="kardex">
                    <thead>
                        <tr>
                            <th scope="col">Fecha</th><th scope="col">Hora</th><th scope="col">N° mov.</th><th scope="col">Movimiento</th>
                            <th scope="col">Almacén</th><th scope="col">Documento</th><th scope="col">Detalle</th>
                            <th scope="col" class="num">Entrada</th><th scope="col" class="num">Salida</th><th scope="col" class="num">Saldo</th>
                            <th scope="col">Usuario</th>
                        </tr>
                    </thead>
                    <tbody>${filaInicial}${filas}${sinMovimientos}${filaFinal}</tbody>
                </table>
            </div>
            <footer class="reporte-pie">
                <span>Cantidades en ${u}. El saldo de cada fila acumula desde el saldo inicial. Los movimientos revertidos se muestran tachados junto a su reversión.</span>
                ${paginador}
            </footer>`;
    }

    reporte.addEventListener("click", (e) => {
        const b = e.target.closest("[data-pagina]");
        if (b && !b.disabled) consultar(Number(b.dataset.pagina));
    });

    // ------------------------------------------------------------ exportar e imprimir
    btnExportar.addEventListener("click", () => {
        if (!ultimaConsulta) return;
        window.location.href = `/api/kardex/csv?${ultimaConsulta}`;
    });

    btnImprimir.addEventListener("click", async () => {
        // Se imprime el periodo completo, no solo la página visible.
        if (!ultimaConsulta) return;
        const r = await api(`/api/kardex?${ultimaConsulta}&pagina=1&tamanoPagina=500`);
        if (!r.ok) { toast(r.message, "error"); return; }
        if (r.data.lineas.totalPaginas > 1) toast("El periodo tiene más de 500 movimientos: se imprimen los primeros 500. Para el total, use Exportar a Excel.", "error");
        pintar(r.data);
        window.print();
        consultar(pagina);
    });

    // ------------------------------------------------------------ inicio
    (async () => {
        const qs = new URLSearchParams(location.search);
        const [ri, ra] = await Promise.all([
            api("/api/insumos/opciones?soloActivos=false"),
            api("/api/almacenes/opciones")
        ]);
        selInsumo.innerHTML = `<option value="">Seleccione un insumo…</option>` +
            (ri.ok ? ri.data.map((o) => `<option value="${o.id}">${esc(o.nombre)}</option>`).join("") : "");
        selAlmacen.innerHTML = `<option value="">Todos los almacenes</option>` +
            (ra.ok ? ra.data.map((o) => `<option value="${o.id}">${esc(o.nombre)}</option>`).join("") : "");
        if (!ri.ok) toast(ri.message, "error");

        aplicarPreset("mes");
        if (qs.get("desde")) form.elements.desde.value = qs.get("desde");
        if (qs.get("hasta")) form.elements.hasta.value = qs.get("hasta");
        if (qs.get("almacen")) selAlmacen.value = qs.get("almacen");
        if (qs.get("insumo")) {
            selInsumo.value = qs.get("insumo");
            if (selInsumo.value) consultar(1);
        }
    })();
})();
