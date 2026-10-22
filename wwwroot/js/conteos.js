"use strict";

(() => {
    const { esc, icono, numero, fechaHora, toast, abrir, cerrar, limpiarErrores, enviar, llenarSelect, Listado } = Spin;

    const ESTADOS = {
        1: `<span class="badge badge-warn">${icono("portapapeles")}Abierto</span>`,
        2: `<span class="badge badge-ok">${icono("check")}Aprobado</span>`,
        3: `<span class="badge badge-off">${icono("cerrar")}Anulado</span>`
    };

    const listado = new Listado({
        url: "/api/conteos",
        tabla: document.getElementById("tabla"),
        filtros: document.getElementById("filtros"),
        segmentos: document.getElementById("segmentos"),
        parametroSegmento: "estado",
        estadoInicial: "",
        pie: document.getElementById("pie"),
        textoVacio: "Aún no hay conteos. Cree el primero con «Nuevo conteo».",
        columnas: [
            { titulo: "N°", clase: "cel-codigo", render: (c) => `N° ${esc(c.numero)}` },
            { titulo: "Almacén", render: (c) => `<span class="cel-principal">${esc(c.almacenCodigo)}</span><span class="cel-sub">${esc(c.almacenNombre)}</span>` },
            { titulo: "Alcance", render: (c) => esc(c.categoriaNombre ?? "Todos los insumos") },
            {
                titulo: "Avance",
                render: (c) => {
                    const pct = c.insumos ? Math.round((c.contados / c.insumos) * 100) : 0;
                    return `<div class="progreso"><span>${numero(c.contados)} de ${numero(c.insumos)} contados</span><div class="progreso-barra"><i style="width:${pct}%"></i></div></div>`;
                }
            },
            { titulo: "Con diferencia", clase: "num", render: (c) => numero(c.conDiferencia) },
            { titulo: "Estado", render: (c) => ESTADOS[c.estado] ?? esc(c.estadoNombre) },
            { titulo: "Creado", render: (c) => `${esc(fechaHora(c.creadoEn))}<span class="cel-sub">${esc(c.creadoPor)}</span>` }
        ],
        acciones: (c) => `<a class="icon-btn" href="/conteos/${c.id}" title="${c.estado === 1 ? "Registrar conteo" : "Ver conteo"}" aria-label="Abrir conteo">${icono(c.estado === 1 ? "editar" : "ojo")}</a>`,
        alClicFila: (c) => { window.location.href = `/conteos/${c.id}`; }
    });

    const dlg = document.getElementById("dlgConteo");
    const form = dlg.querySelector("form");

    async function abrirNuevo() {
        await Promise.all([
            llenarSelect(document.getElementById("cAlmacen"), "/api/almacenes/opciones?soloActivos=true&soloInsumos=true", { vacio: "Seleccione…" }),
            llenarSelect(document.getElementById("cCategoria"), "/api/categorias/opciones?soloActivas=true", { vacio: "Todas las categorías" })
        ]);
        form.reset();
        limpiarErrores(form);
        const almacenes = form.elements.almacenId;
        if (almacenes.options.length === 2) almacenes.selectedIndex = 1;
        abrir(dlg);
        almacenes.focus();
    }

    form.addEventListener("submit", async (e) => {
        e.preventDefault();
        const r = await enviar(form, "/api/conteos", "POST");
        if (!r) return;
        cerrar(dlg);
        toast(r.message);
        window.location.href = `/conteos/${r.data.id}`;
    });

    document.getElementById("btnNuevo")?.addEventListener("click", abrirNuevo);
    llenarSelect(document.getElementById("fAlmacen"), "/api/almacenes/opciones", { vacio: "Todos" });
    listado.cargar();
})();
