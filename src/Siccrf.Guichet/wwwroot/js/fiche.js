"use strict";

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
