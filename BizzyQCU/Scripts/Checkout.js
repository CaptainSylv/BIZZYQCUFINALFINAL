// ─── Cart State ────────────────────────────────────────────────────────────────
let cartItems = [];

// ─── Load cart from sessionStorage (set by ProductChild page) ─────────────────
function loadCartFromSession() {
    var stored = sessionStorage.getItem('bizzyCart');
    if (stored) {
        try { cartItems = JSON.parse(stored); } catch (e) { cartItems = []; }
    }
    renderCart();
}

function saveCartToSession() {
    sessionStorage.setItem('bizzyCart', JSON.stringify(cartItems));
    if (typeof notifyCartUpdated === 'function') {
        notifyCartUpdated();
    } else {
        window.dispatchEvent(new Event('bizzyCartUpdated'));
    }
}

// ─── Called by ProductChild page when "Order Now" is clicked ──────────────────
// Usage in Pchild.js:
//   CartBridge.addItem(productId, productName, unitPrice, enterpriseId, enterpriseName, imageBase64);
window.CartBridge = {
    addItem: function (productId, productName, unitPrice, enterpriseId, enterpriseName, imageBase64) {
        var stored = sessionStorage.getItem('bizzyCart');
        var cart = [];
        if (stored) { try { cart = JSON.parse(stored); } catch (e) { cart = []; } }

        // If product already in cart, increase quantity
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
                UnitPrice: parseFloat(unitPrice),
                Quantity: 1,
                EnterpriseId: enterpriseId,
                EnterpriseName: enterpriseName,
                ImageBase64: imageBase64 || ''
            });
        }

        sessionStorage.setItem('bizzyCart', JSON.stringify(cart));
        if (typeof notifyCartUpdated === 'function') {
            notifyCartUpdated();
        } else {
            window.dispatchEvent(new Event('bizzyCartUpdated'));
        }
        return true;
    }
};

// ─── Cart Operations ───────────────────────────────────────────────────────────
function updateQuantity(itemId, newQty) {
    if (newQty < 1) newQty = 1;
    for (var i = 0; i < cartItems.length; i++) {
        if (cartItems[i].Id === itemId) { cartItems[i].Quantity = newQty; break; }
    }
    saveCartToSession();
    renderCart();
}

function removeItem(itemId) {
    cartItems = cartItems.filter(function (i) { return i.Id !== itemId; });
    saveCartToSession();
    renderCart();
}

function calculateTotal() {
    var sum = 0;
    cartItems.forEach(function (item) { sum += item.UnitPrice * item.Quantity; });
    return sum + (getDeliveryFee() * getEnterpriseGroups().length);
}

function calculateItemsTotal(items) {
    var sum = 0;
    items.forEach(function (item) { sum += item.UnitPrice * item.Quantity; });
    return sum;
}

function getEnterpriseGroups() {
    var groups = [];
    cartItems.forEach(function (item) {
        var enterpriseId = parseInt(item.EnterpriseId);
        var group = null;
        for (var i = 0; i < groups.length; i++) {
            if (groups[i].enterpriseId === enterpriseId) {
                group = groups[i];
                break;
            }
        }

        if (!group) {
            group = {
                enterpriseId: enterpriseId,
                enterpriseName: item.EnterpriseName || 'Unknown Enterprise',
                items: []
            };
            groups.push(group);
        }

        group.items.push(item);
    });
    return groups;
}

function getDeliveryFee() {
    var delivery = document.getElementById('deliveryOptionDisplay').innerText.trim();
    // Flat fee for room-to-room / campus delivery — adjust as needed
    if (delivery === 'Room to Room') return 20;
    if (delivery === 'Campus Delivery') return 50;
    return 0;
}

function escapeHtml(str) {
    if (!str) return '';
    return str.replace(/[&<>"']/g, function (m) {
        return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[m];
    });
}

