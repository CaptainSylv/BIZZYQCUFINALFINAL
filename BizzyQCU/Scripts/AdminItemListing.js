// ============================================================
// AdminItemListing.js - Dynamic Products Listing
// ============================================================

const urlParamsItem = new URLSearchParams(window.location.search);
const enterpriseIdItem = urlParamsItem.get('enterpriseId');

let allProductsItem = [];
let filteredProductsItem = [];
let selectedProductIdItem = null;
let approveUiOnOk = null;
let weeklySalesChart = null;

let currentPageItem = 1;
let totalPagesItem = 1;
let pageSizeItem = 6;

function getColumnsPerRowItem() {
    if (window.innerWidth <= 580) return 1;
    if (window.innerWidth <= 960) return 2;
    return 3;
}

function computePageSizeItem() {
    pageSizeItem = getColumnsPerRowItem() * 2; // max 2 rows only
}

document.addEventListener('DOMContentLoaded', function () {
    computePageSizeItem();

    if (enterpriseIdItem && enterpriseIdItem !== 'null' && enterpriseIdItem !== 'undefined') {
        loadEnterpriseDetailsItem();
        loadProductsItem();
    } else {
        document.getElementById('profileName').textContent = 'No Enterprise Selected';
        document.getElementById('productsGrid').innerHTML = '<div class="empty-state">No enterprise selected</div>';
    }

    setupEventListenersItem();
});

function loadEnterpriseDetailsItem() {
    fetch(`/AdminPanel/GetEnterpriseDetails?enterpriseId=${enterpriseIdItem}`)
        .then(response => {
            if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
            return response.json();
        })
        .then(data => {
            if (data && data.EnterpriseId) {
                populateProfileItem(data);
            } else if (data.message) {
                document.getElementById('profileName').textContent = 'Enterprise not found';
            }
        })
        .catch(() => {
            document.getElementById('profileName').textContent = 'Error loading enterprise';
        });
}

function loadProductsItem() {
    fetch(`/AdminPanel/GetProductsForListing?enterpriseId=${enterpriseIdItem}`)
        .then(response => response.json())
        .then(data => {
            allProductsItem = data || [];
            currentPageItem = 1;
            applyFiltersAndRenderItem();
        })
        .catch(() => {
            document.getElementById('productsGrid').innerHTML = '<div class="empty-state">Error loading products</div>';
            updatePaginationUiItem(0);
        });
}

function populateProfileItem(enterprise) {
    const nameEl = document.getElementById('profileName');
    if (nameEl) nameEl.textContent = enterprise.StoreName || 'Enterprise Name';

    const idEl = document.getElementById('profileId');
    if (idEl) idEl.textContent = enterprise.EnterpriseId || 'N/A';

    const emailEl = document.getElementById('profileEmail');
    if (emailEl) emailEl.textContent = enterprise.Email || 'No email';

    const statusEl = document.getElementById('profileStatus');
    const status = enterprise.Status || 'pending';
    if (statusEl) {
        statusEl.textContent = status.charAt(0).toUpperCase() + status.slice(1);
        statusEl.className = 'info-value status-' + status.toLowerCase();
    }

    const rating = enterprise.RatingAvg || 0;
    const starsEl = document.getElementById('profileStars');
    if (starsEl) {
        starsEl.innerHTML = '';
        const roundedRating = Math.round(rating);
        for (let i = 1; i <= 5; i++) {
            const starSpan = document.createElement('span');
            starSpan.className = 'star' + (i <= roundedRating ? ' filled' : '');
            starSpan.textContent = '?';
            starsEl.appendChild(starSpan);
        }
    }

    const firstLetter = enterprise.StoreName ? enterprise.StoreName.charAt(0).toUpperCase() : 'E';
    const colors = ['#4A6CF7', '#7B3FE4', '#d4a017', '#2e4dbf', '#e03131'];
    const colorIndex = (enterprise.EnterpriseId || 1) % colors.length;
    const bgColor = colors[colorIndex];

    const avatarDiv = document.getElementById('profileAvatar');
    if (avatarDiv) {
        avatarDiv.innerHTML = `<div style="width: 72px; height: 72px; border-radius: 50%; background: ${bgColor}; display: flex; align-items: center; justify-content: center;"><span style="color: white; font-size: 32px; font-weight: 600;">${escapeHtmlItem(firstLetter)}</span></div>`;
    }
}

