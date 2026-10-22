"use strict";

(() => {
    const { esc, api, toast, abrir, cerrar, confirmar, limpiarErrores, enviar, estado, boton, Listado } = Spin;

    const dlg = document.getElementById("dlgPlanta");
    const form = dlg.querySelector("form");
    let editando = null;

    const listado = new Listado({
        url: "/api/plantas",
        tabla: document.getElementById("tabla"),
        filtros: document.getElementById("filtros"),
        segmentos: document.getElementById("segmentos"),
        pie: document.getElementById("pie"),
        textoVacio: "Aún no hay plantas. Registre la primera con «Nueva planta».",
        columnas: [
            { titulo: "Código", clase: "cel-codigo", render: (p) => esc(p.codigo) },
            {
                titulo: "Planta",
                render: (p) => `<span class="cel-principal">${esc(p.nombre)}</span>` +
                    (p.direccion ? `<span class="cel-sub">${esc(p.direccion)}</span>` : "")
            },
            { titulo: "Almacenes", clase: "num", render: (p) => Spin.numero(p.almacenes) },
            { titulo: "Líneas", clase: "num", render: (p) => Spin.numero(p.lineas) },
            { titulo: "Estado", render: (p) => estado(p.activo) }
        ],
        acciones: (p) =>
            boton("editar", "editar", "Editar") +
            boton("estado", "encender", p.activo ? "Desactivar" : "Activar") +
            boton("eliminar", "eliminar", "Eliminar", { peligro: true, deshabilitado: p.almacenes + p.lineas > 0 }),
        alAccion: (accion, p) => {
            if (accion === "editar") abrirFormulario(p);
            else if (accion === "estado") cambiarEstado(p);
            else if (accion === "eliminar") eliminar(p);
        }
    });

    function abrirFormulario(p = null) {
        editando = p;
        form.reset();
        limpiarErrores(form);
        dlg.querySelector("[data-titulo]").textContent = p ? `Editar planta ${p.codigo}` : "Registrar planta";
        if (p) {
            form.elements.codigo.value = p.codigo;
            form.elements.nombre.value = p.nombre;
            form.elements.direccion.value = p.direccion ?? "";
        }
        abrir(dlg);
        form.elements.codigo.focus();
    }

    form.addEventListener("submit", async (e) => {
        e.preventDefault();
        const r = editando
            ? await enviar(form, `/api/plantas/${editando.id}`, "PUT")
            : await enviar(form, "/api/plantas", "POST");
        if (!r) return;
        cerrar(dlg);
        toast(r.message);
        listado.cargar();
    });

    async function cambiarEstado(p) {
        const activar = !p.activo;
        const ok = await confirmar({
            titulo: activar ? "Activar planta" : "Desactivar planta",
            mensaje: activar
                ? `La planta ${p.nombre} volverá a estar disponible.`
                : `La planta ${p.nombre} dejará de estar disponible. Antes debe desactivar sus almacenes y líneas.`,
            aceptar: activar ? "Activar" : "Desactivar",
            peligro: !activar
        });
        if (!ok) return;
        const r = await api(`/api/plantas/${p.id}/estado`, { method: "PATCH", body: { activo: activar } });
        toast(r.message, r.ok ? "ok" : "error");
        if (r.ok) listado.cargar();
    }

    async function eliminar(p) {
        const ok = await confirmar({
            titulo: "Eliminar planta",
            mensaje: `Se eliminará la planta ${p.nombre}. Esta acción no se puede deshacer.`,
            aceptar: "Eliminar",
            peligro: true
        });
        if (!ok) return;
        const r = await api(`/api/plantas/${p.id}`, { method: "DELETE" });
        toast(r.message, r.ok ? "ok" : "error");
        if (r.ok) listado.cargar();
    }

    document.getElementById("btnNuevo").addEventListener("click", () => abrirFormulario());
    listado.cargar();
})();
