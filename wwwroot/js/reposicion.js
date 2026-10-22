"use strict";

(() => {
    const { esc, icono, numero, cantidad, dinero, fecha, fechaHora, hoyIso, boton, llenarSelect, Listado, abrir } = Spin;

    const tabla = document.getElementById("tabla");
    const puedePedir = tabla.dataset.puedePedir === "true";
    const segmentos = document.getElementById("segmentos");
    const c2 = (n) => cantidad(Math.round(n * 100) / 100);
    let diasRevision = 7;

    const ESTADO = {
        urgente: (r) => `<span class="badge badge-crit">${icono("error")}Urgente</span><span class="cel-sub">${esc(r.estadoTexto)}</span>`,
        pedir: (r) => `<span class="badge badge-warn">${icono("alerta")}Pedir ahora</span><span class="cel-sub">Bajo el punto de reorden</span>`,
        ok: (r) => `<span class="badge badge-ok">${icono("check")}Cubierto</span><span class="cel-sub">Pedir antes del ${esc(fecha(r.fechaLimitePedido))}</span>`,
        "sin-proveedor": (r) => `<span class="badge badge-warn">${icono("camion")}Sin proveedor</span><span class="cel-sub">${esc(r.estadoTexto)}</span>`,
        "sin-demanda": () => `<span class="badge badge-off">${icono("pausa")}Sin demanda</span>`
    };

    function cobertura(r) {
        if (r.diasCobertura === null) return `<span class="cel-tenue">—</span>`;
        const critico = r.diasCobertura < r.tiempoEntrega;
        return `<span class="${critico ? "cel-alerta" : ""}">${cantidad(r.diasCobertura)} días</span><span class="cel-sub">Se agota el ${esc(fecha(r.fechaAgotamiento))} · entrega ${r.tiempoEntrega} d</span>`;
    }

    function urlPedido(r) {
        return `/pedidos?nuevo=1&proveedor=${r.proveedorId}&insumo=${r.insumoId}&cantidad=${r.cantidadSugerida}`;
    }

    const listado = new Listado({
        url: "/api/reposicion",
        tabla,
        filtros: document.getElementById("filtros"),
        segmentos,
        estadoInicial: new URLSearchParams(location.search).get("buscar") ? "" : (new URLSearchParams(location.search).get("estado") ?? "pedir"),
        tamanoPagina: 20,
        pie: document.getElementById("pie"),
        textoVacio: "Ningún insumo en esta situación.",
        columnas: [
            { titulo: "Insumo", render: (r) => `<span class="cel-principal">${esc(r.nombre)}${r.esCritico ? ` <span class="tag-critico">crítico</span>` : ""}</span><span class="cel-sub">${esc(r.codigo)} · ${esc(r.categoriaNombre)}</span>` },
            {
                titulo: "Stock / en camino",
                clase: "num",
                render: (r) => `<b>${c2(r.stock)}</b><span class="u">${esc(r.unidadConsumoCodigo)}</span>` +
                    (r.enCamino > 0 ? `<span class="cel-sub">+${c2(r.enCamino)} en camino</span>` : "")
            },
            { titulo: "Consumo diario", clase: "num", render: (r) => r.consumoDiario > 0 ? `${c2(r.consumoDiario)}<span class="u">${esc(r.unidadConsumoCodigo)}</span><span class="cel-sub">${esc(r.fuenteDemanda)}</span>` : `<span class="cel-tenue">—</span>` },
            { titulo: "Cobertura", render: cobertura },
            { titulo: "Punto de reorden", clase: "num", render: (r) => r.consumoDiario > 0 ? `${c2(r.puntoReorden)}<span class="cel-sub">Seguridad ${c2(r.stockSeguridad)}</span>` : "—" },
            {
                titulo: "Pedir",
                clase: "num",
                render: (r) => r.cantidadSugerida > 0
                    ? `<b>${c2(r.cantidadSugerida)}</b><span class="u">${esc(r.unidadCompraCodigo)}</span><span class="cel-sub">${r.costoEstimado !== null ? `≈ ${esc(r.simbolo ?? "")} ${dinero(r.costoEstimado)}` : `${c2(r.cantidadSugeridaConsumo)} ${esc(r.unidadConsumoCodigo)}`}</span>`
                    : `<span class="cel-tenue">—</span>`
            },
            { titulo: "Situación", render: (r) => (ESTADO[r.estado] ?? (() => ""))(r) }
        ],
        acciones: (r) => boton("calculo", "ojo", "Ver cómo se calculó") +
            (puedePedir && r.cantidadSugerida > 0 && r.proveedorId
                ? `<a class="icon-btn" href="${urlPedido(r)}" title="Crear pedido con la cantidad sugerida" aria-label="Crear pedido">${icono("pedido")}</a>` : ""),
        alAccion: (accion, r) => { if (accion === "calculo") verCalculo(r); },
        alCargar: (d) => {
            const poner = (k, v) => { const el = segmentos.querySelector(`[data-resumen="${k}"]`); if (el) el.textContent = numero(v); };
            diasRevision = d.diasRevision;
            poner("pedir", d.urgentes + d.porPedir);
            poner("urgentes", d.urgentes);
            poner("ok", d.ok);
            poner("sin", d.sinDatos);
            document.getElementById("base").innerHTML = d.pronosticoDel
                ? `Calculado con el pronóstico del <b>${esc(fechaHora(d.pronosticoDel))}</b> y el stock de este momento. Revisión de compras cada ${d.diasRevision} días.`
                : `Aún no hay pronóstico entrenado: se usa el consumo real de los últimos 90 días o el consumo estimado de cada insumo. <a href="/pronostico">Entrenar el pronóstico</a>.`;
        }
    });

    function verCalculo(r) {
        const u = esc(r.unidadConsumoCodigo);
        const L = r.tiempoEntrega, sL = r.variacionEntrega;
        const productos = r.productos.length
            ? `<table class="tabla-consumo"><thead><tr><th>Producto</th><th class="num">Paquetes/día (pronóstico)</th><th class="num">${u} por paquete</th><th class="num">${u}/día</th></tr></thead><tbody>
                ${r.productos.map((p) => `<tr><td>${esc(p.codigo)}</td><td class="num">${c2(p.paquetesDiarios)}</td><td class="num">${cantidad(p.consumoPorPaquete)}</td><td class="num">${cantidad(p.consumoDiario)}</td></tr>`).join("")}
               </tbody></table>` : "";
        document.getElementById("dlgCalculoTitulo").textContent = `${r.codigo} · ${r.nombre}`;
        document.getElementById("calculoCuerpo").innerHTML = `
            <ol class="formula">
                <li><b>Consumo diario esperado (d)</b> = ${c2(r.consumoDiario)} ${u}/día <span class="cel-sub">${esc(r.fuenteDemanda)} · desviación σd = ${c2(r.desviacionDiaria)}</span>${productos}</li>
                <li><b>Tiempo de entrega (L)</b> = ${L} días ± ${sL} <span class="cel-sub">${r.proveedorNombre ? `Proveedor: ${esc(r.proveedorNombre)}` : "Sin proveedor asignado: se usa el valor por defecto de Configuración"}</span></li>
                <li><b>Nivel de servicio</b> = ${cantidad(r.nivelServicio)} % → z = ${cantidad(r.z)}</li>
                <li><b>Stock de seguridad</b> = z × √(L × σd² + d² × σL²) = ${cantidad(r.z)} × √(${L} × ${c2(r.desviacionDiaria)}² + ${c2(r.consumoDiario)}² × ${sL}²) = <b>${c2(r.stockSeguridad)} ${u}</b>
                    <span class="cel-sub">Stock mínimo registrado en el insumo: ${c2(r.stockMinimoActual)} ${u}</span></li>
                <li><b>Punto de reorden</b> = d × L + seguridad = ${c2(r.consumoDiario)} × ${L} + ${c2(r.stockSeguridad)} = <b>${c2(r.puntoReorden)} ${u}</b></li>
                <li><b>Posición</b> = stock ${c2(r.stock)} + en camino ${c2(r.enCamino)} = <b>${c2(r.posicion)} ${u}</b>
                    ${r.diasCobertura !== null ? `<span class="cel-sub">El stock actual alcanza ${cantidad(r.diasCobertura)} días (hasta el ${esc(fecha(r.fechaAgotamiento))}).</span>` : ""}</li>
                <li><b>Decisión</b>: ${esc(r.estadoTexto)}.
                    ${r.cantidadSugerida > 0
                        ? `Pedir lo necesario para llegar al punto de reorden más ${diasRevision} días de consumo (revisión): (${c2(r.puntoReorden)} + ${c2(r.consumoDiario)} × ${diasRevision}) − ${c2(r.posicion)} → <b>${c2(r.cantidadSugerida)} ${esc(r.unidadCompraCodigo)}</b> (= ${c2(r.cantidadSugeridaConsumo)} ${u}), ajustado al pedido mínimo (${c2(r.pedidoMinimo)}) y al múltiplo (${c2(r.multiploCompra)}).${r.costoEstimado !== null ? ` Costo estimado: ${esc(r.simbolo ?? "")} ${dinero(r.costoEstimado)}.` : ""}`
                        : r.fechaLimitePedido ? `Fecha límite para pedir: <b>${esc(fecha(r.fechaLimitePedido))}</b>.` : ""}</li>
            </ol>
            ${puedePedir && r.cantidadSugerida > 0 && r.proveedorId ? `<p><a class="btn btn-primary" href="${urlPedido(r)}">${icono("pedido")}Crear pedido con esta cantidad</a></p>` : ""}`;
        abrir(document.getElementById("dlgCalculo"));
    }

    segmentos.querySelectorAll("[data-estado]").forEach((s) => s.setAttribute("aria-pressed", String(s.dataset.estado === listado.estado.estado)));
    llenarSelect(document.getElementById("fCategoria"), "/api/categorias/opciones?soloActivas=false", { vacio: "Todas" });
    listado.cargar();
})();
