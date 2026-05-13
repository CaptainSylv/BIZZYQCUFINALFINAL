document.addEventListener('DOMContentLoaded', function () {
    loadAllStudentRequests();
    loadAllEnterpriseRequests();
    loadFeedbacks();

    const tabUsers = document.getElementById('tabUsers');
    const tabEnterprises = document.getElementById('tabEnterprises');
    const tabFeedbacks = document.getElementById('tabFeedbacks');
    const panelUsers = document.getElementById('panelUsers');
    const panelEnterprises = document.getElementById('panelEnterprises');
    const panelFeedbacks = document.getElementById('panelFeedbacks');
    const feedbackFilterWrap = document.getElementById('feedbackFilterWrap');

    tabUsers.addEventListener('click', function () {
        tabUsers.classList.add('active');
        tabEnterprises.classList.remove('active');
        tabFeedbacks.classList.remove('active');
        panelUsers.classList.remove('hidden');
        panelEnterprises.classList.add('hidden');
        panelFeedbacks.classList.add('hidden');
        feedbackFilterWrap.classList.add('hidden');
    });

    tabEnterprises.addEventListener('click', function () {
        tabEnterprises.classList.add('active');
        tabUsers.classList.remove('active');
        tabFeedbacks.classList.remove('active');
        panelEnterprises.classList.remove('hidden');
        panelUsers.classList.add('hidden');
        panelFeedbacks.classList.add('hidden');
        feedbackFilterWrap.classList.add('hidden');
    });

    tabFeedbacks.addEventListener('click', function () {
        tabFeedbacks.classList.add('active');
        tabUsers.classList.remove('active');
        tabEnterprises.classList.remove('active');
        panelFeedbacks.classList.remove('hidden');
        panelUsers.classList.add('hidden');
        panelEnterprises.classList.add('hidden');
        feedbackFilterWrap.classList.remove('hidden');
    });

    document.getElementById('searchInput').addEventListener('keyup', function () {
        const searchTerm = this.value.toLowerCase();
        const activeTab = document.querySelector('.tab.active').id;

        if (activeTab === 'tabUsers') {
            filterTable('usersTableBody', searchTerm);
        } else if (activeTab === 'tabEnterprises') {
            filterTable('enterprisesTableBody', searchTerm);
        } else {
            filterTable('feedbacksTableBody', searchTerm);
        }
    });

    document.getElementById('ratingFilter').addEventListener('change', applyFeedbackFilters);
    document.getElementById('categoryFilter').addEventListener('change', applyFeedbackFilters);
});

let allFeedbacks = [];
const qcuIdStore = {};
const enterpriseDocStore = {};
let currentPreviewObjectUrl = null;
let allStudents = [];
let allEnterprises = [];
let currentEditContext = null;
let pendingEditSubmitEvent = null;
let pendingFeedbackDeleteId = null;

function normalizeDigits(value, maxLength) {
    return (value || '').replace(/\D/g, '').slice(0, maxLength);
}

function restrictDigitsInput(input, maxLength) {
    if (!input) return;
    input.addEventListener('input', function () {
        this.value = normalizeDigits(this.value, maxLength);
    });
}

function isAccountEnabled(request) {
    return request && request.IsAccountEnabled !== false;
}

function renderApprovalStatus(request) {
    const statusText = request.Status || '';
    const statusLower = statusText.toLowerCase();
    let statusClass = '';

    if (statusLower === 'pending') {
        statusClass = 'status-pending';
    } else if (statusLower === 'approved') {
        statusClass = isAccountEnabled(request) ? 'status-approved' : 'status-disabled';
    } else if (statusLower === 'rejected') {
        statusClass = 'status-rejected';
    }

    const label = statusLower === 'approved' && !isAccountEnabled(request)
        ? 'Approved (Disabled)'
        : statusText;
    return `<span class="${statusClass}">${escapeHtml(label)}</span>`;
}

