"use strict";

(() => {
    const { esc, icono, numero, dinero, api, toast, abrir, cerrar, confirmar, limpiarErrores, leerFormulario, enviar, estado, boton, llenarSelect, Listado } = Spin;

    const tabla = document.getElementById("tabla");
    const puedeCatalogo = tabla.dataset.puedeCatalogo === "true";
    const puedeAbastecer = tabla.dataset.puedeAbastecer === "true";

    const drawer = document.getElementById("drawer");
    const dlgInsumo = document.getElementById("dlgInsumo");
    const formInsumo = dlgInsumo.querySelector("form");
    const dlgCondicion = document.getElementById("dlgCondicion");
    const formCondicion = dlgCondicion.querySelector("form");

    let editando = null;       // insumo que se edita en el formulario
    let abierto = null;        // insumo mostrado en el panel de detalle (con sus condiciones)
    let condicionEditando = null;

    // ------------------------------------------------------------ listado
    const celdaPrincipal = (i) => {
        if (!i.proveedorPrincipal) {
            return `<span class="badge badge-warn">${icono("alerta")}Sin proveedor</span>`;
        }
        const inactivo = i.proveedorPrincipalActivo === false ? ` <span class="badge badge-off">Inactivo</span>` : "";
        return `<span class="prov-principal"><span>${esc(i.proveedorPrincipal)}${inactivo}</span>` +
            `<small>entrega ${numero(i.entregaPrincipalDias)} d${i.proveedores > 1 ? ` · ${i.proveedores} proveedores` : ""}</small></span>`;
    };

    const celdaEstado = (i) => `<span class="estados">${estado(i.activo)}` +
        (i.esCritico ? `<span class="badge badge-crit">${icono("alerta")}Crítico</span>` : "") + `</span>`;

    const listado = new Listado({
        url: "/api/insumos",
        tabla,
        filtros: document.getElementById("filtros"),
        segmentos: document.getElementById("segmentos"),
        pie: document.getElementById("pie"),
        textoVacio: puedeCatalogo ? "Aún no hay insumos. Registre el primero con «Nuevo insumo»." : "Aún no hay insumos registrados.",
        columnas: [
            { titulo: "Código", clase: "cel-codigo", render: (i) => esc(i.codigo) },
            {
                titulo: "Insumo",
                render: (i) => `<span class="cel-principal">${esc(i.nombre)}</span>` +
                    `<span class="cel-sub">${esc(i.categoriaNombre)}${i.esImportado ? " · Importado" : ""}</span>`
            },
            {
                titulo: "Unidades",
                clase: "cel-unidades",
                render: (i) => i.unidadCompraId === i.unidadConsumoId
                    ? `<span class="u">${esc(i.unidadConsumoCodigo)}</span>`
                    : `<span class="u">${esc(i.unidadConsumoCodigo)}</span> <span class="cel-tenue">· 1 ${esc(i.unidadCompraCodigo)} = ${numero(i.factorConversion)}</span>`
            },
            {
                titulo: "Stock mín. – máx.",
                clase: "num rango",
                render: (i) => `${numero(i.stockMinimo)}<span class="sep">–</span>${numero(i.stockMaximo)}`
            },
            { titulo: "Proveedor principal", render: celdaPrincipal },
            { titulo: "Estado", render: celdaEstado }
        ],
        acciones: (i) =>
            boton("ver", "ojo", "Ver detalle") +
            (puedeCatalogo
                ? boton("editar", "editar", "Editar") +
                  boton("estado", "encender", i.activo ? "Desactivar" : "Activar") +
                  boton("eliminar", "eliminar", "Eliminar", { peligro: true, deshabilitado: i.proveedores > 0 })
                : ""),
        alAccion: (accion, i) => {
            if (accion === "ver") verDetalle(i.id);
            else if (accion === "editar") abrirFormulario(i);
            else if (accion === "estado") cambiarEstado(i);
            else if (accion === "eliminar") eliminar(i);
        },
        alClicFila: (i) => verDetalle(i.id),
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
        const r = await api(`/api/insumos/${id}`);
        if (!r.ok) {
            cuerpo.innerHTML = `<div class="drawer-cargando" role="alert">${icono("error")}${esc(r.message)}</div>`;
            return;
        }
        abierto = r.data;
        pintarDetalle();
    }

    drawer.addEventListener("close", () => { abierto = null; listado.marcar(null); });

    function pintarDetalle() {
        const i = abierto;
        drawer.querySelector("[data-codigo]").textContent = i.codigo;
        drawer.querySelector("[data-nombre]").textContent = i.nombre;
        drawer.querySelector("[data-estados]").innerHTML = celdaEstado(i) + (i.esImportado ? ` <span class="chip">Importado</span>` : "");

        const unidades = i.unidadCompraId === i.unidadConsumoId
            ? esc(i.unidadConsumoCodigo)
            : `${esc(i.unidadCompraCodigo)} → ${esc(i.unidadConsumoCodigo)}<small> · 1 = ${numero(i.factorConversion)}</small>`;

        const condiciones = i.condiciones ?? [];
        const lista = condiciones.length
            ? `<ul class="cond-list">${condiciones.map(htmlCondicion).join("")}</ul>`
            : `<p class="vacio-sec">Sin proveedores. Sin al menos uno, SPIn no podrá calcular cuándo ni cuánto pedir.</p>`;

        drawer.querySelector("[data-cuerpo]").innerHTML = `
            <dl class="facts">
                <div><dt>Categoría</dt><dd>${esc(i.categoriaNombre)}</dd></div>
                <div><dt>Unidades</dt><dd>${unidades}</dd></div>
                <div><dt>Consumo</dt><dd>${esc(i.tipoConsumoNombre)}</dd></div>
                <div><dt>Stock mínimo</dt><dd>${numero(i.stockMinimo)} <small>${esc(i.unidadConsumoCodigo)}</small></dd></div>
                <div><dt>Stock máximo</dt><dd>${numero(i.stockMaximo)} <small>${esc(i.unidadConsumoCodigo)}</small></dd></div>
                <div><dt>Capacidad</dt><dd>${i.capacidadMaxima != null ? `${numero(i.capacidadMaxima)} <small>${esc(i.unidadConsumoCodigo)}</small>` : "—"}</dd></div>
                <div><dt>Nivel de servicio</dt><dd>${numero(i.nivelServicio)} %</dd></div>
                <div><dt>Consumo estimado</dt><dd>${i.consumoEstimadoDiario != null ? `${numero(i.consumoEstimadoDiario)} <small>/ día</small>` : "—"}</dd></div>
                <div><dt>Lote y vencimiento</dt><dd>${i.controlaLote ? "Sí" : "No"}</dd></div>
            </dl>
            <section class="drawer-sec" aria-labelledby="secStock">
                <div class="drawer-sec-hd">
                    <h3 id="secStock">Stock actual</h3>
                    <a class="btn btn-secondary btn-sm" href="/kardex?insumo=${i.id}">${icono("libro")}Kardex</a>
                </div>
                ${htmlStock(i)}
            </section>
            <section class="drawer-sec" aria-labelledby="secProv">
                <div class="drawer-sec-hd">
                    <h3 id="secProv">Proveedores y condiciones de compra</h3>
                    ${puedeAbastecer && i.activo ? `<button type="button" class="btn btn-secondary btn-sm" data-agregar>${icono("mas")}Agregar</button>` : ""}
                </div>
                ${lista}
            </section>
            <p class="nota-sec">${icono("alerta")}<span>La cobertura en días y el pedido sugerido se mostrarán desde la Entrega 8.</span></p>`;
    }

    function htmlStock(i) {
        const filas = (i.stock ?? []).filter((s) => s.cantidad !== 0);
        const total = filas.reduce((t, s) => t + s.cantidad, 0);
        if (!filas.length) return `<p class="vacio-sec">Sin stock registrado. Cárguelo en Movimientos con «Stock inicial».</p>`;
        return `<ul class="receta-lista">${filas.map((s) => `
                    <li><span class="ri-nombre">${esc(s.almacenNombre)}<small>${esc(s.almacenCodigo)}</small></span>
                        <span class="ri-cant">${Spin.cantidad(s.cantidad)} ${esc(i.unidadConsumoCodigo)}</span></li>`).join("")}
                    <li><span class="ri-nombre"><b>Total</b></span><span class="ri-cant">${Spin.cantidad(total)} ${esc(i.unidadConsumoCodigo)}
                        <small>${total < i.stockMinimo ? "bajo el mínimo" : total > i.stockMaximo ? "sobre el máximo" : "dentro del rango"}</small></span></li>
                </ul>`;
    }

    function htmlCondicion(c) {
        const acciones = puedeAbastecer
            ? `<span class="acciones">
                   ${c.esPrincipal ? "" : boton("principal", "estrella", "Marcar como principal", { deshabilitado: !c.proveedorActivo })}
                   ${boton("editar-cond", "editar", "Editar condiciones")}
                   ${boton("quitar", "eliminar", "Quitar proveedor", { peligro: true })}
               </span>`
            : "";
        const variacion = c.variacionEntregaDias ? ` ± ${numero(c.variacionEntregaDias)}` : "";
        return `
            <li class="cond${c.esPrincipal ? " is-principal" : ""}" data-cond="${c.id}">
                <div class="cond-hd">
                    <span class="cond-nombre">${esc(c.proveedorNombre)}${c.esPrincipal ? `<span class="tag-principal">Principal</span>` : ""}
                        <small>${esc(c.proveedorPais)}${c.proveedorActivo ? "" : " · proveedor inactivo"}</small></span>
                    ${acciones}
                </div>
                <dl class="cond-datos">
                    <div><dt>Precio</dt><dd>${esc(c.monedaSimbolo)} ${dinero(c.precioUnitario)} / ${esc(c.unidadCompraCodigo)}</dd></div>
                    <div><dt>Entrega</dt><dd>${numero(c.tiempoEntregaDias)}${variacion} días</dd></div>
                    <div><dt>Pedido mínimo</dt><dd>${c.pedidoMinimo ? `${numero(c.pedidoMinimo)} ${esc(c.unidadCompraCodigo)}` : "—"}</dd></div>
                    <div><dt>Se compra de a</dt><dd>${numero(c.multiploCompra)} ${esc(c.unidadCompraCodigo)}</dd></div>
                </dl>
            </li>`;
    }

    drawer.addEventListener("click", async (e) => {
        if (!abierto) return;
        if (e.target.closest("[data-editar]")) { abrirFormulario(abierto); return; }
        if (e.target.closest("[data-agregar]")) { abrirCondicion(null); return; }

        const btn = e.target.closest("[data-accion]");
        if (!btn || btn.disabled) return;
        const id = Number(btn.closest("[data-cond]").dataset.cond);
        const c = abierto.condiciones.find((x) => x.id === id);
        if (!c) return;

        if (btn.dataset.accion === "editar-cond") abrirCondicion(c);
        else if (btn.dataset.accion === "principal") await accionCondicion(`/api/condiciones/${id}/principal`, "POST");
        else if (btn.dataset.accion === "quitar") {
            const ok = await confirmar({
                titulo: "Quitar proveedor",
                mensaje: `${c.proveedorNombre} dejará de abastecer ${abierto.nombre}.` +
                    (c.esPrincipal ? " Como es el principal, SPIn asignará el siguiente que entregue más rápido." : ""),
                aceptar: "Quitar",
                peligro: true
            });
            if (ok) await accionCondicion(`/api/condiciones/${id}`, "DELETE");
        }
    });

    async function accionCondicion(url, method) {
        const r = await api(url, { method });
        toast(r.message, r.ok ? "ok" : "error");
        if (!r.ok) return;
        await refrescarDetalle();
    }

    async function refrescarDetalle() {
        if (abierto) await verDetalle(abierto.id);
        listado.cargar();
    }

    // ------------------------------------------------------------ formulario de insumo
    const selCategoria = document.getElementById("iCategoria");
    const selUCompra = document.getElementById("iUCompra");
    const selUConsumo = document.getElementById("iUConsumo");
    const inFactor = document.getElementById("iFactor");
    const equivalencia = document.getElementById("equivalencia");

    function textoUnidad(select) {
        const opcion = select.selectedOptions[0];
        if (!opcion || !opcion.value) return null;
        const m = opcion.textContent.match(/\(([^)]+)\)\s*$/);
        return m ? m[1] : opcion.textContent;
    }

    function actualizarEquivalencia() {
        const compra = textoUnidad(selUCompra);
        const consumo = textoUnidad(selUConsumo);
        const iguales = selUCompra.value && selUCompra.value === selUConsumo.value;
        if (iguales) inFactor.value = "1";
        inFactor.readOnly = Boolean(iguales);
        const factor = Number(inFactor.value);
        if (compra && consumo && factor > 0) {
            equivalencia.innerHTML = `${icono("flecha")}<span>1 <b>${esc(compra)}</b> = <b>${numero(factor)}</b> ${esc(consumo)}. El stock y el consumo se registran en ${esc(consumo)}.</span>`;
            equivalencia.hidden = false;
        } else {
            equivalencia.hidden = true;
        }
    }
    [selUCompra, selUConsumo].forEach((s) => s.addEventListener("change", actualizarEquivalencia));
    inFactor.addEventListener("input", actualizarEquivalencia);

    function agregarOpcionSiFalta(select, id, texto) {
        if (!select.querySelector(`option[value="${id}"]`)) {
            // Se conserva el valor actual aunque esté inactivo; el código queda al final entre paréntesis.
            select.insertAdjacentHTML("beforeend", `<option value="${id}">Inactiva: ${esc(texto)} (${esc(texto)})</option>`);
        }
    }

    async function abrirFormulario(i = null) {
        editando = i;
        formInsumo.reset();
        limpiarErrores(formInsumo);
        dlgInsumo.querySelector("[data-titulo]").textContent = i ? `Editar insumo ${i.codigo}` : "Registrar insumo";

        await Promise.all([
            llenarSelect(selCategoria, "/api/categorias/opciones?soloActivas=true", { vacio: "Seleccione…" }),
            llenarSelect(selUCompra, "/api/unidades/opciones?soloActivas=true", { vacio: "Seleccione…" }),
            llenarSelect(selUConsumo, "/api/unidades/opciones?soloActivas=true", { vacio: "Seleccione…" })
        ]);

        const f = formInsumo.elements;
        if (i) {
            agregarOpcionSiFalta(selCategoria, i.categoriaId, i.categoriaNombre);
            agregarOpcionSiFalta(selUCompra, i.unidadCompraId, i.unidadCompraCodigo);
            agregarOpcionSiFalta(selUConsumo, i.unidadConsumoId, i.unidadConsumoCodigo);
            f.codigo.value = i.codigo;
            f.nombre.value = i.nombre;
            f.categoriaId.value = String(i.categoriaId);
            f.tipoConsumo.value = String(i.tipoConsumo);
            f.unidadCompraId.value = String(i.unidadCompraId);
            f.unidadConsumoId.value = String(i.unidadConsumoId);
            f.factorConversion.value = i.factorConversion;
            f.stockMinimo.value = i.stockMinimo;
            f.stockMaximo.value = i.stockMaximo;
            f.capacidadMaxima.value = i.capacidadMaxima ?? "";
            f.nivelServicio.value = i.nivelServicio;
            f.consumoEstimadoDiario.value = i.consumoEstimadoDiario ?? "";
            f.esCritico.checked = i.esCritico;
            f.esImportado.checked = i.esImportado;
            f.controlaLote.checked = i.controlaLote;
        } else {
            f.tipoConsumo.value = "1";
            f.factorConversion.value = "1";
            f.nivelServicio.value = "95";
        }
        actualizarEquivalencia();
        if (drawer.open) drawer.close();
        abrir(dlgInsumo);
        f.codigo.focus();
    }

    // Un insumo crítico sugiere 98 % de nivel de servicio (solo si el usuario no lo cambió).
    formInsumo.elements.esCritico.addEventListener("change", (e) => {
        const nivel = formInsumo.elements.nivelServicio;
        if (e.target.checked && nivel.value === "95") nivel.value = "98";
        else if (!e.target.checked && nivel.value === "98") nivel.value = "95";
    });

    formInsumo.addEventListener("submit", async (e) => {
        e.preventDefault();
        const r = editando
            ? await enviar(formInsumo, `/api/insumos/${editando.id}`, "PUT")
            : await enviar(formInsumo, "/api/insumos", "POST");
        if (!r) return;
        cerrar(dlgInsumo);
        toast(r.message);
        listado.cargar();
        // Al crear, se abre el detalle para agregar de inmediato el proveedor principal.
        verDetalle(r.data.id);
    });

    async function cambiarEstado(i) {
        const activar = !i.activo;
        const ok = await confirmar({
            titulo: activar ? "Activar insumo" : "Desactivar insumo",
            mensaje: activar
                ? `${i.nombre} volverá a considerarse en el inventario.`
                : `${i.nombre} dejará de considerarse en pronósticos y alertas. Su historial se conserva.`,
            aceptar: activar ? "Activar" : "Desactivar",
            peligro: !activar
        });
        if (!ok) return;
        const r = await api(`/api/insumos/${i.id}/estado`, { method: "PATCH", body: { activo: activar } });
        toast(r.message, r.ok ? "ok" : "error");
        if (r.ok) listado.cargar();
    }

    async function eliminar(i) {
        const ok = await confirmar({
            titulo: "Eliminar insumo",
            mensaje: `Se eliminará ${i.nombre}. Esta acción no se puede deshacer.`,
            aceptar: "Eliminar",
            peligro: true
        });
        if (!ok) return;
        const r = await api(`/api/insumos/${i.id}`, { method: "DELETE" });
        toast(r.message, r.ok ? "ok" : "error");
        if (r.ok) listado.cargar();
    }

    // ------------------------------------------------------------ condiciones de compra
    const selProveedor = document.getElementById("cProveedor");

    async function abrirCondicion(c) {
        condicionEditando = c;
        formCondicion.reset();
        limpiarErrores(formCondicion);
        dlgCondicion.querySelector("[data-titulo]").textContent = c ? "Editar condiciones de compra" : "Agregar proveedor";
        dlgCondicion.querySelector("[data-insumo]").textContent = abierto.nombre;
        dlgCondicion.querySelectorAll("[data-unidad-compra]").forEach((el) => { el.textContent = abierto.unidadCompraCodigo; });

        const f = formCondicion.elements;
        if (c) {
            selProveedor.innerHTML = `<option value="${c.proveedorId}">${esc(c.proveedorNombre)}</option>`;
            selProveedor.disabled = true;
            f.precioUnitario.value = c.precioUnitario;
            f.tiempoEntregaDias.value = c.tiempoEntregaDias;
            f.variacionEntregaDias.value = c.variacionEntregaDias;
            f.pedidoMinimo.value = c.pedidoMinimo;
            f.multiploCompra.value = c.multiploCompra;
            f.esPrincipal.checked = c.esPrincipal;
            f.esPrincipal.disabled = c.esPrincipal; // el principal se cambia marcando otro
            formCondicion.querySelector(`input[name="moneda"][value="${c.moneda}"]`).checked = true;
        } else {
            selProveedor.disabled = false;
            f.esPrincipal.disabled = false;
            const asignados = new Set(abierto.condiciones.map((x) => x.proveedorId));
            const opciones = await llenarSelect(selProveedor, "/api/proveedores/opciones?soloActivos=true", { vacio: "Seleccione…" });
            selProveedor.querySelectorAll("option[value]").forEach((o) => { if (asignados.has(Number(o.value))) o.remove(); });
            if (!opciones.length) toast("No hay proveedores activos. Regístrelos primero en Proveedores.", "error");
            formCondicion.querySelector(`input[name="moneda"][value="${abierto.esImportado ? 2 : 1}"]`).checked = true;
            f.multiploCompra.value = "1";
            f.variacionEntregaDias.value = "0";
            f.pedidoMinimo.value = "0";
            f.esPrincipal.checked = abierto.condiciones.length === 0;
        }
        abrir(dlgCondicion);
        (c ? f.precioUnitario : selProveedor).focus();
    }

    formCondicion.addEventListener("submit", async (e) => {
        e.preventDefault();
        const datos = leerFormulario(formCondicion);
        datos.insumoId = abierto.id;
        if (condicionEditando) {
            datos.proveedorId = condicionEditando.proveedorId;
            datos.esPrincipal = formCondicion.elements.esPrincipal.checked;
        }
        const r = condicionEditando
            ? await enviar(formCondicion, `/api/condiciones/${condicionEditando.id}`, "PUT", datos)
            : await enviar(formCondicion, "/api/condiciones", "POST", datos);
        if (!r) return;
        cerrar(dlgCondicion);
        toast(r.message);
        abierto.condiciones = r.data;
        pintarDetalle();
        listado.cargar();
    });

    // ------------------------------------------------------------ inicio
    document.getElementById("btnNuevo")?.addEventListener("click", () => abrirFormulario());
    llenarSelect(document.getElementById("fCategoria"), "/api/categorias/opciones?soloActivas=false", { vacio: "Todas" });
    listado.cargar();
})();
