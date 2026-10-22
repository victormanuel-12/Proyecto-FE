"use strict";

(() => {
    const { esc, icono, numero, api, toast, abrir, cerrar, confirmar, limpiarErrores, mostrarErrores, leerFormulario, enviar, ocupado, estado, boton, Listado } = Spin;

    const tabla = document.getElementById("tabla");
    const puedeProduccion = tabla.dataset.puedeProduccion === "true";

    const drawer = document.getElementById("drawer");
    const dlgProducto = document.getElementById("dlgProducto");
    const formProducto = dlgProducto.querySelector("form");
    const dlgReceta = document.getElementById("dlgReceta");
    const formReceta = dlgReceta.querySelector("form");
    const dlgConsumo = document.getElementById("dlgConsumo");
    const formConsumo = dlgConsumo.querySelector("form");

    const NIVELES = { 1: "Por botella", 2: "Por paquete", 3: "Por pallet" };
    const ENVASES = { 1: "PET", 2: "Vidrio retornable", 3: "Lata" };

    let editando = null;   // producto del formulario
    let abierto = null;    // producto del panel (con receta, historial y líneas)
    let formatos = [];
    let insumosReceta = [];

    // Fechas en formato local (yyyy-MM-dd para inputs; dd/MM/yyyy para leer).
    const hoyIso = () => {
        const d = new Date();
        return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
    };
    const fecha = (iso) => (iso ? iso.split("-").reverse().join("/") : "—");
    const diaAnterior = (iso) => {
        const [a, m, d] = iso.split("-").map(Number);
        const f = new Date(a, m - 1, d - 1);
        return `${f.getFullYear()}-${String(f.getMonth() + 1).padStart(2, "0")}-${String(f.getDate()).padStart(2, "0")}`;
    };

    // ------------------------------------------------------------ listado
    const celdaReceta = (p) => p.recetaVersion
        ? `<span class="cel-principal">v${p.recetaVersion}</span><span class="cel-sub">${numero(p.recetaInsumos)} insumos</span>`
        : `<span class="badge badge-warn">${icono("alerta")}Sin receta</span>`;

    const listado = new Listado({
        url: "/api/productos",
        tabla,
        filtros: document.getElementById("filtros"),
        segmentos: document.getElementById("segmentos"),
        pie: document.getElementById("pie"),
        textoVacio: puedeProduccion ? "Aún no hay productos. Registre el primero con «Nuevo producto»." : "Aún no hay productos registrados.",
        columnas: [
            { titulo: "SKU", clase: "cel-codigo", render: (p) => esc(p.codigo) },
            {
                titulo: "Producto",
                render: (p) => `<span class="cel-principal">${esc(p.nombre)}</span>` + (p.sabor ? `<span class="cel-sub">${esc(p.sabor)}</span>` : "")
            },
            { titulo: "Formato", render: (p) => `<span class="chip">${esc(p.formatoNombre)}</span>` },
            {
                titulo: "Empaque",
                clase: "num",
                render: (p) => `${numero(p.unidadesPorPaquete)} × ${numero(p.paquetesPorPallet)}<span class="cel-sub">bot/paq × paq/pallet</span>`
            },
            { titulo: "Receta", render: celdaReceta },
            { titulo: "Estado", render: (p) => estado(p.activo) }
        ],
        acciones: (p) =>
            boton("ver", "ojo", "Ver detalle y receta") +
            (puedeProduccion
                ? boton("editar", "editar", "Editar") +
                  boton("estado", "encender", p.activo ? "Desactivar" : "Activar") +
                  boton("eliminar", "eliminar", "Eliminar", { peligro: true, deshabilitado: p.versiones > 0 })
                : ""),
        alAccion: (accion, p) => {
            if (accion === "ver") verDetalle(p.id);
            else if (accion === "editar") abrirFormulario(p);
            else if (accion === "estado") cambiarEstado(p);
            else if (accion === "eliminar") eliminar(p);
        },
        alClicFila: (p) => verDetalle(p.id),
        alCargar: () => { if (abierto && drawer.open) listado.marcar(abierto.id); }
    });

    // ------------------------------------------------------------ panel de detalle
    async function verDetalle(id) {
        const cuerpo = drawer.querySelector("[data-cuerpo]");
        if (!drawer.open) {
            cuerpo.innerHTML = `<div class="drawer-cargando"><span class="spinner" aria-hidden="true"></span>Cargando…</div>`;
            drawer.showModal();
        }
        listado.marcar(id);
        const r = await api(`/api/productos/${id}`);
        if (!r.ok) {
            cuerpo.innerHTML = `<div class="drawer-cargando" role="alert">${icono("error")}${esc(r.message)}</div>`;
            return;
        }
        abierto = r.data;
        pintarDetalle();
    }

    drawer.addEventListener("close", () => { abierto = null; listado.marcar(null); });

    function htmlReceta(detalles) {
        const grupos = [1, 2, 3].map((nivel) => {
            const filas = detalles.filter((d) => d.nivel === nivel);
            if (!filas.length) return "";
            return `<div class="receta-grupo"><h4>${NIVELES[nivel]}</h4><ul class="receta-lista">${filas.map((d) => `
                <li>
                    <span class="ri-nombre">${esc(d.insumoNombre)}<small>${esc(d.insumoCodigo)}${d.insumoActivo ? "" : " · insumo inactivo"}</small></span>
                    <span class="ri-cant">${Spin.cantidad(d.cantidad)} ${esc(d.unidadConsumoCodigo)}<small>${d.mermaPorcentaje ? `merma ${numero(d.mermaPorcentaje)} %` : "sin merma"}</small></span>
                </li>`).join("")}</ul></div>`;
        }).join("");
        return grupos || `<p class="vacio-sec">La receta no tiene insumos.</p>`;
    }

    function pintarDetalle() {
        const p = abierto;
        drawer.querySelector("[data-codigo]").textContent = p.codigo;
        drawer.querySelector("[data-nombre]").textContent = p.nombre;
        drawer.querySelector("[data-estados]").innerHTML = estado(p.activo) + ` <span class="chip">${esc(p.formatoNombre)}</span>`;
        drawer.querySelector("[data-calcular]").disabled = !p.recetaVigente;

        const lineas = p.lineasCompatibles?.length
            ? `<span class="chips">${p.lineasCompatibles.map((l) => `<span class="chip">${esc(l)}</span>`).join("")}</span>`
            : `<span class="badge badge-warn">${icono("alerta")}Ninguna línea activa</span>`;

        const rv = p.recetaVigente;
        const receta = rv
            ? `<p class="hint">Versión ${rv.version} · rige desde el ${fecha(rv.vigenteDesde)}${rv.comentario ? ` · ${esc(rv.comentario)}` : ""}</p>
               <div style="margin-top:8px">${htmlReceta(rv.detalles)}</div>`
            : `<p class="vacio-sec">Sin receta vigente. Sin ella, SPIn no puede calcular cuántos insumos consume este producto.</p>`;

        const futura = (p.historial ?? []).find((v) => v.esFutura);
        const avisoFutura = futura
            ? `<p class="hint" style="margin-top:8px">${icono("historial")} La versión ${futura.version} entrará en vigencia el ${fecha(futura.vigenteDesde)}.</p>`
            : "";

        const historial = (p.historial ?? []).length
            ? `<ul class="versiones">${p.historial.map((v) => `
                <li>
                    <button type="button" class="version" data-version="${v.id}" aria-expanded="false">
                        <span class="version-num">v${v.version}</span>
                        <span>${fecha(v.vigenteDesde)} – ${v.vigenteHasta ? fecha(v.vigenteHasta) : "sin fin"}
                            <small>${numero(v.insumos)} insumos${v.creadoPor ? ` · ${esc(v.creadoPor)}` : ""}${v.comentario ? ` · ${esc(v.comentario)}` : ""}</small></span>
                        ${v.esVigente ? `<span class="badge badge-ok">${icono("check")}Vigente</span>` : v.esFutura ? `<span class="badge badge-warn">Próxima</span>` : `<span class="badge badge-off">Anterior</span>`}
                    </button>
                    <div class="version-detalle" data-detalle-version="${v.id}" hidden></div>
                </li>`).join("")}</ul>`
            : `<p class="hint">Aún no hay versiones.</p>`;

        drawer.querySelector("[data-cuerpo]").innerHTML = `
            <dl class="facts">
                <div><dt>Formato</dt><dd>${esc(p.formatoNombre)}</dd></div>
                <div><dt>Envase</dt><dd>${esc(p.tipoEnvaseNombre)}</dd></div>
                <div><dt>Sabor</dt><dd>${esc(p.sabor ?? "—")}</dd></div>
                <div><dt>Botellas / paquete</dt><dd>${numero(p.unidadesPorPaquete)}</dd></div>
                <div><dt>Paquetes / pallet</dt><dd>${numero(p.paquetesPorPallet)}</dd></div>
                <div><dt>Botellas / pallet</dt><dd>${numero(p.botellasPorPallet)}</dd></div>
            </dl>
            <section class="drawer-sec">
                <div class="drawer-sec-hd"><h3>Líneas que pueden producirlo</h3></div>
                ${lineas}
            </section>
            <section class="drawer-sec" aria-labelledby="secReceta">
                <div class="drawer-sec-hd">
                    <h3 id="secReceta">Receta vigente</h3>
                    ${puedeProduccion && p.activo ? `<button type="button" class="btn btn-secondary btn-sm" data-receta>${icono(rv ? "editar" : "mas")}${rv ? "Cambiar receta" : "Definir receta"}</button>` : ""}
                </div>
                ${receta}
                ${avisoFutura}
            </section>
            <section class="drawer-sec">
                <div class="drawer-sec-hd"><h3>Historial de versiones</h3></div>
                ${historial}
            </section>`;
    }

    drawer.addEventListener("click", async (e) => {
        if (!abierto) return;
        if (e.target.closest("[data-editar]")) { abrirFormulario(abierto); return; }
        if (e.target.closest("[data-receta]")) { abrirReceta(); return; }
        if (e.target.closest("[data-calcular]")) { abrirConsumo(); return; }

        const btnVersion = e.target.closest("[data-version]");
        if (btnVersion) {
            const id = btnVersion.dataset.version;
            const caja = drawer.querySelector(`[data-detalle-version="${id}"]`);
            const abrirla = btnVersion.getAttribute("aria-expanded") !== "true";
            btnVersion.setAttribute("aria-expanded", String(abrirla));
            caja.hidden = !abrirla;
            if (abrirla && !caja.dataset.cargada) {
                caja.innerHTML = `<p class="hint">Cargando…</p>`;
                const r = await api(`/api/productos/recetas/${id}`);
                caja.innerHTML = r.ok ? htmlReceta(r.data.detalles) : `<p class="field-error">${esc(r.message)}</p>`;
                if (r.ok) caja.dataset.cargada = "1";
            }
        }
    });

    // ------------------------------------------------------------ formulario de producto
    const selFormato = document.getElementById("pFormato");
    const empaque = document.getElementById("empaque");

    async function cargarFormatos() {
        if (formatos.length) return true;
        const r = await api("/api/formatos");
        if (!r.ok) { toast(r.message, "error"); return false; }
        formatos = r.data;
        selFormato.innerHTML = `<option value="">Seleccione…</option>` + [1, 2, 3].map((t) => {
            const lista = formatos.filter((f) => f.tipoEnvase === t);
            return lista.length ? `<optgroup label="${ENVASES[t]}">${lista.map((f) => `<option value="${f.id}">${esc(f.nombre)}</option>`).join("")}</optgroup>` : "";
        }).join("");
        return true;
    }

    function actualizarEmpaque() {
        const f = formProducto.elements;
        const porPaquete = Number(f.unidadesPorPaquete.value), porPallet = Number(f.paquetesPorPallet.value);
        if (porPaquete > 0 && porPallet > 0) {
            empaque.innerHTML = `${icono("flecha")}<span>1 pallet = ${numero(porPallet)} paquetes = <b>${numero(porPaquete * porPallet)}</b> botellas.</span>`;
            empaque.hidden = false;
        } else {
            empaque.hidden = true;
        }
    }
    formProducto.elements.unidadesPorPaquete.addEventListener("input", actualizarEmpaque);
    formProducto.elements.paquetesPorPallet.addEventListener("input", actualizarEmpaque);

    async function abrirFormulario(p = null) {
        if (!(await cargarFormatos())) return;
        editando = p;
        formProducto.reset();
        limpiarErrores(formProducto);
        dlgProducto.querySelector("[data-titulo]").textContent = p ? `Editar producto ${p.codigo}` : "Registrar producto";
        const f = formProducto.elements;
        if (p) {
            f.codigo.value = p.codigo;
            f.nombre.value = p.nombre;
            f.sabor.value = p.sabor ?? "";
            f.formatoId.value = String(p.formatoId);
            f.unidadesPorPaquete.value = p.unidadesPorPaquete;
            f.paquetesPorPallet.value = p.paquetesPorPallet;
        } else {
            f.unidadesPorPaquete.value = "12";
            f.paquetesPorPallet.value = "84";
        }
        actualizarEmpaque();
        if (drawer.open) drawer.close();
        abrir(dlgProducto);
        f.codigo.focus();
    }

    formProducto.addEventListener("submit", async (e) => {
        e.preventDefault();
        const r = editando
            ? await enviar(formProducto, `/api/productos/${editando.id}`, "PUT")
            : await enviar(formProducto, "/api/productos", "POST");
        if (!r) return;
        cerrar(dlgProducto);
        toast(r.message);
        listado.cargar();
        verDetalle(r.data.id);
    });

    async function cambiarEstado(p) {
        const activar = !p.activo;
        const ok = await confirmar({
            titulo: activar ? "Activar producto" : "Desactivar producto",
            mensaje: activar
                ? `${p.nombre} volverá a estar disponible para planificar y producir.`
                : `${p.nombre} ya no se podrá planificar ni producir. Su receta e historial se conservan.`,
            aceptar: activar ? "Activar" : "Desactivar",
            peligro: !activar
        });
        if (!ok) return;
        const r = await api(`/api/productos/${p.id}/estado`, { method: "PATCH", body: { activo: activar } });
        toast(r.message, r.ok ? "ok" : "error");
        if (r.ok) listado.cargar();
    }

    async function eliminar(p) {
        const ok = await confirmar({
            titulo: "Eliminar producto",
            mensaje: `Se eliminará ${p.nombre}. Esta acción no se puede deshacer.`,
            aceptar: "Eliminar",
            peligro: true
        });
        if (!ok) return;
        const r = await api(`/api/productos/${p.id}`, { method: "DELETE" });
        toast(r.message, r.ok ? "ok" : "error");
        if (r.ok) listado.cargar();
    }

    // ------------------------------------------------------------ editor de receta
    const filas = document.getElementById("filasReceta");
    const selCopiar = document.getElementById("rCopiar");
    const inDesde = document.getElementById("rDesde");
    const versionInfo = document.getElementById("versionInfo");
    let ultimaVersion = null;     // versión más reciente del historial (para explicar qué hará el guardado)
    let productoReceta = null;    // producto cuya receta se edita (el panel se cierra al abrir el editor)

    function opcionesInsumos(seleccionado) {
        const porCategoria = new Map();
        for (const i of insumosReceta) {
            if (!porCategoria.has(i.categoriaNombre)) porCategoria.set(i.categoriaNombre, []);
            porCategoria.get(i.categoriaNombre).push(i);
        }
        let html = `<option value="">Seleccione…</option>`;
        for (const [categoria, lista] of porCategoria) {
            html += `<optgroup label="${esc(categoria)}">${lista.map((i) =>
                `<option value="${i.id}"${i.id === seleccionado ? " selected" : ""}>${esc(i.codigo)} · ${esc(i.nombre)}</option>`).join("")}</optgroup>`;
        }
        return html;
    }

    const unidadDe = (insumoId) => insumosReceta.find((i) => i.id === insumoId)?.unidadConsumoCodigo ?? "—";

    function filaHtml(d = {}) {
        return `
            <tr>
                <td class="col-num" data-label="Fila"></td>
                <td data-label="Insumo"><select class="ctl" data-campo="insumoId" aria-label="Insumo">${opcionesInsumos(d.insumoId)}</select></td>
                <td class="col-cant" data-label="Cantidad">
                    <div class="ctl-unidad">
                        <input type="number" class="ctl num" data-campo="cantidad" min="0" step="any" inputmode="decimal" aria-label="Cantidad" value="${d.cantidad ?? ""}" />
                        <span data-unidad>${esc(d.insumoId ? unidadDe(d.insumoId) : "—")}</span>
                    </div>
                </td>
                <td class="col-nivel" data-label="Por">
                    <select class="ctl" data-campo="nivel" aria-label="Por">
                        ${[1, 2, 3].map((n) => `<option value="${n}"${(d.nivel ?? 1) === n ? " selected" : ""}>${NIVELES[n].replace("Por ", "")}</option>`).join("")}
                    </select>
                </td>
                <td class="col-merma" data-label="Merma">
                    <div class="ctl-unidad">
                        <input type="number" class="ctl num" data-campo="mermaPorcentaje" min="0" max="50" step="0.1" inputmode="decimal" aria-label="Merma" value="${d.mermaPorcentaje ?? 0}" />
                        <span>%</span>
                    </div>
                </td>
                <td class="col-quitar" data-label="">${boton("quitar-fila", "eliminar", "Quitar fila", { peligro: true })}</td>
            </tr>`;
    }

    function renumerar() {
        const trs = [...filas.querySelectorAll("tr:not(.editor-vacio)")];
        trs.forEach((tr, i) => { tr.querySelector(".col-num").textContent = i + 1; });
        if (!trs.length) {
            filas.innerHTML = `<tr class="editor-vacio"><td colspan="6">Agregue los insumos que consume este producto con «Agregar insumo» o copie la receta de otro producto.</td></tr>`;
        }
    }

    function agregarFila(d) {
        filas.querySelector(".editor-vacio")?.remove();
        filas.insertAdjacentHTML("beforeend", filaHtml(d));
        renumerar();
    }

    filas.addEventListener("change", (e) => {
        if (e.target.dataset.campo === "insumoId") {
            e.target.closest("tr").querySelector("[data-unidad]").textContent = unidadDe(Number(e.target.value));
        }
        e.target.classList.remove("is-invalid");
    });
    filas.addEventListener("input", (e) => e.target.classList.remove("is-invalid"));
    filas.addEventListener("click", (e) => {
        if (e.target.closest('[data-accion="quitar-fila"]')) {
            e.target.closest("tr").remove();
            renumerar();
        }
    });
    document.getElementById("btnAgregarFila").addEventListener("click", () => {
        agregarFila();
        filas.querySelector("tr:last-child [data-campo='insumoId']")?.focus();
    });

    function explicarVersion() {
        const desde = inDesde.value;
        if (!desde) { versionInfo.hidden = true; return; }
        versionInfo.hidden = false;
        let texto;
        if (!ultimaVersion) {
            texto = `Se creará la versión 1, vigente desde el ${fecha(desde)}.`;
        } else if (desde < ultimaVersion.vigenteDesde) {
            texto = `No se puede: la fecha debe ser igual o posterior al ${fecha(ultimaVersion.vigenteDesde)} (inicio de la versión ${ultimaVersion.version}).`;
        } else if (desde === ultimaVersion.vigenteDesde) {
            texto = `Se corregirá la versión ${ultimaVersion.version} (misma fecha de inicio). La producción ya registrada con ella se recalculará con los nuevos valores.`;
        } else {
            texto = `Se creará la versión ${ultimaVersion.version + 1} desde el ${fecha(desde)}. La versión ${ultimaVersion.version} seguirá rigiendo hasta el ${fecha(diaAnterior(desde))}.`;
        }
        versionInfo.innerHTML = `${icono("historial")}<span>${esc(texto)}</span>`;
    }
    inDesde.addEventListener("change", explicarVersion);
    inDesde.addEventListener("input", explicarVersion);

    async function abrirReceta() {
        const p = abierto;
        productoReceta = p;
        const [rInsumos, rCopiar] = await Promise.all([
            insumosReceta.length ? Promise.resolve({ ok: true, data: insumosReceta }) : api("/api/productos/insumos-receta"),
            api(`/api/productos/con-receta?excluir=${p.id}`)
        ]);
        if (!rInsumos.ok) { toast(rInsumos.message, "error"); return; }
        insumosReceta = rInsumos.data;
        if (!insumosReceta.length) {
            toast("No hay insumos activos de consumo por producción. Regístrelos primero en Insumos.", "error");
            return;
        }

        formReceta.reset();
        limpiarErrores(formReceta);
        dlgReceta.querySelector("[data-titulo]").textContent = `Receta de ${p.nombre}`;
        dlgReceta.querySelector("[data-contexto]").innerHTML =
            `<span>Formato <b>${esc(p.formatoNombre)}</b></span>` +
            `<span>1 paquete = <b>${numero(p.unidadesPorPaquete)}</b> botellas</span>` +
            `<span>1 pallet = <b>${numero(p.paquetesPorPallet)}</b> paquetes (${numero(p.botellasPorPallet)} botellas)</span>`;

        const opciones = rCopiar.ok ? rCopiar.data : [];
        selCopiar.innerHTML = `<option value="">Copiar la receta de otro producto…</option>` +
            opciones.map((o) => `<option value="${o.id}">${esc(o.nombre)}</option>`).join("");
        selCopiar.disabled = !opciones.length;
        document.getElementById("btnCopiar").disabled = !opciones.length;

        filas.innerHTML = "";
        (p.recetaVigente?.detalles ?? []).forEach((d) => agregarFila(d));
        renumerar();

        ultimaVersion = (p.historial ?? [])[0] ?? null; // viene ordenado de la más reciente a la más antigua
        inDesde.value = hoyIso();
        if (ultimaVersion && hoyIso() < ultimaVersion.vigenteDesde) inDesde.value = ultimaVersion.vigenteDesde;
        inDesde.min = ultimaVersion ? ultimaVersion.vigenteDesde : "";
        formReceta.elements.comentario.value = "";
        explicarVersion();

        if (drawer.open) drawer.close();
        abrir(dlgReceta);
    }

    document.getElementById("btnCopiar").addEventListener("click", async () => {
        const id = selCopiar.value;
        if (!id) { selCopiar.focus(); return; }
        const hayFilas = filas.querySelector("tr:not(.editor-vacio)");
        if (hayFilas) {
            const ok = await confirmar({
                titulo: "Reemplazar filas",
                mensaje: "Las filas actuales se reemplazarán por la receta del producto elegido. Podrá ajustarlas antes de guardar.",
                aceptar: "Reemplazar"
            });
            if (!ok) return;
        }
        const r = await api(`/api/productos/${id}/receta`);
        if (!r.ok) { toast(r.message, "error"); return; }
        filas.innerHTML = "";
        const disponibles = new Set(insumosReceta.map((i) => i.id));
        const omitidos = r.data.detalles.filter((d) => !disponibles.has(d.insumoId));
        r.data.detalles.filter((d) => disponibles.has(d.insumoId)).forEach((d) => agregarFila(d));
        renumerar();
        toast(omitidos.length
            ? `Receta copiada. Se omitieron ${omitidos.length} insumo(s) inactivos o que no se consumen por producción.`
            : "Receta copiada. Revise las cantidades antes de guardar.");
    });

    /** Lee y valida las filas en el navegador para señalar exactamente qué corregir. */
    function leerFilas() {
        const trs = [...filas.querySelectorAll("tr:not(.editor-vacio)")];
        const detalles = [];
        const errores = [];
        const vistos = new Map();
        trs.forEach((tr, i) => {
            const insumo = tr.querySelector('[data-campo="insumoId"]');
            const cantidad = tr.querySelector('[data-campo="cantidad"]');
            const nivel = tr.querySelector('[data-campo="nivel"]');
            const merma = tr.querySelector('[data-campo="mermaPorcentaje"]');
            const n = i + 1;
            const insumoId = Number(insumo.value);
            const cant = Number(cantidad.value);
            const mer = merma.value === "" ? 0 : Number(merma.value);

            if (!insumoId) { insumo.classList.add("is-invalid"); errores.push(`Fila ${n}: elija el insumo.`); }
            else if (vistos.has(insumoId)) { insumo.classList.add("is-invalid"); errores.push(`Fila ${n}: el insumo ya está en la fila ${vistos.get(insumoId)}.`); }
            else vistos.set(insumoId, n);
            if (!(cant > 0)) { cantidad.classList.add("is-invalid"); errores.push(`Fila ${n}: la cantidad debe ser mayor que 0.`); }
            if (!(mer >= 0 && mer <= 50)) { merma.classList.add("is-invalid"); errores.push(`Fila ${n}: la merma debe estar entre 0 % y 50 %.`); }

            detalles.push({ insumoId, cantidad: cant, nivel: Number(nivel.value), mermaPorcentaje: mer });
        });
        if (!trs.length) errores.push("Agregue al menos un insumo.");
        return { detalles, errores };
    }

    formReceta.addEventListener("submit", async (e) => {
        e.preventDefault();
        limpiarErrores(formReceta);
        filas.querySelectorAll(".is-invalid").forEach((x) => x.classList.remove("is-invalid"));

        const { detalles, errores } = leerFilas();
        const datos = leerFormulario(formReceta);
        if (!datos.vigenteDesde) errores.unshift("Indique desde qué fecha rige la receta.");
        if (errores.length) {
            const destino = formReceta.querySelector('[data-error-de="detalles"]');
            destino.textContent = errores.slice(0, 3).join(" ") + (errores.length > 3 ? ` (y ${errores.length - 3} más)` : "");
            (filas.querySelector(".is-invalid") ?? inDesde).focus();
            return;
        }

        const r = await enviar(formReceta, `/api/productos/${productoReceta.id}/receta`, "PUT",
            { vigenteDesde: datos.vigenteDesde, comentario: datos.comentario, detalles });
        if (!r) return;
        cerrar(dlgReceta);
        toast(r.message);
        listado.cargar();
        verDetalle(r.data.id);
    });

    // ------------------------------------------------------------ calculadora de consumo
    const resultado = document.getElementById("resultadoConsumo");

    function abrirConsumo() {
        const p = abierto;
        formConsumo.reset();
        limpiarErrores(formConsumo);
        resultado.innerHTML = "";
        dlgConsumo.querySelector("[data-producto]").textContent = p.nombre;
        formConsumo.elements.fecha.value = hoyIso();
        formConsumo.elements.cantidad.value = String(p.botellasPorPallet);
        dlgConsumo.dataset.productoId = p.id;
        abrir(dlgConsumo);
        formConsumo.elements.cantidad.focus();
    }

    formConsumo.addEventListener("submit", async (e) => {
        e.preventDefault();
        const datos = leerFormulario(formConsumo);
        const boton = formConsumo.querySelector('[type="submit"]');
        ocupado(boton, true);
        const r = await api(`/api/productos/${dlgConsumo.dataset.productoId}/consumo`, { method: "POST", body: datos });
        ocupado(boton, false);
        if (!r.ok) {
            mostrarErrores(formConsumo, r.status === 400 ? r.data : null, r.message);
            resultado.innerHTML = "";
            return;
        }
        limpiarErrores(formConsumo);
        const d = r.data;
        resultado.innerHTML = `
            <p class="resumen-consumo">
                <span><b>${numero(d.botellas)}</b> botellas</span>
                <span><b>${numero(d.paquetes)}</b> paquetes</span>
                <span><b>${numero(d.pallets)}</b> pallets</span>
                <span>Receta <b>v${d.recetaVersion}</b> del ${fecha(d.fecha)}</span>
            </p>
            <table class="tabla-consumo">
                <thead><tr><th>Insumo</th><th class="num">Consumo con merma</th><th class="num">Sin merma</th><th class="num">En unidad de compra</th></tr></thead>
                <tbody>${d.insumos.map((c) => `
                    <tr>
                        <td data-label="Insumo"><span class="cel-principal">${esc(c.insumoNombre)}</span><span class="cel-sub">${esc(c.insumoCodigo)}</span></td>
                        <td class="num" data-label="Con merma"><b>${numero(c.consumoTotal)}</b><span class="u">${esc(c.unidadConsumoCodigo)}</span></td>
                        <td class="num" data-label="Sin merma">${numero(c.consumoNeto)}<span class="u">${esc(c.unidadConsumoCodigo)}</span></td>
                        <td class="num" data-label="En compra">${c.unidadCompraCodigo === c.unidadConsumoCodigo ? "—" : `${numero(c.enUnidadCompra)}<span class="u">${esc(c.unidadCompraCodigo)}</span>`}</td>
                    </tr>`).join("")}</tbody>
            </table>`;
    });

    // ------------------------------------------------------------ inicio
    document.getElementById("btnNuevo")?.addEventListener("click", () => abrirFormulario());
    listado.cargar();
})();