function renderRequestActions(request, type) {
    const statusLower = (request.Status || '').toLowerCase();
    const editHandler = type === 'student' ? 'openEditStudentModal' : 'openEditEnterpriseModal';
    const buttons = [];

    if (statusLower === 'pending') {
        buttons.push(`<button class="approve-btn" onclick="approveRequest(${request.RequestId})">Approve</button>`);
        buttons.push(`<button class="reject-btn" onclick="rejectRequest(${request.RequestId})">Reject</button>`);
        buttons.push(`<button class="edit-btn" onclick="${editHandler}(${request.RequestId})">Edit</button>`);
    } else if (statusLower === 'approved') {
        buttons.push(`<button class="edit-btn" onclick="${editHandler}(${request.RequestId})">Edit</button>`);
    } else if (statusLower === 'rejected') {
        buttons.push(`<button class="approve-btn" onclick="approveRequest(${request.RequestId})">Approve</button>`);
        buttons.push(`<button class="edit-btn" onclick="${editHandler}(${request.RequestId})">Edit</button>`);
    }

    return buttons.length
        ? `<div class="action-btns">${buttons.join('')}</div>`
        : '<span>-</span>';
}

function renderAccountToggleAction(request) {
    if (!request || (request.Status || '').toLowerCase() !== 'approved') {
        return '';
    }

    const enabled = isAccountEnabled(request);
    const nextEnabled = enabled ? 'false' : 'true';
    const buttonClass = enabled ? 'disable-account-btn' : 'enable-account-btn';
    const buttonText = enabled ? 'Disable Account' : 'Enable Account';
    return `<button type="button" class="${buttonClass}" onclick="updateRequestAccountStatus(${request.RequestId}, ${nextEnabled})">${buttonText}</button>`;
}

function loadAllStudentRequests() {
    fetch('/AdminPanel/GetAllStudentRequests')
        .then(response => response.json())
        .then(data => {
            allStudents = Array.isArray(data) ? data : [];
            const tbody = document.getElementById('usersTableBody');
            tbody.innerHTML = '';

            if (!allStudents.length) {
                tbody.innerHTML = '<tr><td colspan="7" style="text-align:center;">No student requests found</td></tr>';
                return;
            }

            allStudents.forEach(student => {
                const row = tbody.insertRow();
                const fullName = `${student.Firstname || ''} ${student.Lastname || ''}`.trim() || student.Username;
                row.insertCell(0).innerHTML = fullName;
                row.insertCell(1).innerHTML = student.Email;
                row.insertCell(2).innerHTML = student.Section || 'N/A';
                row.insertCell(3).innerHTML = student.StudentNumber || 'N/A';
                row.insertCell(4).innerHTML = renderQcuIdCell(student.RequestId, student.QcuId);

                row.insertCell(5).innerHTML = renderApprovalStatus(student);
                row.insertCell(6).innerHTML = renderRequestActions(student, 'student');
            });
        })
        .catch(error => {
            console.error('Error loading students:', error);
            document.getElementById('usersTableBody').innerHTML = '<tr><td colspan="7" style="text-align:center;color:red;">Error loading data</td></tr>';
        });
}

function renderQcuIdCell(requestId, rawData) {
    if (!rawData) {
        return '<span>N/A</span>';
    }
    qcuIdStore[requestId] = rawData;
    return `<button type="button" class="qcu-id-btn" onclick="viewQcuId(${requestId})">View ID</button>`;
}

function viewQcuId(requestId) {
    const modal = document.getElementById('qcuIdModal');
    const img = document.getElementById('qcuIdPreviewImage');
    const pdf = document.getElementById('qcuIdPreviewPdf');
    const title = document.getElementById('previewModalTitle');
    if (!modal || !img || !pdf) {
        return;
    }

    const rawData = qcuIdStore[requestId];
    const preview = buildPreviewSource(rawData);
    if (!preview || !preview.src) {
        alert('Invalid QCU ID file data.');
        return;
    }

    setPreviewSource(img, pdf, preview);
    if (title) {
        title.textContent = 'QCU ID Preview';
    }
    modal.classList.remove('hidden');
    modal.setAttribute('aria-hidden', 'false');
}

