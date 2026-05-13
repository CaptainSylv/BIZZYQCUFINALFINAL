window.addEventListener('load', function () {
    const modal = document.getElementById("ent-ad-modal");
    const closeBtn = document.querySelector(".close-btn");

    if (modal && closeBtn) {
        modal.style.display = "flex";

        closeBtn.onclick = function () {
            modal.style.display = "none";
        };

        window.onclick = function (e) {
            if (e.target === modal) {
                modal.style.display = "none";
            }
        };
    }
});
