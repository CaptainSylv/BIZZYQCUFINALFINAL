// EditProduct.js

// Load existing image preview if any
const productId = document.getElementById('productId').value;
const existingImage = document.getElementById('imagePreview');
if (existingImage && productId) {
    existingImage.src = '/EditProduct/GetProductImage?productId=' + productId;
    existingImage.style.display = 'block';
    document.querySelector('.add-product-image-icon').style.display = 'none';
}

// Image preview on new upload
document.getElementById('productImage').addEventListener('change', function (e) {
    const file = e.target.files[0];
    if (file) {
        const reader = new FileReader();
        reader.onload = function (event) {
            const preview = document.getElementById('imagePreview');
            preview.src = event.target.result;
            preview.style.display = 'block';
            document.querySelector('.add-product-image-icon').style.display = 'none';
        };
        reader.readAsDataURL(file);
    }
});

// Update product
document.getElementById('updateProductBtn').addEventListener('click', async function () {
    const productId = document.getElementById('productId').value;
    const name = document.getElementById('productName').value.trim();
    const price = document.getElementById('productPrice').value;
    const category = document.getElementById('productCategory').value;
    const preparationTime = document.getElementById('preparationTime').value.trim();
    const description = document.getElementById('productDescription').value.trim();
    const imageFile = document.getElementById('productImage').files[0];

    if (!name) {
        alert('Please enter product name');
        return;
    }

    if (!price || parseFloat(price) <= 0) {
        alert('Please enter a valid price');
        return;
    }

    if (!category) {
        alert('Please select a category');
        return;
    }

    const formData = new FormData();
    formData.append('productId', productId);
    formData.append('name', name);
    formData.append('price', parseFloat(price));
    formData.append('category', category);
    formData.append('preparationTime', preparationTime);
    formData.append('description', description);
    if (imageFile) {
        formData.append('productImage', imageFile);
    }

    const updateBtn = document.getElementById('updateProductBtn');
    updateBtn.disabled = true;
    updateBtn.textContent = 'Updating...';

    try {
        const response = await fetch('/EditProduct/UpdateProduct', {
            method: 'POST',
            body: formData
        });
        const data = await response.json();

        if (data.success) {
            alert('Product updated successfully!');
            window.location.href = '/EditStore/EditStore';
        } else {
            alert(data.message);
            updateBtn.disabled = false;
            updateBtn.textContent = 'Update Product';
        }
    } catch (error) {
        console.error('Error:', error);
        alert('Failed to update product');
        updateBtn.disabled = false;
        updateBtn.textContent = 'Update Product';
    }
});