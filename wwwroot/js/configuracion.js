"use strict";

(() => {
    const { esc, fechaHora, api, toast, enviar, limpiarErrores } = Spin;
    const form = document.getElementById("formConfig");

    function pintar(c) {
        form.elements.semanasHorizonte.value = c.semanasHorizonte;
        form.elements.semanasValidacion.value = c.semanasValidacion;
        form.elements.semanasMinimas.value = c.semanasMinimas;
        form.querySelector(`input[name="frecuencia"][value="${c.frecuencia}"]`).checked = true;
        form.elements.horaReentrenamiento.value = c.horaReentrenamiento;
        form.elements.diasRevision.value = c.diasRevision;
        form.elements.diasEntregaPorDefecto.value = c.diasEntregaPorDefecto;
        form.elements.umbralConteo.value = c.umbralConteo;
        document.getElementById("proximo").textContent = c.proximoEntrenamiento
            ? `Próximo reentrenamiento automático: ${fechaHora(c.proximoEntrenamiento)}.` : "Reentrenamiento automático desactivado.";
        document.getElementById("modificado").innerHTML = c.modificadoEn
            ? `Última modificación: ${esc(fechaHora(c.modificadoEn))} · ${esc(c.modificadoPor ?? "")}` : "Valores por defecto.";
        actualizarHora();
    }

    function actualizarHora() {
        const manual = form.querySelector('input[name="frecuencia"]:checked')?.value === "3";
        form.elements.horaReentrenamiento.disabled = manual;
    }
    form.addEventListener("change", (e) => { if (e.target.name === "frecuencia") actualizarHora(); });

    form.addEventListener("submit", async (e) => {
        e.preventDefault();
        limpiarErrores(form);
        const hora = form.elements.horaReentrenamiento;
        const cuerpo = {
            semanasHorizonte: Number(form.elements.semanasHorizonte.value) || 0,
            semanasValidacion: Number(form.elements.semanasValidacion.value) || 0,
            semanasMinimas: Number(form.elements.semanasMinimas.value) || 0,
            frecuencia: Number(form.querySelector('input[name="frecuencia"]:checked')?.value ?? 0),
            horaReentrenamiento: hora.value === "" ? 0 : Number(hora.value),
            diasRevision: Number(form.elements.diasRevision.value) || 0,
            diasEntregaPorDefecto: Number(form.elements.diasEntregaPorDefecto.value) || 0,
            umbralConteo: Number(form.elements.umbralConteo.value) || 0
        };
        const r = await enviar(form, "/api/configuracion", "PUT", cuerpo);
        if (!r) return;
        toast(r.message);
        pintar(r.data);
    });

    (async () => {
        const r = await api("/api/configuracion");
        if (r.ok) pintar(r.data);
        else toast(r.message, "error");
    })();
})();
