"use strict";

(() => {
    const { esc, api, toast, abrir, cerrar, confirmar, limpiarErrores, enviar, estado, boton, llenarSelect, Listado } = Spin;

    const dlg = document.getElementById("dlgAlmacen");
    const form = dlg.querySelector("form");
    const selPlantaFiltro = document.getElementById("fPlanta");
    const selPlantaForm = document.getElementById("aPlanta");
    let editando = null;

    const listado = new Listado({
        url: "/api/almacenes",
        tabla: document.getElementById("tabla"),
        filtros: document.getElementById("filtros"),
        segmentos: document.getElementById("segmentos"),
        pie: document.getElementById("pie"),
        textoVacio: "Aún no hay almacenes. Registre el primero con «Nuevo almacén».",
        columnas: [
            { titulo: "Código", clase: "cel-codigo", render: (a) => esc(a.codigo) },
            { titulo: "Almacén", render: (a) => `<span class="cel-principal">${esc(a.nombre)}</span>` },
            { titulo: "Planta", render: (a) => esc(a.plantaNombre) },
            { titulo: "Tipo", render: (a) => `<span class="chip">${esc(a.tipoNombre)}</span>` },
            { titulo: "Estado", render: (a) => estado(a.activo) }
        ],
        acciones: (a) =>
            boton("editar", "editar", "Editar") +
            boton("estado", "encender", a.activo ? "Desactivar" : "Activar") +
            boton("eliminar", "eliminar", "Eliminar", { peligro: true }),
        alAccion: (accion, a) => {
            if (accion === "editar") abrirFormulario(a);
            else if (accion === "estado") cambiarEstado(a);
            else if (accion === "eliminar") eliminar(a);
        }
    });

    async function abrirFormulario(a = null) {
        editando = a;
        form.reset();
        limpiarErrores(form);
        dlg.querySelector("[data-titulo]").textContent = a ? `Editar almacén ${a.codigo}` : "Registrar almacén";
        // Solo plantas activas; al editar se conserva la planta actual aunque esté inactiva.
        const opciones = await llenarSelect(selPlantaForm, "/api/plantas/opciones?soloActivas=true", { vacio: "Seleccione…" });
        if (a && !opciones.some((o) => o.id === a.plantaId)) {
            selPlantaForm.insertAdjacentHTML("beforeend", `<option value="${a.plantaId}">${esc(a.plantaNombre)} (inactiva)</option>`);
        }
        if (a) {
            form.elements.codigo.value = a.codigo;
            form.elements.nombre.value = a.nombre;
            form.elements.plantaId.value = String(a.plantaId);
            form.elements.tipo.value = String(a.tipo);
        } else if (opciones.length === 1) {
            form.elements.plantaId.value = String(opciones[0].id);
        }
        abrir(dlg);
        form.elements.codigo.focus();
    }

    form.addEventListener("submit", async (e) => {
        e.preventDefault();
        const r = editando
            ? await enviar(form, `/api/almacenes/${editando.id}`, "PUT")
            : await enviar(form, "/api/almacenes", "POST");
        if (!r) return;
        cerrar(dlg);
        toast(r.message);
        listado.cargar();
    });

    async function cambiarEstado(a) {
        const activar = !a.activo;
        const ok = await confirmar({
            titulo: activar ? "Activar almacén" : "Desactivar almacén",
            mensaje: activar
                ? `El almacén ${a.nombre} volverá a estar disponible.`
                : `El almacén ${a.nombre} dejará de aparecer para registrar movimientos.`,
            aceptar: activar ? "Activar" : "Desactivar",
            peligro: !activar
        });
        if (!ok) return;
        const r = await api(`/api/almacenes/${a.id}/estado`, { method: "PATCH", body: { activo: activar } });
        toast(r.message, r.ok ? "ok" : "error");
        if (r.ok) listado.cargar();
    }

    async function eliminar(a) {
        const ok = await confirmar({
            titulo: "Eliminar almacén",
            mensaje: `Se eliminará el almacén ${a.nombre}. Esta acción no se puede deshacer.`,
            aceptar: "Eliminar",
            peligro: true
        });
        if (!ok) return;
        const r = await api(`/api/almacenes/${a.id}`, { method: "DELETE" });
        toast(r.message, r.ok ? "ok" : "error");
        if (r.ok) listado.cargar();
    }

    document.getElementById("btnNuevo").addEventListener("click", () => abrirFormulario());
    llenarSelect(selPlantaFiltro, "/api/plantas/opciones?soloActivas=false", { vacio: "Todas" });
    listado.cargar();
})();
