"use strict";

(() => {
    const { esc, icono, numero, cantidad, fecha, fechaHora, hoyIso, api, toast } = Spin;
    const SEV = {
        1: { clase: "crit", texto: "Crítica", ic: "error" },
        2: { clase: "alta", texto: "Alta", ic: "alerta" },
        3: { clase: "media", texto: "Media", ic: "alerta" },
        4: { clase: "info", texto: "Informativa", ic: "ojo" }
    };
    const paq = (n) => numero(Math.round(n));

    function atencion(d) {
        const r = d.alertas;
        document.getElementById("atencionResumen").textContent = r.activas
            ? `${numero(r.criticas)} crítica(s) · ${numero(r.altas)} alta(s) · ${numero(r.medias)} media(s) · ${numero(r.informativas)} informativa(s)`
            : "Sin alertas activas.";
        const ul = document.getElementById("atencion");
        ul.innerHTML = d.principales.length
            ? d.principales.map((a) => `
                <li class="alerta-item sev-${SEV[a.severidad].clase}">
                    <span class="sev-marca" aria-hidden="true">${icono(SEV[a.severidad].ic)}</span>
                    <div><b>${esc(a.titulo)}</b><span class="cel-sub">${esc(a.detalle)}</span></div>
                    ${a.enlace ? `<a class="btn btn-secondary btn-sm" href="${esc(a.enlace)}">${esc(a.textoEnlace ?? "Ir")}</a>` : ""}
                </li>`).join("")
            : `<li class="alerta-vacia">${icono("check")}<span>Todo en orden: no hay insumos por pedir, pedidos atrasados ni producto terminado bajo.</span></li>`;
    }

    function pulso(d) {
        const v = d.ventasUltimos7, p = d.ventasPrevios7;
        const cambio = p > 0 ? Math.round((v - p) / p * 1000) / 10 : null;
        const items = [
            ["Ventas últimos 7 días", `${paq(v)} paq.`, cambio === null ? "Sin semana anterior para comparar" : `${cambio >= 0 ? "+" : "−"}${cantidad(Math.abs(cambio))} % vs. 7 días previos`],
            ["Producido esta semana", `${paq(d.producidoSemana)} paq.`, `${numero(d.partesSemana)} parte(s) · pendiente programado ${paq(d.programadoPendienteSemana)} paq.`],
            ["Pedidos en camino", numero(d.pedidosEnCamino), d.pedidosAtrasados ? `${numero(d.pedidosAtrasados)} atrasado(s)` : "Ninguno atrasado"],
            ["Pronóstico", d.pronosticoDel ? fecha(d.pronosticoDel) : "Sin entrenar",
                d.pronosticoDel ? `${d.mapePromedio === null ? "Error no medido" : `Error promedio ${cantidad(d.mapePromedio)} %`}${d.pronosticoExitoso ? "" : " · el último intento falló"}` : "Importe ventas y recalcule"]
        ];
        document.getElementById("pulso").innerHTML = items.map(([t, v2, s]) => `<div><dt>${esc(t)}</dt><dd>${esc(v2)}<small>${esc(s)}</small></dd></div>`).join("");
    }

    /** Barras horizontales: días de cobertura con marca del tiempo de entrega. */
    function cobertura(d) {
        const el = document.getElementById("cobertura");
        if (!d.insumos.length) {
            el.innerHTML = `<p class="grafico-vacio">${icono("vacio")}Sin consumo previsto todavía: registre recetas, ventas o el consumo estimado de los insumos.</p>`;
            return;
        }
        const escala = Math.max(30, ...d.insumos.map((i) => Math.min(120, Math.max(i.diasCobertura, i.tiempoEntrega * 2))));
        const pct = (x) => Math.min(100, (x / escala) * 100).toFixed(1);
        el.innerHTML = d.insumos.map((i) => {
            const clase = i.estado === "urgente" ? "crit" : i.estado === "pedir" || i.estado === "sin-proveedor" ? "warn" : "ok";
            const texto = i.estado === "urgente" ? "Urgente" : i.estado === "pedir" ? "Pedir ahora" : i.estado === "sin-proveedor" ? "Sin proveedor" : "Cubierto";
            return `<div class="cob-fila">
                <a class="cob-nombre" href="/reposicion?buscar=${encodeURIComponent(i.codigo)}"><b>${esc(i.codigo)}</b><span>${esc(i.nombre)}</span></a>
                <div class="cob-barra" title="${cantidad(i.diasCobertura)} días de cobertura · entrega ${i.tiempoEntrega} días">
                    <div class="cob-relleno ${clase}" style="width:${pct(i.diasCobertura)}%"></div>
                    <div class="cob-marca" style="left:${pct(i.tiempoEntrega)}%"></div>
                </div>
                <span class="cob-dato"><b>${cantidad(i.diasCobertura)}</b> días</span>
                <span class="badge badge-${clase === "crit" ? "crit" : clase === "warn" ? "warn" : "ok"}">${icono(clase === "ok" ? "check" : clase === "crit" ? "error" : "alerta")}${texto}</span>
            </div>`;
        }).join("") + `<p class="grafico-pie"><span class="g-leyenda"><i class="l-marca"></i>Tiempo de entrega</span><span>Escala: ${numero(escala)} días</span></p>`;
    }

    function productos(d) {
        const tbody = document.querySelector("#tablaProductos tbody");
        document.getElementById("productosSub").textContent = d.pronosticoDel
            ? `Stock listo para despacho, demanda pronosticada (entrenada el ${fechaHora(d.pronosticoDel)}) y producción de la semana.`
            : "Stock listo para despacho y producción de la semana. Aún no hay pronóstico entrenado.";
        if (!d.productos.length) { tbody.innerHTML = `<tr><td colspan="6" class="cel-tenue">Aún no hay productos activos.</td></tr>`; return; }
        const calidad = { buena: "badge-ok", aceptable: "badge-warn", baja: "badge-crit", sin: "badge-off" };
        tbody.innerHTML = d.productos.map((p) => {
            const cob = p.diasDeVenta === null ? `<span class="cel-tenue">Sin ventas recientes</span>`
                : `<span class="${p.diasDeVenta < 3 ? "cel-alerta" : ""}">${cantidad(p.diasDeVenta)} días</span><span class="cel-sub">${cantidad(p.ventaDiaria)} paq./día</span>`;
            const varTxt = p.variacion === null ? "" : `<span class="cel-sub">${p.variacion >= 0 ? "+" : "−"}${cantidad(Math.abs(p.variacion))} % vs. reciente</span>`;
            return `<tr>
                <td data-label="Producto"><span class="cel-principal">${esc(p.nombre)}</span><span class="cel-sub">${esc(p.codigo)}</span></td>
                <td data-label="Stock PT" class="num"><b>${paq(p.stockPaquetes)}</b> <span class="cel-tenue">paq.</span></td>
                <td data-label="Cobertura">${cob}</td>
                <td data-label="Próximas 4 semanas" class="num">${p.proximas4 > 0 ? `<b>${paq(p.proximas4)}</b> <span class="cel-tenue">paq.</span>${varTxt}` : `<span class="cel-tenue">—</span>`}</td>
                <td data-label="Pronóstico">${p.mape === null ? `<span class="badge badge-off">No medido</span>` : `<span class="badge ${calidad[p.calidad]}">MAPE ${cantidad(p.mape)} %</span>`}</td>
                <td data-label="Semana" class="num">${paq(p.producidoSemana)} / ${paq(p.programadoSemana + p.producidoSemana)}</td>
            </tr>`;
        }).join("");
    }

    function pedidos(d) {
        document.getElementById("pedidosSub").textContent = `${numero(d.pedidosEnCamino)} en camino${d.pedidosAtrasados ? ` · ${numero(d.pedidosAtrasados)} atrasado(s)` : ""}`;
        const ul = document.getElementById("pedidosPanel");
        const hoy = hoyIso();
        ul.innerHTML = d.pedidos.length ? d.pedidos.map((p) => `
            <li>
                <div><b class="mono">${esc(p.numero)}</b> · ${esc(p.proveedorNombre)}<span class="cel-sub">${esc(p.insumos)} · ${cantidad(p.avance)} % recibido</span></div>
                ${p.diasAtraso > 0 ? `<span class="badge badge-crit">${icono("alerta")}${p.diasAtraso} d de atraso</span>`
                    : `<span class="badge ${p.fechaEstimada === hoy ? "badge-warn" : "badge-off"}">${icono("camion")}${p.fechaEstimada === hoy ? "Llega hoy" : fecha(p.fechaEstimada)}</span>`}
            </li>`).join("") : `<li class="cel-tenue">No hay pedidos en camino.</li>`;
    }

    (async () => {
        const r = await api("/api/panel");
        if (!r.ok) { toast(r.message, "error"); document.getElementById("atencionResumen").textContent = r.message; return; }
        const d = r.data;
        atencion(d);
        pulso(d);
        cobertura(d);
        productos(d);
        pedidos(d);
        document.getElementById("panelSub").textContent = `Actualizado ${fechaHora(d.generado)}. Lo que requiere atención hoy y el estado de la cadena: ventas, producto terminado, producción, insumos y pedidos.`;
    })();
})();
