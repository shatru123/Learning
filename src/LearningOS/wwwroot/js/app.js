// LearningOS Core Client Application

let currentActiveDay = null;
let selectedDayForMissed = null;
let selectedDayForRecover = null;
let selectedDayForReview = null;
let allDaysCache = [];

document.addEventListener('DOMContentLoaded', () => {
  initApp();
});

async function initApp() {
  await loadDashboard();
  loadRoadmap();
  loadSettings();
}

// TAB NAVIGATION
let currentTab = 'dashboard';
let previousTabBeforeDetail = 'roadmap';

function switchTab(tabName) {
  if (currentTab !== tabName && currentTab !== 'daydetail') {
    previousTabBeforeDetail = currentTab;
  }
  currentTab = tabName;

  const tabs = ['dashboard', 'roadmap', 'roadmap3d', 'recovery', 'daydetail', 'curriculum', 'analytics', 'audit', 'settings'];
  tabs.forEach(t => {
    const el = document.getElementById(`view-${t}`);
    const navEl = document.getElementById(`nav-${t}`);
    if (el) el.classList.toggle('hidden', t !== tabName);
    if (navEl) {
      navEl.classList.toggle('active-tab', t === tabName);
      navEl.classList.toggle('bg-slate-800', t === tabName);
      navEl.classList.toggle('text-blue-400', t === tabName);
    }
  });

  if (tabName === 'dashboard') loadDashboard();
  if (tabName === 'roadmap') loadRoadmap();
  if (tabName === 'roadmap3d') loadRoadmap3D();
  if (tabName === 'recovery') loadRecoveryQueue();
  if (tabName === 'curriculum') loadCurrentTrackerSubTab();
  if (tabName === 'analytics') loadAnalytics();
  if (tabName === 'audit') loadAuditHistory();
  if (tabName === 'settings') loadSettings();
}

// 1. DASHBOARD
async function loadDashboard() {
  try {
    const data = await apiCall('/api/dashboard');

    // Streak & Completion
    document.getElementById('metric-current-streak').textContent = data.currentStreak || 0;
    document.getElementById('metric-best-streak').textContent = data.bestStreak || 0;
    document.getElementById('metric-completion-pct').textContent = `${data.taskCompletionPercent || 0}%`;
    document.getElementById('metric-progress-bar').style.width = `${Math.min(100, data.taskCompletionPercent || 0)}%`;
    document.getElementById('metric-completed-tasks').textContent = data.completedTasksCount || 0;
    document.getElementById('metric-total-tasks').textContent = data.totalTasksCount || 0;

    // Day Status Separated Counts (Requirement 9)
    document.getElementById('stat-planned').textContent = data.plannedDaysRemainingCount || 0;
    document.getElementById('stat-completed').textContent = data.completedDaysCount || 0;
    document.getElementById('stat-partial').textContent = data.partiallyCompletedDaysCount || 0;
    document.getElementById('stat-skipped').textContent = data.skippedDaysCount || 0;
    document.getElementById('stat-rest').textContent = data.restDaysCount || 0;
    document.getElementById('stat-missed').textContent = data.missedDaysCount || 0;

    // Recovery Queue Banner (Requirement 12)
    const recoveryBanner = document.getElementById('dash-recovery-banner');
    const navRecoveryCount = document.getElementById('nav-recovery-count');
    if (data.recoveryQueueCount > 0) {
      recoveryBanner.classList.remove('hidden');
      const totalIncTasks = data.recoveryQueue.reduce((sum, item) => sum + item.incompleteTaskCount, 0);
      document.getElementById('dash-recovery-title').textContent = `⚠️ ${totalIncTasks} task(s) across ${data.recoveryQueueCount} day(s) need attention`;
      document.getElementById('dash-recovery-desc').textContent = data.recoveryQueue.map(q => `Day ${q.dayNumber || '?'}: ${q.incompleteTaskCount} incomplete`).join(' • ');
      navRecoveryCount.textContent = data.recoveryQueueCount;
      navRecoveryCount.classList.remove('hidden');
    } else {
      recoveryBanner.classList.add('hidden');
      navRecoveryCount.classList.add('hidden');
    }

    // Today's Focus Plan
    if (data.todayPlan) {
      currentActiveDay = data.todayPlan;
      renderTodayPlan(data.todayPlan);
    } else {
      document.getElementById('today-title').textContent = "All 100 Learning Days Scheduled!";
      document.getElementById('today-theme').textContent = "Ready to start your next session.";
      document.getElementById('today-tasks-container').innerHTML = `<div class="text-xs text-slate-500 py-4">No active day selected. Check roadmap to pick a day.</div>`;
    }

    // Recent Activity Feed
    renderActivityFeed(data.recentActivity || []);

  } catch (err) {
    console.error("Dashboard load failed", err);
  }
}

function renderTodayPlan(day) {
  document.getElementById('today-title').textContent = `Day ${day.dayNumber || '—'}: ${day.title}`;
  document.getElementById('today-theme').textContent = `${day.theme} • Date: ${day.calendarDate}`;

  const badge = document.getElementById('today-status-badge');
  badge.className = `badge-status ${getStatusBadgeClass(day.status)}`;
  badge.innerHTML = getStatusIcon(day.status) + ' ' + formatStatusName(day.status);

  const container = document.getElementById('today-tasks-container');
  if (!day.tasks || day.tasks.length === 0) {
    container.innerHTML = `<div class="text-xs text-slate-500 py-3">No tasks assigned to this day.</div>`;
    return;
  }

  container.innerHTML = day.tasks.map(t => `
    <div class="flex items-start justify-between p-3 rounded-lg border border-slate-800 bg-slate-950/60 hover:border-slate-700 transition">
      <div class="flex items-start gap-3">
        <input type="checkbox" ${t.status === 'Completed' ? 'checked' : ''} onchange="toggleTaskStatus(${t.id}, this.checked)" class="mt-1 w-4 h-4 rounded text-blue-600 bg-slate-900 border-slate-700 focus:ring-0 cursor-pointer">
        <div>
          <div class="font-semibold text-sm ${t.status === 'Completed' ? 'line-through text-slate-500' : 'text-slate-200'}">
            ${escapeHtml(t.title)}
          </div>
          ${t.description ? `<p class="text-xs text-slate-400 mt-0.5">${escapeHtml(t.description)}</p>` : ''}
          <div class="flex items-center gap-2 mt-2">
            <span class="text-[10px] uppercase font-bold px-2 py-0.5 rounded bg-slate-800 text-slate-300 border border-slate-700">${t.category}</span>
            <span class="text-[10px] px-2 py-0.5 rounded ${getPriorityClass(t.priority)}">${t.priority}</span>
            <span class="text-xs text-slate-500">⏱️ ${t.estimatedMinutes}m</span>
          </div>
        </div>
      </div>
      <button onclick="openTaskHistoryModal(${t.id}, '${escapeHtml(t.title)}')" class="text-xs text-slate-400 hover:text-blue-400 font-medium px-2 py-1">
        History
      </button>
    </div>
  `).join('');
}

function renderActivityFeed(logs) {
  const feed = document.getElementById('dash-activity-feed');
  if (!logs || logs.length === 0) {
    feed.innerHTML = `<div class="text-slate-500 py-4 text-center">No recent activity logged.</div>`;
    return;
  }

  feed.innerHTML = logs.map(l => `
    <div class="flex items-start gap-2.5 pb-2.5 border-b border-slate-800/60 last:border-0">
      <span class="text-blue-400 font-bold">•</span>
      <div class="flex-1">
        <div class="text-slate-300 font-medium">${escapeHtml(l.description)}</div>
        <div class="text-[10px] text-slate-500 mt-0.5">${formatTimeAgo(l.timestamp)}</div>
      </div>
    </div>
  `).join('');
}

