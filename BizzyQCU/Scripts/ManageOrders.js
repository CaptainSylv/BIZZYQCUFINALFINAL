// ManageOrders.js - FULL API VERSION (WORKING)

document.addEventListener('DOMContentLoaded', function () {
    loadPendingOrders();
});

let selectedOrderId = null;

function getStatusMeta(status) {
    const normalized = (status || 'pending').toLowerCase();
    const statuses = {
        pending: { text: 'Pending', className: 'status-pending' },
        preparing: { text: 'Preparing', className: 'status-preparing' },
        out_for_delivery: { text: 'Out for Delivery', className: 'status-delivery' },
        completed: { text: 'Completed', className: 'status-completed' },
        cancelled: { text: 'Cancelled', className: 'status-cancelled' }
    };

    return statuses[normalized] || statuses.pending;
}

function getOrderActions(orderId, status) {
    const normalized = (status || 'pending').toLowerCase();

    if (normalized === 'pending') {
        return `
            <button class="btn-confirm" onclick="updateStatus(event, ${orderId}, 'preparing')">Confirm Order</button>
            <button class="btn-cancel-inline" onclick="updateStatus(event, ${orderId}, 'cancelled')">Cancel</button>
        `;
    }

    if (normalized === 'completed' || normalized === 'cancelled') {
        return '';
    }

    return `<button class="btn-status" onclick="toggleDropdown(event, ${orderId})">Change Status ▾</button>`;
}

function getStatusOptions(orderId, status) {
    const normalized = (status || 'pending').toLowerCase();

    if (normalized === 'pending' || normalized === 'completed' || normalized === 'cancelled') {
        return '';
    }

    const options = [];
    if (normalized === 'preparing') {
        options.push(`<button onclick="updateStatus(event, ${orderId}, 'out_for_delivery')">Out for Delivery</button>`);
    }

    options.push(`<button onclick="updateStatus(event, ${orderId}, 'completed')">Complete Order</button>`);
    options.push(`<button onclick="updateStatus(event, ${orderId}, 'cancelled')">Cancel Order</button>`);

    return `
        <div class="status-dropdown" id="dropdown-${orderId}" style="display:none">
            ${options.join('')}
        </div>
    `;
}

async function loadPendingOrders() {
    const container = document.getElementById('ordersContainer');
    if (!container) return;

    container.innerHTML = '<div class="loading-orders">Loading orders...</div>';

    try {
        const response = await fetch('/ManageOrders/GetPendingOrders');
        const data = await response.json();

        if (data.success && data.orders && data.orders.length > 0) {
            renderOrderCards(data.orders);

            // If the currently opened order is no longer active, clear sidebar/modal.
            if (selectedOrderId !== null) {
                const stillActive = data.orders.some(o => Number(o.OrderId || o.orderId) === Number(selectedOrderId));
                if (!stillActive) {
                    closeDetails();
                }
            }
        } else {
            // No active orders left, so details panel must be cleared.
            closeDetails();
            container.innerHTML = `
                <div class="empty-orders">
                    <p>No active orders!</p>
                    <p class="empty-subtitle">All orders have been processed.</p>
                </div>
            `;
        }
    } catch (error) {
        console.error('Error loading orders:', error);
        container.innerHTML = `
            <div class="error-orders">
                <p>❌ Failed to load orders</p>
                <p class="empty-subtitle">Please try again later.</p>
            </div>
        `;
    }
}

