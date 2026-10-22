"use strict";

(() => {
    const { esc, icono, cantidad, fechaHora, api, llenarSelect, Listado } = Spin;

    const tabla = document.getElementById("tabla");
    const puedeAlmacen = tabla.dataset.puedeAlmacen === "true";
    const filtros = document.getElementById("filtros");
    const segmentos = document.getElementById("segmentos");

    const SITUACION = {
        sin: `<span class="badge badge-crit">${icono("error")}Sin stock</span>`,
        bajo: `<span class="badge badge-warn">${icono("alerta")}Bajo el mínimo</span>`,
        sobre: `<span class="badge badge-off">${icono("alerta")}Sobre el máximo</span>`,
        normal: `<span class="badge badge-ok">${icono("check")}Normal</span>`
    };

    /** Barra con el stock respecto a la escala 0 … máximo × 1.25, con marcas de mínimo y máximo. */
    function nivel(i) {
        const escala = Math.max(i.stockMaximo * 1.25, i.total, 1);
        const pct = (v) => Math.min(100, Math.max(0, (v / escala) * 100)).toFixed(1);
        return `<div class="nivel" title="Mínimo ${cantidad(i.stockMinimo)} · Máximo ${cantidad(i.stockMaximo)}">
                    <div class="nivel-barra">
                        <div class="nivel-relleno ${i.situacion}" style="width:${pct(i.total)}%"></div>
                        <div class="nivel-marca" style="left:${pct(i.stockMinimo)}%"></div>
                        <div class="nivel-marca max" style="left:${pct(i.stockMaximo)}%"></div>
                    </div>
                    <div class="nivel-pie"><span>mín. ${cantidad(i.stockMinimo)}</span><span>máx. ${cantidad(i.stockMaximo)}</span></div>
                </div>`;
    }

    const listado = new Listado({
        url: "/api/stock",
        tabla,
        filtros,
        segmentos,
        parametroSegmento: "situacion",
        estadoInicial: "",
        pie: document.getElementById("pie"),
        textoVacio: "No hay insumos activos.",
        columnas: [
            { titulo: "Código", clase: "cel-codigo", render: (i) => esc(i.codigo) },
            {
                titulo: "Insumo",
                render: (i) => `<span class="cel-principal">${esc(i.nombre)}</span><span class="cel-sub">${esc(i.categoriaNombre)}${i.esCritico ? " · crítico" : ""}</span>`
            },
            {
                titulo: "Stock",
                clase: "num cel-stock",
                render: (i) => `<b>${cantidad(i.total)}</b><span class="u">${esc(i.unidadConsumoCodigo)}</span>` +
                    (i.unidadCompraCodigo !== i.unidadConsumoCodigo && i.factorConversion > 0 && i.total > 0
                        ? `<span class="cel-sub">≈ ${cantidad(Math.round((i.total / i.factorConversion) * 100) / 100)} ${esc(i.unidadCompraCodigo)}</span>` : "")
            },
            {
                titulo: "Por almacén",
                render: (i) => i.almacenes.length
                    ? `<span class="chips">${i.almacenes.map((a) => `<span class="chip" title="${esc(a.almacenNombre)}">${esc(a.almacenCodigo)}: ${cantidad(a.cantidad)}</span>`).join("")}</span>`
                    : `<span class="cel-tenue">—</span>`
            },
            {
                titulo: "En camino",
                clase: "num",
                render: (i) => i.enCamino > 0
                    ? `<a href="/pedidos?estado=encamino" title="Ver pedidos en camino">+${cantidad(i.enCamino)}</a><span class="u">${esc(i.unidadConsumoCodigo)}</span>`
                    : `<span class="cel-tenue">—</span>`
            },
            { titulo: "Nivel", render: nivel },
            { titulo: "Situación", render: (i) => SITUACION[i.situacion] ?? "" },
            { titulo: "Último movimiento", render: (i) => i.ultimoMovimiento ? fechaHora(i.ultimoMovimiento) : `<span class="cel-tenue">Sin movimientos</span>` }
        ],
        acciones: (i) =>
            `<a class="icon-btn" href="/kardex?insumo=${i.insumoId}" title="Ver kardex" aria-label="Ver kardex">${icono("libro")}</a>` +
            (puedeAlmacen
                ? `<a class="icon-btn" href="/movimientos?nuevo=1&insumo=${i.insumoId}" title="Registrar movimiento" aria-label="Registrar movimiento">${icono("flechas")}</a>`
                : ""),
        alCargar: () => cargarResumen()
    });

    async function cargarResumen() {
        const almacen = filtros.elements.almacenId.value;
        const r = await api(`/api/stock/resumen${almacen ? `?almacenId=${almacen}` : ""}`);
        if (!r.ok) return;
        for (const [clave, valor] of Object.entries(r.data)) {
            const el = segmentos.querySelector(`[data-resumen="${clave}"]`);
            if (el) el.textContent = Spin.numero(valor);
        }
    }

    llenarSelect(document.getElementById("fAlmacen"), "/api/almacenes/opciones", { vacio: "Todos" });
    llenarSelect(document.getElementById("fCategoria"), "/api/categorias/opciones?soloActivas=false", { vacio: "Todas" });
    listado.cargar();
})();