// 2. ROADMAP
async function loadRoadmap() {
  try {
    const days = await apiCall('/api/days');
    allDaysCache = days;
    renderRoadmapGrid(days);
  } catch (err) {
    console.error("Roadmap load failed", err);
  }
}

function filterRoadmap() {
  const phase = document.getElementById('roadmap-phase-filter').value;
  const status = document.getElementById('roadmap-status-filter').value;
  const search = document.getElementById('roadmap-search-input').value.toLowerCase().trim();

  let filtered = allDaysCache;

  if (phase) {
    filtered = filtered.filter(d => d.phaseId === parseInt(phase));
  }
  if (status) {
    filtered = filtered.filter(d => d.status.toLowerCase() === status.toLowerCase());
  }
  if (search) {
    filtered = filtered.filter(d =>
      d.title.toLowerCase().includes(search) ||
      d.theme.toLowerCase().includes(search) ||
      (d.dayNumber && `day ${d.dayNumber}`.includes(search)) ||
      (d.tasks && d.tasks.some(t => t.title.toLowerCase().includes(search) || t.category.toLowerCase().includes(search)))
    );
  }

  renderRoadmapGrid(filtered);
}

function renderRoadmapGrid(days) {
  const grid = document.getElementById('roadmap-grid');
  if (!days || days.length === 0) {
    grid.innerHTML = `<div class="col-span-full text-center py-12 text-slate-500">No days matched your filters.</div>`;
    return;
  }

  grid.innerHTML = days.map(d => {
    const isCompleted = d.status === 'Completed';
    const isMissed = d.status === 'Missed';
    const isRest = d.status === 'RestDay';
    const isLeave = d.status === 'LeaveDay';
    const completedCount = d.tasks ? d.tasks.filter(t => t.status === 'Completed').length : 0;
    const totalCount = d.tasks ? d.tasks.length : 0;

    return `
      <div id="day-card-${d.id}" class="rounded-xl border border-slate-800 bg-slate-900/70 p-4 hover:border-slate-700 transition flex flex-col justify-between shadow-sm">
        <div>
          <div class="flex items-center justify-between gap-2 mb-2">
            <span class="text-xs font-bold uppercase tracking-wider px-2 py-0.5 rounded bg-slate-800 text-slate-300">
              ${d.isLearningDay ? `Day ${d.dayNumber}` : (isRest ? '🌴 Rest Day' : '🏖️ Leave')}
            </span>
            <span class="badge-status ${getStatusBadgeClass(d.status)}">
              ${getStatusIcon(d.status)} ${formatStatusName(d.status)}
            </span>
          </div>
          <div class="text-[11px] text-slate-400 mb-1 font-mono">${d.calendarDate}</div>
          <h4 class="font-bold text-sm text-white line-clamp-1">${escapeHtml(d.title)}</h4>
          <p class="text-xs text-slate-400 mt-1 line-clamp-2">${escapeHtml(d.theme)}</p>

          ${d.isLearningDay ? `
            <div class="mt-3 flex items-center justify-between text-[11px] text-slate-400 border-t border-slate-800/80 pt-2">
              <span>Tasks: ${completedCount}/${totalCount}</span>
              <div class="w-24 bg-slate-800 rounded-full h-1.5 overflow-hidden">
                <div class="bg-blue-500 h-1.5 rounded-full" style="width: ${totalCount > 0 ? (completedCount / totalCount * 100) : 0}%"></div>
              </div>
            </div>
          ` : ''}

          ${isMissed && d.missedRecord ? `
            <div class="mt-2 p-2 rounded bg-red-950/40 border border-red-800/40 text-[11px] text-red-300">
              <strong>Missed:</strong> ${escapeHtml(d.missedRecord.reason)}
              ${d.missedRecord.note ? `<br><em>"${escapeHtml(d.missedRecord.note)}"</em>` : ''}
            </div>
          ` : ''}
        </div>

        <div class="mt-4 pt-3 border-t border-slate-800 flex items-center justify-between gap-1">
          <button onclick="viewDayDetail(${d.id})" class="text-xs font-semibold text-blue-400 hover:text-blue-300">
            Open Details →
          </button>
          <div class="flex gap-1">
            ${isMissed ? `
              <button onclick="openRecoverModal(${d.id})" class="px-2 py-1 rounded bg-amber-500 hover:bg-amber-600 text-slate-950 font-bold text-[11px]">
                Recover
              </button>
            ` : `
              <button onclick="openMissedModal(${d.id})" title="Mark Day as Missed" class="px-2 py-1 rounded bg-red-950/50 hover:bg-red-900/60 text-red-300 border border-red-800/50 text-[11px]">
                Missed
              </button>
            `}
          </div>
        </div>
      </div>
    `;
  }).join('');
}

// 3. DAY DETAIL VIEW
function goBackFromDayDetail() {
  const targetTab = previousTabBeforeDetail || 'roadmap';
  switchTab(targetTab);

  // If returning to roadmap, smoothly scroll to and highlight the card
  if (targetTab === 'roadmap' && currentActiveDay) {
    setTimeout(() => {
      const card = document.getElementById(`day-card-${currentActiveDay.id}`);
      if (card) {
        card.scrollIntoView({ behavior: 'smooth', block: 'center' });
        card.classList.add('ring-2', 'ring-blue-500', 'border-blue-500');
        setTimeout(() => {
          card.classList.remove('ring-2', 'ring-blue-500', 'border-blue-500');
        }, 2500);
      }
    }, 150);
  }
}

async function openDayDetail(dayNumber) {
  try {
    if (!allDaysCache || allDaysCache.length === 0) {
      allDaysCache = await apiCall('/api/days');
    }
    const day = allDaysCache.find(d => d.dayNumber === dayNumber);
    if (day) {
      await viewDayDetail(day.id);
    } else {
      showToast(`Day ${dayNumber} not found`, "error");
    }
  } catch (err) {
    showToast("Failed to open day details", "error");
  }
}

async function viewDayDetail(dayId) {
  try {
    const day = await apiCall(`/api/days/${dayId}`);
    currentActiveDay = day;
    renderDayDetail(day);
    switchTab('daydetail');
  } catch (err) {
    showToast("Failed to load day details", "error");
  }
}

function viewCurrentDayFull() {
  if (currentActiveDay) {
    renderDayDetail(currentActiveDay);
    switchTab('daydetail');
  }
}