function renderOrderCards(orders) {
    const container = document.getElementById('ordersContainer');
    if (!container) return;

    let cardsHtml = '';

    orders.forEach(order => {
        const orderId = order.OrderId || order.orderId;
        const customerName = order.CustomerName || order.customerName || 'Unknown Customer';
        const totalAmount = order.TotalAmount || order.totalAmount || 0;
        const orderTime = order.OrderTime || order.orderTime || '';
        const orderDateFormatted = order.OrderDateFormatted || order.orderDateFormatted || '';
        const deliveryOption = order.DeliveryOption || order.deliveryOption || 'pickup';
        const orderStatus = order.Status || order.status || 'pending';
        const statusMeta = getStatusMeta(orderStatus);
        const itemsSummary = order.Items || order.items || '';

        cardsHtml += `
            <div class="glass-card" onclick="viewOrder(${orderId})">
                <div class="card-header">
                    <div>
                        <h4 class="cust-name">${escapeHtml(customerName)}</h4>
                        <span class="card-timestamp">${orderDateFormatted} • ${orderTime}</span>
                    </div>
                    <span class="badge-pill ${statusMeta.className}" id="pill-${orderId}">${statusMeta.text}</span>
                </div>
                <div class="order-summary">
                    <p class="item-count">${deliveryOption === 'pickup' ? '📍 Pickup' : '🚚 Delivery'}</p>
                    <p class="item-list">${escapeHtml(itemsSummary || 'Order details available on select')}</p>
                </div>
                <div class="card-footer">
                    <span class="price-text">₱ ${Number(totalAmount).toFixed(2)}</span>
                    <div class="card-actions" id="action-area-${orderId}">
                        ${getOrderActions(orderId, orderStatus)}
                    </div>
                </div>
                ${getStatusOptions(orderId, orderStatus)}
            </div>
        `;
    });

    container.innerHTML = cardsHtml;
}

async function updateStatus(event, orderId, newStatus) {
    if (event) event.stopPropagation();

    if (!orderId) {
        showNotification('Invalid order ID', 'error');
        return;
    }

    try {
        const response = await fetch('/ManageOrders/UpdateOrderStatus', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded'
            },
            body: `orderId=${encodeURIComponent(orderId)}&status=${encodeURIComponent(newStatus)}`
        });

        const data = await response.json();

        if (data.success) {
            showNotification(data.message || 'Status updated!', 'success');

            // If currently opened order is moved to terminal state, remove sidebar content immediately.
            const normalizedStatus = (newStatus || '').toLowerCase();
            if (Number(selectedOrderId) === Number(orderId) &&
                (normalizedStatus === 'completed' || normalizedStatus === 'cancelled')) {
                closeDetails();
            }
            else if (Number(selectedOrderId) === Number(orderId)) {
                // Keep sidebar in sync when an active order status changes.
                viewOrder(orderId);
            }

            setTimeout(() => {
                loadPendingOrders();
            }, 1000);
        } else {
            showNotification(data.message || 'Failed to update status', 'error');
        }
    } catch (error) {
        console.error('Error updating status:', error);
        showNotification('Failed to update order status', 'error');
    }

    const dropdown = document.getElementById(`dropdown-${orderId}`);
    if (dropdown) dropdown.style.display = 'none';
}

function toggleDropdown(event, orderId) {
    event.stopPropagation();

    document.querySelectorAll('.status-dropdown').forEach(d => {
        d.style.display = 'none';
    });

    const dropdown = document.getElementById(`dropdown-${orderId}`);
    if (dropdown) {
        dropdown.style.display = dropdown.style.display === 'flex' ? 'none' : 'flex';
    }
}