function isActiveProductItem(product) {
    const normalizedStatus = (product.Status || '').toLowerCase();
    const approvalState = Number(product.ApprovalState ?? 0);
    return approvalState === 1 && (normalizedStatus === 'active' || normalizedStatus === 'approved');
}

function isInactiveProductItem(product) {
    const normalizedStatus = (product.Status || '').toLowerCase();
    return normalizedStatus === 'inactive';
}

function applyFiltersAndRenderItem() {
    const queryEl = document.getElementById('productSearch');
    const statusEl = document.getElementById('statusFilter');

    const query = queryEl ? queryEl.value.toLowerCase().trim() : '';
    const status = statusEl ? statusEl.value : 'all';

    filteredProductsItem = (allProductsItem || []).filter(p => {
        const matchesSearch = (p.ProductName || '').toLowerCase().includes(query);
        if (!matchesSearch) return false;

        if (status === 'active') return isActiveProductItem(p);
        if (status === 'inactive') return isInactiveProductItem(p);
        return true;
    });

    totalPagesItem = Math.max(1, Math.ceil(filteredProductsItem.length / pageSizeItem));
    if (currentPageItem > totalPagesItem) currentPageItem = totalPagesItem;
    if (currentPageItem < 1) currentPageItem = 1;

    renderCurrentPageItem();
    updatePaginationUiItem(filteredProductsItem.length);
}

function renderCurrentPageItem() {
    const start = (currentPageItem - 1) * pageSizeItem;
    const end = start + pageSizeItem;
    const pageProducts = filteredProductsItem.slice(start, end);
    renderProductsItem(pageProducts);
}

function renderProductsItem(products) {
    const grid = document.getElementById('productsGrid');
    if (!grid) return;

    if (!products || products.length === 0) {
        grid.innerHTML = '<div class="empty-state">No products found</div>';
        selectedProductIdItem = null;
        return;
    }

    grid.innerHTML = '';

    products.forEach(product => {
        const card = document.createElement('div');
        card.className = 'product-card';
        card.dataset.id = product.ProductId;
        card.dataset.name = (product.ProductName || '').toLowerCase();
        card.addEventListener('click', () => selectProductItem(product.ProductId));

        const normalizedStatus = (product.Status || '').toLowerCase();
        const approvalState = Number(product.ApprovalState ?? 0);
        let statusBadge = '<span class="status-badge unknown">Unknown</span>';

        if (approvalState === -1) {
            statusBadge = '<span class="status-badge rejected">Rejected</span>';
        } else if (approvalState === 0) {
            statusBadge = '<span class="status-badge pending">Pending Approval</span>';
        } else if (approvalState === 1 && (normalizedStatus === 'active' || normalizedStatus === 'approved')) {
            statusBadge = '<span class="status-badge active">Active</span>';
        } else if (normalizedStatus === 'inactive') {
            statusBadge = '<span class="status-badge inactive">Inactive</span>';
        }

        const productImageMarkup = product.ProductImage
            ? `<img class="product-img" src="${product.ProductImage}" alt="${escapeHtmlItem(product.ProductName || 'Product')}" />`
            : `<div class="product-placeholder">${escapeHtmlItem((product.ProductName || 'P').charAt(0))}</div>`;

        card.innerHTML = `
            <div class="product-image-wrap">${productImageMarkup}</div>
            <div class="product-info-wrap">
                <div class="product-title">${escapeHtmlItem(product.ProductName || 'Unknown')}</div>
                <div class="product-detail-line"><span class="detail-label">Price:</span> ?${parseFloat(product.Price || 0).toFixed(2)}</div>
                <div class="product-detail-line"><span class="detail-label">Description:</span> ${escapeHtmlItem(product.Description || 'No description')}</div>
                ${statusBadge}
            </div>
        `;
        grid.appendChild(card);
    });

    const selectedStillVisible = products.some(p => parseInt(p.ProductId, 10) === parseInt(selectedProductIdItem, 10));
    if (!selectedStillVisible) {
        selectedProductIdItem = products[0].ProductId;
    }
    selectProductItem(selectedProductIdItem);
}

