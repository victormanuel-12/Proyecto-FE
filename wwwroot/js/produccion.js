"use strict";

(() => {
    const { esc, icono, numero, cantidad, fecha, fechaHora, hoyIso, api, toast, abrir, cerrar, limpiarErrores, leerFormulario, enviar, boton, confirmar, debounce, Listado } = Spin;

    const tablaPartes = document.getElementById("tablaPartes");
    const tablaPlan = document.getElementById("tablaPlan");
    const puedePlan = tablaPlan.dataset.puedePlan === "true";
    const puedePartes = tablaPlan.dataset.puedePartes === "true";
    const paq = (n) => cantidad(Math.round(n * 100) / 100);
    let op = { lineas: [], almacenes: [], productos: [] };

    // ------------------------------------------------------------ pestañas
    const pestanas = document.getElementById("pestanas");
    function mostrarVista(vista) {
        pestanas.querySelectorAll("[data-vista]").forEach((b) => {
            const activa = b.dataset.vista === vista;
            b.setAttribute("aria-pressed", String(activa));
            b.setAttribute("aria-selected", String(activa));
        });
        document.getElementById("vistaPartes").hidden = vista !== "partes";
        document.getElementById("vistaPlan").hidden = vista !== "plan";
        history.replaceState(null, "", vista === "plan" ? "?vista=plan" : location.pathname);
    }
    pestanas.addEventListener("click", (e) => { const b = e.target.closest("[data-vista]"); if (b) mostrarVista(b.dataset.vista); });

    // ------------------------------------------------------------ partes
    const partes = new Listado({
        url: "/api/produccion/partes",
        tabla: tablaPartes,
        filtros: document.getElementById("filtrosPartes"),
        estadoInicial: "",
        tamanoPagina: 20,
        pie: document.getElementById("piePartes"),
        textoVacio: "Aún no hay partes de producción. Registre el primero con «Registrar parte».",
        columnas: [
            { titulo: "N°", clase: "mono", render: (p) => `PP-${esc(p.numero)}` },
            { titulo: "Fecha", render: (p) => `${esc(fecha(p.fecha))}<span class="cel-sub">Turno ${esc(p.turnoNombre.toLowerCase())}</span>` },
            { titulo: "Línea", clase: "cel-codigo", render: (p) => esc(p.lineaCodigo) },
            { titulo: "Producto", render: (p) => `<span class="cel-principal">${esc(p.productoNombre)}</span><span class="cel-sub">${esc(p.productoCodigo)} · receta v${p.recetaVersion}</span>` },
            { titulo: "Paquetes", clase: "num", render: (p) => `<b>${paq(p.paquetes)}</b><span class="cel-sub">${numero(Math.round(p.botellas))} botellas</span>` },
            {
                titulo: "Eficiencia",
                render: (p) => p.eficiencia === null ? `<span class="cel-tenue">—</span>`
                    : `${cantidad(p.eficiencia)} %<span class="cel-sub">${cantidad(p.horasOperacion)} h</span>`
            },
            { titulo: "Almacenes", render: (p) => `<span class="cel-codigo">${esc(p.almacenInsumosCodigo)}</span> ${icono("flecha")} <span class="cel-codigo">${esc(p.almacenProductoCodigo)}</span>` },
            {
                titulo: "Estado",
                render: (p) => p.estado === 2
                    ? `<span class="badge badge-off">${icono("deshacer")}Anulado</span><span class="cel-sub">${esc(p.motivoAnulacion)}</span>`
                    : `<span class="badge badge-ok">${icono("check")}Registrado</span><span class="cel-sub">${esc(p.creadoPor)}</span>`
            }
        ],
        acciones: (p) => boton("ver", "ojo", "Ver insumos consumidos") +
            (puedePlan ? boton("anular", "deshacer", p.estado === 2 ? "Parte anulado" : "Anular parte", { deshabilitado: p.estado === 2 }) : ""),
        alAccion: (accion, p) => { if (accion === "ver") verParte(p.id); else if (accion === "anular") abrirAnular(p); },
        alCargar: (d) => tablaPartes.tBodies[0].querySelectorAll("tr[data-id]").forEach((tr) =>
            tr.classList.toggle("is-revertido", d.items.find((x) => String(x.id) === tr.dataset.id)?.estado === 2))
    });

    // ------------------------------------------------------------ programación
    const plan = new Listado({
        url: "/api/produccion/planes",
        tabla: tablaPlan,
        filtros: document.getElementById("filtrosPlan"),
        estadoInicial: "",
        tamanoPagina: 20,
        pie: document.getElementById("piePlan"),
        textoVacio: "No hay producción programada con estos filtros.",
        columnas: [
            { titulo: "Fecha", render: (p) => esc(fecha(p.fecha)) + (p.vencido ? `<span class="cel-sub cel-alerta">Fecha pasada sin parte</span>` : "") },
            { titulo: "Línea", clase: "cel-codigo", render: (p) => esc(p.lineaCodigo) },
            { titulo: "Producto", render: (p) => `<span class="cel-principal">${esc(p.productoNombre)}</span><span class="cel-sub">${esc(p.productoCodigo)}</span>` },
            { titulo: "Paquetes", clase: "num", render: (p) => `<b>${paq(p.paquetes)}</b><span class="cel-sub">${numero(Math.round(p.botellas))} botellas</span>` },
            { titulo: "Horas de línea", clase: "num", render: (p) => p.horasEstimadas === null ? "—" : `${cantidad(p.horasEstimadas)} h` },
            {
                titulo: "Estado",
                render: (p) => p.estado === 1
                    ? (p.vencido ? `<span class="badge badge-warn">${icono("alerta")}Pendiente</span>` : `<span class="badge badge-off">${icono("calendario")}Planificado</span>`)
                    : p.estado === 2 ? `<span class="badge badge-ok">${icono("check")}Ejecutado</span><span class="cel-sub">PP-${esc(p.parteNumero ?? "")}</span>`
                        : `<span class="badge badge-off">${icono("cerrar")}Cancelado</span>`
            },
            { titulo: "Comentario", render: (p) => p.comentario ? esc(p.comentario) : `<span class="cel-tenue">—</span>` }
        ],
        acciones: (puedePlan || puedePartes)
            ? (p) => (puedePartes ? boton("ejecutar", "check", "Registrar el parte de esta programación", { deshabilitado: p.estado !== 1 || p.fecha > hoyIso() }) : "") +
                (puedePlan ? boton("editar", "editar", "Editar", { deshabilitado: p.estado !== 1 }) + boton("cancelar", "cerrar", "Cancelar programación", { deshabilitado: p.estado !== 1, peligro: true }) : "")
            : null,
        alAccion: async (accion, p) => {
            if (accion === "ejecutar") abrirParte(p);
            else if (accion === "editar") abrirPlan(p);
            else if (accion === "cancelar") {
                const ok = await confirmar({ titulo: "Cancelar programación", mensaje: `${fecha(p.fecha)} · ${p.lineaCodigo} · ${paq(p.paquetes)} paquetes de ${p.productoNombre}.`, aceptar: "Cancelar programación", peligro: true });
                if (!ok) return;
                const r = await api(`/api/produccion/planes/${p.id}/cancelar`, { method: "POST" });
                toast(r.message, r.ok ? "ok" : "error");
                if (r.ok) recargarPlan();
            }
        },
        alCargar: () => contarPendientes()
    });

    async function contarPendientes() {
        const r = await api("/api/produccion/planes?estado=1&tamanoPagina=5");
        document.getElementById("pendientesPlan").textContent = r.ok ? numero(r.data.total) : "–";
    }
    const recargarPlan = () => { plan.cargar(); };

    // ------------------------------------------------------------ selects dependientes
    const opcionesLinea = () => `<option value="">Seleccione…</option>` +
        op.lineas.map((l) => `<option value="${l.id}">${esc(l.codigo)} · ${esc(l.nombre)}</option>`).join("");
    const linea = (id) => op.lineas.find((l) => l.id === Number(id));
    const producto = (id) => op.productos.find((p) => p.id === Number(id));

    function llenarProductos(select, lineaId, actual) {
        const l = linea(lineaId);
        const lista = l ? op.productos.filter((p) => l.formatoIds.includes(p.formatoId)) : [];
        select.innerHTML = `<option value="">${l ? (lista.length ? "Seleccione…" : "La línea no tiene formatos de productos activos") : "Elija primero la línea"}</option>` +
            lista.map((p) => `<option value="${p.id}"${p.tieneReceta ? "" : " disabled"}>${esc(p.codigo)} · ${esc(p.nombre)}${p.tieneReceta ? "" : " (sin receta)"}</option>`).join("");
        if (actual && lista.some((p) => p.id === Number(actual))) select.value = String(actual);
    }

    function llenarAlmacenes(select, lineaId, tipo, actual) {
        const l = linea(lineaId);
        const lista = l ? op.almacenes.filter((a) => a.plantaId === l.plantaId && a.tipo === tipo) : [];
        select.innerHTML = `<option value="">${l ? (lista.length ? "Seleccione…" : "No hay almacenes de este tipo en la planta") : "Elija primero la línea"}</option>` +
            lista.map((a) => `<option value="${a.id}">${esc(a.codigo)} · ${esc(a.nombre)}</option>`).join("");
        if (actual && lista.some((a) => a.id === Number(actual))) select.value = String(actual);
        else if (lista.length === 1) select.value = String(lista[0].id);
    }

    function capacidad(el, lineaId, productoId, paquetes, horas) {
        const l = linea(lineaId), p = producto(productoId);
        if (!l || !p || !(paquetes > 0)) { el.hidden = true; return false; }
        const botellas = paquetes * p.unidadesPorPaquete;
        const horasNominal = l.velocidadNominal > 0 ? botellas / l.velocidadNominal : null;
        let texto = `${numero(botellas)} botellas · ${cantidad(Math.round(paquetes / p.paquetesPorPallet * 100) / 100)} pallets`;
        if (horasNominal !== null) texto += ` · ${cantidad(Math.round(horasNominal * 10) / 10)} h de ${l.codigo} a ${numero(l.velocidadNominal)} bot/h`;
        let excede = false;
        if (horas > 0 && l.velocidadNominal > 0) {
            const ef = botellas / (l.velocidadNominal * horas) * 100;
            excede = ef > 110;
            texto += ` · eficiencia ${cantidad(Math.round(ef * 10) / 10)} %`;
        }
        el.hidden = false;
        el.classList.toggle("is-error", excede);
        el.textContent = excede ? `${texto}: supera la capacidad de la línea en esas horas.` : texto;
        return excede;
    }

    /** Muestra qué insumos se necesitan y si alcanza el stock. */
    async function requerimiento(contenedor, { productoId, paquetes, fecha: f, almacenId, titulo }) {
        if (!productoId || !(paquetes > 0)) { contenedor.innerHTML = ""; return; }
        const r = await api("/api/produccion/requerimiento", { method: "POST", body: { productoId: Number(productoId), paquetes, fecha: f || null, almacenInsumosId: almacenId ? Number(almacenId) : null } });
        if (!r.ok) { contenedor.innerHTML = `<p class="previsto is-error">${esc(r.message || "No se pudo calcular.")}</p>`; return; }
        const d = r.data;
        const filas = d.lineas.map((l) => `<tr class="${l.falta > 0 ? "is-falta" : ""}">
                <td><span class="cel-principal">${esc(l.insumoCodigo)}</span><span class="cel-sub">${esc(l.insumoNombre)}</span></td>
                <td class="num">${cantidad(l.necesario)}<span class="u">${esc(l.unidadConsumoCodigo)}</span></td>
                <td class="num">${cantidad(l.disponible)}<span class="u">${esc(l.unidadConsumoCodigo)}</span></td>
                <td class="num">${l.falta > 0 ? `<b>${icono("error")} ${cantidad(l.falta)}</b>` : `${icono("check")}`}</td>
                <td class="num cel-tenue">${l.enCamino > 0 ? cantidad(l.enCamino) : "—"}</td></tr>`).join("");
        contenedor.innerHTML = `
            <div class="req-hd"><h3>${esc(titulo)} · receta v${d.recetaVersion}</h3>
                ${d.alcanza ? `<span class="badge badge-ok">${icono("check")}Alcanza el stock</span>` : `<span class="badge badge-crit">${icono("error")}Falta stock</span>`}</div>
            <table class="tabla-consumo"><thead><tr><th>Insumo</th><th class="num">Necesario (con merma)</th><th class="num">Disponible</th><th class="num">Falta</th><th class="num">En camino</th></tr></thead>
            <tbody>${filas}</tbody></table>`;
    }

    // ------------------------------------------------------------ registrar parte
    const dlgParte = document.getElementById("dlgParte");
    const fParte = dlgParte.querySelector("form");
    let planActual = null;

    const actualizarParte = debounce(() => {
        const d = leerFormulario(fParte);
        capacidad(document.getElementById("pCapacidad"), d.lineaId, d.productoId, d.paquetes, d.horasOperacion);
        requerimiento(document.getElementById("pReq"), { productoId: d.productoId, paquetes: d.paquetes, fecha: d.fecha, almacenId: d.almacenInsumosId, titulo: "Insumos que se descontarán" });
    }, 250);

    fParte.addEventListener("change", (e) => {
        if (e.target.name === "lineaId") {
            llenarProductos(fParte.elements.productoId, e.target.value, fParte.elements.productoId.value);
            llenarAlmacenes(fParte.elements.almacenInsumosId, e.target.value, 1);
            llenarAlmacenes(fParte.elements.almacenProductoId, e.target.value, 2);
        }
        actualizarParte();
    });
    fParte.addEventListener("input", (e) => { if (e.target.type === "number") actualizarParte(); });

    function abrirParte(p) {
        planActual = p ?? null;
        fParte.reset();
        limpiarErrores(fParte);
        fParte.elements.fecha.value = p ? p.fecha : hoyIso();
        fParte.elements.fecha.max = hoyIso();
        fParte.elements.lineaId.innerHTML = opcionesLinea();
        if (op.lineas.length === 1 && !p) fParte.elements.lineaId.value = String(op.lineas[0].id);
        if (p) fParte.elements.lineaId.value = String(p.lineaId);
        const lineaId = fParte.elements.lineaId.value;
        llenarProductos(fParte.elements.productoId, lineaId, p?.productoId);
        llenarAlmacenes(fParte.elements.almacenInsumosId, lineaId, 1);
        llenarAlmacenes(fParte.elements.almacenProductoId, lineaId, 2);
        if (p) fParte.elements.paquetes.value = p.paquetes;
        const info = document.getElementById("pPlanInfo");
        info.hidden = !p;
        info.innerHTML = p ? `${icono("calendario")}<span>Ejecuta la programación del ${esc(fecha(p.fecha))}: ${paq(p.paquetes)} paquetes de ${esc(p.productoNombre)} en ${esc(p.lineaCodigo)}. Ajuste los paquetes a lo realmente producido.</span>` : "";
        document.getElementById("pCapacidad").hidden = true;
        document.getElementById("pReq").innerHTML = "";
        abrir(dlgParte);
        actualizarParte();
    }

    fParte.addEventListener("submit", async (e) => {
        e.preventDefault();
        const d = leerFormulario(fParte);
        const r = await enviar(fParte, "/api/produccion/partes", "POST", {
            fecha: d.fecha ?? hoyIso(), turno: d.turno, lineaId: d.lineaId, productoId: d.productoId, paquetes: d.paquetes,
            horasOperacion: d.horasOperacion, almacenInsumosId: d.almacenInsumosId, almacenProductoId: d.almacenProductoId,
            planId: planActual?.id ?? null, observacion: d.observacion
        });
        if (!r) return;
        cerrar(dlgParte);
        toast(r.message);
        partes.cargar();
        recargarPlan();
        mostrarVista("partes");
    });

    // ------------------------------------------------------------ programar
    const dlgPlan = document.getElementById("dlgPlan");
    const fPlan = dlgPlan.querySelector("form");
    let editandoPlan = null;

    const actualizarPlan = debounce(() => {
        const d = leerFormulario(fPlan);
        capacidad(document.getElementById("lCapacidad"), d.lineaId, d.productoId, d.paquetes, null);
        requerimiento(document.getElementById("lReq"), { productoId: d.productoId, paquetes: d.paquetes, fecha: d.fecha, almacenId: null, titulo: "Insumos necesarios (stock de todos los almacenes de insumos)" });
    }, 250);
    fPlan.addEventListener("change", (e) => {
        if (e.target.name === "lineaId") llenarProductos(fPlan.elements.productoId, e.target.value, fPlan.elements.productoId.value);
        actualizarPlan();
    });
    fPlan.addEventListener("input", (e) => { if (e.target.type === "number") actualizarPlan(); });

    function abrirPlan(p) {
        editandoPlan = p ?? null;
        fPlan.reset();
        limpiarErrores(fPlan);
        document.getElementById("dlgPlanTitulo").textContent = p ? "Editar programación" : "Programar producción";
        fPlan.elements.fecha.min = p && p.fecha < hoyIso() ? p.fecha : hoyIso();
        fPlan.elements.fecha.value = p ? p.fecha : hoyIso();
        fPlan.elements.lineaId.innerHTML = opcionesLinea();
        if (p) fPlan.elements.lineaId.value = String(p.lineaId);
        else if (op.lineas.length === 1) fPlan.elements.lineaId.value = String(op.lineas[0].id);
        llenarProductos(fPlan.elements.productoId, fPlan.elements.lineaId.value, p?.productoId);
        if (p) { fPlan.elements.paquetes.value = p.paquetes; fPlan.elements.comentario.value = p.comentario ?? ""; }
        document.getElementById("lCapacidad").hidden = true;
        document.getElementById("lReq").innerHTML = "";
        abrir(dlgPlan);
        actualizarPlan();
    }

    fPlan.addEventListener("submit", async (e) => {
        e.preventDefault();
        const d = leerFormulario(fPlan);
        const cuerpo = { fecha: d.fecha ?? hoyIso(), lineaId: d.lineaId, productoId: d.productoId, paquetes: d.paquetes, comentario: d.comentario };
        const r = editandoPlan
            ? await enviar(fPlan, `/api/produccion/planes/${editandoPlan.id}`, "PUT", cuerpo)
            : await enviar(fPlan, "/api/produccion/planes", "POST", cuerpo);
        if (!r) return;
        cerrar(dlgPlan);
        toast(r.message);
        mostrarVista("plan");
        recargarPlan();
    });

    // ------------------------------------------------------------ detalle y anulación
    async function verParte(id) {
        const r = await api(`/api/produccion/partes/${id}`);
        if (!r.ok) { toast(r.message, "error"); return; }
        const p = r.data;
        document.getElementById("dlgDetalleTitulo").textContent = `Parte de producción PP-${p.numero}`;
        document.getElementById("detalleCuerpo").innerHTML = `
            <dl class="facts facts-borde">
                <div><dt>Fecha y turno</dt><dd>${esc(fecha(p.fecha))} <small>${esc(p.turnoNombre)}</small></dd></div>
                <div><dt>Línea</dt><dd>${esc(p.lineaCodigo)}</dd></div>
                <div><dt>Producto</dt><dd>${esc(p.productoNombre)} <small>receta v${p.recetaVersion}</small></dd></div>
                <div><dt>Producido</dt><dd>${paq(p.paquetes)} paquetes <small>${numero(Math.round(p.botellas))} botellas</small></dd></div>
                <div><dt>Horas / eficiencia</dt><dd>${p.horasOperacion ? `${cantidad(p.horasOperacion)} h <small>${cantidad(p.eficiencia)} %</small>` : "—"}</dd></div>
                <div><dt>Registró</dt><dd>${esc(p.creadoPor)} <small>${esc(fechaHora(p.creadoEn))}</small></dd></div>
            </dl>
            ${p.estado === 2 ? `<p class="form-alert">${icono("deshacer")}<span>Anulado: ${esc(p.motivoAnulacion)}. Los insumos volvieron a ${esc(p.almacenInsumosCodigo)}.</span></p>` : ""}
            ${p.observacion ? `<p class="hint">Observación: ${esc(p.observacion)}</p>` : ""}
            <h3 class="sub-titulo">Insumos descontados de ${esc(p.almacenInsumosCodigo)}</h3>
            <table class="tabla-consumo"><thead><tr><th>Insumo</th><th class="num">Cantidad (con merma)</th><th></th></tr></thead><tbody>
            ${p.consumos.map((c) => `<tr><td><span class="cel-principal">${esc(c.insumoCodigo)}</span><span class="cel-sub">${esc(c.insumoNombre)}</span></td><td class="num">${cantidad(c.cantidad)}<span class="u">${esc(c.unidadConsumoCodigo)}</span></td>
                <td class="num"><a href="/kardex?insumo=${c.insumoId}">Kardex</a></td></tr>`).join("")}
            </tbody></table>`;
        abrir(document.getElementById("dlgDetalle"));
    }

    const dlgAnular = document.getElementById("dlgAnular");
    const fAnular = dlgAnular.querySelector("form");
    let anulando = null;
    function abrirAnular(p) {
        anulando = p;
        fAnular.reset();
        limpiarErrores(fAnular);
        dlgAnular.querySelector("[data-resumen]").innerHTML =
            `PP-${esc(p.numero)} del ${esc(fecha(p.fecha))}: ${paq(p.paquetes)} paquetes de ${esc(p.productoNombre)}. ` +
            `Los insumos volverán a ${esc(p.almacenInsumosCodigo)} y los paquetes se retirarán de ${esc(p.almacenProductoCodigo)} (si aún están en stock).`;
        abrir(dlgAnular);
        fAnular.elements.motivo.focus();
    }
    fAnular.addEventListener("submit", async (e) => {
        e.preventDefault();
        const r = await enviar(fAnular, `/api/produccion/partes/${anulando.id}/anular`, "POST");
        if (!r) return;
        cerrar(dlgAnular);
        toast(r.message);
        partes.cargar();
        recargarPlan();
    });

    // ------------------------------------------------------------ inicio
    (async () => {
        const r = await api("/api/produccion/opciones");
        if (r.ok) op = r.data;
        else toast(r.message, "error");
        const lineas = op.lineas.map((l) => `<option value="${l.id}">${esc(l.codigo)}</option>`).join("");
        const productos = op.productos.map((p) => `<option value="${p.id}">${esc(p.codigo)}</option>`).join("");
        document.querySelectorAll("[data-lineas]").forEach((s) => s.insertAdjacentHTML("beforeend", lineas));
        document.querySelectorAll("[data-productos]").forEach((s) => s.insertAdjacentHTML("beforeend", productos));
        partes.cargar();
        plan.cargar();
        const parametros = new URLSearchParams(location.search);
        if (parametros.get("vista") === "plan") mostrarVista("plan");
    })();
    document.getElementById("btnParte")?.addEventListener("click", () => abrirParte());
    document.getElementById("btnPlan")?.addEventListener("click", () => abrirPlan());
})();
