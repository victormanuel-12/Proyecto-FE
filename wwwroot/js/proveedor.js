"use strict";

(() => {
    const { esc, numero, api, toast, abrir, cerrar, confirmar, limpiarErrores, enviar, estado, boton, Listado } = Spin;

    const tabla = document.getElementById("tabla");
    const puedeAbastecer = tabla.dataset.puedeAbastecer === "true";
    const dlg = document.getElementById("dlgProveedor");
    const form = dlg.querySelector("form");
    const campoRuc = document.getElementById("campoRuc");
    let editando = null;

    const esPeru = (pais) => (pais ?? "").normalize("NFD").replace(/[̀-ͯ]/g, "").trim().toLowerCase() === "peru";

    const listado = new Listado({
        url: "/api/proveedores",
        tabla,
        filtros: document.getElementById("filtros"),
        segmentos: document.getElementById("segmentos"),
        pie: document.getElementById("pie"),
        textoVacio: puedeAbastecer ? "Aún no hay proveedores. Registre el primero con «Nuevo proveedor»." : "Aún no hay proveedores registrados.",
        columnas: [
            {
                titulo: "Proveedor",
                render: (p) => `<span class="cel-principal">${esc(p.nombreComercial ?? p.razonSocial)}</span>` +
                    (p.nombreComercial ? `<span class="cel-sub">${esc(p.razonSocial)}</span>` : "")
            },
            {
                titulo: "RUC / País",
                render: (p) => p.ruc
                    ? `<span class="cel-codigo">${esc(p.ruc)}</span>`
                    : `<span class="chip">${esc(p.pais)}</span>`
            },
            {
                titulo: "Contacto",
                render: (p) => {
                    const linea2 = [p.telefono, p.email].filter(Boolean).map(esc).join(" · ");
                    if (!p.contacto && !linea2) return `<span class="cel-tenue">—</span>`;
                    return `${esc(p.contacto ?? "")}${linea2 ? `<span class="cel-sub">${linea2}</span>` : ""}`;
                }
            },
            {
                titulo: "Insumos",
                clase: "num",
                render: (p) => `${numero(p.insumos)}${p.insumosPrincipal ? `<span class="cel-sub">principal en ${numero(p.insumosPrincipal)}</span>` : ""}`
            },
            { titulo: "Estado", render: (p) => estado(p.activo) }
        ],
        acciones: puedeAbastecer
            ? (p) => boton("editar", "editar", "Editar") +
                boton("estado", "encender", p.activo ? "Desactivar" : "Activar") +
                boton("eliminar", "eliminar", "Eliminar", { peligro: true, deshabilitado: p.insumos > 0 })
            : null,
        alAccion: (accion, p) => {
            if (accion === "editar") abrirFormulario(p);
            else if (accion === "estado") cambiarEstado(p);
            else if (accion === "eliminar") eliminar(p);
        }
    });

    function actualizarRuc() {
        const peru = esPeru(form.elements.pais.value);
        campoRuc.hidden = !peru;
        form.elements.ruc.disabled = !peru;
    }
    form.elements.pais.addEventListener("input", actualizarRuc);
    form.elements.ruc.addEventListener("input", (e) => {
        e.target.value = e.target.value.replace(/\D/g, "").slice(0, 11);
    });

    function abrirFormulario(p = null) {
        editando = p;
        form.reset();
        limpiarErrores(form);
        dlg.querySelector("[data-titulo]").textContent = p ? "Editar proveedor" : "Registrar proveedor";
        const f = form.elements;
        f.pais.value = p?.pais ?? "Perú";
        f.ruc.value = p?.ruc ?? "";
        f.razonSocial.value = p?.razonSocial ?? "";
        f.nombreComercial.value = p?.nombreComercial ?? "";
        f.contacto.value = p?.contacto ?? "";
        f.telefono.value = p?.telefono ?? "";
        f.email.value = p?.email ?? "";
        actualizarRuc();
        abrir(dlg);
        (esPeru(f.pais.value) ? f.ruc : f.razonSocial).focus();
    }

    form.addEventListener("submit", async (e) => {
        e.preventDefault();
        const r = editando
            ? await enviar(form, `/api/proveedores/${editando.id}`, "PUT")
            : await enviar(form, "/api/proveedores", "POST");
        if (!r) return;
        cerrar(dlg);
        toast(r.message);
        listado.cargar();
    });

    async function cambiarEstado(p) {
        const activar = !p.activo;
        const nombre = p.nombreComercial ?? p.razonSocial;
        const ok = await confirmar({
            titulo: activar ? "Activar proveedor" : "Desactivar proveedor",
            mensaje: activar
                ? `${nombre} volverá a estar disponible para asignarlo a insumos.`
                : `${nombre} no se podrá asignar a nuevos insumos.` +
                  (p.insumosPrincipal ? ` Es el principal de ${p.insumosPrincipal} insumo(s): deberá asignarles otro.` : ""),
            aceptar: activar ? "Activar" : "Desactivar",
            peligro: !activar
        });
        if (!ok) return;
        const r = await api(`/api/proveedores/${p.id}/estado`, { method: "PATCH", body: { activo: activar } });
        toast(r.message, r.ok ? "ok" : "error");
        if (r.ok) listado.cargar();
    }

    async function eliminar(p) {
        const ok = await confirmar({
            titulo: "Eliminar proveedor",
            mensaje: `Se eliminará ${p.nombreComercial ?? p.razonSocial}. Esta acción no se puede deshacer.`,
            aceptar: "Eliminar",
            peligro: true
        });
        if (!ok) return;
        const r = await api(`/api/proveedores/${p.id}`, { method: "DELETE" });
        toast(r.message, r.ok ? "ok" : "error");
        if (r.ok) listado.cargar();
    }

    document.getElementById("btnNuevo")?.addEventListener("click", () => abrirFormulario());
    listado.cargar();
})();
