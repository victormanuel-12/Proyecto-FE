"use strict";

(() => {
    const { esc, api, toast, abrir, cerrar, confirmar, limpiarErrores, enviar, estado, boton, llenarSelect, icono, Listado } = Spin;

    const tabla = document.getElementById("tabla");
    const esSuperAdmin = tabla.dataset.superadmin === "true";
    const dlg = document.getElementById("dlgUsuario");
    const form = dlg.querySelector("form");
    const dlgClave = document.getElementById("dlgClave");
    const formClave = dlgClave.querySelector("form");
    let editando = null;
    let restableciendo = null;

    const columnas = [
        {
            titulo: "Usuario",
            render: (u) => `<span class="cel-principal">${esc(u.nombreCompleto)}${u.esUsuarioActual ? '<span class="marca-tu">TÚ</span>' : ""}</span>` +
                `<span class="cel-sub">${esc(u.email)}</span>`
        },
        { titulo: "Rol", render: (u) => esc(u.rolNombre) }
    ];
    if (esSuperAdmin) columnas.push({ titulo: "Empresa", render: (u) => esc(u.empresaNombre) });
    columnas.push({
        titulo: "Estado",
        render: (u) => estado(u.activo) +
            (u.bloqueado ? ` <span class="badge badge-warn">${icono("candado")}Bloqueado</span>` : "")
    });

    const listado = new Listado({
        url: "/api/usuarios",
        tabla,
        filtros: document.getElementById("filtros"),
        segmentos: document.getElementById("segmentos"),
        pie: document.getElementById("pie"),
        textoVacio: "No hay usuarios registrados.",
        columnas,
        acciones: (u) =>
            boton("editar", "editar", "Editar") +
            boton("clave", "llave", "Restablecer contraseña") +
            boton("estado", "encender", u.activo ? "Desactivar" : "Activar", { deshabilitado: u.esUsuarioActual }),
        alAccion: (accion, u) => {
            if (accion === "editar") abrirFormulario(u);
            else if (accion === "clave") abrirRestablecer(u);
            else if (accion === "estado") cambiarEstado(u);
        }
    });

    // ------------------------------------------------ crear / editar
    async function abrirFormulario(u = null) {
        editando = u;
        form.reset();
        limpiarErrores(form);
        dlg.querySelector("[data-titulo]").textContent = u ? "Editar usuario" : "Nuevo usuario";

        // Campos que solo existen al crear: se ocultan y se deshabilitan para que no se envíen.
        form.querySelectorAll("[data-solo-creacion]").forEach((campo) => {
            campo.hidden = Boolean(u);
            campo.querySelectorAll("input, select").forEach((c) => { c.disabled = Boolean(u); });
        });
        form.querySelector("[data-solo-edicion]").hidden = !u;
        form.elements.email.disabled = Boolean(u);

        if (!u && esSuperAdmin) {
            const empresas = await llenarSelect(document.getElementById("uEmpresa"), "/api/empresas/opciones", { vacio: "Seleccione…" });
            if (empresas.length === 1) form.elements.empresaId.value = String(empresas[0].id);
        }

        if (u) {
            form.elements.nombreCompleto.value = u.nombreCompleto;
            form.elements.email.value = u.email;
            form.elements.rol.value = u.rol;
            form.elements.rol.disabled = u.esUsuarioActual;
        } else {
            form.elements.rol.disabled = false;
        }
        abrir(dlg);
        form.elements.nombreCompleto.focus();
    }

    form.addEventListener("submit", async (e) => {
        e.preventDefault();
        let r;
        if (editando) {
            // El rol se envía aunque el campo esté deshabilitado (caso: el propio usuario).
            const cuerpo = { nombreCompleto: form.elements.nombreCompleto.value.trim(), rol: form.elements.rol.value };
            r = await enviar(form, `/api/usuarios/${encodeURIComponent(editando.id)}`, "PUT", cuerpo);
        } else {
            r = await enviar(form, "/api/usuarios", "POST");
        }
        if (!r) return;
        cerrar(dlg);
        toast(r.message);
        listado.cargar();
    });

    // ------------------------------------------------ restablecer contraseña
    function abrirRestablecer(u) {
        restableciendo = u;
        formClave.reset();
        limpiarErrores(formClave);
        dlgClave.querySelector("[data-nombre]").textContent = u.nombreCompleto;
        abrir(dlgClave);
        formClave.elements.claveTemporal.focus();
    }

    formClave.addEventListener("submit", async (e) => {
        e.preventDefault();
        const r = await enviar(formClave, `/api/usuarios/${encodeURIComponent(restableciendo.id)}/restablecer-clave`, "POST");
        if (!r) return;
        cerrar(dlgClave);
        toast(r.message);
        listado.cargar();
    });

    // ------------------------------------------------ activar / desactivar
    async function cambiarEstado(u) {
        const activar = !u.activo;
        const ok = await confirmar({
            titulo: activar ? "Activar usuario" : "Desactivar usuario",
            mensaje: activar
                ? `${u.nombreCompleto} podrá volver a ingresar.`
                : `${u.nombreCompleto} ya no podrá ingresar y su sesión abierta se cerrará.`,
            aceptar: activar ? "Activar" : "Desactivar",
            peligro: !activar
        });
        if (!ok) return;
        const r = await api(`/api/usuarios/${encodeURIComponent(u.id)}/estado`, { method: "PATCH", body: { activo: activar } });
        toast(r.message, r.ok ? "ok" : "error");
        if (r.ok) listado.cargar();
    }

    document.getElementById("btnNuevo").addEventListener("click", () => abrirFormulario());
    const filtroEmpresa = document.getElementById("fEmpresa");
    if (filtroEmpresa) llenarSelect(filtroEmpresa, "/api/empresas/opciones", { vacio: "Todas" });
    listado.cargar();
})();
