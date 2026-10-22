"use strict";

(() => {
    const { esc, api, toast, abrir, cerrar, confirmar, limpiarErrores, enviar, estado, boton, numero, Listado } = Spin;

    const dlg = document.getElementById("dlgEmpresa");
    const form = dlg.querySelector("form");
    let editando = null;

    const listado = new Listado({
        url: "/api/empresas",
        tabla: document.getElementById("tabla"),
        filtros: document.getElementById("filtros"),
        segmentos: document.getElementById("segmentos"),
        pie: document.getElementById("pie"),
        textoVacio: "No hay empresas registradas.",
        columnas: [
            { titulo: "RUC", clase: "cel-codigo", render: (e) => esc(e.ruc) },
            {
                titulo: "Empresa",
                render: (e) => `<span class="cel-principal">${esc(e.nombreComercial ?? e.razonSocial)}</span>` +
                    (e.nombreComercial ? `<span class="cel-sub">${esc(e.razonSocial)}</span>` : "")
            },
            { titulo: "Plantas", clase: "num", render: (e) => numero(e.plantas) },
            { titulo: "Usuarios", clase: "num", render: (e) => numero(e.usuarios) },
            { titulo: "Estado", render: (e) => estado(e.activo) }
        ],
        acciones: (e) =>
            boton("editar", "editar", "Editar") +
            boton("estado", "encender", e.activo ? "Desactivar" : "Activar"),
        alAccion: (accion, e) => {
            if (accion === "editar") abrirFormulario(e);
            else if (accion === "estado") cambiarEstado(e);
        }
    });

    function abrirFormulario(e = null) {
        editando = e;
        form.reset();
        limpiarErrores(form);
        dlg.querySelector("[data-titulo]").textContent = e ? "Editar empresa" : "Registrar empresa";
        if (e) {
            form.elements.ruc.value = e.ruc;
            form.elements.razonSocial.value = e.razonSocial;
            form.elements.nombreComercial.value = e.nombreComercial ?? "";
        }
        abrir(dlg);
        form.elements.ruc.focus();
    }

    // El RUC solo admite dígitos.
    form.elements.ruc.addEventListener("input", (ev) => {
        ev.target.value = ev.target.value.replace(/\D/g, "").slice(0, 11);
    });

    form.addEventListener("submit", async (ev) => {
        ev.preventDefault();
        const r = editando
            ? await enviar(form, `/api/empresas/${editando.id}`, "PUT")
            : await enviar(form, "/api/empresas", "POST");
        if (!r) return;
        cerrar(dlg);
        toast(r.message);
        listado.cargar();
    });

    async function cambiarEstado(e) {
        const activar = !e.activo;
        const nombre = e.nombreComercial ?? e.razonSocial;
        const ok = await confirmar({
            titulo: activar ? "Activar empresa" : "Desactivar empresa",
            mensaje: activar
                ? `${nombre} y sus usuarios podrán volver a ingresar.`
                : `Los usuarios de ${nombre} ya no podrán iniciar sesión. Sus datos se conservan.`,
            aceptar: activar ? "Activar" : "Desactivar",
            peligro: !activar
        });
        if (!ok) return;
        const r = await api(`/api/empresas/${e.id}/estado`, { method: "PATCH", body: { activo: activar } });
        toast(r.message, r.ok ? "ok" : "error");
        if (r.ok) listado.cargar();
    }

    document.getElementById("btnNuevo").addEventListener("click", () => abrirFormulario());
    listado.cargar();
})();
