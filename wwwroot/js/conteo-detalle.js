"use strict";

(() => {
    const { esc, icono, cantidad, numero, fechaHora, api, toast, confirmar, ocupado } = Spin;

    const pagina = document.getElementById("pagina");
    const id = Number(pagina.dataset.conteo);
    const puedeAlmacen = pagina.dataset.puedeAlmacen === "true";
    const acciones = document.getElementById("acciones");
    let conteo = null;
    let filtro = "todos";
    let cambios = false;

    const porcentaje = (sistema, contado) => sistema === 0 ? (contado === 0 ? 0 : 100) : Math.abs(contado - sistema) / Math.abs(sistema) * 100;

    async function cargar() {
        const r = await api(`/api/conteos/${id}`);
        if (!r.ok) {
            pagina.innerHTML = `<div class="kardex-vacio" role="alert">${icono("error")} ${esc(r.message)}</div>`;
            return;
        }
        conteo = r.data;
        cambios = false;
        pintar();
    }

    function pintar() {
        const c = conteo;
        const abierto = c.estado === 1;
        // Si ya no quedan pendientes (o el conteo se cerró), mostrar todo para no dejar la tabla vacía.
        if (filtro === "pendientes" && (!abierto || !c.detalles.some((d) => d.cantidadContada == null))) filtro = "todos";
        const editable = abierto && puedeAlmacen;
        document.getElementById("titulo").textContent = `Conteo físico N° ${c.numero}`;
        document.title = `Conteo N° ${c.numero} · SPIn`;
        acciones.hidden = false;
        document.getElementById("btnHoja").href = `/conteos/${c.id}/hoja`;
        acciones.querySelectorAll("[data-abierto]").forEach((b) => { b.hidden = !editable; });

        const estado = { 1: ["badge-warn", "Abierto"], 2: ["badge-ok", "Aprobado"], 3: ["badge-off", "Anulado"] }[c.estado];
        const contados = c.detalles.filter((d) => d.cantidadContada != null).length;
        const pct = c.insumos ? Math.round((contados / c.insumos) * 100) : 0;

        const filas = c.detalles.map((d, i) => {
            const contado = d.cantidadContada;
            const base = abierto ? d.stockSistema : (d.stockAlAprobar ?? d.stockSistema);
            const dif = contado == null ? null : contado - base;
            const p = contado == null ? null : porcentaje(base, contado);
            const grande = p != null && p > c.umbralAprobacion;
            const cambioStock = abierto && d.stockActual != null && d.stockActual !== d.stockSistema;
            const visible = filtro === "todos" || (filtro === "pendientes" && contado == null) || (filtro === "diferencias" && dif != null && dif !== 0);
            return `
                <tr data-fila="${i}" ${visible ? "" : "hidden"}>
                    <td data-label="Código" class="cel-codigo">${esc(d.insumoCodigo)}</td>
                    <td data-label="Insumo"><span class="cel-principal">${esc(d.insumoNombre)}</span><span class="cel-sub">${esc(d.categoriaNombre)}</span></td>
                    <td data-label="Unidad" class="mono">${esc(d.unidadConsumoCodigo)}</td>
                    <td data-label="${abierto ? "Stock del sistema" : "Stock al aprobar"}" class="num">${cantidad(base)}
                        ${cambioStock ? `<span class="aviso-mov" title="Hubo movimientos durante el conteo">${icono("alerta")}ahora ${cantidad(d.stockActual)}</span>` : ""}</td>
                    <td data-label="Contado" class="num">${editable
                        ? `<input type="number" class="ctl num" min="0" step="any" inputmode="decimal" data-insumo="${d.insumoId}" value="${contado ?? ""}" aria-label="Cantidad contada de ${esc(d.insumoNombre)}" />`
                        : (contado != null ? cantidad(contado) : `<span class="cel-tenue">—</span>`)}</td>
                    <td data-label="Diferencia" class="num dif ${dif > 0 ? "mas" : dif < 0 ? "menos" : ""}" data-dif>${dif == null ? "—" : (dif > 0 ? "+" : dif < 0 ? "−" : "") + cantidad(Math.abs(dif))}</td>
                    <td data-label="%" class="num" data-pct>${p == null ? "—" : `<span class="badge ${grande ? "badge-crit" : dif === 0 ? "badge-ok" : "badge-warn"}">${numero(Math.round(p * 10) / 10)} %</span>`}</td>
                </tr>`;
        }).join("");

        pagina.innerHTML = `
            <section class="conteo-cab">
                <dl>
                    <div><dt>Almacén</dt><dd>${esc(c.almacenCodigo)} · ${esc(c.almacenNombre)}</dd></div>
                    <div><dt>Alcance</dt><dd>${esc(c.categoriaNombre ?? "Todos los insumos")}</dd></div>
                    <div><dt>Estado</dt><dd><span class="badge ${estado[0]}">${estado[1]}</span></dd></div>
                    <div><dt>Creado</dt><dd>${esc(fechaHora(c.creadoEn))} · ${esc(c.creadoPor)}</dd></div>
                    ${c.cerradoEn ? `<div><dt>${c.estado === 2 ? "Aprobado" : "Anulado"}</dt><dd>${esc(fechaHora(c.cerradoEn))} · ${esc(c.cerradoPor ?? "")}</dd></div>` : ""}
                </dl>
                <div class="progreso">
                    <span><b data-contados>${numero(contados)}</b> de ${numero(c.insumos)} contados</span>
                    <div class="progreso-barra"><i data-barra style="width:${pct}%"></i></div>
                </div>
            </section>
            ${c.observacion ? `<p class="hint" style="margin:-4px 0 10px">${esc(c.observacion)}</p>` : ""}
            ${abierto ? `<p class="nota" style="margin:0 0 12px">${icono("alerta")}<span>Registre lo contado en la unidad indicada (0 si no hay). Las diferencias mayores al ${numero(c.umbralAprobacion)} % solo las puede aprobar un Administrador${c.puedeAprobarDiferenciasGrandes ? " (usted puede)" : ""}. Al aprobar, el ajuste se calcula contra el stock de ese momento.</span></p>` : ""}
            ${c.estado === 2 ? `<p class="nota" style="margin:0 0 12px">${icono("check")}<span>Los ajustes de este conteo están en el <a href="/movimientos">kardex</a> con la referencia «Conteo N° ${c.numero}».</span></p>` : ""}
            <div class="seg-strip" role="group" aria-label="Mostrar">
                <button type="button" class="seg" data-filtro="todos" aria-pressed="${filtro === "todos"}">Todos</button>
                ${abierto ? `<button type="button" class="seg" data-filtro="pendientes" aria-pressed="${filtro === "pendientes"}">Pendientes</button>` : ""}
                <button type="button" class="seg" data-filtro="diferencias" aria-pressed="${filtro === "diferencias"}">Con diferencia</button>
            </div>
            <div class="tbl">
                <table class="dt conteo-tabla">
                    <thead><tr>
                        <th scope="col">Código</th><th scope="col">Insumo</th><th scope="col">Unidad</th>
                        <th scope="col" class="num">${abierto ? "Stock del sistema" : "Stock al aprobar"}</th>
                        <th scope="col" class="num">Contado</th><th scope="col" class="num">Diferencia</th><th scope="col" class="num">%</th>
                    </tr></thead>
                    <tbody>${filas}</tbody>
                </table>
            </div>`;
    }

    // Diferencia en vivo al escribir.
    pagina.addEventListener("input", (e) => {
        const input = e.target.closest("[data-insumo]");
        if (!input) return;
        cambios = true;
        const fila = input.closest("tr");
        const d = conteo.detalles[Number(fila.dataset.fila)];
        d.cantidadContada = input.value === "" ? null : Number(input.value);
        input.classList.toggle("is-invalid", d.cantidadContada != null && (Number.isNaN(d.cantidadContada) || d.cantidadContada < 0));
        const dif = d.cantidadContada == null ? null : d.cantidadContada - d.stockSistema;
        const p = dif == null ? null : porcentaje(d.stockSistema, d.cantidadContada);
        const celDif = fila.querySelector("[data-dif]");
        celDif.className = `num dif ${dif > 0 ? "mas" : dif < 0 ? "menos" : ""}`;
        celDif.textContent = dif == null ? "—" : (dif > 0 ? "+" : dif < 0 ? "−" : "") + cantidad(Math.abs(dif));
        fila.querySelector("[data-pct]").innerHTML = p == null ? "—"
            : `<span class="badge ${p > conteo.umbralAprobacion ? "badge-crit" : dif === 0 ? "badge-ok" : "badge-warn"}">${numero(Math.round(p * 10) / 10)} %</span>`;
        const contados = conteo.detalles.filter((x) => x.cantidadContada != null).length;
        pagina.querySelector("[data-contados]").textContent = numero(contados);
        pagina.querySelector("[data-barra]").style.width = `${Math.round((contados / conteo.insumos) * 100)}%`;
    });

    // Enter pasa al siguiente campo (como en una planilla).
    pagina.addEventListener("keydown", (e) => {
        if (e.key !== "Enter" || !e.target.matches("[data-insumo]")) return;
        e.preventDefault();
        const campos = [...pagina.querySelectorAll("[data-insumo]")].filter((x) => !x.closest("tr").hidden);
        const siguiente = campos[campos.indexOf(e.target) + 1];
        (siguiente ?? document.getElementById("btnGuardar"))?.focus();
    });

    pagina.addEventListener("click", (e) => {
        const b = e.target.closest("[data-filtro]");
        if (!b) return;
        filtro = b.dataset.filtro;
        pintar();
    });

    function cantidades() {
        const invalidos = conteo.detalles.filter((d) => d.cantidadContada != null && (Number.isNaN(d.cantidadContada) || d.cantidadContada < 0));
        if (invalidos.length) {
            toast(`Revise ${invalidos.length} cantidad(es): deben ser números iguales o mayores que 0.`, "error");
            pagina.querySelector(".is-invalid")?.focus();
            return null;
        }
        return conteo.detalles.map((d) => ({ insumoId: d.insumoId, cantidadContada: d.cantidadContada }));
    }

    async function accion(boton, url, cuerpo) {
        ocupado(boton, true);
        const r = await api(url, { method: url.endsWith(`/${id}`) ? "PUT" : "POST", body: cuerpo });
        ocupado(boton, false);
        toast(r.message, r.ok ? "ok" : "error");
        if (r.ok) { conteo = r.data; cambios = false; pintar(); }
        return r.ok;
    }

    document.getElementById("btnGuardar")?.addEventListener("click", async (e) => {
        const c = cantidades();
        if (c) await accion(e.currentTarget, `/api/conteos/${id}`, { cantidades: c });
    });

    document.getElementById("btnAprobar")?.addEventListener("click", async (e) => {
        const boton = e.currentTarget;
        const c = cantidades();
        if (!c) return;
        const pendientes = conteo.detalles.filter((d) => d.cantidadContada == null).length;
        if (pendientes) {
            toast(`Faltan contar ${pendientes} insumo(s). Si no hay existencias, registre 0.`, "error");
            filtro = "pendientes";
            pintar();
            pagina.querySelector("[data-insumo]")?.focus();
            return;
        }
        const conDif = conteo.detalles.filter((d) => d.cantidadContada !== d.stockSistema).length;
        const ok = await confirmar({
            titulo: `Aprobar conteo N° ${conteo.numero}`,
            mensaje: conDif
                ? `Se registrarán ajustes para ${conDif} insumo(s) con diferencia, calculados contra el stock actual. El conteo quedará cerrado.`
                : "No hay diferencias: el conteo se cerrará sin ajustes.",
            aceptar: "Aprobar"
        });
        if (ok) await accion(boton, `/api/conteos/${id}/aprobar`, { cantidades: c });
    });

    document.getElementById("btnAnular")?.addEventListener("click", async (e) => {
        const boton = e.currentTarget;
        const ok = await confirmar({
            titulo: `Anular conteo N° ${conteo.numero}`,
            mensaje: "Lo registrado se descarta y el stock no cambia. Esta acción no se puede deshacer.",
            aceptar: "Anular",
            peligro: true
        });
        if (ok) await accion(boton, `/api/conteos/${id}/anular`);
    });

    // Aviso si se sale con cambios sin guardar.
    window.addEventListener("beforeunload", (e) => {
        if (cambios) { e.preventDefault(); e.returnValue = ""; }
    });

    cargar();
})();
