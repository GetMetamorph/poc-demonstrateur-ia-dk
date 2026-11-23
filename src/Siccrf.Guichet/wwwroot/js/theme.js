// Appliqué avant les styles pour éviter un éclair clair au chargement.
(() => {
    const storageKey = "siccrf-theme";
    const root = document.documentElement;
    const systemTheme = window.matchMedia?.("(prefers-color-scheme: dark)");
    let preference;
    try {
        const saved = localStorage.getItem(storageKey);
        if (saved === "light" || saved === "dark") preference = saved;
    } catch { /* La bascule fonctionne aussi si le stockage est indisponible. */ }

    const applyTheme = () => {
        const dark = preference ? preference === "dark" : Boolean(systemTheme?.matches);
        root.dataset.bsTheme = dark ? "dark" : "light";
        const button = document.getElementById("theme-toggle");
        if (!button) return;
        const label = dark ? "Mode clair" : "Mode sombre";
        button.setAttribute("aria-pressed", String(dark));
        button.setAttribute("aria-label", `Activer le ${label.toLowerCase()}`);
        button.title = `Activer le ${label.toLowerCase()}`;
        document.getElementById("theme-label").textContent = label;
        button.querySelector(".theme-icon").textContent = dark ? "☀" : "☾";
        button.hidden = false;
    };

    applyTheme();
    systemTheme?.addEventListener("change", () => {
        if (!preference) applyTheme();
    });
    document.addEventListener("DOMContentLoaded", () => {
        applyTheme();
        document.getElementById("theme-toggle")?.addEventListener("click", () => {
            preference = root.dataset.bsTheme === "dark" ? "light" : "dark";
            try { localStorage.setItem(storageKey, preference); } catch { /* Choix valable pour cette page. */ }
            applyTheme();
        });
    }, { once: true });
})();
