(() => {
    "use strict";

    const lightbox = document.getElementById("lightbox");
    const content = document.getElementById("lightbox-content");
    const closeBtn = document.getElementById("lightbox-close");
    const prevBtn = document.getElementById("lightbox-prev");
    const nextBtn = document.getElementById("lightbox-next");
    if (!lightbox) return;

    // Snapshot the gallery in order so we can page through it.
    const items = Array.from(document.querySelectorAll(".card")).map((card) => ({
        type: card.dataset.type,
        src: card.dataset.src
    }));

    let index = -1;
    const multiple = items.length > 1;

    function render() {
        const item = items[index];
        content.innerHTML = "";
        if (item.type === "Video") {
            const video = document.createElement("video");
            video.src = item.src;
            video.controls = true;
            video.autoplay = true;
            video.playsInline = true;
            content.appendChild(video);
        } else {
            const img = document.createElement("img");
            img.src = item.src;
            img.alt = "";
            content.appendChild(img);
        }
    }

    function open(i) {
        index = i;
        render();
        lightbox.hidden = false;
        document.body.style.overflow = "hidden";
        prevBtn.hidden = nextBtn.hidden = !multiple;
    }

    function close() {
        lightbox.hidden = true;
        content.innerHTML = "";
        document.body.style.overflow = "";
        index = -1;
    }

    // Wrap around so there's always a next/previous.
    function go(delta) {
        if (!multiple) return;
        index = (index + delta + items.length) % items.length;
        render();
    }

    document.querySelectorAll(".card").forEach((card, i) =>
        card.addEventListener("click", () => open(i)));

    prevBtn.addEventListener("click", (e) => { e.stopPropagation(); go(-1); });
    nextBtn.addEventListener("click", (e) => { e.stopPropagation(); go(1); });
    closeBtn.addEventListener("click", close);
    lightbox.addEventListener("click", (e) => { if (e.target === lightbox) close(); });

    document.addEventListener("keydown", (e) => {
        if (lightbox.hidden) return;
        if (e.key === "Escape") close();
        else if (e.key === "ArrowLeft") go(-1);
        else if (e.key === "ArrowRight") go(1);
    });

    // Swipe left/right on touch. Threshold + horizontal-dominance so it won't fight video controls.
    let startX = 0, startY = 0, tracking = false;
    lightbox.addEventListener("touchstart", (e) => {
        if (e.touches.length !== 1) { tracking = false; return; }
        startX = e.touches[0].clientX;
        startY = e.touches[0].clientY;
        tracking = true;
    }, { passive: true });

    lightbox.addEventListener("touchend", (e) => {
        if (!tracking) return;
        tracking = false;
        const dx = e.changedTouches[0].clientX - startX;
        const dy = e.changedTouches[0].clientY - startY;
        if (Math.abs(dx) > 50 && Math.abs(dx) > Math.abs(dy) * 1.5) {
            go(dx < 0 ? 1 : -1);
        }
    }, { passive: true });
})();