function closeQcuIdModal() {
    const modal = document.getElementById('qcuIdModal');
    const img = document.getElementById('qcuIdPreviewImage');
    const pdf = document.getElementById('qcuIdPreviewPdf');
    if (!modal || !img || !pdf) {
        return;
    }
    modal.classList.add('hidden');
    modal.setAttribute('aria-hidden', 'true');
    img.src = '';
    img.style.display = 'none';
    pdf.src = '';
    pdf.style.display = 'none';
    if (currentPreviewObjectUrl) {
        URL.revokeObjectURL(currentPreviewObjectUrl);
        currentPreviewObjectUrl = null;
    }
}

function inferImageMimeType(base64Data) {
    try {
        const normalized = normalizeBase64(base64Data);
        const signature = atob(normalized).slice(0, 12);
        if (signature.startsWith('\x89PNG')) return 'image/png';
        if (signature.startsWith('\xFF\xD8\xFF')) return 'image/jpeg';
        if (signature.startsWith('GIF87a') || signature.startsWith('GIF89a')) return 'image/gif';
        if (signature.startsWith('BM')) return 'image/bmp';
        if (signature.startsWith('RIFF') && signature.slice(8, 12) === 'WEBP') return 'image/webp';
    } catch (e) {
        console.warn('Unable to infer mime type for QCU ID image.', e);
    }
    return 'image/jpeg';
}

function buildQcuIdImageSrc(rawValue) {
    if (!rawValue) return null;

    if (Array.isArray(rawValue) && rawValue.length > 0) {
        const uint8 = new Uint8Array(rawValue);
        const mimeType = inferImageMimeTypeFromBytes(uint8);
        const blob = new Blob([uint8], { type: mimeType });
        return URL.createObjectURL(blob);
    }

    if (typeof rawValue === 'object') {
        // Handles serializers that send byte[] as objects, e.g. { "$values": [...] }.
        const values = rawValue.$values || rawValue.values || rawValue.data;
        if (Array.isArray(values) && values.length > 0) {
            const uint8 = new Uint8Array(values);
            const mimeType = inferImageMimeTypeFromBytes(uint8);
            const blob = new Blob([uint8], { type: mimeType });
            return URL.createObjectURL(blob);
        }
    }

    const text = String(rawValue).trim();
    if (!text) return null;

    if (text.startsWith('data:image/')) {
        return text;
    }

    if (text.startsWith('[') && text.endsWith(']')) {
        try {
            const bytes = JSON.parse(text);
            if (Array.isArray(bytes) && bytes.length > 0) {
                const uint8 = new Uint8Array(bytes);
                const mimeType = inferImageMimeTypeFromBytes(uint8);
                const blob = new Blob([uint8], { type: mimeType });
                return URL.createObjectURL(blob);
            }
        } catch (e) {
            console.warn('Invalid byte-array format for QCU ID image.', e);
        }
    }

    const normalized = normalizeBase64(text);
    if (!normalized) return null;
    const mimeType = inferImageMimeType(normalized);
    return `data:${mimeType};base64,${normalized}`;
}

function normalizeBase64(value) {
    if (!value) return null;
    let cleaned = String(value).trim();
    cleaned = cleaned.replace(/^"|"$/g, '');
    cleaned = cleaned.replace(/\s+/g, '');
    cleaned = cleaned.replace(/-/g, '+').replace(/_/g, '/');

    const remainder = cleaned.length % 4;
    if (remainder !== 0) {
        cleaned = cleaned.padEnd(cleaned.length + (4 - remainder), '=');
    }

    if (!/^[A-Za-z0-9+/=]+$/.test(cleaned)) {
        return null;
    }

    return cleaned;
}