function updatePaginationUiItem(totalCount) {
    const prevBtn = document.getElementById('prevPageBtn');
    const nextBtn = document.getElementById('nextPageBtn');
    const pageInfo = document.getElementById('pageInfo');

    const hasData = totalCount > 0;
    const effectivePages = hasData ? totalPagesItem : 1;

    if (pageInfo) pageInfo.textContent = hasData ? `Page ${currentPageItem} of ${effectivePages}` : 'Page 0 of 0';
    if (prevBtn) prevBtn.disabled = !hasData || currentPageItem <= 1;
    if (nextBtn) nextBtn.disabled = !hasData || currentPageItem >= effectivePages;
}

function selectProductItem(productId) {
    selectedProductIdItem = productId;
    const cards = document.querySelectorAll('.product-card');
    cards.forEach(card => {
        card.classList.toggle('selected', parseInt(card.dataset.id, 10) === parseInt(productId, 10));
    });
}

function setupEventListenersItem() {
    const productSearch = document.getElementById('productSearch');
    if (productSearch) {
        productSearch.addEventListener('input', function () {
            currentPageItem = 1;
            applyFiltersAndRenderItem();
        });
    }

    const statusFilter = document.getElementById('statusFilter');
    if (statusFilter) {
        statusFilter.addEventListener('change', function () {
            currentPageItem = 1;
            applyFiltersAndRenderItem();
        });
    }

    const prevBtn = document.getElementById('prevPageBtn');
    if (prevBtn) {
        prevBtn.addEventListener('click', function () {
            if (currentPageItem > 1) {
                currentPageItem--;
                renderCurrentPageItem();
                updatePaginationUiItem(filteredProductsItem.length);
            }
        });
    }

    const nextBtn = document.getElementById('nextPageBtn');
    if (nextBtn) {
        nextBtn.addEventListener('click', function () {
            if (currentPageItem < totalPagesItem) {
                currentPageItem++;
                renderCurrentPageItem();
                updatePaginationUiItem(filteredProductsItem.length);
            }
        });
    }

    window.addEventListener('resize', function () {
        const oldSize = pageSizeItem;
        computePageSizeItem();
        if (oldSize !== pageSizeItem) {
            currentPageItem = 1;
            applyFiltersAndRenderItem();
        }
    });

    const approveBtn = document.getElementById('approveItemBtn');
    if (approveBtn) {
        approveBtn.addEventListener('click', () => {
            const effectiveProductId = selectedProductIdItem || getFirstVisibleProductId();
            if (effectiveProductId) {
                const selected = getProductById(effectiveProductId);
                if (!selected) {
                    showToastItem('Product not found.', 'error');
                    return;
                }

                const normalizedStatus = (selected.Status || '').toLowerCase();
                const approvalState = Number(selected.ApprovalState ?? 0);
                if (approvalState === 1 && (normalizedStatus === 'active' || normalizedStatus === 'approved')) {
                    showApproveUi('Already Approved', `"${selected.ProductName || 'This product'}" is already approved.`, false);
                    return;
                }

                showApproveUi('Confirm Approval', `Are you sure you want to approve "${selected.ProductName || 'this product'}"?`, true, () => approveProductItem(effectiveProductId));
            } else {
                showToastItem('Please select a product first', 'error');
            }
        });
    }

    const removeBtn = document.getElementById('removeItemBtn');
    if (removeBtn) {
        removeBtn.addEventListener('click', () => {
            const effectiveProductId = selectedProductIdItem || getFirstVisibleProductId();
            if (effectiveProductId) {
                if (confirm('Are you sure you want to reject this product?')) {
                    removeProductItem(effectiveProductId);
                }
            } else {
                showToastItem('Please select a product first', 'error');
            }
        });
    }

    const deleteBtn = document.getElementById('deleteBtn');
    const modalBackdrop = document.getElementById('deleteModal');
    const cancelDelete = document.getElementById('cancelDelete');
    const confirmDeleteBtn = document.getElementById('confirmDelete');

    if (deleteBtn && modalBackdrop) {
        deleteBtn.addEventListener('click', () => {
            const name = document.getElementById('profileName').textContent;
            const modalName = document.getElementById('modalEnterpriseName');
            if (modalName) modalName.textContent = name;
            modalBackdrop.classList.add('active');
        });
    }

    if (cancelDelete) {
        cancelDelete.addEventListener('click', () => {
            if (modalBackdrop) modalBackdrop.classList.remove('active');
        });
    }

    if (modalBackdrop) {
        modalBackdrop.addEventListener('click', (e) => {
            if (e.target === modalBackdrop) modalBackdrop.classList.remove('active');
        });
    }

    if (confirmDeleteBtn) {
        confirmDeleteBtn.addEventListener('click', () => {
            fetch('/AdminPanel/DeleteEnterprise', {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: `enterpriseId=${enterpriseIdItem}`
            })
                .then(response => response.json())
                .then(data => {
                    if (data.success) {
                        showToastItem('Enterprise deleted successfully', 'success');
                        setTimeout(() => { window.location.href = '/AdminPanel/AdminLandingEntrep'; }, 1500);
                    } else {
                        showToastItem('Failed to delete enterprise: ' + (data.message || 'Unknown error'), 'error');
                        if (modalBackdrop) modalBackdrop.classList.remove('active');
                    }
                })
                .catch(() => {
                    showToastItem('An error occurred', 'error');
                    if (modalBackdrop) modalBackdrop.classList.remove('active');
                });
        });
    }

    const viewReportsBtn = document.getElementById('viewReportsBtn');
    if (viewReportsBtn) {
        viewReportsBtn.addEventListener('click', openWeeklySalesReport);
    }

    const closeReportBtn = document.getElementById('closeReportBtn');
    if (closeReportBtn) {
        closeReportBtn.addEventListener('click', closeWeeklySalesReport);
    }

    const reportModal = document.getElementById('reportModal');
    if (reportModal) {
        reportModal.addEventListener('click', function (e) {
            if (e.target === reportModal) closeWeeklySalesReport();
        });
    }
}

