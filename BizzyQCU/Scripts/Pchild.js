document.addEventListener('DOMContentLoaded', function () {

    var btnMinus = document.getElementById('btnMinus');
    var btnPlus = document.getElementById('btnPlus');
    var qtyInput = document.getElementById('qtyInput');
    var btnOrder = document.getElementById('btnOrder');

    // ─── Quantity controls ────────────────────────────────────────────────────
    btnMinus.addEventListener('click', function () {
        var val = parseInt(qtyInput.value) || 1;
        if (val > 1) { qtyInput.value = val - 1; }
    });

    btnPlus.addEventListener('click', function () {
        var val = parseInt(qtyInput.value) || 1;
        qtyInput.value = val + 1;
    });

    qtyInput.addEventListener('change', function () {
        var val = parseInt(qtyInput.value);
        if (isNaN(val) || val < 1) qtyInput.value = 1;
    });

    // ─── Order Now ────────────────────────────────────────────────────────────
    btnOrder.addEventListener('click', function () {

        // Read values from hidden inputs set by ProductChild.cshtml
        var productId = parseInt(document.getElementById('productId').value);
        var productName = document.getElementById('productName').value;
        var unitPrice = parseFloat(document.getElementById('productPrice').value);
        var enterpriseId = parseInt(document.getElementById('enterpriseId').value);

        // Enterprise name comes from the visible span on the page
        var enterpriseNameEl = document.querySelector('.enterprise-name');
        var enterpriseName = enterpriseNameEl ? enterpriseNameEl.innerText.trim() : 'Unknown';

        // Get quantity
        var qty = parseInt(qtyInput.value) || 1;

        // Get product image as base64 (already rendered as data URI in the img tag)
        var imageBase64 = '';
        var imgEl = document.querySelector('.product-img-wrap img');
        if (imgEl && imgEl.src && imgEl.src.indexOf('base64,') !== -1) {
            imageBase64 = imgEl.src.split('base64,')[1];
        }

        // Add to cart (with quantity loop if qty > 1, or store qty directly)
        // We store qty directly by modifying the bridge call
        var stored = sessionStorage.getItem('bizzyCart');
        var cart = [];
        if (stored) { try { cart = JSON.parse(stored); } catch (e) { cart = []; } }

        // Find existing item
        var existing = null;
        for (var i = 0; i < cart.length; i++) {
            if (cart[i].ProductId === productId) { existing = cart[i]; break; }
        }

        if (existing) {
            existing.Quantity += qty;
        } else {
            cart.push({
                Id: Date.now(),
                ProductId: productId,
                ProductName: productName,
                UnitPrice: unitPrice,
                Quantity: qty,
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

        // Show brief toast then redirect to checkout
        var toast = document.getElementById('checkoutToast');
        if (toast) {
            toast.innerText = '✅ Added to cart! Redirecting...';
            toast.style.display = 'block';
        }

        setTimeout(function () {
            window.location.href = '/Checkout/Checkout';
        }, 800);
    });
});
