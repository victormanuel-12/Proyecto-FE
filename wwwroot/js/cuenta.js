"use strict";

// Evita el doble envío de los formularios de cuenta (ingreso y cambio de contraseña).
document.querySelectorAll("form").forEach((form) => {
    form.addEventListener("submit", () => {
        const boton = form.querySelector("[data-enviar-una-vez]");
        if (!boton) return;
        setTimeout(() => {
            boton.disabled = true;
            boton.classList.add("is-busy");
        }, 0);
    });
});