function inferImageMimeTypeFromBytes(bytes) {
    if (!bytes || bytes.length < 4) return 'image/jpeg';
    if (bytes[0] === 0x89 && bytes[1] === 0x50 && bytes[2] === 0x4E && bytes[3] === 0x47) return 'image/png';
    if (bytes[0] === 0xFF && bytes[1] === 0xD8 && bytes[2] === 0xFF) return 'image/jpeg';
    if (bytes[0] === 0x47 && bytes[1] === 0x49 && bytes[2] === 0x46) return 'image/gif';
    if (bytes[0] === 0x42 && bytes[1] === 0x4D) return 'image/bmp';
    if (
        bytes[0] === 0x52 && bytes[1] === 0x49 && bytes[2] === 0x46 && bytes[3] === 0x46 &&
        bytes[8] === 0x57 && bytes[9] === 0x45 && bytes[10] === 0x42 && bytes[11] === 0x50
    ) return 'image/webp';
    return 'image/jpeg';
}

function inferFileMimeTypeFromBytes(bytes) {
    if (!bytes || bytes.length < 4) return 'application/octet-stream';
    if (bytes[0] === 0x25 && bytes[1] === 0x50 && bytes[2] === 0x44 && bytes[3] === 0x46) return 'application/pdf';
    return inferImageMimeTypeFromBytes(bytes);
}

function setPreviewSource(imgEl, pdfEl, preview) {
    if (currentPreviewObjectUrl) {
        URL.revokeObjectURL(currentPreviewObjectUrl);
        currentPreviewObjectUrl = null;
    }

    if (preview.kind === 'pdf') {
        imgEl.src = '';
        imgEl.style.display = 'none';
        pdfEl.src = preview.src;
        pdfEl.style.display = 'block';
    } else {
        pdfEl.src = '';
        pdfEl.style.display = 'none';
        imgEl.src = preview.src;
        imgEl.style.display = 'block';
    }

    if (preview.isObjectUrl) {
        currentPreviewObjectUrl = preview.src;
    }
}

function buildPreviewSource(rawValue) {
    if (!rawValue) return null;

    if (Array.isArray(rawValue) && rawValue.length > 0) {
        const uint8 = new Uint8Array(rawValue);
        const mimeType = inferFileMimeTypeFromBytes(uint8);
        const blob = new Blob([uint8], { type: mimeType });
        const src = URL.createObjectURL(blob);
        return { src, kind: mimeType === 'application/pdf' ? 'pdf' : 'image', isObjectUrl: true };
    }

    if (typeof rawValue === 'object') {
        const values = rawValue.$values || rawValue.values || rawValue.data;
        if (Array.isArray(values) && values.length > 0) {
            const uint8 = new Uint8Array(values);
            const mimeType = inferFileMimeTypeFromBytes(uint8);
            const blob = new Blob([uint8], { type: mimeType });
            const src = URL.createObjectURL(blob);
            return { src, kind: mimeType === 'application/pdf' ? 'pdf' : 'image', isObjectUrl: true };
        }
    }

    const text = String(rawValue).trim();
    if (!text) return null;

    if (text.startsWith('data:application/pdf')) {
        return { src: text, kind: 'pdf', isObjectUrl: false };
    }
    if (text.startsWith('data:image/')) {
        return { src: text, kind: 'image', isObjectUrl: false };
    }

    if (text.startsWith('[') && text.endsWith(']')) {
        try {
            const bytes = JSON.parse(text);
            if (Array.isArray(bytes) && bytes.length > 0) {
                const uint8 = new Uint8Array(bytes);
                const mimeType = inferFileMimeTypeFromBytes(uint8);
                const blob = new Blob([uint8], { type: mimeType });
                const src = URL.createObjectURL(blob);
                return { src, kind: mimeType === 'application/pdf' ? 'pdf' : 'image', isObjectUrl: true };
            }
        } catch (e) {
            console.warn('Invalid file byte-array format.', e);
        }
    }

    const normalized = normalizeBase64(text);
    if (!normalized) return null;

    try {
        const signature = atob(normalized).slice(0, 4);
        if (signature === '%PDF') {
            return { src: `data:application/pdf;base64,${normalized}`, kind: 'pdf', isObjectUrl: false };
        }
    } catch (e) {
    }

    const mimeType = inferImageMimeType(normalized);
    return { src: `data:${mimeType};base64,${normalized}`, kind: 'image', isObjectUrl: false };
}