// ─── Render ────────────────────────────────────────────────────────────────────
function renderCart() {
    var tbody = document.getElementById('cartBody');
    if (!tbody) return;
    tbody.innerHTML = '';
    var container = document.getElementById('enterpriseHeaderContainer');

    if (!cartItems.length) {
        tbody.innerHTML = '<tr><td colspan="6" class="text-center text-muted">No products ordered. Add products from the catalog.</td></tr>';
        document.getElementById('totalPayment').innerText = '₱0.00';
        if (container) container.style.display = 'none';
        return;
    }

    var groups = getEnterpriseGroups();
    if (container) {
        container.innerHTML = '<div class="enterprise-header"><i class="fas fa-store"></i> ' + groups.length + ' enterprise' + (groups.length === 1 ? '' : 's') + ' in this checkout</div>';
        container.style.display = 'block';
    }

    groups.forEach(function (group) {
        var headerRow = document.createElement('tr');
        headerRow.classList.add('enterprise-group-row');
        headerRow.innerHTML = '<td colspan="6"><i class="fas fa-store"></i> ' + escapeHtml(group.enterpriseName) + '</td>';
        tbody.appendChild(headerRow);

        group.items.forEach(function (item) {
            var subtotal = item.UnitPrice * item.Quantity;
            var imgHtml;
            if (item.ImageBase64) {
                imgHtml = '<img src="data:image/jpeg;base64,' + item.ImageBase64 + '" class="product-image">';
            } else {
                imgHtml = '<img src="/Images/default.jpg" class="product-image">';
            }

            var row = document.createElement('tr');
            row.classList.add('product-row');
            row.innerHTML =
                '<td data-label="Image" style="text-align:center">' + imgHtml + '</td>' +
                '<td data-label="Product" class="product-name">' + escapeHtml(item.ProductName) + '</td>' +
                '<td data-label="Unit Price">&#8369;' + item.UnitPrice.toFixed(2) + '</td>' +
                '<td data-label="Quantity"><input type="number" class="quantity-input" data-id="' + item.Id + '" value="' + item.Quantity + '" min="1" step="1"></td>' +
                '<td data-label="Subtotal">&#8369;' + subtotal.toFixed(2) + '</td>' +
                '<td data-label="Action" class="text-center"><i class="fas fa-trash-alt action-icon" data-id="' + item.Id + '"></i></td>';
            tbody.appendChild(row);
        });
    });

    var grandTotal = calculateTotal();
    document.getElementById('totalPayment').innerHTML = '&#8369;' + grandTotal.toFixed(2);

    document.querySelectorAll('.quantity-input').forEach(function (input) {
        input.addEventListener('change', handleQuantityChange);
    });
    document.querySelectorAll('.action-icon').forEach(function (icon) {
        icon.addEventListener('click', handleRemove);
    });
}

function handleQuantityChange(e) {
    var id = parseInt(e.target.getAttribute('data-id'));
    var val = parseInt(e.target.value);
    if (!isNaN(val)) updateQuantity(id, val);
}
function handleRemove(e) {
    var id = parseInt(e.target.getAttribute('data-id'));
    if (confirm('Remove this item?')) removeItem(id);
}

// ─── Delivery / Payment UI ─────────────────────────────────────────────────────
function initDeliveryPayment() {
    updateRoomVisibility();
    updateQrButtonVisibility();
    document.querySelectorAll('.change-btn').forEach(function (btn) {
        btn.addEventListener('click', changeHandler);
    });

    var viewQrBtn = document.getElementById('viewQrBtn');
    if (viewQrBtn) {
        viewQrBtn.addEventListener('click', openQrModal);
    }

    var closeQrModalBtn = document.getElementById('closeQrModalBtn');
    if (closeQrModalBtn) {
        closeQrModalBtn.addEventListener('click', closeQrModal);
    }

    var qrModal = document.getElementById('qrModal');
    if (qrModal) {
        qrModal.addEventListener('click', function (event) {
            if (event.target === qrModal) {
                closeQrModal();
            }
        });
    }
}

function changeHandler(e) {
    var type = e.target.getAttribute('data-type');
    if (type === 'delivery') {
        var span = document.getElementById('deliveryOptionDisplay');
        var options = ['Pickup', 'Room to Room', 'Campus Delivery'];
        var current = options.indexOf(span.innerText.trim());
        span.innerText = options[(current + 1) % options.length];
        updateRoomVisibility();
        renderCart(); // re-render to update total with new delivery fee
    } else if (type === 'payment') {
        var pspan = document.getElementById('paymentMethodDisplay');
        pspan.innerText = pspan.innerText.trim() === 'Cash on Delivery' ? 'GCash' : 'Cash on Delivery';
        updateQrButtonVisibility();
    }
}

function updateQrButtonVisibility() {
    var paymentDisplay = document.getElementById('paymentMethodDisplay');
    var viewQrBtn = document.getElementById('viewQrBtn');
    if (!paymentDisplay || !viewQrBtn) return;

    var isGcash = paymentDisplay.innerText.trim() === 'GCash';
    viewQrBtn.style.display = isGcash ? 'inline-block' : 'none';
}

function closeQrModal() {
    var modal = document.getElementById('qrModal');
    if (modal) {
        modal.style.display = 'none';
    }
}