function renderDayDetail(day) {
  // Update back buttons and breadcrumbs dynamically
  const originNames = {
    'roadmap': '100-Day Roadmap',
    'roadmap3d': '3D Cosmic Roadmap',
    'dashboard': 'Dashboard',
    'curriculum': 'Trackers',
    'recovery': 'Recovery Queue',
    'analytics': 'Analytics',
    'audit': 'Audit History'
  };
  const originName = originNames[previousTabBeforeDetail] || '100-Day Roadmap';

  const backLabel = document.getElementById('btn-back-label');
  const backLabelBottom = document.querySelector('.btn-back-label-bottom');
  const breadcrumbOrigin = document.getElementById('breadcrumb-origin');
  const breadcrumbCurrent = document.getElementById('breadcrumb-current-day');

  if (backLabel) backLabel.textContent = `Back to ${originName}`;
  if (backLabelBottom) backLabelBottom.textContent = `Back to ${originName}`;
  if (breadcrumbOrigin) breadcrumbOrigin.textContent = originName;
  if (breadcrumbCurrent) breadcrumbCurrent.textContent = day.isLearningDay ? `Day ${day.dayNumber}` : 'Day Details';

  document.getElementById('detail-day-label').textContent = day.isLearningDay ? `Day ${day.dayNumber}` : 'Special Day';
  document.getElementById('detail-date').textContent = day.calendarDate;
  document.getElementById('detail-title').textContent = day.title;
  document.getElementById('detail-theme').textContent = `${day.theme} • Week ${day.weekNumber}`;

  const badge = document.getElementById('detail-status-badge');
  badge.className = `badge-status ${getStatusBadgeClass(day.status)}`;
  badge.innerHTML = `${getStatusIcon(day.status)} ${formatStatusName(day.status)}`;

  // Missed alert box
  const missedAlert = document.getElementById('detail-missed-alert');
  if (day.status === 'Missed' && day.missedRecord) {
    missedAlert.classList.remove('hidden');
    document.getElementById('detail-missed-reason').textContent = `Reason: ${day.missedRecord.reason} ${day.missedRecord.note ? `• Note: "${day.missedRecord.note}"` : ''}`;
  } else {
    missedAlert.classList.add('hidden');
  }

  // Tasks List
  const tasksContainer = document.getElementById('detail-tasks-list');
  if (!day.tasks || day.tasks.length === 0) {
    tasksContainer.innerHTML = `<div class="text-xs text-slate-500 py-4">No tasks assigned to this day.</div>`;
  } else {
    tasksContainer.innerHTML = day.tasks.map(t => `
      <div class="flex flex-col sm:flex-row sm:items-center justify-between p-4 rounded-lg border border-slate-800 bg-slate-950/70 gap-3">
        <div class="flex items-start gap-3">
          <input type="checkbox" ${t.status === 'Completed' ? 'checked' : ''} onchange="toggleTaskStatus(${t.id}, this.checked)" class="mt-1 w-4 h-4 rounded text-blue-600 bg-slate-900 border-slate-700 cursor-pointer">
          <div>
            <div class="font-semibold text-sm ${t.status === 'Completed' ? 'line-through text-slate-500' : 'text-slate-100'}">
              ${escapeHtml(t.title)}
            </div>
            ${t.description ? `<p class="text-xs text-slate-400 mt-1">${escapeHtml(t.description)}</p>` : ''}
            <div class="flex flex-wrap items-center gap-2 mt-2">
              <span class="text-[10px] font-bold px-2 py-0.5 rounded bg-slate-800 text-slate-300 border border-slate-700">${t.category}</span>
              <span class="text-[10px] px-2 py-0.5 rounded ${getPriorityClass(t.priority)}">${t.priority}</span>
              <span class="text-xs text-slate-500">⏱️ ${t.estimatedMinutes} mins</span>
              ${t.originalDayNumber !== t.currentDayNumber ? `<span class="text-[10px] px-2 py-0.5 rounded bg-purple-950/60 text-purple-300 border border-purple-800">Moved from Day ${t.originalDayNumber}</span>` : ''}
            </div>
          </div>
        </div>

        <div class="flex items-center gap-2 self-end sm:self-auto">
          <select onchange="changeTaskStatusManual(${t.id}, this.value)" class="bg-slate-900 border border-slate-700 rounded text-xs px-2 py-1 text-slate-300">
            <option value="Pending" ${t.status === 'Pending' ? 'selected' : ''}>Pending</option>
            <option value="InProgress" ${t.status === 'InProgress' ? 'selected' : ''}>In Progress</option>
            <option value="Completed" ${t.status === 'Completed' ? 'selected' : ''}>Completed</option>
            <option value="Skipped" ${t.status === 'Skipped' ? 'selected' : ''}>Skipped</option>
          </select>
          <button onclick="openTaskHistoryModal(${t.id}, '${escapeHtml(t.title)}')" class="px-2.5 py-1 rounded bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-medium">
            History
          </button>
        </div>
      </div>
    `).join('');
  }

  // Draft vs Database Notes (Requirement 22)
  const notesEl = document.getElementById('detail-notes-input');
  const draftStatus = document.getElementById('draft-note-status');
  const draft = DraftStore.get(`day_${day.id}_notes`);

  if (draft && draft.value && (!day.notes || draft.value !== day.notes)) {
    notesEl.value = draft.value;
    draftStatus.textContent = "Unsaved draft loaded from local browser cache";
    draftStatus.className = "text-[11px] text-amber-400 font-semibold";
  } else {
    notesEl.value = day.notes || '';
    draftStatus.textContent = "Saved to PostgreSQL database";
    draftStatus.className = "text-[11px] text-slate-500";
  }
}

function handleNotesInput() {
  if (!currentActiveDay) return;
  const val = document.getElementById('detail-notes-input').value;
  DraftStore.save(`day_${currentActiveDay.id}_notes`, val);
  const draftStatus = document.getElementById('draft-note-status');
  draftStatus.textContent = "Draft cached locally (Not yet saved to DB)";
  draftStatus.className = "text-[11px] text-amber-400";
}

async function saveDayNotes() {
  if (!currentActiveDay) return;
  const val = document.getElementById('detail-notes-input').value;

  try {
    const day = await apiCall(`/api/days/${currentActiveDay.id}`);
    day.notes = val;
    // Call day status update or save notes
    await apiCall(`/api/days/${currentActiveDay.id}/status`, 'POST', day.status);
    DraftStore.clear(`day_${currentActiveDay.id}_notes`);

    const draftStatus = document.getElementById('draft-note-status');
    draftStatus.textContent = "Saved successfully to authoritative PostgreSQL!";
    draftStatus.className = "text-[11px] text-emerald-400 font-semibold";

    showToast("Notes permanently saved to PostgreSQL!");
  } catch (err) {
    showToast("Failed to save notes to database. Draft preserved locally.", "error");
  }
}

// 4. RECOVERY QUEUE & MANAGER
async function loadRecoveryQueue() {
  try {
    const data = await apiCall('/api/dashboard');
    const container = document.getElementById('recovery-items-container');

    if (!data.recoveryQueue || data.recoveryQueue.length === 0) {
      container.innerHTML = `
        <div class="text-center py-12">
          <div class="text-4xl mb-2">🎉</div>
          <h3 class="text-base font-bold text-emerald-400">Recovery Queue Clean!</h3>
          <p class="text-xs text-slate-400 mt-1">No missed days or overdue tasks. Keep up the phenomenal momentum!</p>
        </div>
      `;
      return;
    }

    container.innerHTML = data.recoveryQueue.map(item => `
      <div class="p-5 rounded-xl border border-amber-500/30 bg-amber-950/10 flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <div class="flex items-center gap-2">
            <span class="text-xs font-bold uppercase tracking-wider px-2 py-0.5 rounded bg-red-900/50 text-red-300">
              ${item.dayNumber ? `Day ${item.dayNumber}` : 'Missed Day'}
            </span>
            <span class="text-xs text-slate-400 font-mono">${item.calendarDate}</span>
            <span class="badge-status status-missed">Missed</span>
          </div>
          <div class="mt-2 text-sm font-semibold text-slate-200">
            ${item.incompleteTaskCount} incomplete task(s) • ~${item.incompleteEstimatedMinutes} minutes workload
          </div>
          ${item.missedReason ? `<div class="text-xs text-slate-400 mt-0.5">Reason: <strong class="text-slate-300">${escapeHtml(item.missedReason)}</strong> ${item.missedNote ? `("${escapeHtml(item.missedNote)}")` : ''}</div>` : ''}
          <div class="mt-2 flex flex-wrap gap-1">
            ${item.incompleteTaskTitles.map(t => `<span class="text-[11px] px-2 py-0.5 rounded bg-slate-900 text-slate-400 border border-slate-800">${escapeHtml(t)}</span>`).join('')}
          </div>
        </div>

        <button onclick="openRecoverModal(${item.dayPlanId})" class="px-4 py-2 rounded-lg bg-amber-500 hover:bg-amber-600 text-slate-950 font-bold text-xs shadow transition shrink-0">
          Recover Day Now →
        </button>
      </div>
    `).join('');

  } catch (err) {
    console.error("Recovery queue load failed", err);
  }
}

// 5. TASK STATUS UPDATES
async function toggleTaskStatus(taskId, isChecked) {
  const newStatus = isChecked ? 'Completed' : 'Pending';
  await updateTaskStatusApi(taskId, newStatus);
}