function loadAllEnterpriseRequests() {
    fetch('/AdminPanel/GetAllEnterpriseRequests')
        .then(response => response.json())
        .then(data => {
            allEnterprises = Array.isArray(data) ? data : [];
            const tbody = document.getElementById('enterprisesTableBody');
            tbody.innerHTML = '';

            if (!allEnterprises.length) {
                tbody.innerHTML = '<tr><td colspan="7" style="text-align:center;">No enterprise requests found</td></tr>';
                return;
            }

            allEnterprises.forEach(enterprise => {
                const row = tbody.insertRow();
                row.insertCell(0).innerHTML = enterprise.StoreName || enterprise.Username;
                row.insertCell(1).innerHTML = enterprise.Email;
                row.insertCell(2).innerHTML = enterprise.StoreDescription || 'N/A';
                row.insertCell(3).innerHTML = enterprise.ContactNumber || 'N/A';
                row.insertCell(4).innerHTML = renderEnterpriseDocumentCell(enterprise.RequestId, enterprise.UploadedDocument);

                row.insertCell(5).innerHTML = renderApprovalStatus(enterprise);
                row.insertCell(6).innerHTML = renderRequestActions(enterprise, 'enterprise');
            });
        })
        .catch(error => {
            console.error('Error loading enterprises:', error);
            document.getElementById('enterprisesTableBody').innerHTML = '<tr><td colspan="7" style="text-align:center;color:red;">Error loading data</td></tr>';
        });
}

function renderEnterpriseDocumentCell(requestId, rawData) {
    if (!rawData) {
        return '<span>N/A</span>';
    }
    enterpriseDocStore[requestId] = rawData;
    return `<button type="button" class="qcu-id-btn" onclick="viewEnterpriseDocument(${requestId})">View Doc</button>`;
}

function viewEnterpriseDocument(requestId) {
    const rawData = enterpriseDocStore[requestId];
    const preview = buildPreviewSource(rawData);
    if (!preview || !preview.src) {
        alert('Invalid enterprise document data.');
        return;
    }

    const modal = document.getElementById('qcuIdModal');
    const img = document.getElementById('qcuIdPreviewImage');
    const pdf = document.getElementById('qcuIdPreviewPdf');
    const title = document.getElementById('previewModalTitle');
    if (!modal || !img || !pdf) {
        return;
    }

    setPreviewSource(img, pdf, preview);
    if (title) {
        title.textContent = 'Enterprise Document Preview';
    }
    modal.classList.remove('hidden');
    modal.setAttribute('aria-hidden', 'false');
}

function loadFeedbacks() {
    const tbody = document.getElementById('feedbacksTableBody');
    tbody.innerHTML = '<tr><td colspan="5" style="text-align:center;">Loading...</td></tr>';

    fetch('/AdminPanel/GetFeedbacks')
        .then(response => response.json())
        .then(data => {
            allFeedbacks = Array.isArray(data) ? data : [];
            populateCategoryFilter(allFeedbacks);
            applyFeedbackFilters();
        })
        .catch(error => {
            console.error('Error loading feedbacks:', error);
            tbody.innerHTML = '<tr><td colspan="5" style="text-align:center;color:red;">Error loading feedbacks</td></tr>';
        });
}

function populateCategoryFilter(feedbacks) {
    const categoryFilter = document.getElementById('categoryFilter');
    const selected = categoryFilter.value;
    const categories = [...new Set(feedbacks.map(f => (f.Category || 'General').trim()).filter(Boolean))].sort();

    categoryFilter.innerHTML = '<option value="">All Categories</option>';
    categories.forEach(category => {
        const option = document.createElement('option');
        option.value = category;
        option.textContent = category;
        categoryFilter.appendChild(option);
    });

    if (categories.includes(selected)) {
        categoryFilter.value = selected;
    }
}

