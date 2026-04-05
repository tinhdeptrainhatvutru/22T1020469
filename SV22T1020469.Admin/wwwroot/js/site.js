// =====================================================================
// SITE.JS – Admin SV22T1020548
// =====================================================================

// Hiển thị ảnh preview khi chọn file
function previewImage(input) {
    if (!input.files || !input.files[0]) return;
    const previewId = input.dataset.imgPreview;
    if (!previewId) return;
    const img = document.getElementById(previewId);
    if (!img) return;
    const reader = new FileReader();
    reader.onload = e => img.src = e.target.result;
    reader.readAsDataURL(input.files[0]);
}

// ── AJAX pagination search ──────────────────────────────────────────
function paginationSearch(event, form, page) {
    if (event) event.preventDefault();
    if (!form) return;

    const url      = form.action;
    const method   = (form.method || "GET").toUpperCase();
    const targetId = form.dataset.target;
    const formData = new FormData(form);
    formData.set("page", page);

    let fetchUrl = url;
    if (method === "GET") {
        fetchUrl = url + "?" + new URLSearchParams(formData).toString();
    }

    const targetEl = targetId ? document.getElementById(targetId) : null;
    if (targetEl) {
        targetEl.innerHTML = `<div class="text-center py-4 text-muted">
            <div class="spinner-border spinner-border-sm me-2"></div>Đang tải...</div>`;
    }

    fetch(fetchUrl, { method, body: method === "GET" ? null : formData })
        .then(r => r.text())
        .then(html => { if (targetEl) targetEl.innerHTML = html; })
        .catch(() => {
            if (targetEl) targetEl.innerHTML =
                `<div class="alert alert-danger m-2">Không tải được dữ liệu.</div>`;
        });
}

// ── Modal chung (load partial view vào modal, hỗ trợ cả GET & POST) ─
(function () {
    const modalEl = document.getElementById("dialogModal");
    if (!modalEl) return;

    const modalContent = modalEl.querySelector(".modal-content");

    // Xóa nội dung khi đóng
    modalEl.addEventListener("hidden.bs.modal", () => {
        modalContent.innerHTML = "";
    });

    // Mở modal bằng link GET (open-modal class)
    window.openModal = function (event, link) {
        if (!link) return;
        if (event) event.preventDefault();

        const url = link.getAttribute("href");
        modalContent.innerHTML = `<div class="modal-body text-center py-5">
            <div class="spinner-border text-primary"></div></div>`;

        let modal = bootstrap.Modal.getInstance(modalEl);
        if (!modal) modal = new bootstrap.Modal(modalEl, { backdrop: "static", keyboard: false });
        modal.show();

        fetch(url)
            .then(r => r.text())
            .then(html => {
                modalContent.innerHTML = html;
                // Gán submit cho form bên trong modal
                bindModalForm(modalEl, url);
            })
            .catch(() => {
                modalContent.innerHTML =
                    `<div class="modal-body text-danger">Không tải được dữ liệu.</div>`;
            });
    };

    // Gắn sự kiện submit cho form bên trong modal
    function bindModalForm(modalEl, loadUrl) {
        const form = modalEl.querySelector("form");
        if (!form) return;

        form.addEventListener("submit", function (e) {
            e.preventDefault();
            const action = form.action || loadUrl;
            const formData = new FormData(form);

            fetch(action, { method: "POST", body: formData })
                .then(r => {
                    // Nếu server redirect (status 200 sau redirect), reload trang
                    if (r.redirected) {
                        window.location.href = r.url;
                        return;
                    }
                    return r.text();
                })
                .then(html => {
                    if (!html) return; // đã redirect
                    // Nếu trả về HTML (lỗi validation), cập nhật modal
                    modalContent.innerHTML = html;
                    bindModalForm(modalEl, loadUrl);
                })
                .catch(() => {
                    alert("Có lỗi xảy ra, vui lòng thử lại.");
                });
        });
    }
})();

// ── Gắn open-modal cho các link sau khi DOM sẵn sàng ───────────────
document.addEventListener("DOMContentLoaded", function () {
    attachOpenModal(document);
});

function attachOpenModal(container) {
    container.querySelectorAll(".open-modal").forEach(el => {
        // Tránh gắn lại
        if (el.dataset.modalBound) return;
        el.dataset.modalBound = "1";
        el.addEventListener("click", function (e) {
            openModal(e, this);
        });
    });
}