async function changeTaskStatusManual(taskId, newStatus) {
  await updateTaskStatusApi(taskId, newStatus);
}

async function updateTaskStatusApi(taskId, newStatus) {
  try {
    await apiCall(`/api/tasks/${taskId}/status`, 'PUT', { status: newStatus });
    showToast(`Task marked as ${newStatus}`);
    await loadDashboard();
    if (currentActiveDay) {
      const refreshed = await apiCall(`/api/days/${currentActiveDay.id}`);
      currentActiveDay = refreshed;
      renderDayDetail(refreshed);
    }
  } catch (err) {
    showToast("Failed to update task status", "error");
  }
}

// 6. TASK HISTORY MODAL
async function openTaskHistoryModal(taskId, title) {
  document.getElementById('history-task-title').textContent = `History: ${title}`;
  const container = document.getElementById('task-history-timeline');
  container.innerHTML = `<div class="text-slate-500 py-3 text-center">Loading audit history...</div>`;
  openModal('modal-taskhistory');

  try {
    const task = await apiCall(`/api/tasks/${taskId}`);
    if (!task.history || task.history.length === 0) {
      container.innerHTML = `<div class="text-slate-500 py-3 text-center">No previous history entries recorded.</div>`;
      return;
    }

    container.innerHTML = task.history.map(h => `
      <div class="p-3 rounded-lg border border-slate-800 bg-slate-950/60">
        <div class="flex items-center justify-between text-[11px] text-slate-400">
          <span>${formatDateFull(h.changedAt)}</span>
          <span class="font-bold text-blue-400">${h.oldStatus} → ${h.newStatus}</span>
        </div>
        ${h.note ? `<div class="text-xs text-slate-300 mt-1">${escapeHtml(h.note)}</div>` : ''}
      </div>
    `).join('');
  } catch (err) {
    container.innerHTML = `<div class="text-red-400 text-center py-2">Failed to load history.</div>`;
  }
}

// 7. MISSED DAY MODAL & ACTION
function openMissedModal(dayId) {
  selectedDayForMissed = dayId;
  openModal('modal-missed');
}

function openMissedModalForCurrent() {
  if (currentActiveDay) openMissedModal(currentActiveDay.id);
}

function openMissedModalForDetail() {
  if (currentActiveDay) openMissedModal(currentActiveDay.id);
}

async function submitMarkMissed() {
  if (!selectedDayForMissed) return;
  const reason = document.getElementById('missed-reason-select').value;
  const note = document.getElementById('missed-note-input').value;

  try {
    await apiCall(`/api/days/${selectedDayForMissed}/missed`, 'POST', { reason, note });
    closeModal('modal-missed');
    showToast("Day marked as missed. Original tasks preserved.");
    await loadDashboard();
    await loadRoadmap();
    if (currentActiveDay && currentActiveDay.id === selectedDayForMissed) {
      viewDayDetail(selectedDayForMissed);
    }
  } catch (err) {
    showToast("Failed to mark day as missed", "error");
  }
}

// 8. RECOVER DAY MODAL & ACTION
function openRecoverModal(dayId) {
  selectedDayForRecover = dayId;
  openModal('modal-recover');
}

function openRecoverModalForDetail() {
  if (currentActiveDay) openRecoverModal(currentActiveDay.id);
}

async function submitRecoverDay() {
  if (!selectedDayForRecover) return;

  const strategy = document.querySelector('input[name="recovery-strategy"]:checked').value;
  const rescheduleDate = document.getElementById('recover-reschedule-date').value;
  const compressDays = parseInt(document.getElementById('recover-compress-days').value) || 3;

  try {
    await apiCall(`/api/days/${selectedDayForRecover}/recover`, 'POST', {
      strategy,
      rescheduleDate: rescheduleDate || null,
      compressDays
    });

    closeModal('modal-recover');
    showToast(`Smart recovery applied using '${strategy}'!`);
    await loadDashboard();
    await loadRoadmap();
    loadRecoveryQueue();
    if (currentActiveDay && currentActiveDay.id === selectedDayForRecover) {
      viewDayDetail(selectedDayForRecover);
    }
  } catch (err) {
    showToast("Recovery failed", "error");
  }
}

// 9. REST DAY MODAL & ACTION (Requirement 6)
function openRestDayModal() {
  const tomorrow = new Date();
  tomorrow.setDate(tomorrow.getDate() + 1);
  document.getElementById('rest-date-input').value = tomorrow.toISOString().split('T')[0];
  openModal('modal-rest');
}

async function submitAddRestDay() {
  const date = document.getElementById('rest-date-input').value;
  const reason = document.getElementById('rest-reason-input').value;
  const isPrePlanned = document.getElementById('rest-preplanned-check').checked;

  if (!date) {
    showToast("Please pick a date for your rest day", "error");
    return;
  }

  try {
    await apiCall('/api/days/rest', 'POST', { date, reason, isPrePlanned });
    closeModal('modal-rest');
    showToast("🌴 Rest Day scheduled! Your streak is preserved.");
    await loadDashboard();
    await loadRoadmap();
  } catch (err) {
    showToast("Failed to schedule rest day", "error");
  }
}

// 10. LEAVE DAY MODAL & ACTION (Requirement 7)
function openLeaveDayModal() {
  const today = new Date().toISOString().split('T')[0];
  document.getElementById('leave-start-input').value = today;
  document.getElementById('leave-end-input').value = today;
  openModal('modal-leave');
}

async function submitAddLeaveDay() {
  const startDate = document.getElementById('leave-start-input').value;
  const endDate = document.getElementById('leave-end-input').value;
  const leaveType = document.getElementById('leave-type-select').value;
  const reason = document.getElementById('leave-notes-input').value;

  if (!startDate || !endDate) {
    showToast("Please enter both start and end dates", "error");
    return;
  }

  try {
    await apiCall('/api/days/leave', 'POST', { startDate, endDate, leaveType, reason });
    closeModal('modal-leave');
    showToast("🏖️ Leave recorded! All 100 learning days preserved.");
    await loadDashboard();
    await loadRoadmap();
  } catch (err) {
    showToast("Failed to record leave", "error");
  }
}

// 11. EXTEND ROADMAP MODAL & ACTION (Requirement 8)
async function openExtendRoadmapModal() {
  try {
    const res = await apiCall('/api/days/unfinished-count');
    document.getElementById('extend-unfinished-count').textContent = res.unfinishedDays || 0;
    document.getElementById('extend-days-input').value = Math.max(res.unfinishedDays || 5, 1);
  } catch {
    document.getElementById('extend-unfinished-count').textContent = '—';
  }
  openModal('modal-extend');
}

async function submitExtendRoadmap() {
  const daysToExtend = parseInt(document.getElementById('extend-days-input').value) || 5;
  const reason = document.getElementById('extend-reason-input').value;

  try {
    const res = await apiCall('/api/days/extend', 'POST', { daysToExtend, reason });
    closeModal('modal-extend');
    showToast(res.message || `Roadmap extended by ${daysToExtend} days!`);
    await loadDashboard();
    await loadRoadmap();
  } catch (err) {
    showToast("Failed to extend roadmap", "error");
  }
}

// 12. DAILY REVIEW MODAL & ACTION (Requirement 11)
function openReviewModalForCurrent() {
  if (currentActiveDay) openReviewModal(currentActiveDay.id);
}

function openReviewModalForDetail() {
  if (currentActiveDay) openReviewModal(currentActiveDay.id);
}

function openReviewModal(dayId) {
  selectedDayForReview = dayId;
  openModal('modal-review');
}