function approveProductItem(productId) {
    const formData = new URLSearchParams();
    formData.append('productId', productId);

    fetch('/AdminPanel/ApproveProduct', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: formData
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                showToastItem('Product approved successfully', 'success');
                loadProductsItem();
                selectedProductIdItem = null;
            } else {
                showToastItem('Failed to approve product: ' + (data.message || 'Unknown error'), 'error');
            }
        })
        .catch(() => showToastItem('An error occurred', 'error'));
}

function removeProductItem(productId) {
    const formData = new URLSearchParams();
    formData.append('productId', productId);

    fetch('/AdminPanel/RemoveProduct', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: formData
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                showToastItem('Product rejected successfully', 'success');
                loadProductsItem();
                selectedProductIdItem = null;
            } else {
                showToastItem('Failed to reject product: ' + (data.message || 'Unknown error'), 'error');
            }
        })
        .catch(() => showToastItem('An error occurred', 'error'));
}

function showToastItem(message, type) {
    const toast = document.getElementById('toast');
    if (!toast) return;

    toast.textContent = message;
    toast.className = 'toast ' + type;
    toast.style.display = 'block';

    setTimeout(() => {
        toast.style.display = 'none';
    }, 3000);
}

function escapeHtmlItem(str) {
    if (!str) return '';
    return str
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}

function openWeeklySalesReport() {
    if (!enterpriseIdItem) {
        showToastItem('No enterprise selected.', 'error');
        return;
    }

    const modal = document.getElementById('reportModal');
    const emptyMsg = document.getElementById('reportEmptyMsg');
    if (!modal) return;

    if (emptyMsg) emptyMsg.style.display = 'none';
    modal.classList.add('show');
    modal.setAttribute('aria-hidden', 'false');

    fetch(`/AdminPanel/GetSalesData?enterpriseId=${enterpriseIdItem}&days=7`)
        .then(response => response.json())
        .then(data => renderWeeklySalesChart(Array.isArray(data) ? data : []))
        .catch(() => {
            renderWeeklySalesChart([]);
            showToastItem('Failed to load weekly sales report.', 'error');
        });
}

