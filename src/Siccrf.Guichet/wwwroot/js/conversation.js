"use strict";

import { ChatFeedback } from "./chat-feedback.js";

(() => {
    const form = document.getElementById("chat-form");
    if (!form) return;

    const message = document.getElementById("message");
    const send = document.getElementById("chat-envoyer");
    const select = document.getElementById("modeles-list");
    const discover = document.getElementById("modeles-charger");
    const correction = document.getElementById("correction-form");
    const error = document.getElementById("chat-error");
    const status = document.getElementById("chat-status");
    const configured = document.querySelector(".model-settings").dataset.configure === "true";
    let submitted = document.querySelector(".model-settings").dataset.transmise === "true";
    let remaining = Number(document.getElementById("completion-progress").max) - Number(document.getElementById("completion-progress").value);
    const feedback = new ChatFeedback();
    let busy = false;
    let dirty = false;
    let retryAt = 0;
    let acceptedModel = select?.value || "";

    function refreshControls() {
        send.disabled = submitted || !configured || busy || dirty || !acceptedModel || Date.now() < retryAt;
        message.disabled = submitted || !configured || busy;
        if (select) select.disabled = submitted || busy || dirty;
        if (discover) discover.disabled = submitted || busy || dirty;
        correction.querySelectorAll("textarea, button").forEach(control => { control.disabled = submitted || busy; });
        document.getElementById("review-button").disabled = submitted || busy || dirty || remaining > 0;
        document.querySelectorAll(".role-switch button").forEach(control => { control.disabled = busy; });
    }

    function showError(text) {
        error.textContent = text;
        error.hidden = !text;
    }

    const revision = () => Number(form.querySelector("[data-revision]").value);

    async function request(url, data) {
        const response = await fetch(url, {
            method: data === undefined ? "GET" : "POST",
            credentials: "same-origin",
            headers: data === undefined ? {} : {
                "Content-Type": "application/json",
                "RequestVerificationToken": form.querySelector('[name="__RequestVerificationToken"]').value
            },
            body: data === undefined ? undefined : JSON.stringify(data)
        });
        const result = await response.json().catch(() => null);
        if (!response.ok) {
            if (response.status === 409 && result?.etat) applyState(result.etat);
            if (response.status === 429) {
                const seconds = Math.min(300, Math.max(1, Number(response.headers.get("Retry-After")) || 30));
                retryAt = Date.now() + seconds * 1000;
                setTimeout(refreshControls, seconds * 1000 + 100);
            }
            throw new Error(result?.erreur || "La requête n'a pas abouti. Rechargez la page si votre session a expiré ; votre message reste dans la zone de saisie.");
        }
        if (!result) throw new Error("La réponse du serveur est illisible. Rechargez la page pour retrouver l'état de la fiche.");
        return result;
    }

    async function run(action, waiting, reply = false) {
        if (busy) return;
        busy = true;
        showError("");
        status.textContent = waiting;
        feedback.start(reply, message.value.trim());
        refreshControls();
        let success = false;
        try {
            await action();
            success = true;
            status.textContent = "";
        } catch (exception) {
            status.textContent = "";
            showError(exception instanceof TypeError
                ? "La connexion a été interrompue. Votre message reste saisi. Rechargez la page pour vérifier si une réponse a été conservée."
                : exception.message);
        } finally {
            busy = false;
            refreshControls();
            feedback.finish(success, reply);
            if (reply && !message.disabled) message.focus();
        }
    }

    function applyState(state) {
        submitted = Boolean(state.demande);
        remaining = state.manquants.length;
        document.getElementById("submission-controls").hidden = submitted;
        document.getElementById("submitted-notice").hidden = !submitted;
        document.getElementById("submitted-reference").textContent = state.demande?.reference || "";
        document.getElementById("submission-hint").textContent = remaining === 0
            ? "La fiche est complète. Relisez-la avant de confirmer l'envoi."
            : "La transmission sera possible lorsque les champs obligatoires seront renseignés.";
        document.querySelectorAll("[data-revision]").forEach(input => { input.value = state.revision; });
        document.querySelectorAll("[data-preview]").forEach(preview => {
            const value = state.champs[preview.dataset.preview] || "";
            preview.textContent = value || "À préciser";
            preview.classList.toggle("field-empty", !value);
        });
        correction.querySelectorAll("[data-field]").forEach(input => {
            input.value = state.champs[input.dataset.field] || "";
        });
        const completed = state.nombreObligatoires - state.manquants.length;
        document.getElementById("completion-count").textContent = `${completed} / ${state.nombreObligatoires}`;
        document.getElementById("fiche-toggle-count").textContent = `${completed}/${state.nombreObligatoires}`;
        document.getElementById("completion-progress").value = completed;
        document.getElementById("missing-details").hidden = state.manquants.length === 0;
        document.getElementById("complete-note").hidden = state.manquants.length > 0;
        document.getElementById("missing-summary").textContent = `${state.manquants.length} champ(s) obligatoire(s) à compléter`;
        const list = document.getElementById("missing-list");
        list.replaceChildren(...state.manquants.map(field => {
            const item = document.createElement("li");
            item.textContent = field.libelle;
            return item;
        }));
        const history = document.getElementById("chat-history");
        history.replaceChildren(...state.messages.map(turn => {
            const container = document.createElement("div");
            container.className = `history-message ${turn.role === "user" ? "user-message" : "assistant-reply"}`;
            const author = document.createElement("strong");
            author.textContent = turn.role === "user" ? "Vous" : "Assistant SICCRF";
            const content = document.createElement("p");
            content.textContent = turn.content;
            container.append(author, content);
            return container;
        }));
        acceptedModel = state.modele || "";
        const target = document.getElementById("chat-cible");
        target.textContent = state.question ? `À préciser : ${state.question.libelle}` : "";
        target.hidden = !state.question;
        if (select && ![...select.options].some(option => option.value === acceptedModel) && acceptedModel)
            select.add(new Option(acceptedModel, acceptedModel));
        if (select) select.value = acceptedModel;
        dirty = false;
        document.getElementById("unsaved").hidden = true;
    }

    correction.addEventListener("input", () => {
        dirty = true;
        status.textContent = "Appliquez vos corrections avant de poursuivre la conversation.";
        refreshControls();
    });

    discover?.addEventListener("click", () => run(async () => {
        const result = await request("/conversation/modeles");
        // Une découverte ne doit pas acquitter une révision modifiée dans un autre onglet.
        if (result.revision !== revision())
            throw new Error("La fiche a changé dans un autre onglet. Rechargez la page avant de poursuivre.");
        select.replaceChildren(new Option("Choisir un modèle", ""),
            ...result.modeles.map(model => new Option(model.id, model.id)));
        select.value = result.selection;
        acceptedModel = select.value;
        if (!result.modeles.length) throw new Error("Aucun modèle de génération de texte n'est accessible à ce compte.");
    }, "Découverte des modèles Albert…"));

    select?.addEventListener("change", () => run(async () => {
        if (!select.value) {
            select.value = acceptedModel;
            return;
        }
        try {
            applyState(await request("/conversation/modele", { modele: select.value, revision: revision() }));
        } catch (exception) {
            select.value = acceptedModel;
            throw exception;
        }
    }, "Sélection du modèle…"));

    message.addEventListener("keydown", event => {
        if (event.key !== "Enter" || event.shiftKey || event.isComposing || event.keyCode === 229) return;
        event.preventDefault();
        if (!send.disabled) form.requestSubmit(send);
    });

    form.addEventListener("submit", event => {
        event.preventDefault();
        if (submitted || busy || dirty || !acceptedModel || Date.now() < retryAt || !message.value.trim()) return;
        feedback.prepareAudio();
        run(async () => {
            applyState(await request("/conversation/messages", { texte: message.value.trim(), revision: revision() }));
            message.value = "";
        }, "Albert prépare sa réponse…", true);
    });

    refreshControls();
})();
