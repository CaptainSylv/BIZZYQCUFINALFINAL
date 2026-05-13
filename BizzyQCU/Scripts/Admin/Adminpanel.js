document.addEventListener('DOMContentLoaded', function () {
    const logoutBtn = document.getElementById('logoutBtn');
    const modalBackdrop = document.getElementById('modalBackdrop');
    const cancelLogout = document.getElementById('cancelLogout');
    const confirmLogout = document.getElementById('confirmLogout');

 
    if (logoutBtn) {
        logoutBtn.addEventListener('click', function () {
            modalBackdrop.classList.add('active'); 
        });
    }

 
    if (cancelLogout) {
        cancelLogout.addEventListener('click', function () {
            modalBackdrop.classList.remove('active'); 
        });
    }

    
    if (modalBackdrop) {
        modalBackdrop.addEventListener('click', function (e) {
            if (e.target === modalBackdrop) {
                modalBackdrop.classList.remove('active'); 
            }
        });
    }

   
    if (confirmLogout) {
        confirmLogout.addEventListener('click', function () {
            window.location.href = '/Account/Logout';
        });
    }

   
    const panelUsers = document.getElementById('panelUsers');
    const panelEnterprises = document.getElementById('panelEnterprises');

    if (panelUsers) {
        panelUsers.addEventListener('click', function (e) {
            e.preventDefault();
            window.location.href = '/AdminPanel/AdminUsers';
        });
    }

    if (panelEnterprises) {
        panelEnterprises.addEventListener('click', function (e) {
            e.preventDefault();
            window.location.href = '/AdminPanel/AdminlandingEntrep';
        });
    }
});