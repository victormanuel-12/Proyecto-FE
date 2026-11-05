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
    const MESES = ["ene", "feb", "mar", "abr", "may", "jun", "jul", "ago", "sept", "oct", "nov", "dic"];
    const aFecha = (iso) => { const [a, m, d] = String(iso).slice(0, 10).split("-").map(Number); return new Date(a, m - 1, d); };
    const fCorta = (iso) => { const f = aFecha(iso); return `${String(f.getDate()).padStart(2, "0")} ${MESES[f.getMonth()]}`; };
    const fLarga = (iso) => `${fCorta(iso)} ${aFecha(iso).getFullYear()}`;
    const rangoSemana = (iso) => { const f = aFecha(iso), h = new Date(f); h.setDate(f.getDate() + 6); return `${fCorta(iso)} – ${String(h.getDate()).padStart(2, "0")} ${MESES[h.getMonth()]} ${h.getFullYear()}`; };
    const compacto = (n) => n >= 10000 ? `${numero(Math.round(n / 1000))} mil` : numero(Math.round(n));
    const pct = (v) => `${v > 0 ? "+" : v < 0 ? "−" : "±"}${cantidad(Math.abs(Math.round(v * 10) / 10))} %`;

    let datosDetalle = null, mesesVista = 12, observador = null;

    async function verDetalle(id) {
        seleccionado = id;
        marcar();
        const r = await api(`/api/pronostico/productos/${id}`);
        if (!r.ok) { toast(r.message, "error"); return; }
        const d = r.data;
        datosDetalle = d;
        detalle.hidden = false;
        document.getElementById("detalleCodigo").textContent = d.codigo;
        document.getElementById("detalleTitulo").textContent = d.nombre;
        document.getElementById("detalleSub").innerHTML =
            `<span class="pd-chip">${icono("check")}${esc(d.metodoNombre)}</span><span>${numero(d.semanasHistorial)} semanas de historia</span>` +
            (d.historial.length ? `<span>Datos hasta el ${esc(fLarga(d.historial[d.historial.length - 1].semana))}</span>` : "");
        pintarKpis(d);
        pintarLeyenda(d);
        pintarGrafico();
        pintarMetodos(d);
        pintarSemanas(d);
        if (!observador && "ResizeObserver" in window) {
            let ancho = 0;
            observador = new ResizeObserver((e) => { const w = Math.round(e[0].contentRect.width); if (w !== ancho) { ancho = w; pintarGrafico(); } });
            observador.observe(document.getElementById("graficoPronostico"));
        }
        detalle.scrollIntoView({ behavior: "smooth", block: "start" });
    }

    function pintarKpis(d) {
        const h = d.historial, f = d.pronostico;
        const ult = h.slice(-8), prom = ult.length ? ult.reduce((a, p) => a + p.real, 0) / ult.length : null;
        const prox4 = f.slice(0, 4).reduce((a, p) => a + p.pronostico, 0);
        const prom4 = f.length ? prox4 / Math.min(4, f.length) : null;
        const pico = f.length ? f.reduce((m, p) => p.pronostico > m.pronostico ? p : m) : null;
        const varia = prom && prom4 !== null ? (prom4 / prom - 1) * 100 : null;
        const cal = d.mape === null ? ["off", "No medido"] : d.mape < 15 ? ["ok", "Buena precisión"] : d.mape <= 30 ? ["warn", "Precisión aceptable"] : ["crit", "Precisión baja"];
        const kpi = (tit, val, uni, pie, extra = "") => `<div class="pd-kpi ${extra}"><p class="pd-kpi-tit">${tit}</p><p class="pd-kpi-val">${val}${uni ? `<span>${uni}</span>` : ""}</p><p class="pd-kpi-pie">${pie}</p></div>`;
        document.getElementById("detalleKpis").innerHTML = [
            kpi("Próximas 4 semanas", f.length ? paq(prox4) : "—", "paq.",
                varia === null ? "Total pronosticado" : `<span class="pd-delta ${varia >= 0 ? "is-up" : "is-down"}">${pct(varia)}</span> vs. ritmo reciente`),
            kpi("Promedio reciente", prom === null ? "—" : paq(prom), "paq./sem.", "Últimas 8 semanas reales"),
            kpi("Semana pico", pico ? paq(pico.pronostico) : "—", "paq.", pico ? `Semana del ${esc(fLarga(pico.semana))}` : "Sin pronóstico"),
            kpi("Error del modelo (MAPE)", d.mape === null ? "—" : cantidad(d.mape), d.mape === null ? "" : "%", `<span class="pd-dot is-${cal[0]}"></span>${cal[1]}`)
        ].join("");
    }

    function pintarLeyenda(d) {
        document.getElementById("detalleLeyenda").innerHTML = `
            <span><i class="lg-real"></i>Ventas reales</span>
            ${d.validacion.length ? `<span><i class="lg-val"></i>Validación (lo que el modelo habría pronosticado)</span>` : ""}
            <span><i class="lg-pron"></i>Pronóstico</span>
            <span><i class="lg-banda"></i>Rango probable 90 %</span>`;
    }

    function pintarMetodos(d) {
        const max = Math.max(1, ...d.metodos.filter((m) => m.mape !== null).map((m) => m.mape));
        const orden = [...d.metodos].sort((a, b) => (a.mape ?? Infinity) - (b.mape ?? Infinity));
        document.getElementById("tablaMetodos").innerHTML = `<ul class="pd-metodos">${orden.map((m) => `
            <li class="${m.elegido ? "is-elegido" : ""} ${m.mape === null ? "is-na" : ""}">
                <div class="pd-met-fila">
                    <span class="pd-met-nom">${esc(m.nombre)}${m.elegido ? `<span class="pd-chip is-ok">${icono("check")}Elegido</span>` : ""}</span>
                    <span class="pd-met-val">${m.mape === null ? "No disponible" : `${cantidad(m.mape)} %`}</span>
                </div>
                <div class="pd-met-barra"><i style="width:${m.mape === null ? 0 : Math.max(2, (m.mape / max) * 100)}%"></i></div>
            </li>`).join("")}</ul>`;
        const nota = document.getElementById("detalleNota");
        if (d.nota) {
            const corta = d.nota.split(/:\s/)[0].replace(/\.$/, "");
            nota.hidden = false;
            nota.innerHTML = `${icono("alerta")}<span>${esc(corta.length > 90 ? corta.slice(0, 90) + "…" : corta)}.</span><details><summary>Ver detalle técnico</summary><code>${esc(d.nota)}</code></details>`;
        } else nota.hidden = true;
    }

    function pintarSemanas(d) {
        const f = d.pronostico;
        const top = Math.max(1, ...f.map((s) => s.maximo));
        const ultimoReal = d.historial.length ? d.historial[d.historial.length - 1].real : null;
        const pico = f.length ? Math.max(...f.map((s) => s.pronostico)) : null;
        document.querySelector("#tablaSemanas tbody").innerHTML = f.map((s, i) => {
            const prev = i ? f[i - 1].pronostico : ultimoReal;
            const v = prev ? (s.pronostico / prev - 1) * 100 : null;
            const vtxt = v === null || Math.abs(v) < 0.5 ? `<span class="pd-delta is-flat">—</span>` : `<span class="pd-delta ${v > 0 ? "is-up" : "is-down"}">${pct(v)}</span>`;
            return `<tr class="${s.pronostico === pico ? "is-pico" : ""}">
                <td data-label="Semana"><span class="pd-sem">${esc(rangoSemana(s.semana))}</span>${i === 0 ? `<span class="pd-tag">Actual</span>` : ""}${s.pronostico === pico ? `<span class="pd-tag is-pico">Pico</span>` : ""}</td>
                <td data-label="Pronóstico" class="num"><b>${paq(s.pronostico)}</b></td>
                <td data-label="vs. sem. ant." class="num">${vtxt}</td>
                <td data-label="Rango 90 %"><div class="pd-rbar" title="${paq(s.minimo)} – ${paq(s.maximo)} paquetes">
                    <span class="pd-rbar-t"><i style="left:${(s.minimo / top) * 100}%;width:${((s.maximo - s.minimo) / top) * 100}%"></i><b style="left:${(s.pronostico / top) * 100}%"></b></span>
                    <span class="pd-rbar-n">${paq(s.minimo)} – ${paq(s.maximo)}</span></div></td>
            </tr>`;
        }).join("");
    }

    document.getElementById("detalleRango").addEventListener("click", (e) => {
        const b = e.target.closest("button[data-meses]");
        if (!b) return;
        mesesVista = Number(b.dataset.meses);
        b.parentElement.querySelectorAll("button").forEach((x) => x.classList.toggle("is-on", x === b));
        pintarGrafico();
    });

    function pasoBonito(max, n) { const b = max / n, p = 10 ** Math.floor(Math.log10(b)), r = b / p; return (r <= 1 ? 1 : r <= 2 ? 2 : r <= 2.5 ? 2.5 : r <= 5 ? 5 : 10) * p; }

    /** SVG a tamaño real en píxeles (sin deformar texto): historia, validación, pronóstico con banda 90 %, tooltip. */
    function pintarGrafico() {
        const cont = document.getElementById("graficoPronostico");
        const d = datosDetalle;
        if (!d) return;
        let hist = d.historial;
        const val = d.validacion, fut = d.pronostico;
        if (!hist.length && !fut.length) { cont.innerHTML = `<p class="grafico-vacio">${icono("vacio")}Sin datos para graficar.</p>`; return; }
        if (mesesVista && hist.length) {
            const ref = aFecha(hist[hist.length - 1].semana); ref.setMonth(ref.getMonth() - mesesVista);
            hist = hist.filter((p) => aFecha(p.semana) >= ref);
        }
        const desde = hist.length ? hist[0].semana : fut[0].semana;
        const valV = val.filter((p) => p.semana >= desde);
        const semanas = [...new Set([...hist.map((p) => p.semana), ...valV.map((p) => p.semana), ...fut.map((p) => p.semana)])].sort();
        const idx = new Map(semanas.map((s, i) => [s, i]));

        const W = Math.max(320, Math.round(cont.clientWidth || 900));
        const H = W < 600 ? 260 : 340;
        const m = { t: 28, r: 20, b: 46, l: 56 };
        const iw = W - m.l - m.r, ih = H - m.t - m.b;
        const vmax = Math.max(1, ...hist.map((p) => p.real), ...fut.map((p) => p.maximo), ...valV.map((p) => p.maximo ?? p.pronostico));
        const paso = pasoBonito(vmax * 1.05, 5), top = Math.ceil((vmax * 1.05) / paso) * paso;
        const x = (s) => m.l + (idx.get(s) / Math.max(1, semanas.length - 1)) * iw;
        const y = (v) => m.t + ih - (v / top) * ih;
        const P = (n) => n.toFixed(1);
        const ruta = (lista, campo) => lista.map((p, i) => `${i ? "L" : "M"}${P(x(p.semana))},${P(y(p[campo]))}`).join("");

        // ejes Y
        let g = "";
        for (let v = 0; v <= top + 1e-9; v += paso)
            g += `<line x1="${m.l}" x2="${W - m.r}" y1="${P(y(v))}" y2="${P(y(v))}" class="${v === 0 ? "pg-base" : "pg-grid"}"/><text x="${m.l - 10}" y="${P(y(v) + 4)}" class="pg-ty" text-anchor="end">${compacto(v)}</text>`;
        // ejes X: un tick por mes (primera semana del mes); año bajo enero y el primer tick
        const ticks = [];
        let mesAnt = null;
        semanas.forEach((s) => { const f = aFecha(s), k = f.getFullYear() * 12 + f.getMonth(); if (k !== mesAnt) { ticks.push({ s, f }); mesAnt = k; } });
        // descarta el primer tick si cae demasiado cerca del siguiente (mes incompleto al inicio)
        if (ticks.length > 1 && x(ticks[1].s) - x(ticks[0].s) < 40) { ticks[1].primero = true; ticks.shift(); }
        const espacio = iw / Math.max(1, ticks.length), salto = Math.max(1, Math.ceil(46 / espacio));
        ticks.forEach((t, i) => {
            if (i % salto) return;
            const tx = x(t.s), anio = i === 0 || t.primero || t.f.getMonth() === 0 || (salto > 1 && ticks.slice(Math.max(0, i - salto + 1), i + 1).some((q) => q.f.getMonth() === 0));
            g += `<line x1="${P(tx)}" x2="${P(tx)}" y1="${m.t + ih}" y2="${m.t + ih + 5}" class="pg-base"/>`;
            g += `<text x="${P(tx)}" y="${m.t + ih + 19}" class="pg-tx" text-anchor="middle">${MESES[t.f.getMonth()]}</text>`;
            if (anio) g += `<text x="${P(tx)}" y="${m.t + ih + 34}" class="pg-ty pg-anio" text-anchor="middle">${t.f.getFullYear()}</text>`;
        });

        // zona de pronóstico
        let zona = "";
        if (fut.length) {
            const zx = hist.length ? (x(hist[hist.length - 1].semana) + x(fut[0].semana)) / 2 : m.l;
            zona = `<rect x="${P(zx)}" y="${m.t}" width="${P(W - m.r - zx)}" height="${ih}" class="pg-zona"/>
                <line x1="${P(zx)}" x2="${P(zx)}" y1="${m.t - 6}" y2="${m.t + ih}" class="pg-hoy"/>
                <text x="${P(zx + 8)}" y="${m.t - 10}" class="pg-zona-t">PRONÓSTICO</text>
                <text x="${P(zx - 8)}" y="${m.t - 10}" class="pg-zona-t is-hist" text-anchor="end">HISTORIA</text>`;
        }
        const banda = fut.length ? `M${fut.map((p) => `${P(x(p.semana))},${P(y(p.maximo))}`).join("L")}L${[...fut].reverse().map((p) => `${P(x(p.semana))},${P(y(p.minimo))}`).join("L")}Z` : "";
        const area = hist.length > 1 ? `${ruta(hist, "real")}L${P(x(hist[hist.length - 1].semana))},${P(y(0))}L${P(x(hist[0].semana))},${P(y(0))}Z` : "";
        // une la última venta real con el primer punto pronosticado
        const futRuta = fut.length ? (hist.length ? `M${P(x(hist[hist.length - 1].semana))},${P(y(hist[hist.length - 1].real))}L` + ruta(fut, "pronostico").slice(1) : ruta(fut, "pronostico")) : "";
        const ultimo = hist[hist.length - 1];

        cont.innerHTML = `
            <svg width="${W}" height="${H}" viewBox="0 0 ${W} ${H}" role="img" aria-label="Ventas reales y pronóstico semanal de ${esc(d.nombre)}">
                <defs><linearGradient id="pgArea" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="var(--petrol)" stop-opacity=".16"/><stop offset="1" stop-color="var(--petrol)" stop-opacity="0"/></linearGradient></defs>
                ${zona}${g}
                ${area ? `<path d="${area}" fill="url(#pgArea)"/>` : ""}
                ${banda ? `<path d="${banda}" class="pg-banda"/>` : ""}
                ${hist.length ? `<path d="${ruta(hist, "real")}" class="pg-real"/>` : ""}
                ${valV.length ? `<path d="${ruta(valV, "pronostico")}" class="pg-val"/>` : ""}
                ${futRuta ? `<path d="${futRuta}" class="pg-pron"/>` : ""}
                ${fut.map((p) => `<circle cx="${P(x(p.semana))}" cy="${P(y(p.pronostico))}" r="3.5" class="pg-punto"/>`).join("")}
                ${ultimo ? `<circle cx="${P(x(ultimo.semana))}" cy="${P(y(ultimo.real))}" r="4" class="pg-ult"/>` : ""}
                <g class="pg-cursor" hidden><line class="pg-cursor-l" y1="${m.t}" y2="${m.t + ih}"/><circle r="5" class="pg-cursor-p"/></g>
                <rect x="${m.l}" y="${m.t}" width="${iw}" height="${ih}" class="pg-captura"/>
            </svg>
            <div class="pg-tip" hidden></div>`;

        // tooltip
        const porSemana = new Map(semanas.map((s) => [s, {}]));
        hist.forEach((p) => (porSemana.get(p.semana).real = p.real));
        valV.forEach((p) => (porSemana.get(p.semana).val = p.pronostico));
        fut.forEach((p) => Object.assign(porSemana.get(p.semana), { pron: p.pronostico, min: p.minimo, max: p.maximo }));
        const svg = cont.querySelector("svg"), cur = svg.querySelector(".pg-cursor"), tip = cont.querySelector(".pg-tip");
        const linea = cur.querySelector("line"), punto = cur.querySelector("circle");
        const fila = (c, t, v) => `<div class="pg-tip-f"><i class="${c}"></i><span>${t}</span><b>${v}</b></div>`;
        const mover = (ev) => {
            const r = svg.getBoundingClientRect();
            const px = ev.clientX - r.left;
            const i = Math.round(((px - m.l) / iw) * (semanas.length - 1));
            const s = semanas[Math.max(0, Math.min(semanas.length - 1, i))], o = porSemana.get(s), cx = x(s);
            const vy = o.real ?? o.pron;
            cur.hidden = false; tip.hidden = false;
            linea.setAttribute("x1", cx); linea.setAttribute("x2", cx);
            punto.setAttribute("cx", cx); punto.setAttribute("cy", y(vy ?? 0));
            punto.setAttribute("class", `pg-cursor-p ${o.real !== undefined ? "" : "is-pron"}`);
            tip.innerHTML = `<p class="pg-tip-t">${o.pron !== undefined && o.real === undefined ? "Pronóstico · " : ""}Semana ${esc(rangoSemana(s))}</p>
                ${o.real !== undefined ? fila("lg-real", "Vendido", `${paq(o.real)} paq.`) : ""}
                ${o.val !== undefined ? fila("lg-val", "Modelo habría dicho", `${paq(o.val)} paq.`) : ""}
                ${o.val !== undefined && o.real ? `<p class="pg-tip-n">Desvío ${pct((o.val / o.real - 1) * 100)}</p>` : ""}
                ${o.pron !== undefined ? fila("lg-pron", "Pronóstico", `${paq(o.pron)} paq.`) + fila("lg-banda", "Rango 90 %", `${paq(o.min)} – ${paq(o.max)}`) : ""}`;
            const tw = tip.offsetWidth, th = tip.offsetHeight;
            let tx = cx + 14; if (tx + tw > W - 4) tx = cx - tw - 14;
            tip.style.left = `${Math.max(4, tx)}px`;
            tip.style.top = `${Math.max(0, Math.min(H - th - 4, y(vy ?? 0) - th / 2))}px`;
        };
        const salir = () => { cur.hidden = true; tip.hidden = true; };
        const cap = svg.querySelector(".pg-captura");
        cap.addEventListener("pointermove", mover);
        cap.addEventListener("pointerdown", mover);
        cap.addEventListener("pointerleave", salir);
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