function openQrModal() {
    var groups = getEnterpriseGroups();
    if (!groups.length) {
        alert('No enterprise found in cart.');
        return;
    }

    var modal = document.getElementById('qrModal');
    var content = document.getElementById('qrModalContent');
    if (!modal || !content) return;

    content.innerHTML = '<p>Loading QR code...</p>';
    modal.style.display = 'flex';

    var requests = groups.map(function (group) {
        return fetch('/Checkout/GetEnterpriseQr?enterpriseId=' + encodeURIComponent(group.enterpriseId))
            .then(function (res) { return res.json(); })
            .then(function (data) {
                return {
                    enterpriseName: group.enterpriseName || 'Enterprise',
                    success: data && data.success,
                    qrUrl: data ? data.qrUrl : null,
                    message: data && data.message ? data.message : 'QR code not available yet.'
                };
            })
            .catch(function () {
                return {
                    enterpriseName: group.enterpriseName || 'Enterprise',
                    success: false,
                    qrUrl: null,
                    message: 'Failed to fetch QR code.'
                };
            });
    });

    Promise.all(requests).then(function (results) {
        var html = '<div class="qr-enterprise-grid">';

        results.forEach(function (result) {
            html += '<div class="qr-enterprise-card">';
            html += '<strong>' + escapeHtml(result.enterpriseName) + '</strong><br>';

            if (result.success && result.qrUrl) {
                html += '<img src="' + result.qrUrl + '" alt="GCash QR for ' + escapeHtml(result.enterpriseName) + '">';
            } else {
                html += '<p style="margin-top:10px;color:#b91c1c;">' + escapeHtml(result.message) + '</p>';
            }

            html += '</div>';
        });

        html += '</div>';
        content.innerHTML = html;
    });
}

function updateRoomVisibility() {
    var delivery = document.getElementById('deliveryOptionDisplay').innerText.trim();
    var container = document.getElementById('roomSpecifyContainer');
    var isRoom = (delivery === 'Room to Room' || delivery === 'Campus Delivery');
    container.style.display = isRoom ? 'block' : 'none';
    if (!isRoom) document.getElementById('roomSpecifyInput').value = '';
}

// ─── Place Order ───────────────────────────────────────────────────────────────
function placeOrder() {
    if (!cartItems.length) { alert('Your cart is empty.'); return; }

    var delivery = document.getElementById('deliveryOptionDisplay').innerText.trim();
    var payment = document.getElementById('paymentMethodDisplay').innerText.trim();
    var note = document.getElementById('orderNote').value.trim();
    var roomInput = document.getElementById('roomSpecifyInput').value.trim();
    var location = (delivery === 'Pickup') ? 'Store Pickup' : (roomInput || 'Not specified');
    var deliveryFee = getDeliveryFee();
    var groups = getEnterpriseGroups();

    // Map display text → DB enum value
    var deliveryOption;
    if (delivery === 'Room to Room') { deliveryOption = 'room_to_room'; }
    else if (delivery === 'Campus Delivery') { deliveryOption = 'campus_delivery'; }
    else { deliveryOption = 'pickup'; }

    var paymentMethod = (payment === 'GCash') ? 'gcash' : 'cash';

    // Get anti-forgery token
    var token = '';
    var tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    if (tokenInput) token = tokenInput.value;

    var btn = document.getElementById('placeOrderBtn');
    btn.disabled = true;
    btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Placing...';

    var orderIds = [];
    var chain = Promise.resolve();

    groups.forEach(function (group) {
        chain = chain.then(function () {
            var itemsPayload = group.items.map(function (item) {
                return { ProductId: item.ProductId, Quantity: item.Quantity, UnitPrice: item.UnitPrice };
            });
            var orderTotal = calculateItemsTotal(group.items) + deliveryFee;

            return fetch('/Checkout/PlaceOrder', {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: [
                    'enterpriseId=' + encodeURIComponent(group.enterpriseId),
                    'totalAmount=' + encodeURIComponent(orderTotal.toFixed(2)),
                    'paymentMethod=' + encodeURIComponent(paymentMethod),
                    'deliveryOption=' + encodeURIComponent(deliveryOption),
                    'customerLocation=' + encodeURIComponent(location),
                    'orderNote=' + encodeURIComponent(note),
                    'deliveryFee=' + encodeURIComponent(deliveryFee.toFixed(2)),
                    'itemsJson=' + encodeURIComponent(JSON.stringify(itemsPayload)),
                    '__RequestVerificationToken=' + encodeURIComponent(token)
                ].join('&')
            })
                .then(function (res) { return res.json(); })
                .then(function (data) {
                    if (!data.success) {
                        throw new Error(data.message || 'Unknown error.');
                    }
                    orderIds.push(data.orderId);
                });
        });
    });

    chain
        .then(function () {
            sessionStorage.removeItem('bizzyCart');
            cartItems = [];
            if (orderIds.length === 1) {
                window.location.href = '/Tracking/Tracking?orderId=' + orderIds[0];
            } else {
                window.location.href = '/Tracking/History';
            }
        })
        .catch(function (err) {
            alert('Order failed: ' + (err.message || 'Please try again.'));
            btn.disabled = false;
            btn.innerHTML = '<i class="fas fa-check-circle"></i> Place Order';
            console.error(err);
        });
}

// ─── Init ──────────────────────────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', function () {
    loadCartFromSession();
    initDeliveryPayment();

    document.getElementById('buyMoreBtn').addEventListener('click', function () {
        window.location.href = '/ProductList/ProductList';
    });
    document.getElementById('placeOrderBtn').addEventListener('click', placeOrder);
});
