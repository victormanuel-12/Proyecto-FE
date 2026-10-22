"use strict";

(() => {
    const { esc, api, toast, abrir, cerrar, confirmar, limpiarErrores, enviar, estado, boton, llenarSelect, numero, Listado } = Spin;

    const dlg = document.getElementById("dlgLinea");
    const form = dlg.querySelector("form");
    const selPlantaFiltro = document.getElementById("fPlanta");
    const selPlantaForm = document.getElementById("lPlanta");
    const cajaFormatos = document.getElementById("lFormatos");
    let editando = null;
    let formatos = [];

    const listado = new Listado({
        url: "/api/lineas",
        tabla: document.getElementById("tabla"),
        filtros: document.getElementById("filtros"),
        segmentos: document.getElementById("segmentos"),
        pie: document.getElementById("pie"),
        textoVacio: "Aún no hay líneas. Registre la primera con «Nueva línea».",
        columnas: [
            { titulo: "Código", clase: "cel-codigo", render: (l) => esc(l.codigo) },
            {
                titulo: "Línea",
                render: (l) => `<span class="cel-principal">${esc(l.nombre)}</span><span class="cel-sub">${esc(l.plantaNombre)}</span>`
            },
            { titulo: "Envase", render: (l) => esc(l.tipoEnvaseNombre) },
            { titulo: "Velocidad", clase: "num", render: (l) => `${numero(l.velocidadNominal)} <span class="cel-tenue">bot/h</span>` },
            {
                titulo: "Formatos",
                render: (l) => `<span class="chips">${l.formatos.map((f) => `<span class="chip">${esc(f.nombre)}</span>`).join("")}</span>`
            },
            { titulo: "Estado", render: (l) => estado(l.activo) }
        ],
        acciones: (l) =>
            boton("editar", "editar", "Editar") +
            boton("estado", "encender", l.activo ? "Desactivar" : "Activar") +
            boton("eliminar", "eliminar", "Eliminar", { peligro: true }),
        alAccion: (accion, l) => {
            if (accion === "editar") abrirFormulario(l);
            else if (accion === "estado") cambiarEstado(l);
            else if (accion === "eliminar") eliminar(l);
        }
    });

    function pintarFormatos(tipoEnvase, seleccionados = []) {
        const lista = formatos.filter((f) => f.tipoEnvase === tipoEnvase);
        if (!tipoEnvase) {
            cajaFormatos.innerHTML = `<p class="hint">Primero elija el tipo de envase.</p>`;
            return;
        }
        if (!lista.length) {
            cajaFormatos.innerHTML = `<p class="hint">No hay formatos para este envase.</p>`;
            return;
        }
        cajaFormatos.innerHTML = lista.map((f) => `
            <label>
                <input type="checkbox" name="formatoIds" value="${f.id}" data-multiple${seleccionados.includes(f.id) ? " checked" : ""} />
                <span>${esc(f.nombre)}</span>
            </label>`).join("");
    }

    form.addEventListener("change", (e) => {
        if (e.target.name === "tipoEnvase") pintarFormatos(Number(e.target.value));
    });

    async function asegurarFormatos() {
        if (formatos.length) return true;
        const r = await api("/api/formatos");
        if (!r.ok) { toast(r.message, "error"); return false; }
        formatos = r.data;
        return true;
    }

    async function abrirFormulario(l = null) {
        if (!(await asegurarFormatos())) return;
        editando = l;
        form.reset();
        limpiarErrores(form);
        dlg.querySelector("[data-titulo]").textContent = l ? `Editar línea ${l.codigo}` : "Registrar línea";

        const opciones = await llenarSelect(selPlantaForm, "/api/plantas/opciones?soloActivas=true", { vacio: "Seleccione…" });
        if (l && !opciones.some((o) => o.id === l.plantaId)) {
            selPlantaForm.insertAdjacentHTML("beforeend", `<option value="${l.plantaId}">${esc(l.plantaNombre)} (inactiva)</option>`);
        }

        if (l) {
            form.elements.codigo.value = l.codigo;
            form.elements.nombre.value = l.nombre;
            form.elements.plantaId.value = String(l.plantaId);
            form.elements.velocidadNominal.value = l.velocidadNominal;
            const radio = form.querySelector(`input[name="tipoEnvase"][value="${l.tipoEnvase}"]`);
            if (radio) radio.checked = true;
            pintarFormatos(l.tipoEnvase, l.formatos.map((f) => f.id));
        } else {
            if (opciones.length === 1) form.elements.plantaId.value = String(opciones[0].id);
            pintarFormatos(0);
        }
        abrir(dlg);
        form.elements.codigo.focus();
    }

    form.addEventListener("submit", async (e) => {
        e.preventDefault();
        const datos = Spin.leerFormulario(form);
        datos.formatoIds ??= []; // si aún no se eligió envase no hay casillas que leer
        const r = editando
            ? await enviar(form, `/api/lineas/${editando.id}`, "PUT", datos)
            : await enviar(form, "/api/lineas", "POST", datos);
        if (!r) return;
        cerrar(dlg);
        toast(r.message);
        listado.cargar();
    });

    async function cambiarEstado(l) {
        const activar = !l.activo;
        const ok = await confirmar({
            titulo: activar ? "Activar línea" : "Desactivar línea",
            mensaje: activar
                ? `La línea ${l.nombre} volverá a estar disponible para registrar producción.`
                : `La línea ${l.nombre} dejará de estar disponible para registrar producción.`,
            aceptar: activar ? "Activar" : "Desactivar",
            peligro: !activar
        });
        if (!ok) return;
        const r = await api(`/api/lineas/${l.id}/estado`, { method: "PATCH", body: { activo: activar } });
        toast(r.message, r.ok ? "ok" : "error");
        if (r.ok) listado.cargar();
    }

    async function eliminar(l) {
        const ok = await confirmar({
            titulo: "Eliminar línea",
            mensaje: `Se eliminará la línea ${l.nombre}. Esta acción no se puede deshacer.`,
            aceptar: "Eliminar",
            peligro: true
        });
        if (!ok) return;
        const r = await api(`/api/lineas/${l.id}`, { method: "DELETE" });
        toast(r.message, r.ok ? "ok" : "error");
        if (r.ok) listado.cargar();
    }

    document.getElementById("btnNuevo").addEventListener("click", () => abrirFormulario());
    llenarSelect(selPlantaFiltro, "/api/plantas/opciones?soloActivas=false", { vacio: "Todas" });
    listado.cargar();
})();
