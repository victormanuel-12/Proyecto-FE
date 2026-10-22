"use strict";

(() => {
    const { esc, icono, numero, cantidad, fecha, fechaHora, api, toast, ocupado } = Spin;

    const tabla = document.getElementById("tabla");
    const detalle = document.getElementById("detalle");
    const paq = (n) => numero(Math.round(n));
    let seleccionado = null;

    const CALIDAD = {
        buena: (m) => `<span class="badge badge-ok">${icono("check")}${cantidad(m)} % · bueno</span>`,
        aceptable: (m) => `<span class="badge badge-warn">${icono("alerta")}${cantidad(m)} % · aceptable</span>`,
        baja: (m) => `<span class="badge badge-crit">${icono("error")}${cantidad(m)} % · bajo</span>`,
        sin: () => `<span class="badge badge-off">${icono("pausa")}No medido</span>`
    };

    function variacion(v) {
        if (v === null || v === undefined) return "";
        const signo = v > 0 ? "+" : v < 0 ? "−" : "±";
        return `<span class="cel-sub">${signo}${cantidad(Math.abs(v))} % vs. reciente</span>`;
    }

    async function cargar() {
        const r = await api("/api/pronostico");
        if (!r.ok) { document.getElementById("estadoMotor").innerHTML = `<p class="form-alert">${esc(r.message)}</p>`; return; }
        const d = r.data;
        pintarEstado(d);
        const tbody = tabla.tBodies[0];
        if (!d.productos.length) {
            tbody.innerHTML = `<tr><td colspan="6" class="tbl-estado"><div class="tbl-estado-box">${icono("vacio")}<p>${d.ultima ? "El último entrenamiento no produjo pronósticos." : "Aún no hay pronóstico. Importe el historial de ventas y pulse «Recalcular pronóstico»."}</p></div></td></tr>`;
            return;
        }
        tbody.innerHTML = d.productos.map((p) => `
            <tr data-id="${p.productoId}" class="is-clicable" tabindex="0">
                <td data-label="Producto"><span class="cel-principal">${esc(p.nombre)}</span><span class="cel-sub">${esc(p.codigo)}</span></td>
                <td data-label="Método">${esc(p.metodoNombre)}${p.nota ? `<span class="cel-sub" title="${esc(p.nota)}">${esc(p.nota.length > 70 ? p.nota.slice(0, 70) + "…" : p.nota)}</span>` : ""}</td>
                <td data-label="Error">${CALIDAD[p.calidad](p.mape)}</td>
                <td data-label="Historia" class="num">${numero(p.semanasHistorial)} sem.</td>
                <td data-label="Promedio reciente" class="num">${paq(p.promedioReciente)} <span class="cel-tenue">paq./sem.</span></td>
                <td data-label="Próximas 4 semanas" class="num"><b>${paq(p.proximas4)}</b> <span class="cel-tenue">paq.</span>${variacion(p.variacion)}</td>
            </tr>`).join("");
        if (seleccionado && d.productos.some((p) => p.productoId === seleccionado)) verDetalle(seleccionado);
        marcar();
    }

    function pintarEstado(d) {
        const u = d.ultima;
        const proximo = d.proximoEntrenamiento
            ? `Próximo reentrenamiento automático: <b>${esc(fechaHora(d.proximoEntrenamiento))}</b>`
            : "Reentrenamiento automático desactivado (solo manual)";
        let html;
        if (!u) {
            html = `<div class="motor-fila"><span class="badge badge-off">${icono("pausa")}Sin entrenar</span><span>${proximo}</span></div>`;
        } else {
            const mape = u.mapePromedio === null ? "no medido" : `${cantidad(u.mapePromedio)} %`;
            html = `<div class="motor-fila">
                ${u.exitosa ? `<span class="badge badge-ok">${icono("check")}Entrenado</span>` : `<span class="badge badge-crit">${icono("error")}Falló</span>`}
                <span>Último entrenamiento: <b>${esc(fechaHora(u.fecha))}</b> · ${esc(u.origenNombre.toLowerCase())} · ${esc(u.usuario)}</span>
                <span>${numero(u.productos)} producto(s) · ${cantidad(Math.round(u.duracionMs / 100) / 10)} s</span>
                <span>Error promedio (ponderado por volumen): <b>${mape}</b></span>
                <span>Historia hasta la semana del ${esc(fecha(u.ultimaSemanaHistorial))} · horizonte ${u.semanasHorizonte} semanas</span>
                <span>${proximo}</span>
            </div>
            ${u.mensaje ? `<p class="motor-msg ${u.exitosa ? "" : "is-error"}">${icono(u.exitosa ? "alerta" : "error")}<span>${esc(u.mensaje)}</span></p>` : ""}`;
        }
        if (d.enCurso) html += `<p class="motor-msg">${icono("recargar")}<span>Hay un entrenamiento en curso…</span></p>`;
        document.getElementById("estadoMotor").innerHTML = html;
    }

    function marcar() {
        tabla.tBodies[0].querySelectorAll("tr[data-id]").forEach((tr) => tr.classList.toggle("is-seleccionada", Number(tr.dataset.id) === seleccionado));
    }

    // ------------------------------------------------------------ detalle con gráfico
    async function verDetalle(id) {
        seleccionado = id;
        marcar();
        const r = await api(`/api/pronostico/productos/${id}`);
        if (!r.ok) { toast(r.message, "error"); return; }
        const d = r.data;
        detalle.hidden = false;
        document.getElementById("detalleTitulo").textContent = `${d.codigo} · ${d.nombre}`;
        document.getElementById("detalleSub").textContent =
            `${d.metodoNombre} · ${d.mape === null ? "error no medido" : `MAPE ${cantidad(d.mape)} %`} · ${numero(d.semanasHistorial)} semanas de historia`;
        document.getElementById("graficoPronostico").innerHTML = grafico(d);
        document.querySelector("#tablaMetodos tbody").innerHTML = d.metodos.map((m) => `
            <tr class="${m.elegido ? "is-elegido" : ""}"><td>${esc(m.nombre)}</td>
                <td class="num">${m.mape === null ? `<span class="cel-tenue">No aplica</span>` : `${cantidad(m.mape)} %`}</td>
                <td class="num">${m.elegido ? `<span class="badge badge-ok">${icono("check")}Elegido</span>` : ""}</td></tr>`).join("");
        document.getElementById("detalleNota").textContent = d.nota ?? "";
        document.querySelector("#tablaSemanas tbody").innerHTML = d.pronostico.map((s) => `
            <tr><td>${esc(fecha(s.semana))}</td><td class="num"><b>${paq(s.pronostico)}</b></td><td class="num cel-tenue">${paq(s.minimo)} – ${paq(s.maximo)}</td></tr>`).join("");
        detalle.scrollIntoView({ behavior: "smooth", block: "start" });
    }

    /** SVG: historia real, validación (pronóstico vs. real) y pronóstico futuro con banda del 90 %. */
    function grafico(d) {
        const hist = d.historial, val = d.validacion, fut = d.pronostico;
        const puntos = [...hist.map((p) => p.semana), ...fut.map((p) => p.semana)];
        if (!puntos.length) return `<p class="grafico-vacio">${icono("vacio")}Sin datos para graficar.</p>`;
        const semanas = [...new Set(puntos)].sort();
        const idx = new Map(semanas.map((s, i) => [s, i]));
        const max = Math.max(1, ...hist.map((p) => p.real), ...fut.map((p) => p.maximo), ...val.map((p) => p.maximo ?? 0));
        const W = 900, H = 260, mi = 56, md = 12, ms = 14, mb = 28;
        const x = (s) => mi + (idx.get(s) / Math.max(1, semanas.length - 1)) * (W - mi - md);
        const y = (v) => H - mb - (v / max) * (H - ms - mb);
        const linea = (lista, campo) => lista.map((p, i) => `${i ? "L" : "M"}${x(p.semana).toFixed(1)},${y(p[campo]).toFixed(1)}`).join(" ");

        const paso = (() => { const b = max / 4, p = 10 ** Math.floor(Math.log10(b)), n = b / p; return (n <= 1 ? 1 : n <= 2 ? 2 : n <= 5 ? 5 : 10) * p; })();
        let ejes = "";
        for (let v = 0; v <= max; v += paso) ejes += `<line x1="${mi}" x2="${W - md}" y1="${y(v)}" y2="${y(v)}" class="g-grid"/><text x="${mi - 6}" y="${y(v) + 4}" class="g-eje" text-anchor="end">${numero(v)}</text>`;
        const cada = Math.ceil(semanas.length / 12);
        semanas.forEach((s, i) => { if (i % cada === 0) ejes += `<text x="${x(s)}" y="${H - 8}" class="g-eje" text-anchor="middle">${fecha(s).slice(0, 5)}</text>`; });

        const banda = fut.length ? `M${fut.map((p) => `${x(p.semana).toFixed(1)},${y(p.maximo).toFixed(1)}`).join(" L")} L${[...fut].reverse().map((p) => `${x(p.semana).toFixed(1)},${y(p.minimo).toFixed(1)}`).join(" L")} Z` : "";
        const hoyX = fut.length ? x(fut[0].semana) : null;
        return `
            <svg viewBox="0 0 ${W} ${H}" role="img" aria-label="Ventas reales y pronóstico semanal" preserveAspectRatio="none">
                ${ejes}
                ${hoyX ? `<line x1="${hoyX}" x2="${hoyX}" y1="${ms}" y2="${H - mb}" class="g-hoy"/><text x="${hoyX + 4}" y="${ms + 10}" class="g-eje">semana actual</text>` : ""}
                ${banda ? `<path d="${banda}" class="g-banda"/>` : ""}
                ${hist.length ? `<path d="${linea(hist, "real")}" class="g-real"/>` : ""}
                ${val.length ? `<path d="${linea(val, "pronostico")}" class="g-validacion"/>` : ""}
                ${fut.length ? `<path d="${linea(fut, "pronostico")}" class="g-pronostico"/>` : ""}
                ${fut.map((p) => `<circle cx="${x(p.semana)}" cy="${y(p.pronostico)}" r="3" class="g-punto"><title>Semana del ${fecha(p.semana)}: ${paq(p.pronostico)} paquetes (${paq(p.minimo)} – ${paq(p.maximo)})</title></circle>`).join("")}
                ${hist.map((p) => `<circle cx="${x(p.semana)}" cy="${y(p.real)}" r="6" class="g-hover"><title>Semana del ${fecha(p.semana)}: ${paq(p.real)} paquetes vendidos</title></circle>`).join("")}
            </svg>
            <p class="grafico-pie">
                <span class="g-leyenda"><i class="l-real"></i>Ventas reales</span>
                ${val.length ? `<span class="g-leyenda"><i class="l-validacion"></i>Validación: lo que el modelo habría pronosticado</span>` : ""}
                <span class="g-leyenda"><i class="l-pronostico"></i>Pronóstico</span>
                <span class="g-leyenda"><i class="l-banda"></i>Rango probable (90 %)</span>
            </p>`;
    }

    tabla.tBodies[0].addEventListener("click", (e) => { const tr = e.target.closest("tr[data-id]"); if (tr) verDetalle(Number(tr.dataset.id)); });
    tabla.tBodies[0].addEventListener("keydown", (e) => { const tr = e.target.closest("tr[data-id]"); if (tr && e.key === "Enter") verDetalle(Number(tr.dataset.id)); });
    document.getElementById("btnCerrarDetalle").addEventListener("click", () => { detalle.hidden = true; seleccionado = null; marcar(); });

    // ------------------------------------------------------------ historial de entrenamientos
    async function cargarEjecuciones() {
        const r = await api("/api/pronostico/ejecuciones");
        const tbody = document.querySelector("#tablaEjecuciones tbody");
        if (!r.ok || !r.data.length) { tbody.innerHTML = `<tr><td colspan="7" class="cel-tenue">Sin entrenamientos.</td></tr>`; return; }
        tbody.innerHTML = r.data.map((e) => `
            <tr><td data-label="Fecha">${esc(fechaHora(e.fecha))}</td><td data-label="Origen">${esc(e.origenNombre)}</td>
                <td data-label="Resultado">${e.exitosa ? `<span class="badge badge-ok">${icono("check")}Correcto</span>` : `<span class="badge badge-crit" title="${esc(e.mensaje ?? "")}">${icono("error")}Falló</span>`}</td>
                <td data-label="Productos" class="num">${numero(e.productos)}</td>
                <td data-label="MAPE" class="num">${e.mapePromedio === null ? "—" : `${cantidad(e.mapePromedio)} %`}</td>
                <td data-label="Duración" class="num">${cantidad(Math.round(e.duracionMs / 100) / 10)} s</td>
                <td data-label="Usuario">${esc(e.usuario)}</td></tr>`).join("");
    }

    // ------------------------------------------------------------ recalcular
    document.getElementById("btnRecalcular")?.addEventListener("click", async (e) => {
        const boton = e.currentTarget;
        ocupado(boton, true);
        const r = await api("/api/pronostico/recalcular", { method: "POST" });
        ocupado(boton, false);
        toast(r.message, r.ok ? "ok" : "error");
        cargar();
        cargarEjecuciones();
    });

    cargar();
    cargarEjecuciones();
})();
