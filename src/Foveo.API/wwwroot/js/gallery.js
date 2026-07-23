(() => {
    "use strict";

    const lightbox = document.getElementById("lightbox");
    const content = document.getElementById("lightbox-content");
    const closeBtn = document.getElementById("lightbox-close");
    if (!lightbox) return;

    function open(type, src) {
        content.innerHTML = "";
        if (type === "Video") {
            const video = document.createElement("video");
            video.src = src;
            video.controls = true;
            video.autoplay = true;
            video.playsInline = true;
            content.appendChild(video);
        } else {
            const img = document.createElement("img");
            img.src = src;
            img.alt = "";
            content.appendChild(img);
        }
        lightbox.hidden = false;
        document.body.style.overflow = "hidden";
    }

    function close() {
        lightbox.hidden = true;
        content.innerHTML = "";
        document.body.style.overflow = "";
    }

    document.querySelectorAll(".card").forEach((card) =>
        card.addEventListener("click", () => open(card.dataset.type, card.dataset.src)));

    closeBtn.addEventListener("click", close);
    lightbox.addEventListener("click", (e) => { if (e.target === lightbox) close(); });
    document.addEventListener("keydown", (e) => { if (e.key === "Escape" && !lightbox.hidden) close(); });
})();
