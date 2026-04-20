// SignalR connection
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/notificationHub")
    .build();

connection.start().catch(err => console.error(err));

// Receive notifications
connection.on("ReceiveNotification", (title, message) => {
    showToast(title, message);
    updateNotificationBadge();
});

connection.on("ClaimUpdated", (claimId, status) => {
    showToast("Claim Updated", `Claim #${claimId} is now ${status}`);
    setTimeout(() => location.reload(), 2000);
});

function showToast(title, message) {
    const toast = document.createElement('div');
    toast.className = 'toast-notification';
    toast.innerHTML = `
        <div class="toast-header">
            <strong>${title}</strong>
            <button onclick="this.parentElement.parentElement.remove()">×</button>
        </div>
        <div class="toast-body">${message}</div>
    `;
    document.body.appendChild(toast);
    setTimeout(() => toast.remove(), 5000);
}

async function updateNotificationBadge() {
    const response = await fetch('/Admin/GetNotifications');
    const notifications = await response.json();
    const badge = document.getElementById('notificationBadge');
    if (badge) {
        const unreadCount = notifications.filter(n => !n.isRead).length;
        badge.innerText = unreadCount;
        badge.style.display = unreadCount > 0 ? 'inline-block' : 'none';
    }
}

// Auto-refresh for call centre
let autoRefreshInterval = null;

function startAutoRefresh(intervalSeconds = 10) {
    if (autoRefreshInterval) clearInterval(autoRefreshInterval);
    autoRefreshInterval = setInterval(() => {
        location.reload();
    }, intervalSeconds * 1000);
}

function stopAutoRefresh() {
    if (autoRefreshInterval) {
        clearInterval(autoRefreshInterval);
        autoRefreshInterval = null;
    }
}