function applyFeedbackFilters() {
    const tbody = document.getElementById('feedbacksTableBody');
    const ratingValue = document.getElementById('ratingFilter').value;
    const categoryValue = document.getElementById('categoryFilter').value;
    const ratingNumber = ratingValue ? parseInt(ratingValue, 10) : null;

    const filtered = allFeedbacks.filter(feedback => {
        const matchesRating = ratingNumber ? Number(feedback.Rating) === ratingNumber : true;
        const currentCategory = (feedback.Category || 'General').trim();
        const matchesCategory = categoryValue ? currentCategory === categoryValue : true;
        return matchesRating && matchesCategory;
    });

    tbody.innerHTML = '';

    if (filtered.length === 0) {
        tbody.innerHTML = '<tr><td colspan="5" style="text-align:center;">No feedback found</td></tr>';
        return;
    }

    filtered.forEach(feedback => {
        const row = tbody.insertRow();
        row.insertCell(0).innerHTML = feedback.Email || 'N/A';
        row.insertCell(1).innerHTML = feedback.ContactNumber || 'N/A';
        row.insertCell(2).innerHTML = feedback.Category || 'General';
        row.insertCell(3).innerHTML = feedback.Message || '';
        row.insertCell(4).innerHTML = `
            <div class="rating-actions">
                ${renderStars(feedback.Rating)}
                <button type="button" class="delete-btn" onclick="openDeleteFeedbackConfirmModal(${feedback.FeedbackId})">Delete</button>
            </div>
        `;
    });
}

function renderStars(rating) {
    const safeRating = Number.isInteger(rating) ? Math.max(1, Math.min(5, rating)) : 0;
    const filled = '&#9733;'.repeat(safeRating);
    const empty = '&#9734;'.repeat(5 - safeRating);
    return `<span class="rating-stars">${filled}${empty}</span>`;
}

function approveRequest(requestId) {
    if (confirm('Approve this user?')) {
        const formData = new URLSearchParams();
        formData.append('requestId', requestId);

        fetch('/AdminPanel/ApproveRequest', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
            },
            body: formData
        })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    alert('User approved successfully!');
                    loadAllStudentRequests();
                    loadAllEnterpriseRequests();
                } else {
                    alert('Failed to approve user: ' + (data.message || 'Unknown error'));
                }
            })
            .catch(error => {
                console.error('Error:', error);
                alert('An error occurred: ' + error.message);
            });
    }
}

function rejectRequest(requestId) {
    if (confirm('Reject this user?')) {
        const formData = new URLSearchParams();
        formData.append('requestId', requestId);

        fetch('/AdminPanel/RejectRequest', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
            },
            body: formData
        })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    alert('User rejected!');
                    loadAllStudentRequests();
                    loadAllEnterpriseRequests();
                } else {
                    alert('Failed to reject user: ' + (data.message || 'Unknown error'));
                }
            })
            .catch(error => {
                console.error('Error:', error);
                alert('An error occurred: ' + error.message);
            });
    }
}

function updateRequestAccountStatus(requestId, isEnabled) {
    const action = isEnabled ? 'enable' : 'disable';
    if (!confirm(`Are you sure you want to ${action} this account?`)) {
        return;
    }

    const formData = new URLSearchParams();
    formData.append('requestId', requestId);
    formData.append('isEnabled', isEnabled);

    fetch('/AdminPanel/UpdateRequestAccountStatus', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
        },
        body: formData
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                alert(data.message || `Account ${isEnabled ? 'enabled' : 'disabled'}.`);
                closeEditModal();
                loadAllStudentRequests();
                loadAllEnterpriseRequests();
            } else {
                alert(data.message || 'Failed to update account status.');
            }
        })
        .catch(error => {
            console.error('Account status error:', error);
            alert('An error occurred: ' + error.message);
        });
}

function filterTable(tableId, searchTerm) {
    const table = document.getElementById(tableId);
    const rows = table.getElementsByTagName('tr');

    for (let i = 0; i < rows.length; i++) {
        const cells = rows[i].getElementsByTagName('td');
        if (cells.length === 0) continue;

        let found = false;
        for (let j = 0; j < cells.length; j++) {
            const text = cells[j].textContent.toLowerCase();
            if (text.includes(searchTerm)) {
                found = true;
                break;
            }
        }
        rows[i].style.display = found ? '' : 'none';
    }
}

