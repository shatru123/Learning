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

// Authentication State Manager
const AuthManager = {
  tokenKey: 'learningos_token',
  userKey: 'learningos_user',

  getToken() {
    return localStorage.getItem(this.tokenKey);
  },

  setToken(token) {
    if (token) localStorage.setItem(this.tokenKey, token);
    else localStorage.removeItem(this.tokenKey);
  },

  getUser() {
    try {
      const u = localStorage.getItem(this.userKey);
      return u ? JSON.parse(u) : null;
    } catch {
      return null;
    }
  },

  setUser(user) {
    if (user) localStorage.setItem(this.userKey, JSON.stringify(user));
    else localStorage.removeItem(this.userKey);
  },

  clear() {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.userKey);
  },

  isAuthenticated() {
    return !!this.getToken();
  },

  isAdmin() {
    const u = this.getUser();
    return u && (u.role || '').toLowerCase() === 'admin';
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

// Robust HTTP API caller with Outage Protection & JWT Auth
async function apiCall(endpoint, method = 'GET', body = null) {
  const options = {
    method,
    headers: {
      'Accept': 'application/json'
    }
  };

  const token = AuthManager.getToken();
  if (token) {
    options.headers['Authorization'] = `Bearer ${token}`;
  }

  if (body) {
    options.headers['Content-Type'] = 'application/json';
    options.body = JSON.stringify(body);
  }

  try {
    const response = await fetch(endpoint, options);

    if (!response.ok) {
      if (response.status === 401 && !endpoint.includes('/api/auth/login') && !endpoint.includes('/api/auth/register')) {
        AuthManager.clear();
        if (typeof updateAuthUi === 'function') updateAuthUi();
        if (typeof openAuthModal === 'function') openAuthModal('login');
      }

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
  toast.className = `toast ${type === 'error' ? 'border-red-500 text-red-300' : (type === 'warning' ? 'border-amber-500 text-amber-300' : 'border-emerald-500 text-emerald-300')}`;
  
  const icon = type === 'error' ? '⚠️' : (type === 'warning' ? '🔔' : '✅');
  toast.innerHTML = `<span>${icon}</span> <span>${message}</span>`;
  container.appendChild(toast);

  setTimeout(() => {
    toast.style.opacity = '0';
    toast.style.transition = 'opacity 0.3s ease';
    setTimeout(() => toast.remove(), 300);
  }, 4000);
}
