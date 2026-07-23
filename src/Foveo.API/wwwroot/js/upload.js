(() => {
    "use strict";

    const form = document.getElementById("upload-form");
    const fileInput = document.getElementById("file-input");
    const dropzone = document.querySelector(".dropzone");
    const list = document.getElementById("upload-list");
    const button = document.getElementById("upload-button");
    const nameInput = document.getElementById("uploader-name");
    const doneMessage = document.getElementById("upload-done");

    /** @type {File[]} */
    let files = [];

    // Browsers often report an empty MIME type for HEIC/MOV; fall back to the extension.
    const EXT_TYPES = {
        heic: "image/heic", heif: "image/heif",
        jpg: "image/jpeg", jpeg: "image/jpeg", png: "image/png",
        webp: "image/webp", gif: "image/gif",
        mp4: "video/mp4", mov: "video/quicktime", webm: "video/webm"
    };

    function contentTypeOf(file) {
        if (file.type) return file.type;
        const ext = file.name.split(".").pop().toLowerCase();
        return EXT_TYPES[ext] || "application/octet-stream";
    }

    function setFiles(selected) {
        files = Array.from(selected);
        list.innerHTML = "";
        doneMessage.hidden = true;
        files.forEach((file, i) => {
            const item = document.createElement("li");
            item.className = "upload-item";
            item.id = `item-${i}`;
            item.innerHTML =
                `<span class="upload-item-name"></span>` +
                `<span class="upload-item-status">Klar</span>` +
                `<span class="upload-bar"><span></span></span>`;
            item.querySelector(".upload-item-name").textContent = file.name;
            list.appendChild(item);
        });
        button.disabled = files.length === 0;
    }

    function setStatus(i, text, state) {
        const item = document.getElementById(`item-${i}`);
        if (!item) return;
        item.querySelector(".upload-item-status").textContent = text;
        item.classList.remove("is-done", "is-error");
        if (state) item.classList.add(state);
    }

    function setProgress(i, fraction) {
        const bar = document.querySelector(`#item-${i} .upload-bar span`);
        if (bar) bar.style.width = `${Math.round(fraction * 100)}%`;
    }

    function putWithProgress(url, file, contentType, onProgress) {
        return new Promise((resolve, reject) => {
            const xhr = new XMLHttpRequest();
            xhr.open("PUT", url);
            xhr.setRequestHeader("Content-Type", contentType);
            xhr.upload.onprogress = (e) => { if (e.lengthComputable) onProgress(e.loaded / e.total); };
            xhr.onload = () => (xhr.status >= 200 && xhr.status < 300)
                ? resolve()
                : reject(new Error(`Upload failed (${xhr.status})`));
            xhr.onerror = () => reject(new Error("Network error during upload"));
            xhr.send(file);
        });
    }

    async function requestTickets() {
        const payload = {
            uploaderName: nameInput.value.trim() || null,
            files: files.map((f) => ({ fileName: f.name, contentType: contentTypeOf(f), sizeBytes: f.size }))
        };
        const response = await fetch("/api/media/upload-tickets", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        });
        if (!response.ok) {
            const problem = await response.json().catch(() => null);
            throw new Error(problem?.detail || "Could not start the upload.");
        }
        return response.json();
    }

    async function upload() {
        button.disabled = true;
        let tickets;
        try {
            tickets = await requestTickets();
        } catch (err) {
            files.forEach((_, i) => setStatus(i, err.message, "is-error"));
            button.disabled = false;
            return;
        }

        let allOk = true;
        for (let i = 0; i < files.length; i++) {
            const ticket = tickets[i];
            try {
                setStatus(i, "Uploader…");
                await putWithProgress(ticket.uploadUrl, files[i], ticket.contentType, (f) => setProgress(i, f));
                await fetch(`/api/media/${ticket.mediaId}/complete`, { method: "POST" });
                setProgress(i, 1);
                setStatus(i, "Færdig", "is-done");
            } catch (err) {
                allOk = false;
                setStatus(i, err.message, "is-error");
            }
        }

        doneMessage.hidden = !allOk;
        button.disabled = false;
    }

    fileInput.addEventListener("change", () => setFiles(fileInput.files));

    ["dragenter", "dragover"].forEach((evt) =>
        dropzone.addEventListener(evt, (e) => { e.preventDefault(); dropzone.classList.add("is-dragover"); }));
    ["dragleave", "drop"].forEach((evt) =>
        dropzone.addEventListener(evt, (e) => { e.preventDefault(); dropzone.classList.remove("is-dragover"); }));
    dropzone.addEventListener("drop", (e) => { if (e.dataTransfer?.files?.length) setFiles(e.dataTransfer.files); });

    form.addEventListener("submit", (e) => { e.preventDefault(); if (files.length) upload(); });
})();
