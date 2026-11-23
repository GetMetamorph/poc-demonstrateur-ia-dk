"use strict";

const ficheToggle = document.getElementById("fiche-toggle");
const fichePanel = document.getElementById("fiche-panel");

ficheToggle?.addEventListener("click", () => {
    const open = fichePanel.hidden;
    fichePanel.hidden = !open;
    document.getElementById("workspace").classList.toggle("fiche-visible", open);
    ficheToggle.setAttribute("aria-expanded", String(open));
    document.getElementById("fiche-toggle-label").textContent = open ? "Masquer la fiche" : "Afficher la fiche";
    if (open && window.matchMedia("(max-width: 1050px)").matches) {
        document.getElementById("fiche-title").focus({ preventScroll: true });
        fichePanel.scrollIntoView({ behavior: "instant", block: "start" });
    }
});

const correctionForm = document.getElementById("correction-form");

if (correctionForm) {
    correctionForm.addEventListener("input", event => {
        const input = event.target;
        if (!(input instanceof HTMLTextAreaElement) || !input.dataset.field) return;

        const preview = document.querySelector(`[data-preview="${input.dataset.field}"]`);
        if (!preview) return;

        const value = input.value.trim();
        // Toute donnée saisie ou renvoyée par l'IA est du texte, jamais du HTML.
        preview.textContent = value || "À préciser";
        preview.classList.toggle("field-empty", !value);
        document.getElementById("unsaved").hidden = false;
    });
}
