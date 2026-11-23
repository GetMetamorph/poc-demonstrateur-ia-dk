// Retours visuels et sonores : aucun fichier audio ni permission de notification.
export class ChatFeedback {
    constructor() {
        this.panel = document.querySelector(".chat-panel");
        this.working = document.getElementById("chat-working");
        this.pending = document.getElementById("chat-pending");
        this.soundButton = document.getElementById("chat-sound");
        this.audio = null;
        this.sound = true;
        try { this.sound = localStorage.getItem("siccrf-chat-sound") !== "off"; } catch { /* Préférence facultative. */ }
        this.soundButton.addEventListener("click", () => {
            this.sound = !this.sound;
            try { localStorage.setItem("siccrf-chat-sound", this.sound ? "on" : "off"); } catch { /* Sans stockage, la bascule reste utilisable. */ }
            if (this.sound) this.prepareAudio();
            this.updateSoundButton();
        });
        this.updateSoundButton();
    }

    updateSoundButton() {
        this.soundButton.textContent = this.sound ? "Son activé" : "Son coupé";
        this.soundButton.setAttribute("aria-pressed", String(this.sound));
        this.soundButton.title = this.sound ? "Couper le son de réponse" : "Activer le son de réponse";
    }

    prepareAudio() {
        if (!this.sound) return;
        try {
            const Audio = window.AudioContext || window.webkitAudioContext;
            if (!Audio) return;
            this.audio ??= new Audio();
            // Appelé depuis un clic ou une touche pour respecter les restrictions du navigateur.
            if (this.audio.state === "suspended") this.audio.resume().catch(() => {});
        } catch { /* Le retour visuel fonctionne aussi sans audio. */ }
    }

    playReply() {
        if (!this.sound || this.audio?.state !== "running") return;
        try {
            const now = this.audio.currentTime;
            [660, 880].forEach((frequency, index) => {
                const oscillator = this.audio.createOscillator();
                const volume = this.audio.createGain();
                const start = now + index * 0.11;
                oscillator.type = "sine";
                oscillator.frequency.value = frequency;
                volume.gain.setValueAtTime(0, start);
                volume.gain.linearRampToValueAtTime(0.025, start + 0.015);
                volume.gain.exponentialRampToValueAtTime(0.001, start + 0.13);
                oscillator.connect(volume);
                volume.connect(this.audio.destination);
                oscillator.onended = () => { oscillator.disconnect(); volume.disconnect(); };
                oscillator.start(start);
                oscillator.stop(start + 0.15);
            });
        } catch { /* Ne jamais empêcher une réponse pour un son indisponible. */ }
    }

    start(reply, text) {
        this.panel.classList.remove("has-chat-error");
        this.panel.classList.add("is-working");
        document.getElementById("chat-form").setAttribute("aria-busy", "true");
        document.getElementById("assistant-status-label").textContent = reply ? "Réponse en préparation" : "Connexion en cours";
        this.working.hidden = !reply;
        this.pending.hidden = !reply;
        document.getElementById("chat-pending-text").textContent = reply ? text : "";
        document.querySelector(".button-spinner").hidden = !reply;
        document.getElementById("chat-send-label").textContent = reply ? "En cours…" : "Envoyer";
        if (reply) this.scrollToEnd();
    }

    finish(success, reply) {
        this.panel.classList.remove("is-working");
        this.panel.classList.toggle("has-chat-error", !success);
        document.getElementById("chat-form").setAttribute("aria-busy", "false");
        document.getElementById("assistant-status-label").textContent = success ? "Prêt à vous aider" : "À réessayer";
        this.working.hidden = true;
        this.pending.hidden = true;
        document.querySelector(".button-spinner").hidden = true;
        document.getElementById("chat-send-label").textContent = "Envoyer";
        if (success && reply) {
            this.playReply();
            this.scrollToEnd();
        }
    }

    scrollToEnd() {
        const messages = document.querySelector(".chat-messages");
        messages.scrollTop = messages.scrollHeight;
    }
}
