"use strict";

(() => {
    const { esc, icono, numero, fechaHora, api, toast, abrir, cerrar, limpiarErrores, enviar, boton, Listado } = Spin;
    const segmentos = document.getElementById("segmentos");
    const SEV = {
        1: `<span class="badge badge-crit">${icono("error")}Crítica</span>`,
        2: `<span class="badge badge-warn">${icono("alerta")}Alta</span>`,
        3: `<span class="badge badge-media">${icono("alerta")}Media</span>`,
        4: `<span class="badge badge-off">${icono("ojo")}Informativa</span>`
    };
    const parametros = new URLSearchParams(location.search);

    const listado = new Listado({
        url: "/api/alertas",
        tabla: document.getElementById("tabla"),
        filtros: document.getElementById("filtros"),
        segmentos,
        estadoInicial: parametros.get("estado") ?? "activas",
        tamanoPagina: 20,
        pie: document.getElementById("pie"),
        textoVacio: "No hay alertas en esta vista.",
        columnas: [
            { titulo: "Severidad", render: (a) => SEV[a.severidad] },
            {
                titulo: "Alerta",
                render: (a) => `<span class="cel-principal">${esc(a.titulo)}</span><span class="cel-sub">${esc(a.detalle)}</span>` +
                    (a.motivoDescarte ? `<span class="cel-sub">Descartada: ${esc(a.motivoDescarte)}</span>` : "")
            },
            { titulo: "Tipo", render: (a) => esc(a.tipoNombre) },
            { titulo: "Desde", render: (a) => `${esc(fechaHora(a.primeraVez))}<span class="cel-sub">Revisada ${esc(fechaHora(a.ultimaVez))}</span>` },
            {
                titulo: "Estado",
                render: (a) => a.estado === 3 ? `<span class="badge badge-ok">${icono("check")}Resuelta</span><span class="cel-sub">${esc(fechaHora(a.resueltaEn))}</span>`
                    : a.estado === 2 ? `<span class="badge badge-off">${icono("pausa")}Descartada</span><span class="cel-sub">${esc(a.descartadaPor ?? "")}</span>`
                        : `<span class="badge badge-off">Activa</span>`
            }
        ],
        acciones: (a) => (a.enlace ? `<a class="btn btn-secondary btn-sm" href="${esc(a.enlace)}">${esc(a.textoEnlace ?? "Ir")}</a>` : "") +
            (a.estado === 1 ? boton("descartar", "check", "Descartar (ya la vi)") : a.estado === 2 ? boton("reactivar", "recargar", "Reactivar") : ""),
        alAccion: async (accion, a) => {
            if (accion === "descartar") abrirDescartar(a);
            else if (accion === "reactivar") {
                const r = await api(`/api/alertas/${a.id}/reactivar`, { method: "POST" });
                toast(r.message, r.ok ? "ok" : "error");
                if (r.ok) { listado.cargar(); Spin.actualizarCampana?.(); }
            }
        },
        alCargar: (d) => {
            const r = d.resumen;
            const poner = (k, v) => { const el = segmentos.querySelector(`[data-resumen="${k}"]`); if (el) el.textContent = numero(v); };
            poner("activas", r.criticas + r.altas + r.medias + r.informativas);
            poner("importantes", r.criticas + r.altas);
            poner("descartadas", r.descartadas);
            document.getElementById("revisado").textContent = `Revisado ${fechaHora(d.revisado)}.`;
        }
    });
    segmentos.querySelectorAll("[data-estado]").forEach((s) => s.setAttribute("aria-pressed", String(s.dataset.estado === listado.estado.estado)));

    const dlg = document.getElementById("dlgDescartar");
    const form = dlg.querySelector("form");
    let actual = null;
    function abrirDescartar(a) {
        actual = a;
        form.reset();
        limpiarErrores(form);
        dlg.querySelector("[data-resumen]").textContent =
            `«${a.titulo}». Dejará de contar en la campana. Se cerrará sola cuando se resuelva y volverá a activarse si la situación empeora.`;
        abrir(dlg);
        form.elements.motivo.focus();
    }
    form.addEventListener("submit", async (e) => {
        e.preventDefault();
        const r = await enviar(form, `/api/alertas/${actual.id}/descartar`, "POST");
        if (!r) return;
        cerrar(dlg);
        toast(r.message);
        listado.cargar();
        Spin.actualizarCampana?.();
    });

    listado.cargar();
})();
