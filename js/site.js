/* ========================================
   SONGHAI HMO - MAIN JAVASCRIPT
   ======================================== */

// Show notification function
function showNotification(message, isError = false) {
    const notification = document.getElementById('notification');
    if (!notification) {
        // Create notification element if it doesn't exist
        const newNotification = document.createElement('div');
        newNotification.id = 'notification';
        newNotification.className = 'notification';
        document.body.appendChild(newNotification);
    }
    
    const notif = document.getElementById('notification');
    notif.style.backgroundColor = isError ? '#dc3545' : '#28a745';
    notif.innerHTML = `<span>${message}</span>`;
    notif.style.display = 'block';
    
    setTimeout(() => {
        notif.style.display = 'none';
    }, 3000);
}

// Format currency
function formatCurrency(amount) {
    return '₦' + parseFloat(amount).toLocaleString('en-NG');
}

// Format date
function formatDate(dateString) {
    return new Date(dateString).toLocaleDateString('en-NG');
}

// Loader functions
function showLoader(elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        element.innerHTML = '<div class="loading">Loading... <div class="spinner"></div></div>';
    }
}

function hideLoader(elementId) {
    // Remove loader class
}

// Confirm action dialog
function confirmAction(message, callback) {
    if (confirm(message)) {
        callback();
    }
}

// Table search filter
function filterTable(tableId, searchTerm) {
    const table = document.getElementById(tableId);
    if (!table) return;
    
    const rows = table.getElementsByTagName('tr');
    for (let i = 1; i < rows.length; i++) {
        const row = rows[i];
        let found = false;
        const cells = row.getElementsByTagName('td');
        
        for (let j = 0; j < cells.length; j++) {
            if (cells[j].innerText.toLowerCase().includes(searchTerm.toLowerCase())) {
                found = true;
                break;
            }
        }
        
        row.style.display = found ? '' : 'none';
    }
}

// Export table to CSV
function exportToCSV(tableId, filename) {
    const table = document.getElementById(tableId);
    if (!table) return;
    
    let csv = [];
    const rows = table.querySelectorAll('tr');
    
    for (let i = 0; i < rows.length; i++) {
        const row = [];
        const cols = rows[i].querySelectorAll('td, th');
        
        for (let j = 0; j < cols.length; j++) {
            let data = cols[j].innerText.replace(/,/g, '');
            row.push(data);
        }
        
        csv.push(row.join(','));
    }
    
    const csvFile = new Blob([csv.join('\n')], { type: 'text/csv' });
    const downloadLink = document.createElement('a');
    downloadLink.download = filename + '.csv';
    downloadLink.href = URL.createObjectURL(csvFile);
    downloadLink.click();
}

// Toggle sidebar on mobile
function toggleSidebar() {
    const sidebar = document.querySelector('.sidebar');
    if (sidebar) {
        sidebar.classList.toggle('active');
    }
}

// Smooth scroll to element
function smoothScrollTo(elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        element.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
}

// Auto-refresh data (for call centre)
let refreshInterval = null;

function startAutoRefresh(callback, interval = 10000) {
    if (refreshInterval) clearInterval(refreshInterval);
    refreshInterval = setInterval(callback, interval);
}

function stopAutoRefresh() {
    if (refreshInterval) {
        clearInterval(refreshInterval);
        refreshInterval = null;
    }
}

// Chart color helper
const chartColors = {
    pending: '#ff9800',
    approved: '#28a745',
    rejected: '#dc3545',
    primary: '#1a73e8',
    secondary: '#17a2b8',
    warning: '#ffc107',
    danger: '#dc3545',
    success: '#28a745'
};

// Initialize tooltips (if any)
document.addEventListener('DOMContentLoaded', function() {
    // Add active class to current navigation link
    const currentPath = window.location.pathname;
    document.querySelectorAll('.nav-links a, .nav-menu a').forEach(link => {
        if (link.getAttribute('href') === currentPath) {
            link.classList.add('active');
        }
    });
    
    // Add search functionality to tables if search input exists
    const searchInput = document.getElementById('tableSearch');
    if (searchInput) {
        searchInput.addEventListener('keyup', function() {
            const tableId = this.getAttribute('data-table');
            if (tableId) {
                filterTable(tableId, this.value);
            }
        });
    }
});