async function viewOrder(orderId) {
    const id = Number(orderId);

    if (!id || isNaN(id)) {
        showNotification('Invalid order ID', 'error');
        return;
    }

    try {
        const response = await fetch(`/ManageOrders/GetOrderDetails?orderId=${id}`);
        const data = await response.json();

        if (data.success && data.order) {
            selectedOrderId = id;
            const order = data.order;
            const statusMeta = getStatusMeta(order.Status);

            let itemsHtml = '';
            if (order.Items && order.Items.length > 0) {
                itemsHtml = '<div class="items-list">';
                order.Items.forEach(item => {
                    itemsHtml += `
                        <div class="item-row">
                            <span class="item-qty">${item.Quantity}x</span>
                            <span class="item-name">${escapeHtml(item.ProductName)}</span>
                            <span class="item-price">₱ ${Number(item.UnitPrice).toFixed(2)}</span>
                        </div>
                    `;
                });
                itemsHtml += '</div>';
            } else {
                itemsHtml = '<div class="items-list">No items found</div>';
            }

            const content = `
                <div class="details-container">
                    <button class="close-details" onclick="closeDetails()">✕</button>
                    <h1 class="order-id-header">Order #QCU-${order.OrderId}</h1>
                    <p class="order-subtitle">${escapeHtml(order.CustomerName)} • ${order.OrderTime || ''}</p>

                    <div class="detail-group">
                        <div class="detail-row">
                            <span class="detail-label">📍 Location</span>
                            <span class="detail-value">${order.DeliveryOption === 'pickup' ? 'Store Pickup' : escapeHtml(order.CustomerLocation || 'Campus Delivery')}</span>
                        </div>
                        <div class="detail-row">
                            <span class="detail-label">💳 Payment</span>
                            <span class="detail-value">${order.PaymentMethod ? order.PaymentMethod.toUpperCase() : 'Cash'}</span>
                        </div>
                        <div class="detail-row">
                            <span class="detail-label">📦 Status</span>
                            <span class="detail-value ${statusMeta.className}">${statusMeta.text}</span>
                        </div>
                        <div class="detail-row">
                            <span class="detail-label">⏱ ETA</span>
                            <span class="detail-value">${order.EstimatedTime || 'TBD'}</span>
                        </div>
                        <div class="detail-row">
                            <span class="detail-label">🛒 Items</span>
                        </div>
                        ${itemsHtml}
                       <!-- No delivery fee - school campus only -->
                    </div>

                    ${order.OrderNote ? `
                        <div class="note-box">
                            <p class="note-text">"${escapeHtml(order.OrderNote)}"</p>
                        </div>
                    ` : ''}

                    <div class="total-section">
                        <span class="total-label">Total Amount</span>
                        <span class="total-amount">₱ ${Number(order.TotalAmount).toFixed(2)}</span>
                    </div>

                    <p class="courier-label">👤 CUSTOMER DETAILS</p>
                    <div class="courier-card">
                        <div class="courier-info">
                            <div class="courier-avatar">${escapeHtml(order.CustomerName).charAt(0)}</div>
                            <div>
                                <p class="courier-name">${escapeHtml(order.CustomerName)}</p>
                                <p class="courier-type">${order.CustomerPhone || 'No phone number'}</p>
                            </div>
                        </div>
                        <button class="btn-message-emoji" onclick="window.location.href='/EnterpriseDashboard/EnterpriseChatroom?orderId=${order.OrderId}'">💬 Message</button>
                    </div>
                </div>
            `;

            if (window.innerWidth > 1100) {
                const detailsPanel = document.getElementById('detailsPanel');
                if (detailsPanel) {
                    detailsPanel.innerHTML = content;
                }
            } else {
                const modalContent = document.getElementById('modal-content');
                if (modalContent) {
                    modalContent.innerHTML = content;
                }
                const mobileModal = document.getElementById('mobile-modal');
                if (mobileModal) {
                    mobileModal.style.display = 'flex';
                }
            }
        } else {
            showNotification('Failed to load order details', 'error');
        }
    } catch (error) {
        console.error('Error loading order details:', error);
        showNotification('Failed to load order details', 'error');
    }
}

function closeDetails() {
    selectedOrderId = null;

    const detailsPanel = document.getElementById('detailsPanel');
    if (detailsPanel && window.innerWidth > 1100) {
        detailsPanel.innerHTML = `
            <div class="sidebar-placeholder">
                <p class="sidebar-placeholder-text">Select an order card<br>to view details</p>
            </div>
        `;
    }
    const mobileModal = document.getElementById('mobile-modal');
    if (mobileModal) {
        mobileModal.style.display = 'none';
    }
}

function showNotification(message, type) {
    const notification = document.createElement('div');
    notification.className = `notification ${type}`;
    notification.innerText = message;
    notification.style.cssText = `
        position: fixed;
        bottom: 20px;
        right: 20px;
        padding: 12px 20px;
        border-radius: 8px;
        color: white;
        z-index: 9999;
        background-color: ${type === 'success' ? '#4CAF50' : '#f44336'};
        animation: slideIn 0.3s ease;
    `;
    document.body.appendChild(notification);
    setTimeout(() => notification.remove(), 3000);
}

function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

window.onclick = function (event) {
    if (!event.target.closest('.btn-status')) {
        document.querySelectorAll('.status-dropdown').forEach(d => d.style.display = 'none');
    }
}
