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

    // Stream one file straight to the API; the file IS the request body.
    function uploadFile(file, i) {
        const name = nameInput.value.trim();
        let url = `/api/media?fileName=${encodeURIComponent(file.name)}`;
        if (name) url += `&uploaderName=${encodeURIComponent(name)}`;

        return new Promise((resolve, reject) => {
            const xhr = new XMLHttpRequest();
            xhr.open("POST", url);
            xhr.setRequestHeader("Content-Type", contentTypeOf(file));
            xhr.upload.onprogress = (e) => { if (e.lengthComputable) setProgress(i, e.loaded / e.total); };
            xhr.onload = () => (xhr.status >= 200 && xhr.status < 300)
                ? resolve()
                : reject(new Error(`Upload fejlede (${xhr.status})`));
            xhr.onerror = () => reject(new Error("Netværksfejl"));
            xhr.send(file);
        });
    }

    async function upload() {
        button.disabled = true;
        let allOk = true;
        for (let i = 0; i < files.length; i++) {
            try {
                setStatus(i, "Uploader…");
                await uploadFile(files[i], i);
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