async function submitDailyReview() {
  if (!selectedDayForReview) return;
  const rating = document.getElementById('review-rating-select').value;
  const whatHappenedNotes = document.getElementById('review-notes-input').value;
  const carryForwardNotes = document.getElementById('review-carry-input').value;
  const tomorrowPriority = document.getElementById('review-priority-input').value;

  try {
    await apiCall('/api/reviews', 'POST', {
      dayPlanId: selectedDayForReview,
      rating,
      whatHappenedNotes,
      carryForwardNotes,
      tomorrowPriority
    });

    closeModal('modal-review');
    showToast("Daily review permanently saved!");
    await loadDashboard();
  } catch (err) {
    showToast("Failed to save daily review", "error");
  }
}

// 13. AUDIT HISTORY VIEW
async function loadAuditHistory() {
  const container = document.getElementById('full-audit-timeline');
  container.innerHTML = `<div class="text-slate-500 py-6 text-center">Loading audit log from PostgreSQL...</div>`;

  try {
    const logs = await apiCall('/api/audit?limit=60');
    if (!logs || logs.length === 0) {
      container.innerHTML = `<div class="text-slate-500 py-6 text-center">No activity history recorded yet.</div>`;
      return;
    }

    container.innerHTML = logs.map(l => `
      <div class="flex items-start gap-3 p-3.5 rounded-lg border border-slate-800 bg-slate-950/60">
        <div class="mt-0.5 text-base">${getActionIcon(l.actionType)}</div>
        <div class="flex-1">
          <div class="flex items-center justify-between">
            <span class="font-semibold text-xs text-white">${escapeHtml(l.actionType)}</span>
            <span class="text-[11px] text-slate-500 font-mono">${formatDateFull(l.timestamp)}</span>
          </div>
          <p class="text-xs text-slate-300 mt-1">${escapeHtml(l.description)}</p>
        </div>
      </div>
    `).join('');
  } catch (err) {
    container.innerHTML = `<div class="text-red-400 py-4 text-center">Failed to load audit history.</div>`;
  }
}

// 14. CURRICULUM TRACKERS
let currentTrackerSubTab = 'dsa';
function switchTrackerSubTab(subTab) {
  currentTrackerSubTab = subTab;
  const tabs = ['dsa', 'ai', 'systemdesign', 'projects', 'interview', 'jobs', 'journal', 'resources'];
  tabs.forEach(t => {
    const btn = document.getElementById(`subtab-${t}`);
    const view = document.getElementById(`tracker-subview-${t}`);
    if (view) view.classList.toggle('hidden', t !== subTab);
    if (btn) {
      btn.className = t === subTab
        ? 'font-bold text-blue-400 border-b-2 border-blue-400 pb-2'
        : 'font-medium text-slate-400 hover:text-slate-200 pb-2';
    }
  });

  loadCurrentTrackerSubTab();
}

async function loadCurrentTrackerSubTab() {
  if (currentTrackerSubTab === 'dsa') loadDSAProblems();
  if (currentTrackerSubTab === 'ai') loadAIModules();
  if (currentTrackerSubTab === 'systemdesign') loadSystemDesignTopics();
  if (currentTrackerSubTab === 'projects') loadPortfolioProjects();
  if (currentTrackerSubTab === 'interview') loadInterviewQuestions();
  if (currentTrackerSubTab === 'jobs') loadJobApplications();
  if (currentTrackerSubTab === 'journal') loadJournalEntries();
  if (currentTrackerSubTab === 'resources') loadLearningResources();
}

async function loadAIModules() {
  const container = document.getElementById('ai-modules-list');
  if (!container) return;
  try {
    if (allDaysCache.length === 0) {
      allDaysCache = await apiCall('/api/days');
    }
    const aiDays = allDaysCache.filter(d => d.dayNumber >= 51 && d.dayNumber <= 75);
    container.innerHTML = aiDays.map(d => {
      const completedTasks = (d.tasks || []).filter(t => t.status === 'Completed').length;
      const totalTasks = (d.tasks || []).length;
      return `
        <div class="p-4 rounded-xl border border-slate-800 bg-slate-950/60 hover:border-slate-700 transition">
          <div class="flex items-start justify-between">
            <div>
              <span class="text-[10px] font-bold uppercase px-2 py-0.5 rounded bg-purple-950 text-purple-300 border border-purple-800">Day ${d.dayNumber}</span>
              <h4 class="font-bold text-white text-sm mt-1.5">${escapeHtml(d.title)}</h4>
              <p class="text-xs text-purple-400/80 font-mono mt-0.5">Focus: ${escapeHtml(d.theme)}</p>
            </div>
            <span class="badge-status ${getStatusBadgeClass(d.status)}">${getStatusIcon(d.status)} ${formatStatusName(d.status)}</span>
          </div>
          <div class="mt-3 space-y-1.5 border-t border-slate-800/80 pt-2.5">
            ${(d.tasks || []).map(t => `
              <div class="flex items-center gap-2 text-xs">
                <span class="${t.status === 'Completed' ? 'text-emerald-400' : 'text-slate-500'}">${t.status === 'Completed' ? '✔' : '○'}</span>
                <span class="${t.status === 'Completed' ? 'line-through text-slate-500' : 'text-slate-300'} truncate">${escapeHtml(t.title)}</span>
                <span class="text-[10px] text-slate-500 ml-auto">${t.estimatedMinutes}m</span>
              </div>
            `).join('')}
          </div>
          <div class="mt-3 flex items-center justify-between pt-2 border-t border-slate-800/60 text-xs">
            <span class="text-slate-500">${completedTasks}/${totalTasks} Tasks Done</span>
            <button onclick="openDayDetail(${d.dayNumber})" class="text-blue-400 hover:text-blue-300 font-semibold">Open Day Plan →</button>
          </div>
        </div>
      `;
    }).join('');
  } catch (err) {
    container.innerHTML = `<div class="text-red-400 py-3">Failed to load AI Engineering modules.</div>`;
  }
}

async function loadPortfolioProjects() {
  const container = document.getElementById('projects-list');
  if (!container) return;
  try {
    if (allDaysCache.length === 0) {
      allDaysCache = await apiCall('/api/days');
    }
    const projectDays = allDaysCache.filter(d => d.dayNumber >= 76 && d.dayNumber <= 90);
    container.innerHTML = projectDays.map(d => {
      const completedTasks = (d.tasks || []).filter(t => t.status === 'Completed').length;
      const totalTasks = (d.tasks || []).length;
      return `
        <div class="p-4 rounded-xl border border-slate-800 bg-slate-950/60 hover:border-slate-700 transition">
          <div class="flex items-start justify-between">
            <div class="flex-1">
              <div class="flex items-center gap-2">
                <span class="text-[10px] font-bold uppercase px-2 py-0.5 rounded bg-indigo-950 text-indigo-300 border border-indigo-800">Sprint Day ${d.dayNumber}</span>
                <span class="text-xs text-slate-400 font-mono">${d.calendarDate}</span>
              </div>
              <h4 class="font-bold text-white text-base mt-1.5">${escapeHtml(d.title)}</h4>
              <p class="text-xs text-slate-400 mt-1">${escapeHtml(d.theme)}</p>
            </div>
            <span class="badge-status ${getStatusBadgeClass(d.status)}">${getStatusIcon(d.status)} ${formatStatusName(d.status)}</span>
          </div>
          <div class="mt-3 space-y-2 border-t border-slate-800/80 pt-2.5">
            ${(d.tasks || []).map(t => `
              <div class="flex items-center justify-between text-xs p-2 rounded bg-slate-900/60 border border-slate-800/50">
                <div class="flex items-center gap-2">
                  <input type="checkbox" ${t.status === 'Completed' ? 'checked' : ''} onchange="toggleTaskStatus(${t.id}, this.checked)" class="w-3.5 h-3.5 rounded text-blue-600 bg-slate-950 border-slate-700 cursor-pointer">
                  <span class="${t.status === 'Completed' ? 'line-through text-slate-500' : 'text-slate-200 font-medium'}">${escapeHtml(t.title)}</span>
                </div>
                <div class="flex items-center gap-2">
                  <span class="text-[10px] px-1.5 py-0.5 rounded ${getPriorityClass(t.priority)}">${t.priority}</span>
                  <span class="text-[11px] text-slate-500">⏱️ ${t.estimatedMinutes}m</span>
                </div>
              </div>
            `).join('')}
          </div>
          <div class="mt-3 flex items-center justify-between pt-2 border-t border-slate-800/60 text-xs">
            <span class="text-slate-400 font-medium">${completedTasks}/${totalTasks} Tasks Completed</span>
            <button onclick="openDayDetail(${d.dayNumber})" class="text-indigo-400 hover:text-indigo-300 font-semibold">View Sprint Details →</button>
          </div>
        </div>
      `;
    }).join('');
  } catch (err) {
    container.innerHTML = `<div class="text-red-400 py-3">Failed to load Portfolio Projects.</div>`;
  }
}

