"use strict";

(() => {
    const { esc, numero, api, toast, abrir, cerrar, confirmar, limpiarErrores, enviar, estado, boton, Listado } = Spin;

    const puedeCatalogo = document.getElementById("paginaCatalogo").dataset.puedeCatalogo === "true";

    /**
     * Crea una tabla con su formulario para un catálogo simple (categorías o unidades).
     * Muestra todos los registros (activos e inactivos) en una sola página.
     */
    function crearCatalogo({ url, tabla, pie, dlgId, btnNuevo, columnas, nombre, singular, rellenar, femenino }) {
        const dlg = document.getElementById(dlgId);
        const form = dlg.querySelector("form");
        let editando = null;
        const art = femenino ? "la" : "el";

        const listado = new Listado({
            url,
            tabla: document.getElementById(tabla),
            pie: document.getElementById(pie),
            estadoInicial: "",
            tamanoPagina: 50,
            textoVacio: `No hay ${nombre}.`,
            columnas,
            acciones: puedeCatalogo
                ? (x) => boton("editar", "editar", "Editar") +
                    boton("estado", "encender", x.activo ? "Desactivar" : "Activar") +
                    boton("eliminar", "eliminar", "Eliminar", { peligro: true, deshabilitado: x.insumos > 0 })
                : null,
            alAccion: async (accion, x) => {
                if (accion === "editar") abrirFormulario(x);
                else if (accion === "estado") {
                    const activar = !x.activo;
                    const ok = await confirmar({
                        titulo: `${activar ? "Activar" : "Desactivar"} ${singular}`,
                        mensaje: activar
                            ? `«${x.nombre}» volverá a estar disponible para insumos nuevos.`
                            : `«${x.nombre}» no se podrá elegir para insumos nuevos. Los insumos existentes no cambian.`,
                        aceptar: activar ? "Activar" : "Desactivar",
                        peligro: !activar
                    });
                    if (!ok) return;
                    const r = await api(`${url}/${x.id}/estado`, { method: "PATCH", body: { activo: activar } });
                    toast(r.message, r.ok ? "ok" : "error");
                    if (r.ok) listado.cargar();
                } else if (accion === "eliminar") {
                    const ok = await confirmar({
                        titulo: `Eliminar ${singular}`,
                        mensaje: `Se eliminará ${art} ${singular} «${x.nombre}».`,
                        aceptar: "Eliminar",
                        peligro: true
                    });
                    if (!ok) return;
                    const r = await api(`${url}/${x.id}`, { method: "DELETE" });
                    toast(r.message, r.ok ? "ok" : "error");
                    if (r.ok) listado.cargar();
                }
            }
        });

        function abrirFormulario(x = null) {
            editando = x;
            form.reset();
            limpiarErrores(form);
            dlg.querySelector("[data-titulo]").textContent = x ? `Editar ${singular}` : `${femenino ? "Nueva" : "Nuevo"} ${singular}`;
            if (x) rellenar(form, x);
            abrir(dlg);
            form.querySelector("input").focus();
        }

        form.addEventListener("submit", async (e) => {
            e.preventDefault();
            const r = editando
                ? await enviar(form, `${url}/${editando.id}`, "PUT")
                : await enviar(form, url, "POST");
            if (!r) return;
            cerrar(dlg);
            toast(r.message);
            listado.cargar();
        });

        document.getElementById(btnNuevo)?.addEventListener("click", () => abrirFormulario());
        listado.cargar();
    }

    crearCatalogo({
        url: "/api/categorias",
        tabla: "tablaCategorias",
        pie: "pieCategorias",
        dlgId: "dlgCategoria",
        btnNuevo: "btnNuevaCategoria",
        nombre: "categorías",
        singular: "categoría",
        femenino: true,
        columnas: [
            {
                titulo: "Categoría",
                render: (c) => `<span class="cel-principal">${esc(c.nombre)}</span>` +
                    (c.descripcion ? `<span class="cel-sub">${esc(c.descripcion)}</span>` : "")
            },
            { titulo: "Insumos", clase: "num", render: (c) => numero(c.insumos) },
            { titulo: "Estado", render: (c) => estado(c.activo) }
        ],
        rellenar: (form, c) => {
            form.elements.nombre.value = c.nombre;
            form.elements.descripcion.value = c.descripcion ?? "";
        }
    });

    crearCatalogo({
        url: "/api/unidades",
        tabla: "tablaUnidades",
        pie: "pieUnidades",
        dlgId: "dlgUnidad",
        btnNuevo: "btnNuevaUnidad",
        nombre: "unidades",
        singular: "unidad",
        femenino: true,
        columnas: [
            { titulo: "Código", clase: "cel-codigo", render: (u) => esc(u.codigo) },
            { titulo: "Unidad", render: (u) => esc(u.nombre) },
            { titulo: "Insumos", clase: "num", render: (u) => numero(u.insumos) },
            { titulo: "Estado", render: (u) => estado(u.activo) }
        ],
        rellenar: (form, u) => {
            form.elements.codigo.value = u.codigo;
            form.elements.nombre.value = u.nombre;
        }
    });
})();
