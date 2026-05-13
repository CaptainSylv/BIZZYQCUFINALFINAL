const searchInput = document.getElementById('searchInput');
const grid = document.getElementById('enterprisesGrid');
const countBadge = document.getElementById('countBadge');

let allEnterprises = [];
let filteredEnterprises = [];
let currentPage = 1;
let totalPages = 1;
let pageSize = 6;

function getColumnsPerRow() {
    if (window.innerWidth <= 580) return 1;
    if (window.innerWidth <= 900) return 2;
    return 3;
}

function computePageSize() {
    pageSize = getColumnsPerRow() * 2; // max 2 rows only
}

function loadEnterprises() {
    fetch('/AdminPanel/GetAllApprovedEnterprises')
        .then(response => response.json())
        .then(data => {
            allEnterprises = Array.isArray(data) ? data : [];
            currentPage = 1;
            applyFiltersAndRender();
        })
        .catch(() => {
            allEnterprises = [];
            applyFiltersAndRender();
        });
}

function renderCard(enterprise) {
    const rating = enterprise.RatingAvg || 0;
    const fullStars = Math.floor(rating);
    const hasHalfStar = (rating - fullStars) >= 0.5;

    let starsHtml = '';
    for (let i = 1; i <= 5; i++) {
        if (i <= fullStars) {
            starsHtml += '<span class="star filled">★</span>';
        } else if (i === fullStars + 1 && hasHalfStar) {
            starsHtml += '<span class="star half">★</span>';
        } else {
            starsHtml += '<span class="star">★</span>';
        }
    }

    const firstLetter = enterprise.StoreName ? enterprise.StoreName.charAt(0).toUpperCase() : 'E';
    const colors = ['#4A6CF7', '#7B3FE4', '#d4a017', '#2e4dbf', '#e03131'];
    const colorIndex = (enterprise.EnterpriseId || 1) % colors.length;
    const bgColor = colors[colorIndex];

    const card = document.createElement('div');
    card.className = 'enterprise-card';
    card.dataset.name = (enterprise.StoreName || '').toLowerCase();
    card.dataset.id = enterprise.EnterpriseId;

    card.innerHTML = `
        <div class="card-avatar" style="background: ${bgColor}; display: flex; align-items: center; justify-content: center;">
            <span style="color: white; font-size: 32px; font-weight: 600;">${escapeHtml(firstLetter)}</span>
        </div>
        <div class="card-name">${escapeHtml(enterprise.StoreName)}</div>
        <div class="card-id">${escapeHtml(enterprise.Username || 'No username')}</div>
        <div class="card-stars">${starsHtml}</div>
        <a href="/AdminPanel/AdminEditEntrep?enterpriseId=${enterprise.EnterpriseId}" class="view-btn">
            View Account
        </a>
    `;

    return card;
}

function applyFiltersAndRender() {
    const query = (searchInput ? searchInput.value : '').trim().toLowerCase();
    filteredEnterprises = (allEnterprises || []).filter(e => (e.StoreName || '').toLowerCase().includes(query));

    totalPages = Math.max(1, Math.ceil(filteredEnterprises.length / pageSize));
    if (currentPage > totalPages) currentPage = totalPages;
    if (currentPage < 1) currentPage = 1;

    renderCurrentPage(query);
    updateCount(filteredEnterprises.length);
    updatePaginationUi(filteredEnterprises.length);
}

function renderCurrentPage(query) {
    grid.innerHTML = '';

    if (!filteredEnterprises.length) {
        showEmptyState(query || '');
        return;
    }

    const start = (currentPage - 1) * pageSize;
    const end = start + pageSize;
    const pageItems = filteredEnterprises.slice(start, end);
    pageItems.forEach(enterprise => grid.appendChild(renderCard(enterprise)));
}

function updateCount(n) {
    if (!countBadge) return;
    countBadge.textContent = `${n} ${n === 1 ? 'Enterprise' : 'Enterprises'}`;
}

function updatePaginationUi(totalCount) {
    const prevBtn = document.getElementById('prevPageBtn');
    const nextBtn = document.getElementById('nextPageBtn');
    const pageInfo = document.getElementById('pageInfo');

    const hasData = totalCount > 0;
    const effectivePages = hasData ? totalPages : 1;

    if (pageInfo) pageInfo.textContent = hasData ? `Page ${currentPage} of ${effectivePages}` : 'Page 0 of 0';
    if (prevBtn) prevBtn.disabled = !hasData || currentPage <= 1;
    if (nextBtn) nextBtn.disabled = !hasData || currentPage >= effectivePages;
}

function showEmptyState(query = '') {
    const empty = document.createElement('div');
    empty.className = 'empty-state';
    empty.innerHTML = `
        <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
            <circle cx="11" cy="11" r="8"/>
            <line x1="21" y1="21" x2="16.65" y2="16.65"/>
        </svg>
        <p>
            ${query
            ? `No enterprises found for "<strong>${escapeHtml(query)}</strong>"`
            : 'No enterprises found.'}
        </p>
    `;
    grid.appendChild(empty);
}

function escapeHtml(str) {
    if (!str) return '';
    return str
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}

function setupEventListeners() {
    if (searchInput) {
        searchInput.addEventListener('input', function () {
            currentPage = 1;
            applyFiltersAndRender();
        });
    }

    const prevBtn = document.getElementById('prevPageBtn');
    if (prevBtn) {
        prevBtn.addEventListener('click', function () {
            if (currentPage > 1) {
                currentPage--;
                applyFiltersAndRender();
            }
        });
    }

    const nextBtn = document.getElementById('nextPageBtn');
    if (nextBtn) {
        nextBtn.addEventListener('click', function () {
            if (currentPage < totalPages) {
                currentPage++;
                applyFiltersAndRender();
            }
        });
    }

    window.addEventListener('resize', function () {
        const oldSize = pageSize;
        computePageSize();
        if (oldSize !== pageSize) {
            currentPage = 1;
            applyFiltersAndRender();
        }
    });
}

document.addEventListener('DOMContentLoaded', function () {
    computePageSize();
    setupEventListeners();
    loadEnterprises();
});