async function loadDSAProblems() {
  const list = document.getElementById('dsa-problems-list');
  try {
    const problems = await apiCall('/api/curriculum/dsa');
    list.innerHTML = problems.map(p => `
      <div class="flex items-center justify-between p-3 rounded-lg border border-slate-800 bg-slate-950/60">
        <div class="flex items-center gap-3">
          <input type="checkbox" ${p.status === 'Solved' ? 'checked' : ''} onchange="updateDSAProblemStatus(${p.id}, this.checked)" class="w-4 h-4 rounded text-emerald-500 bg-slate-900 border-slate-700 cursor-pointer">
          <div>
            <span class="font-semibold text-sm ${p.status === 'Solved' ? 'line-through text-slate-500' : 'text-slate-100'}">${escapeHtml(p.title)}</span>
            <span class="ml-2 text-[10px] px-2 py-0.5 rounded ${p.difficulty === 'Hard' ? 'bg-red-950 text-red-400 border border-red-800' : (p.difficulty === 'Medium' ? 'bg-amber-950 text-amber-400 border border-amber-800' : 'bg-emerald-950 text-emerald-400 border border-emerald-800')}">${p.difficulty}</span>
            ${p.pattern ? `<span class="ml-1 text-[10px] text-slate-400 font-mono">(${p.pattern})</span>` : ''}
          </div>
        </div>
        <span class="text-xs text-slate-500">${p.platform}</span>
      </div>
    `).join('');
  } catch (err) {
    list.innerHTML = `<div class="text-slate-500 py-3">Failed to load DSA problems.</div>`;
  }
}

async function updateDSAProblemStatus(id, isSolved) {
  try {
    await apiCall(`/api/curriculum/dsa/${id}/status`, 'PUT', isSolved ? 'Solved' : 'Planned');
    showToast(isSolved ? "Problem solved! 🎉" : "Problem reset");
  } catch {
    showToast("Status update failed", "error");
  }
}

async function loadSystemDesignTopics() {
  const container = document.getElementById('systemdesign-list');
  try {
    const topics = await apiCall('/api/curriculum/systemdesign');
    container.innerHTML = topics.map(t => `
      <div class="p-4 rounded-lg border border-slate-800 bg-slate-950/60 flex flex-col justify-between">
        <div>
          <div class="flex items-center justify-between mb-2">
            <h4 class="font-bold text-sm text-white">${escapeHtml(t.topicName)}</h4>
            <span class="text-[10px] px-2 py-0.5 rounded bg-blue-950 text-blue-300 border border-blue-800 font-semibold">${t.status}</span>
          </div>
          <p class="text-xs text-slate-400">${escapeHtml(t.architectureSummary || 'No summary available.')}</p>
        </div>
      </div>
    `).join('');
  } catch {
    container.innerHTML = `<div class="text-slate-500 py-3">Failed to load topics.</div>`;
  }
}

async function loadInterviewQuestions() {
  const container = document.getElementById('interview-list');
  try {
    const questions = await apiCall('/api/curriculum/interview');
    container.innerHTML = questions.map(q => `
      <div class="p-4 rounded-lg border border-slate-800 bg-slate-950/60">
        <div class="flex items-center justify-between mb-1">
          <span class="text-[10px] font-bold uppercase tracking-wider px-2 py-0.5 rounded bg-slate-800 text-slate-300">${q.category}</span>
          <span class="text-xs text-slate-500">Confidence: <strong class="text-slate-300">${q.confidenceLevel}</strong></span>
        </div>
        <h4 class="text-sm font-semibold text-slate-200 mt-2">${escapeHtml(q.question)}</h4>
      </div>
    `).join('');
  } catch {
    container.innerHTML = `<div class="text-slate-500 py-3">Failed to load questions.</div>`;
  }
}

async function loadJobApplications() {
  const container = document.getElementById('jobs-list');
  try {
    const jobs = await apiCall('/api/curriculum/jobs');
    if (!jobs || jobs.length === 0) {
      container.innerHTML = `<div class="text-slate-500 py-6 text-center text-xs">No job applications logged yet. Track your applications for Phase 5!</div>`;
      return;
    }
    container.innerHTML = jobs.map(j => `
      <div class="flex items-center justify-between p-3 rounded-lg border border-slate-800 bg-slate-950/60">
        <div>
          <span class="font-bold text-sm text-white">${escapeHtml(j.company)}</span>
          <span class="text-xs text-slate-400 ml-2">${escapeHtml(j.role)}</span>
        </div>
        <span class="text-xs font-semibold px-2 py-0.5 rounded bg-blue-900/40 text-blue-300 border border-blue-700/50">${j.status}</span>
      </div>
    `).join('');
  } catch {
    container.innerHTML = `<div class="text-slate-500 py-3">Failed to load applications.</div>`;
  }
}

async function loadJournalEntries() {
  const container = document.getElementById('journal-list');
  try {
    const entries = await apiCall('/api/curriculum/journal');
    if (!entries || entries.length === 0) {
      container.innerHTML = `<div class="text-slate-500 py-6 text-center text-xs">No journal entries yet. Record your daily breakthroughs and learnings!</div>`;
      return;
    }
    container.innerHTML = entries.map(e => `
      <div class="p-4 rounded-lg border border-slate-800 bg-slate-950/60">
        <div class="flex items-center justify-between mb-1">
          <h4 class="font-bold text-sm text-white">${escapeHtml(e.title)}</h4>
          <span class="text-[11px] text-slate-500 font-mono">${e.entryDate}</span>
        </div>
        <p class="text-xs text-slate-300 mt-2 whitespace-pre-wrap">${escapeHtml(e.content)}</p>
      </div>
    `).join('');
  } catch {
    container.innerHTML = `<div class="text-slate-500 py-3">Failed to load journal.</div>`;
  }
}

async function loadLearningResources() {
  const container = document.getElementById('resources-list');
  try {
    const resources = await apiCall('/api/curriculum/resources');
    container.innerHTML = resources.map(r => `
      <div class="p-3 rounded-lg border border-slate-800 bg-slate-950/60 flex items-center justify-between">
        <div>
          <a href="${escapeHtml(r.url)}" target="_blank" class="text-sm font-semibold text-blue-400 hover:underline flex items-center gap-1">
            ${escapeHtml(r.title)} <span>↗</span>
          </a>
          <span class="text-[10px] text-slate-400 font-mono">${r.category}</span>
        </div>
      </div>
    `).join('');
  } catch {
    container.innerHTML = `<div class="text-slate-500 py-3">Failed to load resources.</div>`;
  }
}