function closeWeeklySalesReport() {
    const modal = document.getElementById('reportModal');
    if (!modal) return;
    modal.classList.remove('show');
    modal.setAttribute('aria-hidden', 'true');
}

function renderWeeklySalesChart(salesRows) {
    const canvas = document.getElementById('weeklySalesChart');
    const emptyMsg = document.getElementById('reportEmptyMsg');
    const subtitle = document.getElementById('reportSubtitle');
    if (!canvas || typeof Chart === 'undefined') return;

    const labels = salesRows.map(x => x.Label || '');
    const values = salesRows.map(x => Number(x.Value || 0));

    if (subtitle) subtitle.textContent = 'Last 7 days';
    if (emptyMsg) emptyMsg.style.display = labels.length ? 'none' : 'block';

    if (weeklySalesChart) {
        weeklySalesChart.destroy();
        weeklySalesChart = null;
    }

    weeklySalesChart = new Chart(canvas.getContext('2d'), {
        type: 'line',
        data: {
            labels: labels.length ? labels : ['No Data'],
            datasets: [{
                label: 'Weekly Sales',
                data: values.length ? values : [0],
                borderColor: '#4A6CF7',
                backgroundColor: 'rgba(74,108,247,0.15)',
                borderWidth: 3,
                fill: true,
                tension: 0.25,
                pointRadius: 4,
                pointBackgroundColor: '#2e4dbf'
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    display: true
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    ticks: {
                        callback: function (value) {
                            return '₱' + value;
                        }
                    }
                }
            }
        }
    });
}

function getProductById(productId) {
    for (let i = 0; i < allProductsItem.length; i++) {
        if (parseInt(allProductsItem[i].ProductId, 10) === parseInt(productId, 10)) {
            return allProductsItem[i];
        }
    }
    return null;
}

function showApproveUi(title, message, showCancel, onOk) {
    const modal = document.getElementById('approveUiModal');
    const titleEl = document.getElementById('approveUiTitle');
    const messageEl = document.getElementById('approveUiMessage');
    const cancelBtn = document.getElementById('approveUiCancelBtn');
    const okBtn = document.getElementById('approveUiOkBtn');
    if (!modal || !titleEl || !messageEl || !cancelBtn || !okBtn) return;

    titleEl.textContent = title || 'Notice';
    messageEl.textContent = message || '';
    cancelBtn.style.display = showCancel ? 'inline-block' : 'none';
    okBtn.textContent = showCancel ? 'Yes, Approve' : 'OK';
    approveUiOnOk = typeof onOk === 'function' ? onOk : null;

    modal.classList.add('show');
    modal.setAttribute('aria-hidden', 'false');
}

function closeApproveUi() {
    const modal = document.getElementById('approveUiModal');
    if (!modal) return;
    modal.classList.remove('show');
    modal.setAttribute('aria-hidden', 'true');
    approveUiOnOk = null;
}

const approveUiCancelBtn = document.getElementById('approveUiCancelBtn');
if (approveUiCancelBtn) {
    approveUiCancelBtn.addEventListener('click', closeApproveUi);
}

const approveUiOkBtn = document.getElementById('approveUiOkBtn');
if (approveUiOkBtn) {
    approveUiOkBtn.addEventListener('click', () => {
        const action = approveUiOnOk;
        closeApproveUi();
        if (typeof action === 'function') action();
    });
}

const approveUiModal = document.getElementById('approveUiModal');
if (approveUiModal) {
    approveUiModal.addEventListener('click', (e) => {
        if (e.target === approveUiModal) closeApproveUi();
    });
}

function getFirstVisibleProductId() {
    const cards = document.querySelectorAll('.product-card');
    for (let i = 0; i < cards.length; i++) {
        const card = cards[i];
        if (card.style.display === 'none') continue;
        const id = parseInt(card.dataset.id, 10);
        if (!isNaN(id)) return id;
    }
    return null;
}
