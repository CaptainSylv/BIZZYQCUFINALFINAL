document.addEventListener("DOMContentLoaded", function () {

    var searchInput = document.getElementById('productSearch');
    var categoryFilter = document.getElementById('categoryFilter');
    var popularityFilter = document.getElementById('popularityFilter');
    var priceFilter = document.getElementById('priceFilter');
    var noResults = document.getElementById('noResults');
    var filterToggleBtn = document.getElementById('filterToggleBtn');
    var filterSection = document.getElementById('filterSection');

    // Pagination variables
    var currentPage = 1;
    var itemsPerPage = 4;

    // Mobile filter toggle
    if (filterToggleBtn && filterSection) {
        filterToggleBtn.addEventListener('click', function () {
            var isOpen = filterSection.classList.toggle('filter-section--open');
            filterToggleBtn.classList.toggle('filter-icon-btn--active', isOpen);
        });
    }

    // Function to get filtered cards
    function getFilteredCards() {
        var searchVal = searchInput ? searchInput.value.toLowerCase() : '';
        var categoryVal = categoryFilter ? categoryFilter.value : '';
        var popularityVal = popularityFilter ? popularityFilter.value : '';
        var priceVal = priceFilter ? priceFilter.value : '';
        var cards = document.querySelectorAll('.product-card');
        var filteredCards = [];

        cards.forEach(function (card) {
            var nameEl = card.querySelector('.product-name');
            var storeEl = card.querySelector('.product-store');
            var name = nameEl ? nameEl.textContent.toLowerCase() : '';
            var store = storeEl ? storeEl.textContent.toLowerCase() : '';

            var matchSearch = name.indexOf(searchVal) !== -1 || store.indexOf(searchVal) !== -1;
            var matchCategory = !categoryVal || card.dataset.category === categoryVal;
            var matchPopularity = !popularityVal || card.dataset.popularity === popularityVal;
            var matchPrice = !priceVal || card.dataset.price === priceVal;

            if (matchSearch && matchCategory && matchPopularity && matchPrice) {
                filteredCards.push(card);
            }
        });

        return filteredCards;
    }

    // Function to update pagination display
    function updatePagination() {
        var filteredCards = getFilteredCards();
        var totalPages = Math.ceil(filteredCards.length / itemsPerPage);

        if (currentPage > totalPages) {
            currentPage = totalPages || 1;
        }

        var allCards = document.querySelectorAll('.product-card');
        allCards.forEach(function (card) {
            card.style.display = 'none';
        });

        var start = (currentPage - 1) * itemsPerPage;
        var end = Math.min(start + itemsPerPage, filteredCards.length);

        for (var i = start; i < end; i++) {
            filteredCards[i].style.display = '';
        }

        var pageInfo = document.getElementById('pageInfo');
        if (pageInfo) {
            if (totalPages === 0) {
                pageInfo.textContent = 'Page 0 of 0';
            } else {
                pageInfo.textContent = 'Page ' + currentPage + ' of ' + totalPages;
            }
        }

        var prevBtn = document.getElementById('prevPageBtn');
        var nextBtn = document.getElementById('nextPageBtn');
        if (prevBtn) {
            prevBtn.disabled = currentPage <= 1 || totalPages === 0;
        }
        if (nextBtn) {
            nextBtn.disabled = currentPage >= totalPages || totalPages === 0;
        }

        if (noResults) {
            noResults.style.display = filteredCards.length === 0 ? 'block' : 'none';
        }

        var paginationContainer = document.getElementById('paginationContainer');
        if (paginationContainer) {
            paginationContainer.style.display = filteredCards.length === 0 ? 'none' : 'flex';
        }
    }

    // Go to previous page
    function goToPrevPage() {
        if (currentPage > 1) {
            currentPage--;
            updatePagination();
        }
    }

    // Go to next page
    function goToNextPage() {
        var filteredCards = getFilteredCards();
        var totalPages = Math.ceil(filteredCards.length / itemsPerPage);
        if (currentPage < totalPages) {
            currentPage++;
            updatePagination();
        }
    }

    // Filter products - resets to page 1
    function filterProducts() {
        currentPage = 1;
        updatePagination();
    }

    // Attach filter event listeners
    if (searchInput) searchInput.addEventListener('input', filterProducts);
    if (categoryFilter) categoryFilter.addEventListener('change', filterProducts);
    if (popularityFilter) popularityFilter.addEventListener('change', filterProducts);
    if (priceFilter) priceFilter.addEventListener('change', filterProducts);

    // Pagination button event listeners
    var prevBtn = document.getElementById('prevPageBtn');
    var nextBtn = document.getElementById('nextPageBtn');
    if (prevBtn) {
        prevBtn.addEventListener('click', goToPrevPage);
    }
    if (nextBtn) {
        nextBtn.addEventListener('click', goToNextPage);
    }

    // Handle window resize
    window.addEventListener('resize', function () {
        currentPage = 1;
        updatePagination();
    });

    // Popup message helper
    function showMessage(message, isError) {
        var existingOverlay = document.getElementById('customMessageOverlay');
        if (existingOverlay) { existingOverlay.remove(); }

        var overlay = document.createElement('div');
        overlay.id = 'customMessageOverlay';
        overlay.style.cssText = 'position:fixed;top:0;left:0;width:100%;height:100%;background:rgba(0,0,0,0.5);z-index:10000;display:flex;align-items:center;justify-content:center;';

        var msgBox = document.createElement('div');
        msgBox.style.cssText = 'background:' + (isError ? '#fff5f5' : '#f0fff4') + ';color:' + (isError ? '#c62828' : '#2e7d32') + ';padding:24px 32px;border-radius:16px;font-weight:600;font-size:1.1rem;text-align:center;min-width:280px;max-width:380px;box-shadow:0 20px 40px rgba(0,0,0,0.2);border:2px solid ' + (isError ? '#c62828' : '#2e7d32') + ';font-family:DM Sans,sans-serif;';

        var icon = document.createElement('div');
        icon.style.cssText = 'font-size:2.5rem;margin-bottom:12px;';
        icon.innerHTML = isError ? '??' : '?';
        msgBox.appendChild(icon);

        var text = document.createElement('div');
        text.textContent = message;
        msgBox.appendChild(text);

        var okBtn = document.createElement('button');
        okBtn.textContent = 'OK';
        okBtn.style.cssText = 'margin-top:20px;padding:8px 28px;background:' + (isError ? '#c62828' : '#2e7d32') + ';color:white;border:none;border-radius:8px;font-weight:600;cursor:pointer;font-family:DM Sans,sans-serif;font-size:0.9rem;';
        okBtn.onclick = function () {
            overlay.remove();
            if (isError && message.toLowerCase().indexOf('login') !== -1) {
                window.location.href = '/Home/Login';
            }
        };

        msgBox.appendChild(okBtn);
        overlay.appendChild(msgBox);
        document.body.appendChild(overlay);
    }

    // Login check
    function checkLogin(callback) {
        fetch('/Account/CheckLogin', { method: 'GET' })
            .then(function (r) { return r.json(); })
            .then(function (data) { if (callback) { callback(data.isLoggedIn); } })
            .catch(function () { if (callback) { callback(false); } });
    }

    // Add to cart
    function addToCart(card) {
        var productId = parseInt(card.getAttribute('data-product-id'));
        var productName = card.getAttribute('data-product-name');
        var unitPrice = parseFloat(card.getAttribute('data-price-value'));
        var enterpriseId = parseInt(card.getAttribute('data-enterprise-id'));
        var enterpriseName = card.getAttribute('data-enterprise-name');
        var imageBase64 = card.getAttribute('data-image-base64') || '';

        if (!productId || isNaN(unitPrice)) {
            showMessage('Could not read product info. Please refresh.', true);
            return;
        }

        var stored = sessionStorage.getItem('bizzyCart');
        var cart = [];
        if (stored) {
            try { cart = JSON.parse(stored); } catch (e) { cart = []; }
        }

        var existing = null;
        for (var i = 0; i < cart.length; i++) {
            if (cart[i].ProductId === productId) { existing = cart[i]; break; }
        }

        if (existing) {
            existing.Quantity += 1;
        } else {
            cart.push({
                Id: Date.now(),
                ProductId: productId,
                ProductName: productName,
                UnitPrice: unitPrice,
                Quantity: 1,
                EnterpriseId: enterpriseId,
                EnterpriseName: enterpriseName,
                ImageBase64: imageBase64
            });
        }

        sessionStorage.setItem('bizzyCart', JSON.stringify(cart));

        if (typeof notifyCartUpdated === 'function') {
            notifyCartUpdated();
        } else {
            window.dispatchEvent(new Event('bizzyCartUpdated'));
        }

        showMessage(productName + ' added to cart!', false);
    }

    // Attach events to cards
    function attachCardEvents() {
        var cards = document.querySelectorAll('.product-card');
        cards.forEach(function (card) {
            if (card._eventsAttached) { return; }
            card._eventsAttached = true;

            card.addEventListener('click', function (e) {
                if (e.target.classList.contains('add-btn')) { return; }
                e.preventDefault();

                var url = card.getAttribute('data-product-url');
                if (!url) { return; }

                checkLogin(function (isLoggedIn) {
                    if (isLoggedIn) {
                        window.location.href = url;
                    } else {
                        showMessage('Please login first to view product details.', true);
                    }
                });
            });

            var addBtn = card.querySelector('.add-btn');
            if (addBtn) {
                addBtn.addEventListener('click', function (e) {
                    e.preventDefault();
                    e.stopPropagation();

                    checkLogin(function (isLoggedIn) {
                        if (isLoggedIn) {
                            addToCart(card);
                        } else {
                            showMessage('Please login first to add items to cart.', true);
                        }
                    });
                });
            }
        });
    }

    attachCardEvents();

    var observer = new MutationObserver(attachCardEvents);
    var productGrid = document.getElementById('productGrid');
    if (productGrid) {
        observer.observe(productGrid, { childList: true, subtree: true });
    }

    // Initial pagination update
    updatePagination();
});
