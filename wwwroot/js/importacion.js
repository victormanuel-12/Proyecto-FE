"use strict";

(() => {
    const { esc, icono, numero, api, toast, limpiarErrores, mostrarErrores, ocupado, hoyIso, confirmar } = Spin;

    const lista = document.getElementById("tipos");
    const form = document.getElementById("formArchivo");
    const inArchivo = document.getElementById("archivo");
    const vista = document.getElementById("vista");
    const btnConfirmar = document.getElementById("btnConfirmar");
    let tipos = [];
    let actual = null;
    let previa = null;

    function seleccionar(tipo) {
        actual = tipos.find((t) => t.tipo === tipo) ?? tipos[0];
        lista.querySelectorAll("[data-tipo]").forEach((b) => b.setAttribute("aria-pressed", String(b.dataset.tipo === actual.tipo)));
        document.getElementById("tTitulo").textContent = `Importar ${actual.titulo.toLowerCase()}`;
        document.getElementById("tDescripcion").textContent = actual.descripcion;
        const req = document.getElementById("tRequisito");
        req.hidden = !actual.requisito;
        req.textContent = actual.requisito ? `Requisito: ${actual.requisito}` : "";
        document.getElementById("btnPlantilla").href = `/api/importar/${actual.tipo}/plantilla`;
        document.getElementById("tColumnas").innerHTML = actual.columnas.map((c) =>
            `<tr><td>${esc(c.nombre)}</td><td>${c.obligatoria ? "Sí" : "No"}</td><td>${esc(c.descripcion)}</td><td class="mono">${esc(c.ejemplo)}</td></tr>`).join("");
        document.getElementById("modo").innerHTML = actual.modos.map((m) => `<option value="${esc(m.valor)}">${esc(m.texto)}</option>`).join("");
        document.getElementById("campoFecha").hidden = !actual.pideFecha;
        document.getElementById("lblFecha").textContent = actual.textoFecha ?? "Fecha";
        form.elements.fecha.value = hoyIso();
        reiniciar();
        history.replaceState(null, "", `?tipo=${actual.tipo}`);
    }

    function reiniciar() {
        previa = null;
        form.reset();
        if (actual?.pideFecha) form.elements.fecha.value = hoyIso();
        if (actual) document.getElementById("modo").selectedIndex = 0;
        limpiarErrores(form);
        vista.hidden = true;
    }

    lista.addEventListener("click", (e) => {
        const b = e.target.closest("[data-tipo]");
        if (b) seleccionar(b.dataset.tipo);
    });

    // Arrastrar y soltar sobre la zona del archivo.
    const zona = document.getElementById("zonaArchivo");
    zona.addEventListener("dragover", (e) => { e.preventDefault(); zona.classList.add("is-sobre"); });
    zona.addEventListener("dragleave", () => zona.classList.remove("is-sobre"));
    zona.addEventListener("drop", (e) => {
        e.preventDefault();
        zona.classList.remove("is-sobre");
        if (e.dataTransfer.files.length) inArchivo.files = e.dataTransfer.files;
    });
    inArchivo.addEventListener("change", () => { vista.hidden = true; previa = null; });

    form.addEventListener("submit", async (e) => {
        e.preventDefault();
        limpiarErrores(form);
        const archivo = inArchivo.files[0];
        if (!archivo) { mostrarErrores(form, { archivo: ["Seleccione el archivo."] }); return; }
        if (archivo.size > 5 * 1024 * 1024) { mostrarErrores(form, { archivo: ["El archivo supera los 5 MB."] }); return; }

        const datos = new FormData();
        datos.append("archivo", archivo);
        datos.append("modo", form.elements.modo.value);
        if (actual.pideFecha && form.elements.fecha.value) datos.append("fecha", form.elements.fecha.value);

        const boton = form.querySelector('[type="submit"]');
        ocupado(boton, true);
        const r = await api(`/api/importar/${actual.tipo}/vista-previa`, { method: "POST", body: datos });
        ocupado(boton, false);
        if (!r.ok) { mostrarErrores(form, r.status === 400 ? r.data : null, r.message); vista.hidden = true; return; }
        previa = r.data;
        mostrarVista(previa, r.message);
    });

    function mostrarVista(p, mensaje) {
        vista.hidden = false;
        const cifra = (titulo, valor, error = false) => `<div${error ? ' class="is-error"' : ""}><dt>${titulo}</dt><dd>${numero(valor)}</dd></div>`;
        document.getElementById("cifras").innerHTML =
            cifra("Filas leídas", p.totalFilas) + cifra("Con errores", p.filasConError, p.filasConError > 0) +
            cifra("Nuevos", p.nuevos) + cifra("Se actualizan", p.actualizados) + cifra("Se omiten", p.omitidos);

        const nr = document.getElementById("noReconocidas");
        nr.hidden = !p.columnasNoReconocidas.length;
        nr.textContent = p.columnasNoReconocidas.length ? `Columnas que no se usan (se ignoran): ${p.columnasNoReconocidas.join(", ")}.` : "";

        document.getElementById("errores").hidden = !p.errores.length;
        document.getElementById("erroresTexto").textContent = mensaje +
            (p.errores.length < p.filasConError ? "" : "") + (p.errores.length >= 300 ? " Se muestran los primeros 300 problemas." : "");
        document.getElementById("erroresFilas").innerHTML = p.errores.map((e) =>
            `<tr><td class="num mono">${e.fila}</td><td>${esc(e.columna)}</td><td>${esc(e.mensaje)}</td></tr>`).join("");

        document.getElementById("muestraBox").hidden = !p.muestra.length || !p.puedeConfirmar;
        document.getElementById("muestra").innerHTML = p.muestra.map((m) => `<li>${esc(m)}</li>`).join("");

        btnConfirmar.hidden = !p.puedeConfirmar;
        if (p.puedeConfirmar) btnConfirmar.innerHTML = `${icono("check")}Confirmar importación (${numero(p.operaciones)} operaciones)`;
        else if (!p.errores.length) toast(mensaje, "error");
        vista.scrollIntoView({ behavior: "smooth", block: "nearest" });
    }

    btnConfirmar.addEventListener("click", async () => {
        if (!previa) return;
        const ok = await confirmar({
            titulo: "Confirmar importación",
            mensaje: `Se ejecutarán ${numero(previa.operaciones)} operaciones de «${previa.archivo}». Todo se guarda junto: si una fila falla, no se importa ninguna.`,
            aceptar: "Importar"
        });
        if (!ok) return;
        ocupado(btnConfirmar, true);
        const r = await api(`/api/importar/confirmar/${previa.token}`, { method: "POST" });
        ocupado(btnConfirmar, false);
        if (!r.ok) { toast(r.message, "error"); return; }
        toast(r.message);
        reiniciar();
    });

    document.getElementById("btnOtro").addEventListener("click", reiniciar);

    (async () => {
        const r = await api("/api/importar/tipos");
        if (!r.ok) { toast(r.message, "error"); return; }
        tipos = r.data;
        lista.innerHTML = tipos.map((t) => `<li><button type="button" class="imp-tipo" data-tipo="${esc(t.tipo)}" aria-pressed="false">
                <span class="n">${String(t.orden).padStart(2, "0")}</span><b>${esc(t.titulo)}</b><small>${esc(t.descripcion)}</small></button></li>`).join("");
        seleccionar(new URLSearchParams(location.search).get("tipo"));
    })();
})();