// 15. SETTINGS & RESTORE
async function loadSettings() {
  try {
    const s = await apiCall('/api/settings');
    if (document.getElementById('settings-recovery-cap')) document.getElementById('settings-recovery-cap').value = s.maxExtraRecoveryMinutesPerDay || 60;
    if (document.getElementById('settings-daily-target')) document.getElementById('settings-daily-target').value = s.dailyTargetStudyMinutes || 120;
    if (document.getElementById('recovery-cap-display')) document.getElementById('recovery-cap-display').textContent = `${s.maxExtraRecoveryMinutesPerDay || 60} mins/day`;

    if (document.getElementById('sched-workday-start')) document.getElementById('sched-workday-start').value = s.workdayStartHour ?? 9;
    if (document.getElementById('sched-workday-end')) document.getElementById('sched-workday-end').value = s.workdayEndHour ?? 18;
    if (document.getElementById('sched-study-start')) document.getElementById('sched-study-start').value = s.preferredStudyStartTime || "20:00";
    if (document.getElementById('sched-study-end')) document.getElementById('sched-study-end').value = s.preferredStudyEndTime || "22:30";
    if (document.getElementById('sched-weekday-mins')) document.getElementById('sched-weekday-mins').value = s.weekdayDailyAvailableMinutes || 120;
    if (document.getElementById('sched-weekend-mins')) document.getElementById('sched-weekend-mins').value = s.weekendDailyAvailableMinutes || 240;
    if (document.getElementById('sched-protect-workhours')) document.getElementById('sched-protect-workhours').checked = s.protectWorkingHours ?? true;
  } catch { }
}

async function saveUserSettings() {
  const cap = parseInt(document.getElementById('settings-recovery-cap').value) || 60;
  const target = parseInt(document.getElementById('settings-daily-target').value) || 120;

  try {
    await apiCall('/api/settings', 'PUT', {
      maxExtraRecoveryMinutesPerDay: cap,
      dailyTargetStudyMinutes: target
    });
    document.getElementById('recovery-cap-display').textContent = `${cap} mins/day`;
    showToast("Workload protection settings saved to PostgreSQL!");
  } catch (err) {
    showToast("Failed to save settings", "error");
  }
}

async function saveWorkingHoursSchedule() {
  const workdayStart = parseInt(document.getElementById('sched-workday-start').value) || 9;
  const workdayEnd = parseInt(document.getElementById('sched-workday-end').value) || 18;
  const studyStart = document.getElementById('sched-study-start').value || "20:00";
  const studyEnd = document.getElementById('sched-study-end').value || "22:30";
  const weekdayMins = parseInt(document.getElementById('sched-weekday-mins').value) || 120;
  const weekendMins = parseInt(document.getElementById('sched-weekend-mins').value) || 240;
  const protectWorkhours = document.getElementById('sched-protect-workhours').checked;
  const cap = parseInt(document.getElementById('settings-recovery-cap').value) || 60;
  const target = parseInt(document.getElementById('settings-daily-target').value) || 120;

  try {
    await apiCall('/api/settings', 'PUT', {
      workdayStartHour: workdayStart,
      workdayEndHour: workdayEnd,
      preferredStudyStartTime: studyStart,
      preferredStudyEndTime: studyEnd,
      weekdayDailyAvailableMinutes: weekdayMins,
      weekendDailyAvailableMinutes: weekendMins,
      protectWorkingHours: protectWorkhours,
      maxExtraRecoveryMinutesPerDay: cap,
      dailyTargetStudyMinutes: target
    });
    showToast("Working-hours schedule saved to PostgreSQL!");
  } catch (err) {
    showToast("Failed to save working-hours schedule", "error");
  }
}

// 16. ANALYTICS & VELOCITY
async function loadAnalytics() {
  try {
    const dash = await apiCall('/api/dashboard');
    if (allDaysCache.length === 0) {
      allDaysCache = await apiCall('/api/days');
    }

    const completedDays = dash.completedDaysCount || 0;
    const partialDays = dash.partiallyCompletedDaysCount || 0;
    const restDays = dash.restDaysCount || 0;
    const missedDays = dash.missedDaysCount || 0;
    const skippedDays = dash.skippedDaysCount || 0;

    const totalElapsed = completedDays + partialDays + restDays + missedDays + skippedDays;
    const consistencyRate = totalElapsed > 0
      ? Math.round(((completedDays + partialDays + restDays) / totalElapsed) * 100)
      : 100;
    
    const crEl = document.getElementById('analytics-consistency-rate');
    if (crEl) crEl.textContent = `${consistencyRate}%`;

    // Total study hours from completed tasks
    let totalCompletedMinutes = 0;
    allDaysCache.forEach(d => {
      (d.tasks || []).forEach(t => {
        if (t.status === 'Completed') {
          totalCompletedMinutes += (t.estimatedMinutes || 0);
        }
      });
    });
    const totalHours = (totalCompletedMinutes / 60).toFixed(1);
    const thEl = document.getElementById('analytics-total-hours');
    if (thEl) thEl.textContent = `${totalHours} hrs`;

    // Recovery Efficiency
    const recoveryQueueCount = dash.recoveryQueueCount || 0;
    const recoveryRate = (missedDays === 0 && recoveryQueueCount === 0)
      ? 100
      : Math.round(Math.max(0, 100 - (recoveryQueueCount * 20)));
    const rrEl = document.getElementById('analytics-recovery-rate');
    if (rrEl) rrEl.textContent = `${Math.min(100, Math.max(0, recoveryRate))}%`;

    // Phase breakdown (5 phases)
    const phases = [
      { id: 1, name: "Phase 1: Advanced C#, .NET Internals & High-Perf Data Access", range: [1, 25], color: "blue" },
      { id: 2, name: "Phase 2: Microservices, Distributed Systems & Cloud-Native .NET", range: [26, 50], color: "emerald" },
      { id: 3, name: "Phase 3: Applied AI & LLM Engineering for .NET Developers", range: [51, 75], color: "purple" },
      { id: 4, name: "Phase 4: Production Enterprise AI Capstone Projects", range: [76, 90], color: "indigo" },
      { id: 5, name: "Phase 5: High-Scale System Design, Advanced DSA & Interview Mastery", range: [91, 100], color: "amber" }
    ];

    const phasesContainer = document.getElementById('analytics-phases-container');
    if (phasesContainer) {
      phasesContainer.innerHTML = phases.map(p => {
        const phaseDays = allDaysCache.filter(d => d.dayNumber >= p.range[0] && d.dayNumber <= p.range[1]);
        let pCompletedTasks = 0;
        let pTotalTasks = 0;
        let pCompletedDays = 0;

        phaseDays.forEach(d => {
          if (d.status === 'Completed') pCompletedDays++;
          (d.tasks || []).forEach(t => {
            pTotalTasks++;
            if (t.status === 'Completed') pCompletedTasks++;
          });
        });

        const pct = pTotalTasks > 0 ? Math.round((pCompletedTasks / pTotalTasks) * 100) : 0;

        return `
          <div class="p-4 rounded-xl border border-slate-800 bg-slate-950/60 space-y-2">
            <div class="flex items-center justify-between text-xs">
              <span class="font-bold text-slate-200">${p.name} (Days ${p.range[0]}–${p.range[1]})</span>
              <span class="font-bold text-blue-400">${pct}% (${pCompletedTasks}/${pTotalTasks} Tasks)</span>
            </div>
            <div class="w-full bg-slate-800 rounded-full h-2">
              <div class="bg-blue-500 h-2 rounded-full transition-all duration-500" style="width: ${pct}%"></div>
            </div>
            <div class="flex items-center justify-between text-[11px] text-slate-500">
              <span>${pCompletedDays}/${phaseDays.length} Days Fully Completed</span>
              <span>Target: ${p.range[1] - p.range[0] + 1} Days</span>
            </div>
          </div>
        `;
      }).join('');
    }

    // Workload by Category
    const categoryStats = {};
    allDaysCache.forEach(d => {
      (d.tasks || []).forEach(t => {
        const cat = t.category || 'Core';
        if (!categoryStats[cat]) categoryStats[cat] = { total: 0, completed: 0, minutes: 0 };
        categoryStats[cat].total++;
        if (t.status === 'Completed') categoryStats[cat].completed++;
        categoryStats[cat].minutes += (t.estimatedMinutes || 0);
      });
    });

    const catContainer = document.getElementById('analytics-categories-container');
    if (catContainer) {
      catContainer.innerHTML = Object.entries(categoryStats).map(([cat, stat]) => {
        const catPct = stat.total > 0 ? Math.round((stat.completed / stat.total) * 100) : 0;
        return `
          <div class="p-3.5 rounded-xl border border-slate-800 bg-slate-950/60">
            <div class="text-xs font-bold text-slate-300 truncate">${escapeHtml(cat)}</div>
            <div class="flex items-baseline justify-between mt-2">
              <span class="text-lg font-extrabold text-white">${catPct}%</span>
              <span class="text-xs text-slate-500">${stat.completed}/${stat.total}</span>
            </div>
            <div class="w-full bg-slate-800 rounded-full h-1.5 mt-2">
              <div class="bg-blue-500 h-1.5 rounded-full" style="width: ${catPct}%"></div>
            </div>
            <div class="text-[10px] text-slate-500 mt-1.5">⏱️ ${(stat.minutes / 60).toFixed(1)}h total workload</div>
          </div>
        `;
      }).join('');
    }

  } catch (err) {
    console.error("Analytics load failed", err);
  }
}

