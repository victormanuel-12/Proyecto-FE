"use strict";

(() => {
    const { esc, icono, numero, cantidad, dinero, fecha, fechaHora, hoyIso, api, toast, abrir, cerrar, limpiarErrores, mostrarErrores, leerFormulario, enviar, boton, llenarSelect, Listado } = Spin;

    const tabla = document.getElementById("tabla");
    const puedePedir = tabla.dataset.puedePedir === "true";
    const puedeRecibir = tabla.dataset.puedeRecibir === "true";
    const segmentos = document.getElementById("segmentos");
    const parametros = new URLSearchParams(location.search);

    const sumarDias = (iso, dias) => {
        const d = new Date(`${iso}T00:00:00`);
        d.setDate(d.getDate() + dias);
        return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
    };
    const diasHasta = (iso) => Math.round((new Date(`${iso}T00:00:00`) - new Date(`${hoyIso()}T00:00:00`)) / 86400000);

    function llegada(p) {
        if (p.diasAtraso > 0) return `${esc(fecha(p.fechaEstimada))}<span class="cel-sub"><span class="badge badge-crit">${icono("alerta")}${p.diasAtraso} día(s) de atraso</span></span>`;
        if (!p.enCamino) return esc(fecha(p.fechaEstimada));
        const d = diasHasta(p.fechaEstimada);
        return `${esc(fecha(p.fechaEstimada))}<span class="cel-sub">${d === 0 ? "Llega hoy" : d === 1 ? "Llega mañana" : `En ${d} días`}</span>`;
    }

    function estado(p) {
        const cls = { 1: "badge-off", 2: "badge-warn", 3: "badge-ok", 4: "badge-off" }[p.estado];
        const ic = { 1: "camion", 2: "camion", 3: "check", 4: "cerrar" }[p.estado];
        return `<span class="badge ${cls}">${icono(ic)}${esc(p.estadoNombre)}</span>`;
    }

    const avance = (p) => `<div class="nivel" title="${cantidad(p.avance)} % recibido"><div class="nivel-barra"><div class="nivel-relleno ${p.avance >= 100 ? "normal" : "bajo"}" style="width:${Math.min(100, p.avance)}%"></div></div><div class="nivel-pie"><span>${cantidad(p.avance)} % recibido</span></div></div>`;

    // ------------------------------------------------------------ listado
    const listado = new Listado({
        url: "/api/pedidos",
        tabla,
        filtros: document.getElementById("filtros"),
        segmentos,
        estadoInicial: parametros.get("estado") ?? "encamino",
        tamanoPagina: 20,
        pie: document.getElementById("pie"),
        textoVacio: "No hay pedidos en esta vista.",
        columnas: [
            { titulo: "Pedido", render: (p) => `<span class="cel-principal mono">${esc(p.numero)}</span><span class="cel-sub">${esc(fecha(p.fechaPedido))}${p.documentoProveedor ? ` · ${esc(p.documentoProveedor)}` : ""}</span>` },
            { titulo: "Proveedor", render: (p) => esc(p.proveedorNombre) },
            { titulo: "Llegada estimada", render: llegada },
            {
                titulo: "Insumos",
                render: (p) => `<span class="chips">${p.detalles.slice(0, 3).map((d) => `<span class="chip" title="${esc(d.insumoNombre)}">${esc(d.insumoCodigo)}: ${cantidad(d.cantidad)} ${esc(d.unidadCompraCodigo)}</span>`).join("")}${p.detalles.length > 3 ? `<span class="chip">+${p.detalles.length - 3}</span>` : ""}</span>`
            },
            { titulo: "Avance", render: avance },
            { titulo: "Total", clase: "num", render: (p) => `${esc(p.simbolo)} ${dinero(p.total)}` },
            { titulo: "Estado", render: estado }
        ],
        acciones: (p) => boton("ver", "ojo", "Ver detalle") +
            (puedeRecibir ? boton("recibir", "check", p.enCamino ? "Registrar llegada" : "Ya no está en camino", { deshabilitado: !p.enCamino }) : "") +
            (puedePedir ? boton("editar", "editar", p.editable ? "Editar" : "Solo se edita sin recepciones", { deshabilitado: !p.editable }) +
                boton("cancelar", "cerrar", p.enCamino ? "Cancelar pedido" : "Ya no está en camino", { deshabilitado: !p.enCamino, peligro: true }) : ""),
        alAccion: (accion, p) => {
            if (accion === "ver") verDetalle(p.id);
            else if (accion === "recibir") abrirRecibir(p.id);
            else if (accion === "editar") abrirPedido(p.id);
            else if (accion === "cancelar") abrirMotivo("cancelar", p);
        },
        alCargar: (d) => {
            for (const [k, v] of Object.entries(d.resumen)) {
                const el = segmentos.querySelector(`[data-resumen="${k}"]`);
                if (el) el.textContent = numero(v);
            }
            document.getElementById("llegan").textContent = d.resumen.lleganEn7Dias
                ? `${numero(d.resumen.lleganEn7Dias)} pedido(s) llegan en los próximos 7 días.` : "";
        }
    });
    segmentos.querySelectorAll("[data-estado]").forEach((s) => s.setAttribute("aria-pressed", String(s.dataset.estado === listado.estado.estado)));

    // ------------------------------------------------------------ nuevo / editar pedido
    const dlgPedido = document.getElementById("dlgPedido");
    const fPedido = dlgPedido.querySelector("form");
    const cuerpoLineas = document.querySelector("#lineas tbody");
    let condiciones = [];
    let editando = null;
    let estimadaManual = false;

    const condicion = (id) => condiciones.find((c) => c.insumoId === Number(id));

    function filaLinea(l = {}) {
        const tr = document.createElement("tr");
        tr.innerHTML = `
            <td><select class="ctl" data-campo="insumoId"><option value="">${condiciones.length ? "Seleccione…" : "Elija primero el proveedor"}</option>${condiciones.map((c) => `<option value="${c.insumoId}">${esc(c.codigo)} · ${esc(c.nombre)}</option>`).join("")}</select>
                <span class="cel-sub aviso-linea" data-aviso></span></td>
            <td><div class="ctl-unidad"><input type="number" class="ctl num" data-campo="cantidad" min="0" step="any" inputmode="decimal" /><span data-u>—</span></div></td>
            <td><input type="number" class="ctl num" data-campo="precioUnitario" min="0" step="any" inputmode="decimal" /></td>
            <td class="num" data-subtotal>—</td>
            <td class="col-quitar"><button type="button" class="icon-btn" data-quitar title="Quitar" aria-label="Quitar">${icono("eliminar")}</button></td>`;
        cuerpoLineas.appendChild(tr);
        if (l.insumoId) {
            tr.querySelector('[data-campo="insumoId"]').value = String(l.insumoId);
            tr.querySelector('[data-campo="cantidad"]').value = l.cantidad ?? "";
            tr.querySelector('[data-campo="precioUnitario"]').value = l.precioUnitario ?? "";
        }
        actualizarFila(tr);
        return tr;
    }

    function actualizarFila(tr) {
        const c = condicion(tr.querySelector('[data-campo="insumoId"]').value);
        const cant = Number(tr.querySelector('[data-campo="cantidad"]').value) || 0;
        const precio = Number(tr.querySelector('[data-campo="precioUnitario"]').value) || 0;
        tr.querySelector("[data-u]").textContent = c?.unidadCompraCodigo ?? "—";
        tr.querySelector("[data-subtotal]").textContent = cant > 0 ? dinero(Math.round(cant * precio * 100) / 100) : "—";
        const avisos = [];
        if (c && cant > 0) {
            avisos.push(`= ${cantidad(Math.round(cant * c.factorConversion * 1e4) / 1e4)} ${c.unidadConsumoCodigo} · entrega ${c.tiempoEntregaDias} días`);
            if (c.pedidoMinimo > 0 && cant < c.pedidoMinimo) avisos.push(`Bajo el pedido mínimo (${cantidad(c.pedidoMinimo)})`);
            if (c.multiploCompra > 0 && Math.abs(cant / c.multiploCompra - Math.round(cant / c.multiploCompra)) > 1e-6) avisos.push(`No es múltiplo de ${cantidad(c.multiploCompra)}`);
        }
        const aviso = tr.querySelector("[data-aviso]");
        aviso.textContent = avisos.join(" · ");
        aviso.classList.toggle("cel-alerta", avisos.length > 1);
    }

    function actualizarTotales() {
        let total = 0, entrega = 0;
        cuerpoLineas.querySelectorAll("tr").forEach((tr) => {
            if (!tr.querySelector("select")) return;
            const c = condicion(tr.querySelector('[data-campo="insumoId"]').value);
            const cant = Number(tr.querySelector('[data-campo="cantidad"]').value) || 0;
            const precio = Number(tr.querySelector('[data-campo="precioUnitario"]').value) || 0;
            total += cant * precio;
            if (c) entrega = Math.max(entrega, c.tiempoEntregaDias);
        });
        const moneda = fPedido.querySelector('input[name="moneda"]:checked')?.value === "2" ? "US$" : "S/";
        document.getElementById("totalPedido").innerHTML = `Total: <b>${moneda} ${dinero(Math.round(total * 100) / 100)}</b>`;
        const fechaPedido = fPedido.elements.fechaPedido.value || hoyIso();
        const hint = document.getElementById("oEstimadaHint");
        if (entrega > 0) {
            hint.textContent = `Según el mayor tiempo de entrega: ${entrega} días.`;
            if (!estimadaManual) fPedido.elements.fechaEstimada.value = sumarDias(fechaPedido, entrega);
        } else hint.textContent = "";
    }

    cuerpoLineas.addEventListener("change", (e) => {
        const tr = e.target.closest("tr");
        if (e.target.dataset.campo === "insumoId") {
            const c = condicion(e.target.value);
            const precio = tr.querySelector('[data-campo="precioUnitario"]');
            if (c) {
                precio.value = c.precioUnitario;
                const cant = tr.querySelector('[data-campo="cantidad"]');
                if (!cant.value && c.pedidoMinimo > 0) cant.value = c.pedidoMinimo;
                const moneda = fPedido.querySelector(`input[name="moneda"][value="${c.moneda}"]`);
                if (moneda && cuerpoLineas.rows.length === 1) moneda.checked = true;
            }
        }
        actualizarFila(tr);
        actualizarTotales();
    });
    cuerpoLineas.addEventListener("input", (e) => { const tr = e.target.closest("tr"); actualizarFila(tr); actualizarTotales(); });
    cuerpoLineas.addEventListener("click", (e) => {
        if (!e.target.closest("[data-quitar]")) return;
        e.target.closest("tr").remove();
        if (!cuerpoLineas.rows.length) filaLinea();
        actualizarTotales();
    });
    document.getElementById("btnLinea").addEventListener("click", () => { filaLinea().querySelector("select").focus(); });
    fPedido.elements.fechaEstimada.addEventListener("input", () => { estimadaManual = true; });
    fPedido.elements.fechaPedido.addEventListener("change", actualizarTotales);
    fPedido.addEventListener("change", (e) => { if (e.target.name === "moneda") actualizarTotales(); });

    async function cargarCondiciones(proveedorId, lineas = []) {
        condiciones = [];
        if (proveedorId) {
            const r = await api(`/api/pedidos/condiciones/${proveedorId}`);
            condiciones = r.ok ? r.data : [];
        }
        cuerpoLineas.innerHTML = "";
        if (proveedorId && !condiciones.length) {
            cuerpoLineas.innerHTML = `<tr class="editor-vacio"><td colspan="5">Este proveedor no tiene insumos asignados. Asígnelos en Insumos → Proveedores.</td></tr>`;
        } else if (lineas.length) lineas.forEach((l) => filaLinea(l));
        else filaLinea();
        actualizarTotales();
    }
    fPedido.elements.proveedorId.addEventListener("change", (e) => { estimadaManual = false; cargarCondiciones(e.target.value); });

    async function abrirPedido(id) {
        editando = null;
        fPedido.reset();
        limpiarErrores(fPedido);
        estimadaManual = false;
        await llenarSelect(fPedido.elements.proveedorId, "/api/proveedores/opciones?soloActivos=true", { vacio: "Seleccione…" });
        fPedido.elements.fechaPedido.max = hoyIso();
        if (id) {
            const r = await api(`/api/pedidos/${id}`);
            if (!r.ok) { toast(r.message, "error"); return; }
            editando = r.data;
            document.getElementById("dlgPedidoTitulo").textContent = `Editar pedido ${editando.numero}`;
            fPedido.elements.proveedorId.value = String(editando.proveedorId);
            fPedido.elements.fechaPedido.value = editando.fechaPedido;
            fPedido.querySelector(`input[name="moneda"][value="${editando.moneda}"]`).checked = true;
            fPedido.elements.documentoProveedor.value = editando.documentoProveedor ?? "";
            fPedido.elements.observacion.value = editando.observacion ?? "";
            await cargarCondiciones(editando.proveedorId, editando.detalles);
            fPedido.elements.fechaEstimada.value = editando.fechaEstimada;
            estimadaManual = true;
        } else {
            document.getElementById("dlgPedidoTitulo").textContent = "Nuevo pedido";
            fPedido.elements.fechaPedido.value = hoyIso();
            // Desde «Reposición de insumos»: proveedor, insumo y cantidad sugerida ya elegidos.
            const proveedor = Number(parametros.get("proveedor")) || null;
            const insumo = Number(parametros.get("insumo")) || null;
            const cantidadSugerida = Number(parametros.get("cantidad")) || null;
            if (proveedor && fPedido.elements.proveedorId.querySelector(`option[value="${proveedor}"]`)) {
                fPedido.elements.proveedorId.value = String(proveedor);
                await cargarCondiciones(proveedor);
                const c = condicion(insumo);
                if (c) {
                    cuerpoLineas.innerHTML = "";
                    filaLinea({ insumoId: c.insumoId, cantidad: cantidadSugerida ?? c.pedidoMinimo, precioUnitario: c.precioUnitario });
                    const moneda = fPedido.querySelector(`input[name="moneda"][value="${c.moneda}"]`);
                    if (moneda) moneda.checked = true;
                    actualizarTotales();
                }
            } else {
                await cargarCondiciones(null);
            }
            ["proveedor", "insumo", "cantidad", "nuevo"].forEach((k) => parametros.delete(k));
            history.replaceState(null, "", location.pathname);
        }
        abrir(dlgPedido);
    }

    fPedido.addEventListener("submit", async (e) => {
        e.preventDefault();
        const d = leerFormulario(fPedido);
        const detalles = [...cuerpoLineas.querySelectorAll("tr")].filter((tr) => tr.querySelector("select"))
            .map((tr) => ({
                insumoId: Number(tr.querySelector('[data-campo="insumoId"]').value) || 0,
                cantidad: Number(tr.querySelector('[data-campo="cantidad"]').value) || 0,
                precioUnitario: Number(tr.querySelector('[data-campo="precioUnitario"]').value) || 0
            }))
            .filter((l) => l.insumoId || l.cantidad);
        if (!detalles.length) { mostrarErrores(fPedido, { detalles: ["Agregue al menos un insumo."] }); return; }
        const cuerpo = {
            proveedorId: d.proveedorId, fechaPedido: d.fechaPedido ?? hoyIso(), fechaEstimada: d.fechaEstimada,
            moneda: d.moneda, documentoProveedor: d.documentoProveedor, observacion: d.observacion, detalles
        };
        const r = editando
            ? await enviar(fPedido, `/api/pedidos/${editando.id}`, "PUT", cuerpo)
            : await enviar(fPedido, "/api/pedidos", "POST", cuerpo);
        if (!r) return;
        cerrar(dlgPedido);
        toast(r.message);
        listado.cargar();
    });

    // ------------------------------------------------------------ recibir
    const dlgRecibir = document.getElementById("dlgRecibir");
    const fRecibir = dlgRecibir.querySelector("form");
    const cuerpoRecibir = document.querySelector("#lineasRecibir tbody");
    let recibiendo = null;

    async function abrirRecibir(id) {
        const [r] = await Promise.all([
            api(`/api/pedidos/${id}`),
            llenarSelect(fRecibir.elements.almacenId, "/api/almacenes/opciones?soloActivos=true&soloInsumos=true", { vacio: "Seleccione…" })
        ]);
        if (!r.ok) { toast(r.message, "error"); return; }
        recibiendo = r.data;
        fRecibir.reset();
        limpiarErrores(fRecibir);
        if (fRecibir.elements.almacenId.options.length === 2) fRecibir.elements.almacenId.selectedIndex = 1;
        document.getElementById("dlgRecibirTitulo").textContent = `Recibir pedido ${recibiendo.numero}`;
        dlgRecibir.querySelector("[data-resumen]").innerHTML =
            `<span>Proveedor: <b>${esc(recibiendo.proveedorNombre)}</b></span><span>Pedido el ${esc(fecha(recibiendo.fechaPedido))}</span><span>Llegada estimada: ${esc(fecha(recibiendo.fechaEstimada))}</span>`;
        cuerpoRecibir.innerHTML = recibiendo.detalles.map((d) => `
            <tr data-detalle="${d.id}">
                <td><span class="cel-principal">${esc(d.insumoCodigo)}</span><span class="cel-sub">${esc(d.insumoNombre)}</span></td>
                <td class="num">${cantidad(d.cantidad)}</td>
                <td class="num">${cantidad(d.recibida)}</td>
                <td class="num"><b>${cantidad(d.pendiente)}</b></td>
                <td><div class="ctl-unidad"><input type="number" class="ctl num" min="0" max="${d.pendiente}" step="any" value="${d.pendiente}" ${d.pendiente <= 0 ? "disabled" : ""} aria-label="Cantidad recibida de ${esc(d.insumoCodigo)}" /><span>${esc(d.unidadCompraCodigo)}</span></div></td>
                <td data-eq class="cel-tenue"></td>
            </tr>`).join("");
        cuerpoRecibir.querySelectorAll("tr").forEach(equivalencia);
        abrir(dlgRecibir);
        fRecibir.elements.guia.focus();
    }

    function equivalencia(tr) {
        const d = recibiendo.detalles.find((x) => String(x.id) === tr.dataset.detalle);
        const input = tr.querySelector("input");
        const n = Number(input.value) || 0;
        const eq = tr.querySelector("[data-eq]");
        input.classList.toggle("is-invalid", n > d.pendiente + 1e-6);
        eq.textContent = n > d.pendiente + 1e-6 ? `Excede lo pendiente (${cantidad(d.pendiente)})`
            : n > 0 ? `+${cantidad(Math.round(n * d.factorConversion * 1e4) / 1e4)} ${d.unidadConsumoCodigo}` : "No llegó";
        eq.classList.toggle("cel-alerta", n > d.pendiente + 1e-6);
    }
    cuerpoRecibir.addEventListener("input", (e) => equivalencia(e.target.closest("tr")));

    fRecibir.addEventListener("submit", async (e) => {
        e.preventDefault();
        const d = leerFormulario(fRecibir);
        const detalles = [...cuerpoRecibir.querySelectorAll("tr")].map((tr) => ({
            pedidoDetalleId: Number(tr.dataset.detalle), cantidad: Number(tr.querySelector("input").value) || 0
        })).filter((x) => x.cantidad > 0);
        if (!detalles.length) { mostrarErrores(fRecibir, { detalles: ["Indique la cantidad recibida de al menos un insumo."] }); return; }
        const r = await enviar(fRecibir, `/api/pedidos/${recibiendo.id}/recepciones`, "POST", {
            almacenId: d.almacenId, guia: d.guia ?? "", observacion: d.observacion, detalles
        });
        if (!r) return;
        cerrar(dlgRecibir);
        toast(r.message);
        listado.cargar();
    });

    // ------------------------------------------------------------ detalle
    async function verDetalle(id) {
        const r = await api(`/api/pedidos/${id}`);
        if (!r.ok) { toast(r.message, "error"); return; }
        const p = r.data;
        document.getElementById("dlgDetalleTitulo").textContent = `Pedido ${p.numero}`;
        document.getElementById("detalleCuerpo").innerHTML = `
            <dl class="facts facts-borde">
                <div><dt>Proveedor</dt><dd>${esc(p.proveedorNombre)}</dd></div>
                <div><dt>Pedido / llegada estimada</dt><dd>${esc(fecha(p.fechaPedido))} <small>→ ${esc(fecha(p.fechaEstimada))}${p.diasAtraso ? ` · ${p.diasAtraso} día(s) de atraso` : ""}</small></dd></div>
                <div><dt>Estado</dt><dd>${esc(p.estadoNombre)} <small>${cantidad(p.avance)} % recibido</small></dd></div>
                <div><dt>Total</dt><dd>${esc(p.simbolo)} ${dinero(p.total)}</dd></div>
                <div><dt>Documento</dt><dd>${esc(p.documentoProveedor ?? "—")}</dd></div>
                <div><dt>Registró</dt><dd>${esc(p.creadoPor ?? "—")}</dd></div>
            </dl>
            ${p.motivoCancelacion ? `<p class="form-alert">${icono("cerrar")}<span>Cancelado: ${esc(p.motivoCancelacion)}</span></p>` : ""}
            ${p.observacion ? `<p class="hint">Observación: ${esc(p.observacion)}</p>` : ""}
            <h3 class="sub-titulo">Insumos</h3>
            <table class="tabla-consumo"><thead><tr><th>Insumo</th><th class="num">Pedido</th><th class="num">Recibido</th><th class="num">Pendiente</th><th class="num">Precio</th><th class="num">Subtotal</th></tr></thead><tbody>
            ${p.detalles.map((d) => `<tr><td><span class="cel-principal">${esc(d.insumoCodigo)}</span><span class="cel-sub">${esc(d.insumoNombre)}</span></td>
                <td class="num">${cantidad(d.cantidad)}<span class="u">${esc(d.unidadCompraCodigo)}</span></td><td class="num">${cantidad(d.recibida)}</td>
                <td class="num">${p.enCamino ? cantidad(d.pendiente) : "—"}</td><td class="num">${dinero(d.precioUnitario)}</td><td class="num">${dinero(d.subtotal)}</td></tr>`).join("")}
            </tbody></table>
            <h3 class="sub-titulo">Recepciones</h3>
            ${p.recepciones.length ? `<ul class="recepciones">${p.recepciones.map((x) => `
                <li class="${x.anulada ? "is-anulada" : ""}">
                    <div><b>${esc(fechaHora(x.fecha))}</b> · ${esc(x.almacenCodigo)} · guía ${esc(x.guia)} · ${esc(x.usuario)}
                        ${x.anulada ? `<span class="badge badge-off">${icono("deshacer")}Anulada</span><span class="cel-sub">${esc(x.motivoAnulacion)}</span>` : ""}
                        <span class="cel-sub">${x.lineas.map((l) => `${esc(l.insumoCodigo)}: ${cantidad(l.cantidad)} ${esc(l.unidadCompraCodigo)}`).join(" · ")}</span></div>
                    ${puedeRecibir && !x.anulada && p.estado !== 4 ? `<button type="button" class="btn btn-secondary btn-sm" data-anular-recepcion="${x.id}">${icono("deshacer")}Anular</button>` : ""}
                </li>`).join("")}</ul>` : `<p class="hint">Aún no llega nada de este pedido.</p>`}`;
        document.getElementById("detalleCuerpo").querySelectorAll("[data-anular-recepcion]").forEach((b) =>
            b.addEventListener("click", () => { cerrar(document.getElementById("dlgDetalle")); abrirMotivo("anularRecepcion", p, Number(b.dataset.anularRecepcion)); }));
        abrir(document.getElementById("dlgDetalle"));
    }

    // ------------------------------------------------------------ cancelar pedido / anular recepción
    const dlgMotivo = document.getElementById("dlgMotivo");
    const fMotivo = dlgMotivo.querySelector("form");
    let accionMotivo = null;
    function abrirMotivo(tipo, p, recepcionId) {
        fMotivo.reset();
        limpiarErrores(fMotivo);
        if (tipo === "cancelar") {
            document.getElementById("dlgMotivoTitulo").textContent = `Cancelar pedido ${p.numero}`;
            dlgMotivo.querySelector("[data-resumen]").textContent = p.avance > 0
                ? "Lo ya recibido se conserva en el stock. El saldo pendiente deja de figurar en camino."
                : "El pedido deja de figurar en camino. No se modifica el stock.";
            document.getElementById("btnMotivo").textContent = "Cancelar pedido";
            accionMotivo = { url: `/api/pedidos/${p.id}/cancelar` };
        } else {
            document.getElementById("dlgMotivoTitulo").textContent = "Anular recepción";
            dlgMotivo.querySelector("[data-resumen]").textContent =
                "Se retirará del stock lo recibido en esa guía y el pedido volverá a figurar en camino. Solo es posible si el stock aún alcanza.";
            document.getElementById("btnMotivo").textContent = "Anular recepción";
            accionMotivo = { url: `/api/pedidos/recepciones/${recepcionId}/anular` };
        }
        abrir(dlgMotivo);
        fMotivo.elements.motivo.focus();
    }
    fMotivo.addEventListener("submit", async (e) => {
        e.preventDefault();
        const r = await enviar(fMotivo, accionMotivo.url, "POST");
        if (!r) return;
        cerrar(dlgMotivo);
        toast(r.message);
        listado.cargar();
    });

    // ------------------------------------------------------------ inicio
    document.getElementById("btnNuevo")?.addEventListener("click", () => abrirPedido());
    llenarSelect(document.getElementById("fProveedor"), "/api/proveedores/opciones?soloActivos=false", { vacio: "Todos" });
    listado.cargar();
    if (puedePedir && parametros.get("nuevo") === "1") abrirPedido();
})();