function openEditStudentModal(requestId) {
    const student = allStudents.find(s => s.RequestId === requestId);
    if (!student) return;

    currentEditContext = { type: 'student', requestId: requestId };
    document.getElementById('editModalTitle').textContent = 'Edit Student';
    const form = document.getElementById('editRequestForm');
    form.innerHTML = `
        <div class="edit-form-grid">
            <div class="edit-form-group"><label>Username</label><input name="username" value="${escapeHtml(student.Username || '')}" required /></div>
            <div class="edit-form-group"><label>Email</label><input name="email" type="email" value="${escapeHtml(student.Email || '')}" required /></div>
            <div class="edit-form-group"><label>First Name</label><input name="firstname" value="${escapeHtml(student.Firstname || '')}" required /></div>
            <div class="edit-form-group"><label>Last Name</label><input name="lastname" value="${escapeHtml(student.Lastname || '')}" required /></div>
            <div class="edit-form-group"><label>Student Number</label><input name="studentNumber" value="${escapeHtml(student.StudentNumber || '')}" required /></div>
            <div class="edit-form-group"><label>Section</label><input name="section" value="${escapeHtml(student.Section || '')}" required /></div>
            <div class="edit-form-group full"><label>Contact Number</label><input name="contactNumber" value="${escapeHtml(student.ContactNumber || '')}" /></div>
        </div>
        <div class="edit-form-actions">
            ${renderAccountToggleAction(student)}
            <button type="button" class="edit-cancel-btn" onclick="closeEditModal()">Cancel</button>
            <button type="submit" class="edit-save-btn">Save Changes</button>
        </div>
    `;
    document.getElementById('editRequestModal').classList.remove('hidden');
}

function openEditEnterpriseModal(requestId) {
    const enterprise = allEnterprises.find(e => e.RequestId === requestId);
    if (!enterprise) return;

    currentEditContext = { type: 'enterprise', requestId: requestId };
    document.getElementById('editModalTitle').textContent = 'Edit Enterprise';
    const form = document.getElementById('editRequestForm');
    form.innerHTML = `
        <div class="edit-form-grid">
            <div class="edit-form-group"><label>Username</label><input name="username" value="${escapeHtml(enterprise.Username || '')}" required /></div>
            <div class="edit-form-group"><label>Email</label><input name="email" type="email" value="${escapeHtml(enterprise.Email || '')}" required /></div>
            <div class="edit-form-group"><label>Store Name</label><input name="storeName" value="${escapeHtml(enterprise.StoreName || '')}" required /></div>
            <div class="edit-form-group"><label>Business Type</label><input name="businessType" value="${escapeHtml(enterprise.StoreDescription || '')}" required /></div>
            <div class="edit-form-group"><label>Contact Number</label><input name="contactNumber" value="${escapeHtml(enterprise.ContactNumber || '')}" /></div>
            <div class="edit-form-group"><label>GCash Number</label><input name="gcashNumber" value="${escapeHtml(enterprise.GcashNumber || '')}" inputmode="numeric" maxlength="11" pattern="09[0-9]{9}" title="Enter an 11-digit GCash number starting with 09." /></div>
        </div>
        <div class="edit-form-actions">
            ${renderAccountToggleAction(enterprise)}
            <button type="button" class="edit-cancel-btn" onclick="closeEditModal()">Cancel</button>
            <button type="submit" class="edit-save-btn">Save Changes</button>
        </div>
    `;
    restrictDigitsInput(form.gcashNumber, 11);
    document.getElementById('editRequestModal').classList.remove('hidden');
}

function closeEditModal() {
    const modal = document.getElementById('editRequestModal');
    if (modal) {
        modal.classList.add('hidden');
    }
    currentEditContext = null;
    pendingEditSubmitEvent = null;
}

function submitEditForm(event) {
    event.preventDefault();
    if (!currentEditContext) return;

    if (currentEditContext.type === 'enterprise') {
        const gcashNumber = (event.target.gcashNumber.value || '').trim();
        if (gcashNumber && !/^09\d{9}$/.test(gcashNumber)) {
            alert('GCash number must be 11 digits and start with 09.');
            event.target.gcashNumber.focus();
            return;
        }
    }

    pendingEditSubmitEvent = event;
    openSaveConfirmModal();
}

