"use strict";

(async () => {
    const seccion = document.getElementById("setup");
    if (!seccion) return;

    const r = await Spin.api("/api/inicio/resumen");
    seccion.removeAttribute("data-cargando");
    if (!r.ok) {
        const error = seccion.querySelector("[data-error]");
        error.textContent = r.message;
        error.hidden = false;
        return;
    }

    const valores = {
        plantas: r.data.plantas, almacenes: r.data.almacenes, lineas: r.data.lineas,
        usuarios: r.data.usuarios, proveedores: r.data.proveedores, insumos: r.data.insumos,
        productosConReceta: r.data.productosConReceta, insumosConStock: r.data.insumosConStock,
        productosConVentas: r.data.productosConVentas
    };
    let completos = 0;
    for (const [paso, valor] of Object.entries(valores)) {
        const fila = seccion.querySelector(`[data-paso="${paso}"]`);
        fila.querySelector("[data-dato]").textContent = Spin.numero(valor);
        // Usuarios: el paso cuenta como hecho cuando hay alguien además del administrador.
        const hecho = paso === "usuarios" ? valor > 1 : valor > 0;
        fila.classList.toggle("is-hecho", hecho);
        if (hecho) {
            completos++;
            fila.querySelector(".setup-num").innerHTML = Spin.icono("check");
            fila.querySelector(".setup-num").setAttribute("aria-label", "Completo");
        }
    }
    seccion.querySelector("[data-progreso]").textContent = completos;
    // Con la configuración completa, el bloque se pliega para dar lugar al panel.
    const plegable = document.getElementById("setupPlegable");
    if (plegable) {
        plegable.querySelector("[data-progreso-resumen]").textContent = completos;
        if (completos === Object.keys(valores).length) plegable.open = false;
    }
})();
