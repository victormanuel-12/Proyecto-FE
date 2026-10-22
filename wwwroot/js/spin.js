"use strict";

/**
 * SPIn — utilidades comunes del frontend.
 * - api(): cliente fetch con CSRF, sesión y manejo de errores.
 * - toast(), confirmar(), abrir()/cerrar() de <dialog>.
 * - Formularios: leerFormulario(), mostrarErrores(), limpiarErrores(), ocupado().
 * - Listado: tabla paginada con búsqueda, filtros y franja de estados.
 */
const Spin = (() => {
    // ------------------------------------------------------------ utilidades
    const esc = (valor) => String(valor ?? "").replace(/[&<>"']/g, (c) => ({
        "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;"
    }[c]));

    const icono = (nombre) => `<svg class="ic" aria-hidden="true"><use href="#i-${nombre}"/></svg>`;

    const formatoNumero = new Intl.NumberFormat("es-PE");
    const numero = (n) => (n === null || n === undefined ? "—" : formatoNumero.format(n));
    // Cantidades de receta: hasta 6 decimales (ej.: 0.0042 kg de CO₂ por botella).
    const formatoCantidad = new Intl.NumberFormat("es-PE", { maximumFractionDigits: 6 });
    const cantidad = (n) => (n === null || n === undefined ? "—" : formatoCantidad.format(n));
    const fechaHora = (iso) => {
        if (!iso) return "—";
        const [f, h = ""] = String(iso).split("T");
        return `${f.split("-").reverse().join("/")} ${h.slice(0, 5)}`.trim();
    };
    const fecha = (iso) => (iso ? String(iso).slice(0, 10).split("-").reverse().join("/") : "—");
    const hoyIso = () => {
        const d = new Date();
        return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
    };
    const formatoMoneda = new Intl.NumberFormat("es-PE", { minimumFractionDigits: 2, maximumFractionDigits: 4 });
    const dinero = (n) => (n === null || n === undefined ? "—" : formatoMoneda.format(n));

    function debounce(fn, ms = 300) {
        let t;
        return (...args) => {
            clearTimeout(t);
            t = setTimeout(() => fn(...args), ms);
        };
    }

    // ------------------------------------------------------------ API
    const tokenCsrf = () => document.querySelector('meta[name="csrf-token"]')?.content ?? "";

    /**
     * Llama a la API y devuelve siempre { ok, status, message, data }.
     * Nunca lanza excepción: los errores de red se devuelven como ok = false.
     */
    async function api(url, { method = "GET", body } = {}) {
        const opciones = { method, credentials: "same-origin", headers: { Accept: "application/json" } };
        if (method !== "GET") opciones.headers["X-CSRF-TOKEN"] = tokenCsrf();
        if (body instanceof FormData) {
            opciones.body = body;              // archivos: el navegador pone el Content-Type con su boundary
        } else if (body !== undefined) {
            opciones.headers["Content-Type"] = "application/json";
            opciones.body = JSON.stringify(body);
        }

        let respuesta;
        try {
            respuesta = await fetch(url, opciones);
        } catch {
            return { ok: false, status: 0, data: null, message: "No se pudo conectar con el servidor. Verifique que SPIn esté en ejecución y su conexión de red." };
        }

        if (respuesta.status === 401) {
            const volver = encodeURIComponent(location.pathname + location.search);
            location.href = `/cuenta/login?returnUrl=${volver}`;
            return { ok: false, status: 401, data: null, message: "Su sesión terminó. Vuelva a ingresar." };
        }
        if (respuesta.status === 413) {
            return { ok: false, status: 413, data: null, message: "El archivo es demasiado grande (máximo 5 MB)." };
        }
        let json = null;
        try { json = await respuesta.json(); } catch { json = null; }

        if (respuesta.status === 403) {
            // Una regla de negocio puede responder 403 con su propio mensaje (ej.: solo un administrador aprueba).
            return { ok: false, status: 403, data: null, message: json?.message ?? "No tiene permiso para realizar esta acción." };
        }

        const ok = respuesta.ok && json?.success !== false;
        return {
            ok,
            status: respuesta.status,
            data: json?.data ?? null,
            message: json?.message ?? (ok ? "" : "Ocurrió un error inesperado. Intente nuevamente.")
        };
    }

    // ------------------------------------------------------------ avisos
    function toast(mensaje, tipo = "ok") {
        const zona = document.getElementById("toastZone");
        if (!zona || !mensaje) return;
        const el = document.createElement("div");
        el.className = `toast toast-${tipo}`;
        el.setAttribute("role", tipo === "error" ? "alert" : "status");
        el.innerHTML = `${icono(tipo === "error" ? "error" : "ok")}<span></span>`;
        el.querySelector("span").textContent = mensaje;
        zona.appendChild(el);
        setTimeout(() => {
            el.classList.add("is-leaving");
            setTimeout(() => el.remove(), 250);
        }, tipo === "error" ? 6500 : 3500);
    }

    // ------------------------------------------------------------ diálogos
    function abrir(dialogo) { if (!dialogo.open) dialogo.showModal(); }
    function cerrar(dialogo) { if (dialogo?.open) dialogo.close(); }

    document.addEventListener("click", (e) => {
        const boton = e.target.closest("[data-cerrar]");
        if (boton) cerrar(boton.closest("dialog"));
    });

    /** Muestra el diálogo de confirmación. Resuelve true si el usuario acepta. */
    function confirmar({ titulo, mensaje, aceptar = "Confirmar", peligro = false }) {
        const dlg = document.getElementById("dlgConfirmar");
        dlg.querySelector("[data-titulo]").textContent = titulo;
        dlg.querySelector("[data-mensaje]").textContent = mensaje;
        const boton = dlg.querySelector("[data-aceptar]");
        boton.textContent = aceptar;
        boton.className = `btn ${peligro ? "btn-danger" : "btn-primary"}`;
        dlg.returnValue = "";

        return new Promise((resolve) => {
            dlg.addEventListener("close", () => resolve(dlg.returnValue === "ok"), { once: true });
            abrir(dlg);
            dlg.querySelector('button[value="cancelar"]').focus();
        });
    }

    // ------------------------------------------------------------ formularios
    /**
     * Lee los campos con name. Reglas:
     * - data-tipo="numero": número; vacío = 0 (o null si tiene data-opcional).
     * - checkbox con data-multiple: arreglo de números marcados.
     * - radio: solo el marcado.
     * - Campos deshabilitados no se envían.
     */
    function leerFormulario(form) {
        const datos = {};
        for (const el of form.elements) {
            if (!el.name || el.disabled) continue;
            const esNumero = el.dataset.tipo === "numero";
            if (el.type === "checkbox" && el.dataset.multiple !== undefined) {
                datos[el.name] ??= [];
                if (el.checked) datos[el.name].push(Number(el.value));
            } else if (el.type === "checkbox") {
                datos[el.name] = el.checked;
            } else if (el.type === "radio") {
                if (!(el.name in datos)) datos[el.name] = esNumero ? 0 : null;
                if (el.checked) datos[el.name] = esNumero ? Number(el.value) : el.value;
            } else if (esNumero) {
                const v = el.value.trim();
                datos[el.name] = v === "" ? (el.dataset.opcional !== undefined ? null : 0) : Number(v);
            } else {
                const v = el.value.trim();
                datos[el.name] = v === "" ? null : v;
            }
        }
        return datos;
    }

    function limpiarErrores(form) {
        form.querySelectorAll(".is-invalid").forEach((el) => {
            el.classList.remove("is-invalid");
            el.removeAttribute("aria-invalid");
        });
        form.querySelectorAll("[data-error-de]").forEach((el) => { el.textContent = ""; });
        const general = form.querySelector("[data-error-general]");
        if (general) { general.textContent = ""; general.hidden = true; }
    }

    /** Muestra errores por campo (diccionario del servidor) o un mensaje general. */
    function mostrarErrores(form, errores, mensaje) {
        limpiarErrores(form);
        let enCampo = false;

        if (errores && typeof errores === "object" && !Array.isArray(errores)) {
            for (const [campo, mensajes] of Object.entries(errores)) {
                const destino = form.querySelector(`[data-error-de="${CSS.escape(campo)}"]`);
                if (!destino) continue;
                destino.textContent = Array.isArray(mensajes) ? mensajes[0] : String(mensajes);
                enCampo = true;
                const control = form.querySelector(`[name="${CSS.escape(campo)}"]`);
                const contenedor = control?.closest(".radio-seg, .check-grid");
                const marcar = contenedor ?? control;
                if (marcar) {
                    marcar.classList.add("is-invalid");
                    if (control) control.setAttribute("aria-invalid", "true");
                }
            }
        }

        if (!enCampo) {
            // Si el error es de un campo que este formulario no tiene, se muestra su texto (más útil que uno genérico).
            const primerError = errores && typeof errores === "object" && !Array.isArray(errores)
                ? Object.values(errores).flat()[0]
                : null;
            mensaje = primerError || mensaje;
            const general = form.querySelector("[data-error-general]");
            if (general) {
                general.textContent = mensaje || "Revise los datos ingresados.";
                general.hidden = false;
            } else {
                toast(mensaje || "Revise los datos ingresados.", "error");
            }
        }

        const primero = form.querySelector('[aria-invalid="true"]') ?? form.querySelector("[data-error-general]:not([hidden])");
        primero?.focus?.();
    }

    // Al corregir un campo se quita su marca de error.
    const limpiarCampo = (e) => {
        const control = e.target;
        if (!control.name || !control.form) return;
        (control.closest(".radio-seg, .check-grid") ?? control).classList.remove("is-invalid");
        control.removeAttribute("aria-invalid");
        const destino = control.form.querySelector(`[data-error-de="${CSS.escape(control.name)}"]`);
        if (destino) destino.textContent = "";
    };
    document.addEventListener("input", limpiarCampo);
    document.addEventListener("change", limpiarCampo);

    /** Marca el botón como ocupado y evita doble envío. */
    function ocupado(boton, estaOcupado) {
        if (!boton) return;
        boton.disabled = estaOcupado;
        boton.classList.toggle("is-busy", estaOcupado);
        boton.setAttribute("aria-busy", String(estaOcupado));
    }

    /**
     * Envía un formulario de un diálogo a la API.
     * Devuelve la respuesta si fue exitosa; si no, muestra los errores en el formulario y devuelve null.
     */
    async function enviar(form, url, method, cuerpo) {
        const boton = form.querySelector('[type="submit"]');
        if (boton?.disabled) return null;
        ocupado(boton, true);
        const r = await api(url, { method, body: cuerpo ?? leerFormulario(form) });
        ocupado(boton, false);
        if (!r.ok) {
            mostrarErrores(form, r.status === 400 ? r.data : null, r.message);
            return null;
        }
        return r;
    }

    async function llenarSelect(select, url, { vacio, valor } = {}) {
        const r = await api(url);
        const actual = valor ?? select.value;
        const opciones = r.ok && Array.isArray(r.data) ? r.data : [];
        select.innerHTML = (vacio !== undefined ? `<option value="">${esc(vacio)}</option>` : "") +
            opciones.map((o) => `<option value="${esc(o.id)}">${esc(o.nombre)}</option>`).join("");
        if (actual !== undefined && actual !== null) select.value = String(actual);
        if (!r.ok) toast(r.message, "error");
        return opciones;
    }

    // ------------------------------------------------------------ piezas de tabla
    const estado = (activo) => activo
        ? `<span class="badge badge-ok">${icono("check")}Activo</span>`
        : `<span class="badge badge-off">${icono("pausa")}Inactivo</span>`;

    const boton = (accion, nombreIcono, etiqueta, { peligro = false, deshabilitado = false } = {}) =>
        `<button type="button" class="icon-btn${peligro ? " is-danger" : ""}" data-accion="${accion}"
                 title="${esc(etiqueta)}" aria-label="${esc(etiqueta)}"${deshabilitado ? " disabled" : ""}>${icono(nombreIcono)}</button>`;

    function filaEstado(columnas, tipo, mensaje) {
        const contenido = {
            cargando: `<span class="spinner" aria-hidden="true"></span><span>Cargando…</span>`,
            vacio: `${icono("vacio")}<span>${esc(mensaje)}</span>`,
            error: `${icono("error")}<span>${esc(mensaje)}</span>
                    <button type="button" class="btn btn-secondary btn-sm" data-reintentar>${icono("recargar")}Reintentar</button>`
        }[tipo];
        return `<tr class="tbl-estado"><td colspan="${columnas}">
                  <div class="tbl-estado-box${tipo === "error" ? " is-error" : ""}" role="${tipo === "error" ? "alert" : "status"}">${contenido}</div>
                </td></tr>`;
    }

    // ------------------------------------------------------------ Listado
    class Listado {
        /**
         * @param {object} cfg
         * @param {string} cfg.url               Endpoint de la API (GET paginado).
         * @param {HTMLTableElement} cfg.tabla
         * @param {Array<{titulo:string, render:(item)=>string, clase?:string}>} cfg.columnas
         * @param {(item)=>string} [cfg.acciones] HTML de los botones de la fila.
         * @param {(accion:string, item:object)=>void} [cfg.alAccion]
         * @param {HTMLFormElement} [cfg.filtros]
         * @param {HTMLElement} [cfg.segmentos]
         * @param {HTMLElement} [cfg.pie]
         * @param {string} [cfg.textoVacio]
         * @param {string} [cfg.estadoInicial]  "activos" | "inactivos" | ""
         */
        constructor(cfg) {
            this.cfg = cfg;
            this.estado = { pagina: 1, tamanoPagina: cfg.tamanoPagina ?? 10, estado: cfg.estadoInicial ?? "activos" };
            this.items = new Map();
            this.solicitud = 0;
            // Sin acciones (usuario de solo consulta) se oculta la columna de acciones del encabezado.
            if (!cfg.acciones) cfg.tabla.querySelector("thead .col-acciones")?.remove();
            // Enlaces de alertas y del panel: ?buscar=PRE-500 llega con la búsqueda aplicada.
            const buscarUrl = new URLSearchParams(location.search).get("buscar");
            const campoBuscar = cfg.filtros?.querySelector('input[name="buscar"]');
            if (buscarUrl && campoBuscar) campoBuscar.value = buscarUrl;
            this.enlazar();
        }

        get columnas() { return this.cfg.columnas.length + (this.cfg.acciones ? 1 : 0); }

        enlazar() {
            const { filtros, segmentos, tabla, pie } = this.cfg;
            const recargar = () => { this.estado.pagina = 1; this.cargar(); };

            if (filtros) {
                filtros.addEventListener("submit", (e) => { e.preventDefault(); recargar(); });
                filtros.querySelectorAll('input[type="search"]').forEach((i) => i.addEventListener("input", debounce(recargar, 300)));
                filtros.querySelectorAll('select, input[type="date"], input[type="checkbox"]').forEach((s) => s.addEventListener("change", recargar));
            }

            if (segmentos) {
                segmentos.addEventListener("click", (e) => {
                    const seg = e.target.closest("[data-estado]");
                    if (!seg) return;
                    this.estado.estado = seg.dataset.estado;
                    segmentos.querySelectorAll("[data-estado]").forEach((s) => s.setAttribute("aria-pressed", String(s === seg)));
                    recargar();
                });
            }

            tabla.tBodies[0].addEventListener("click", (e) => {
                if (e.target.closest("[data-reintentar]")) { this.cargar(); return; }
                const btn = e.target.closest("[data-accion]");
                if (btn) {
                    if (btn.disabled) return;
                    const item = this.items.get(btn.closest("tr")?.dataset.id);
                    if (item) this.cfg.alAccion?.(btn.dataset.accion, item, btn);
                    return;
                }
                // Clic en el resto de la fila (opcional): abre el detalle.
                const fila = e.target.closest("tr[data-id]");
                if (fila && this.cfg.alClicFila && !e.target.closest("a, button, input, select")) {
                    this.cfg.alClicFila(this.items.get(fila.dataset.id), fila);
                }
            });

            // Teclado: Enter sobre una fila clicable abre el detalle.
            tabla.tBodies[0].addEventListener("keydown", (e) => {
                if (e.key !== "Enter" || !this.cfg.alClicFila) return;
                const fila = e.target.closest("tr[data-id]");
                if (fila && e.target === fila) this.cfg.alClicFila(this.items.get(fila.dataset.id), fila);
            });

            pie?.addEventListener("click", (e) => {
                const btn = e.target.closest("[data-pagina]");
                if (!btn || btn.disabled) return;
                this.estado.pagina = Number(btn.dataset.pagina);
                this.cargar();
            });
        }

        parametros() {
            const p = new URLSearchParams();
            p.set("pagina", this.estado.pagina);
            p.set("tamanoPagina", this.estado.tamanoPagina);
            if (this.estado.estado) p.set(this.cfg.parametroSegmento ?? "estado", this.estado.estado);
            if (this.cfg.filtros) {
                for (const el of this.cfg.filtros.elements) {
                    if (el.name && el.value.trim() !== "") p.set(el.name, el.value.trim());
                }
            }
            return p;
        }

        async cargar() {
            const idSolicitud = ++this.solicitud;
            const { tabla, pie, segmentos } = this.cfg;
            const tbody = tabla.tBodies[0];

            tabla.setAttribute("aria-busy", "true");
            if (!tbody.rows.length || tbody.querySelector(".tbl-estado")) tbody.innerHTML = filaEstado(this.columnas, "cargando");
            else tbody.classList.add("is-refreshing");

            const r = await api(`${this.cfg.url}?${this.parametros()}`);
            if (idSolicitud !== this.solicitud) return; // llegó una respuesta más nueva

            tabla.removeAttribute("aria-busy");
            tbody.classList.remove("is-refreshing");

            if (!r.ok || !r.data) {
                tbody.innerHTML = filaEstado(this.columnas, "error", r.message || "No se pudo cargar la información.");
                if (pie) pie.innerHTML = "";
                return;
            }

            const d = r.data;
            if (segmentos && d.activos !== undefined) {
                const poner = (clave, valor) => {
                    const el = segmentos.querySelector(`[data-conteo="${clave}"]`);
                    if (el) el.textContent = numero(valor);
                };
                poner("activos", d.activos);
                poner("inactivos", d.inactivos);
                poner("todos", d.activos + d.inactivos);
            }

            this.items.clear();
            if (!d.items.length) {
                const filtrado = this.parametros().has("buscar") || [...(this.cfg.filtros?.querySelectorAll("select") ?? [])].some((s) => s.value);
                tbody.innerHTML = filaEstado(this.columnas, "vacio",
                    filtrado ? "No hay resultados con esos filtros." : (this.cfg.textoVacio ?? "Aún no hay registros."));
            } else {
                tbody.innerHTML = d.items.map((item) => {
                    const id = String(item.id);
                    this.items.set(id, item);
                    const celdas = this.cfg.columnas.map((c) =>
                        `<td data-label="${esc(c.titulo)}"${c.clase ? ` class="${c.clase}"` : ""}>${c.render(item)}</td>`).join("");
                    const acciones = this.cfg.acciones
                        ? `<td class="col-acciones" data-label="Acciones"><span class="acciones">${this.cfg.acciones(item)}</span></td>`
                        : "";
                    const clicable = this.cfg.alClicFila ? ' class="is-clicable" tabindex="0"' : "";
                    return `<tr data-id="${esc(id)}"${clicable}>${celdas}${acciones}</tr>`;
                }).join("");
            }

            this.estado.pagina = d.pagina;
            if (pie) pie.innerHTML = this.htmlPie(d);
            this.cfg.alCargar?.(d);
        }

        /** Resalta la fila del elemento abierto en el panel de detalle (o ninguna). */
        marcar(id) {
            this.cfg.tabla.tBodies[0].querySelectorAll("tr[data-id]").forEach((tr) =>
                tr.classList.toggle("is-seleccionada", id !== null && tr.dataset.id === String(id)));
        }

        htmlPie(d) {
            if (!d.total) return "";
            const desde = (d.pagina - 1) * d.tamanoPagina + 1;
            const hasta = Math.min(d.pagina * d.tamanoPagina, d.total);
            const texto = `<span>Mostrando ${numero(desde)}–${numero(hasta)} de ${numero(d.total)}</span>`;
            if (d.totalPaginas <= 1) return texto;

            const paginas = [];
            const inicio = Math.max(1, d.pagina - 2);
            const fin = Math.min(d.totalPaginas, inicio + 4);
            for (let p = Math.max(1, fin - 4); p <= fin; p++) paginas.push(p);

            const btn = (p, contenido, etiqueta, deshabilitado = false, actual = false) =>
                `<button type="button" data-pagina="${p}" aria-label="${etiqueta}"${deshabilitado ? " disabled" : ""}${actual ? ' aria-current="page"' : ""}>${contenido}</button>`;

            return `${texto}<nav class="pager" aria-label="Paginación">
                ${btn(d.pagina - 1, icono("izq"), "Página anterior", d.pagina <= 1)}
                ${paginas.map((p) => btn(p, p, `Página ${p}`, false, p === d.pagina)).join("")}
                ${btn(d.pagina + 1, icono("der"), "Página siguiente", d.pagina >= d.totalPaginas)}
            </nav>`;
        }
    }

    // ------------------------------------------------------------ menú en pantallas pequeñas
    function iniciarMenu() {
        const boton = document.getElementById("btnMenu");
        const capa = document.getElementById("railOverlay");
        if (!boton || !capa) return;
        const alternar = (abrirMenu) => {
            document.body.classList.toggle("menu-abierto", abrirMenu);
            boton.setAttribute("aria-expanded", String(abrirMenu));
            capa.hidden = !abrirMenu;
        };
        boton.addEventListener("click", () => alternar(!document.body.classList.contains("menu-abierto")));
        capa.addEventListener("click", () => alternar(false));
        document.addEventListener("keydown", (e) => {
            if (e.key === "Escape" && document.body.classList.contains("menu-abierto")) alternar(false);
        });
    }
    iniciarMenu();

    // ------------------------------------------------------------ campana de alertas (Entrega 9)
    async function actualizarCampana() {
        const campana = document.getElementById("hdCampana");
        if (!campana) return;
        const r = await api("/api/alertas/resumen");
        if (!r.ok || !r.data) return;
        const importantes = r.data.criticas + r.data.altas;
        const contador = campana.querySelector(".hd-contador");
        contador.hidden = importantes === 0;
        contador.textContent = importantes > 99 ? "99+" : String(importantes);
        contador.classList.toggle("is-critica", r.data.criticas > 0);
        const texto = r.data.criticas + r.data.altas + r.data.medias + r.data.informativas === 0
            ? "Alertas: ninguna activa"
            : `Alertas: ${r.data.criticas} crítica(s), ${r.data.altas} alta(s), ${r.data.medias} media(s), ${r.data.informativas} informativa(s)`;
        campana.title = texto;
        campana.setAttribute("aria-label", texto);
    }
    if (document.getElementById("hdCampana")) {
        actualizarCampana();
        setInterval(actualizarCampana, 5 * 60 * 1000);
    }

    return {
        esc, icono, numero, cantidad, dinero, fecha, fechaHora, hoyIso, debounce,
        api, toast, abrir, cerrar, confirmar,
        leerFormulario, limpiarErrores, mostrarErrores, ocupado, enviar, llenarSelect,
        estado, boton, Listado, actualizarCampana
    };
})();