function openSaveConfirmModal() {
    const modal = document.getElementById('saveConfirmModal');
    if (!modal) return;
    modal.classList.remove('hidden');
    modal.setAttribute('aria-hidden', 'false');
}

function closeSaveConfirmModal() {
    const modal = document.getElementById('saveConfirmModal');
    if (!modal) return;
    modal.classList.add('hidden');
    modal.setAttribute('aria-hidden', 'true');
}

function confirmAndSaveChanges() {
    if (!pendingEditSubmitEvent || !currentEditContext) {
        closeSaveConfirmModal();
        return;
    }
    closeSaveConfirmModal();

    const form = pendingEditSubmitEvent.target;
    pendingEditSubmitEvent = null;
    const payload = new URLSearchParams();
    payload.append('requestId', currentEditContext.requestId);

    let url = '';
    if (currentEditContext.type === 'student') {
        url = '/AdminPanel/UpdateStudentRequestDetails';
        payload.append('username', form.username.value.trim());
        payload.append('email', form.email.value.trim());
        payload.append('firstname', form.firstname.value.trim());
        payload.append('lastname', form.lastname.value.trim());
        payload.append('studentNumber', form.studentNumber.value.trim());
        payload.append('section', form.section.value.trim());
        payload.append('contactNumber', form.contactNumber.value.trim());
    } else {
        url = '/AdminPanel/UpdateEnterpriseRequestDetails';
        payload.append('username', form.username.value.trim());
        payload.append('email', form.email.value.trim());
        payload.append('storeName', form.storeName.value.trim());
        payload.append('businessType', form.businessType.value.trim());
        payload.append('contactNumber', form.contactNumber.value.trim());
        payload.append('gcashNumber', form.gcashNumber.value.trim());
    }

    fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: payload
    })
        .then(async response => {
            const rawText = await response.text();
            let parsed;
            try {
                parsed = rawText ? JSON.parse(rawText) : null;
            } catch (e) {
                parsed = null;
            }

            if (!response.ok) {
                const serverMsg = parsed && parsed.message
                    ? parsed.message
                    : `Request failed (${response.status}).`;
                throw new Error(serverMsg);
            }

            if (!parsed) {
                throw new Error('Invalid server response while saving changes.');
            }

            return parsed;
        })
        .then(data => {
            if (data.success) {
                alert('Changes saved successfully.');
                closeEditModal();
                loadAllStudentRequests();
                loadAllEnterpriseRequests();
            } else {
                alert(data.message || 'Failed to save changes.');
            }
        })
        .catch(error => {
            console.error('Edit save error:', error);
            alert(error.message || 'An error occurred while saving changes.');
        });
}

function escapeHtml(value) {
    return String(value)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}

function openDeleteFeedbackConfirmModal(feedbackId) {
    pendingFeedbackDeleteId = feedbackId;
    const modal = document.getElementById('deleteFeedbackConfirmModal');
    if (!modal) return;
    modal.classList.remove('hidden');
    modal.setAttribute('aria-hidden', 'false');
}

function closeDeleteFeedbackConfirmModal() {
    const modal = document.getElementById('deleteFeedbackConfirmModal');
    if (!modal) return;
    modal.classList.add('hidden');
    modal.setAttribute('aria-hidden', 'true');
    pendingFeedbackDeleteId = null;
}

function confirmDeleteFeedback() {
    if (!pendingFeedbackDeleteId) {
        closeDeleteFeedbackConfirmModal();
        return;
    }

    const payload = new URLSearchParams();
    payload.append('feedbackId', pendingFeedbackDeleteId);

    fetch('/AdminPanel/DeleteFeedback', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: payload
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                closeDeleteFeedbackConfirmModal();
                loadFeedbacks();
            } else {
                alert(data.message || 'Failed to delete feedback.');
            }
        })
        .catch(error => {
            console.error('Delete feedback error:', error);
            alert('An error occurred while deleting feedback.');
        });
}
