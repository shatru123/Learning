// LearningOS Outage Safety & Centralized API Client
const OutageManager = {
  isAvailable: true,

  setUnavailable(message) {
    this.isAvailable = false;
    const banner = document.getElementById('outage-banner');
    const msgEl = document.getElementById('outage-banner-msg');
    const statusPill = document.getElementById('server-status-pill');

    if (banner && msgEl) {
      msgEl.textContent = message || "LearningOS is temporarily unavailable. Your local changes have not been confirmed as saved.";
      banner.style.display = 'block';
    }

    if (statusPill) {
      statusPill.className = "px-2.5 py-1 text-xs font-semibold rounded-full bg-red-900/40 text-red-400 border border-red-700/50 flex items-center gap-1.5";
      statusPill.innerHTML = `<span class="w-2 h-2 rounded-full bg-red-500 animate-pulse"></span> Service Offline / Retrying`;
    }
  },

  setAvailable() {
    this.isAvailable = true;
    const banner = document.getElementById('outage-banner');
    const statusPill = document.getElementById('server-status-pill');

    if (banner) {
      banner.style.display = 'none';
    }

    if (statusPill) {
      statusPill.className = "px-2.5 py-1 text-xs font-semibold rounded-full bg-emerald-900/40 text-emerald-400 border border-emerald-700/50 flex items-center gap-1.5";
      statusPill.innerHTML = `<span class="w-2 h-2 rounded-full bg-emerald-500"></span> PostgreSQL Connected`;
    }
  }
};

// Form Draft Storage (Requirement 22: Browser draft != database)
const DraftStore = {
  save(key, value) {
    try {
      localStorage.setItem(`learningos_draft_${key}`, JSON.stringify({
        value,
        timestamp: new Date().toISOString()
      }));
    } catch (e) {
      console.warn("Draft store failed", e);
    }
  },

  get(key) {
    try {
      const item = localStorage.getItem(`learningos_draft_${key}`);
      return item ? JSON.parse(item) : null;
    } catch {
      return null;
    }
  },

  clear(key) {
    try {
      localStorage.removeItem(`learningos_draft_${key}`);
    } catch { }
  }
};

// Robust HTTP API caller with Outage Protection
async function apiCall(endpoint, method = 'GET', body = null) {
  const options = {
    method,
    headers: {
      'Accept': 'application/json'
    }
  };

  if (body) {
    options.headers['Content-Type'] = 'application/json';
    options.body = JSON.stringify(body);
  }

  try {
    const response = await fetch(endpoint, options);

    if (!response.ok) {
      if (response.status >= 500) {
        OutageManager.setUnavailable("LearningOS is temporarily unavailable. Your local changes have not been confirmed as saved.");
      }
      const errData = await response.json().catch(() => ({ message: response.statusText }));
      throw new Error(errData.message || `Server responded with ${response.status}`);
    }

    // Success confirmed by authoritative server
    OutageManager.setAvailable();
    return await response.json().catch(() => ({}));
  } catch (error) {
    if (error.name === 'TypeError' || error.message.includes('Failed to fetch') || error.message.includes('NetworkError')) {
      OutageManager.setUnavailable("LearningOS is temporarily unavailable. Your local changes have not been confirmed as saved.");
    }
    throw error;
  }
}

// Toast notification helper
function showToast(message, type = 'success') {
  const container = document.getElementById('toast-container');
  if (!container) return;

  const toast = document.createElement('div');
  toast.className = `toast ${type === 'error' ? 'border-red-500 text-red-300' : 'border-emerald-500 text-emerald-300'}`;
  
  const icon = type === 'error' ? '⚠️' : '✅';
  toast.innerHTML = `<span>${icon}</span> <span>${message}</span>`;
  container.appendChild(toast);

  setTimeout(() => {
    toast.style.opacity = '0';
    toast.style.transition = 'opacity 0.3s ease';
    setTimeout(() => toast.remove(), 300);
  }, 4000);
}
