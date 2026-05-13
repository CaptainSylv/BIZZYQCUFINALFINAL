// EditStore.js

// Delete product function
function deleteProduct(productId) {
    if (confirm('Are you sure you want to delete this product?')) {
        fetch('/EditStore/DeleteProduct', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8'
            },
            body: 'productId=' + encodeURIComponent(productId)
        })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    alert(data.message);
                    location.reload();
                } else {
                    alert(data.message);
                }
            })
            .catch(error => {
                console.error('Error:', error);
                alert('Failed to delete product');
            });
    }
}

// Edit product function
function editProduct(productId) {
    window.location.href = '/EditProduct/EditProduct/' + productId;
}