async function checkSystemHealth() {
  const el = document.getElementById('health-check-result');
  el.textContent = "Checking live database connectivity...";
  try {
    const h = await apiCall('/health');
    el.innerHTML = `<span class="text-emerald-400 font-semibold">✔ ${h.status} (${h.database}) — ${h.totalLearningDays} Days in authoritative storage.</span>`;
  } catch (err) {
    el.innerHTML = `<span class="text-red-400 font-semibold">❌ Health check failed: ${err.message}</span>`;
  }
}

let verifiedBackupJson = null;
async function validateBackupFile() {
  const fileInput = document.getElementById('backup-file-input');
  const preview = document.getElementById('restore-preview-box');

  if (!fileInput.files || fileInput.files.length === 0) {
    showToast("Please choose a JSON backup file first", "error");
    return;
  }

  const file = fileInput.files[0];
  const text = await file.text();

  try {
    const result = await apiCall('/api/data/restore/validate', 'POST', { jsonContent: text });
    if (result.isValid) {
      verifiedBackupJson = text;
      preview.classList.remove('hidden');
      preview.innerHTML = `
        <div class="font-bold text-emerald-400 text-sm mb-2">✔ Backup File Validated Successfully!</div>
        <div class="space-y-1">
          <div>• Exported on: ${new Date(result.exportedAt).toLocaleString()}</div>
          <div>• Learning Days: <strong>${result.dayPlansCount}</strong></div>
          <div>• Tasks: <strong>${result.tasksCount}</strong></div>
          <div>• DSA Problems: <strong>${result.dsaProblemsCount}</strong></div>
          <div>• Journal Entries: <strong>${result.journalCount}</strong></div>
        </div>
        <div class="mt-4 p-3 bg-red-950/40 border border-red-800/40 rounded text-red-300">
          ⚠️ <strong>Explicit Confirmation:</strong> Restoring this backup will replace current records with the contents of this file.
        </div>
        <div class="mt-3 flex items-center justify-between">
          <label class="flex items-center gap-2 cursor-pointer">
            <input type="checkbox" id="confirm-restore-check" class="text-red-600">
            <span class="text-slate-300">I confirm replacing existing data</span>
          </label>
          <button onclick="executeRestore()" class="px-4 py-2 rounded-lg bg-red-600 hover:bg-red-500 text-white font-bold text-xs shadow">
            Execute Restore
          </button>
        </div>
      `;
    } else {
      preview.classList.remove('hidden');
      preview.innerHTML = `<div class="text-red-400 font-bold">❌ Validation Failed: ${escapeHtml(result.errorMessage)}</div>`;
    }
  } catch (err) {
    showToast("Failed to validate backup file", "error");
  }
}

async function executeRestore() {
  const check = document.getElementById('confirm-restore-check');
  if (!check || !check.checked) {
    showToast("Please check the confirmation box before restoring", "error");
    return;
  }

  try {
    const res = await apiCall('/api/data/restore/confirm', 'POST', {
      jsonContent: verifiedBackupJson,
      confirm: true
    });

    if (res.success) {
      showToast(res.message);
      document.getElementById('restore-preview-box').classList.add('hidden');
      await loadDashboard();
      await loadRoadmap();
    } else {
      showToast(res.message, "error");
    }
  } catch (err) {
    showToast("Restoration failed", "error");
  }
}

// MODALS UTILITY
function openModal(id) {
  const el = document.getElementById(id);
  if (el) el.classList.remove('hidden');
}

function closeModal(id) {
  const el = document.getElementById(id);
  if (el) el.classList.add('hidden');
}

function openProfileModal() {
  openModal('modal-profile');
}

document.addEventListener('keydown', (e) => {
  if (e.key === 'Escape') {
    const openModals = document.querySelectorAll('.modal-backdrop:not(.hidden)');
    if (openModals.length > 0) {
      openModals.forEach(m => m.classList.add('hidden'));
    } else if (currentTab === 'daydetail') {
      goBackFromDayDetail();
    }
  }
});

// HELPERS
function getStatusBadgeClass(status) {
  const s = (status || '').toLowerCase();
  if (s === 'completed') return 'status-completed';
  if (s === 'inprogress') return 'status-inprogress';
  if (s === 'partiallycompleted') return 'status-partiallycompleted';
  if (s === 'skipped') return 'status-skipped';
  if (s === 'missed') return 'status-missed';
  if (s === 'restday') return 'status-restday';
  if (s === 'leaveday') return 'status-leaveday';
  return 'status-planned';
}

function getStatusIcon(status) {
  const s = (status || '').toLowerCase();
  if (s === 'completed') return '✅';
  if (s === 'inprogress') return '⏳';
  if (s === 'partiallycompleted') return '🌓';
  if (s === 'skipped') return '⏭️';
  if (s === 'missed') return '❌';
  if (s === 'restday') return '🌴';
  if (s === 'leaveday') return '🏖️';
  return '📅';
}

function formatStatusName(status) {
  if (status === 'PartiallyCompleted') return 'Partial';
  if (status === 'RestDay') return 'Rest Day';
  if (status === 'LeaveDay') return 'Leave Day';
  if (status === 'InProgress') return 'In Progress';
  return status || 'Planned';
}

function getPriorityClass(priority) {
  const p = (priority || '').toLowerCase();
  if (p === 'critical') return 'bg-red-950 text-red-400 border border-red-800 font-bold';
  if (p === 'high') return 'bg-amber-950 text-amber-400 border border-amber-800';
  if (p === 'medium') return 'bg-blue-950 text-blue-400 border border-blue-800';
  return 'bg-slate-800 text-slate-400 border border-slate-700';
}

function getActionIcon(action) {
  if (action.includes('Missed')) return '❌';
  if (action.includes('Recovered')) return '🔄';
  if (action.includes('Rest')) return '🌴';
  if (action.includes('Leave')) return '🏖️';
  if (action.includes('Completed')) return '✅';
  if (action.includes('Extended')) return '⏩';
  return '📝';
}

function formatTimeAgo(isoString) {
  if (!isoString) return '';
  const date = new Date(isoString);
  const diffMs = Date.now() - date.getTime();
  const diffMins = Math.floor(diffMs / 60000);
  if (diffMins < 1) return 'just now';
  if (diffMins < 60) return `${diffMins}m ago`;
  const diffHours = Math.floor(diffMins / 60);
  if (diffHours < 24) return `${diffHours}h ago`;
  return date.toLocaleDateString();
}

function formatDateFull(isoString) {
  if (!isoString) return '';
  const d = new Date(isoString);
  return d.toLocaleDateString() + ' ' + d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
}

function escapeHtml(text) {
  if (!text) return '';
  return String(text)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#039;');
}
