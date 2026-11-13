// OpsTrack UI (vanilla JS). Talks to the API under /api and sends the lab user headers.
'use strict';

const STATUS_LABEL = { Open: 'รอรับงาน', InProgress: 'กำลังซ่อม', Closed: 'ปิดแล้ว' };

const $ = (id) => document.getElementById(id);

function currentUser() {
  return { name: $('user-name').value.trim(), role: $('user-role').value };
}

function saveUser() {
  try {
    localStorage.setItem('opstrack.user', JSON.stringify(currentUser()));
  } catch (e) {
    // storage may be blocked; the page still works without it
  }
}

function loadUser() {
  try {
    const saved = JSON.parse(localStorage.getItem('opstrack.user') || 'null');
    if (saved && saved.name) {
      $('user-name').value = saved.name;
      $('user-role').value = saved.role;
    }
  } catch (e) {
    // ignore broken storage
  }
}

function showMessage(text, kind) {
  const box = $('message');
  box.textContent = text;
  box.className = 'message ' + (kind || '');
  box.hidden = !text;
}

async function api(path, options) {
  const user = currentUser();
  const headers = Object.assign({ 'Accept': 'application/json' }, (options && options.headers) || {});
  if (user.name) {
    headers['X-User'] = user.name;
    headers['X-User-Role'] = user.role;
  }
  const res = await fetch(path, Object.assign({}, options, { headers }));
  const text = await res.text();
  const body = text ? JSON.parse(text) : null;
  if (!res.ok) {
    const detail = body && (body.detail || body.title);
    const err = new Error(detail || ('HTTP ' + res.status));
    err.status = res.status;
    throw err;
  }
  return body;
}

function formatDateTime(value) {
  if (!value) return '';
  return value.replace('T', ' ').substring(0, 16);
}

function escapeHtml(value) {
  return String(value == null ? '' : value)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

// ---------- header ----------

async function loadMeta() {
  try {
    const meta = await api('/api/meta');
    $('meta').textContent = 'วันผลิต ' + meta.productionDate + ' กะ ' + meta.shift + ' (' + meta.environment + ')';
  } catch (e) {
    $('meta').textContent = '';
  }
}

// ---------- equipment ----------

async function loadEquipment() {
  const items = await api('/api/equipment');
  const select = $('equipment');
  const lineFilter = $('filter-line');
  const lines = new Map();
  select.innerHTML = '';
  for (const eq of items) {
    if (!lines.has(eq.lineCode)) lines.set(eq.lineCode, eq.lineName);
    const opt = document.createElement('option');
    opt.value = eq.code;
    opt.textContent = eq.code + ' ' + eq.name;
    select.appendChild(opt);
  }
  for (const [code, name] of lines) {
    const opt = document.createElement('option');
    opt.value = code;
    opt.textContent = code + ' ' + name;
    lineFilter.appendChild(opt);
  }
}

// ---------- work requests ----------

async function loadWorkRequests() {
  const params = new URLSearchParams({ pageSize: '100' });
  if ($('filter-status').value) params.set('status', $('filter-status').value);
  if ($('filter-line').value) params.set('line', $('filter-line').value);

  const result = await api('/api/work-requests?' + params.toString());
  const tbody = $('work-request-rows');
  tbody.innerHTML = '';
  for (const wr of result.items) {
    const tr = document.createElement('tr');
    tr.dataset.number = wr.number;
    tr.innerHTML =
      '<td>' + escapeHtml(wr.number) + '</td>' +
      '<td>' + escapeHtml(wr.equipmentCode) + '</td>' +
      '<td title="' + escapeHtml(wr.description) + '">' + escapeHtml(wr.title) + '</td>' +
      '<td>' + escapeHtml(wr.priority) + '</td>' +
      '<td><span class="badge ' + escapeHtml(wr.status) + '">' + escapeHtml(STATUS_LABEL[wr.status] || wr.status) + '</span></td>' +
      '<td>' + escapeHtml(wr.reportedBy) + '</td>' +
      '<td>' + escapeHtml(wr.assignedTo) + '</td>' +
      '<td>' + escapeHtml(formatDateTime(wr.createdAt)) + '</td>' +
      '<td class="' + (wr.isOverdue ? 'overdue' : '') + '">' + escapeHtml(formatDateTime(wr.dueAt)) + (wr.isOverdue ? ' เกิน SLA' : '') + '</td>' +
      '<td class="actions"></td>';

    const actions = tr.querySelector('.actions');
    if (wr.status === 'Open') {
      actions.appendChild(actionButton('รับงาน', () => assign(wr)));
    }
    if (wr.status !== 'Closed') {
      actions.appendChild(actionButton('ปิดงาน', () => closeRequest(wr)));
    }
    tbody.appendChild(tr);
  }
  $('list-count').textContent = 'แสดง ' + result.items.length + ' จาก ' + result.total + ' รายการ';
}

function actionButton(label, onClick) {
  const btn = document.createElement('button');
  btn.type = 'button';
  btn.textContent = label;
  btn.addEventListener('click', async () => {
    btn.disabled = true;
    try {
      await onClick();
    } finally {
      btn.disabled = false;
    }
  });
  return btn;
}

async function createRequest(event) {
  event.preventDefault();
  const payload = {
    equipmentCode: $('equipment').value,
    title: $('title').value,
    description: $('description').value,
    priority: $('priority').value,
  };
  try {
    const created = await api('/api/work-requests', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload),
    });
    showMessage('บันทึกงานซ่อม ' + created.number + ' แล้ว', 'ok');
    $('title').value = '';
    $('description').value = '';
    await loadWorkRequests();
  } catch (e) {
    showMessage('แจ้งซ่อมไม่สำเร็จ: ' + e.message, 'error');
  }
}

async function assign(wr) {
  try {
    await api('/api/work-requests/' + wr.id + '/assign', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ assignedTo: '' }),
    });
    showMessage('รับงาน ' + wr.number + ' แล้ว', 'ok');
    await loadWorkRequests();
  } catch (e) {
    showMessage('รับงานไม่สำเร็จ: ' + e.message, 'error');
  }
}

async function closeRequest(wr) {
  if (!confirm('ยืนยันปิดงาน ' + wr.number + ' ?')) return;
  try {
    await api('/api/work-requests/' + wr.id + '/close', { method: 'POST' });
    showMessage('ปิดงาน ' + wr.number + ' แล้ว', 'ok');
    await loadWorkRequests();
  } catch (e) {
    showMessage('ปิดงานไม่สำเร็จ: ' + e.message, 'error');
  }
}

// ---------- report ----------

function defaultReportRange() {
  const now = new Date();
  const pad = (n) => String(n).padStart(2, '0');
  const first = new Date(now.getFullYear(), now.getMonth(), 1);
  const last = new Date(now.getFullYear(), now.getMonth() + 1, 0);
  const iso = (d) => d.getFullYear() + '-' + pad(d.getMonth() + 1) + '-' + pad(d.getDate());
  $('report-from').value = iso(first);
  $('report-to').value = iso(last);
}

async function loadReport(event) {
  if (event) event.preventDefault();
  const from = $('report-from').value;
  const to = $('report-to').value;
  console.info('[report] loading summary', { from, to });

  const rows = await api('/api/reports/summary?from=' + encodeURIComponent(from) + '&to=' + encodeURIComponent(to));
  console.info('[report] received ' + rows.length + ' rows');

  const tbody = $('report-rows');
  tbody.innerHTML = '';
  for (const r of rows) {
    const tr = document.createElement('tr');
    tr.innerHTML =
      '<td>' + escapeHtml(r.lineCode) + '</td>' +
      '<td>' + escapeHtml(r.lineName) + '</td>' +
      '<td>' + r.total + '</td>' +
      '<td>' + r.open + '</td>' +
      '<td>' + r.inProgress + '</td>' +
      '<td>' + r.closed + '</td>';
    tbody.appendChild(tr);
  }

  $('report-empty').hidden = rows.length > 0;
  if (rows.length === 0) {
    console.warn('[report] no data for range', from, 'to', to);
  }
}

// ---------- wiring ----------

function switchView(name) {
  for (const btn of document.querySelectorAll('.tab')) {
    btn.classList.toggle('active', btn.dataset.view === name);
  }
  $('view-requests').hidden = name !== 'requests';
  $('view-report').hidden = name !== 'report';
  showMessage('');
  if (name === 'report') {
    loadReport().catch((e) => showMessage('โหลดรายงานไม่สำเร็จ: ' + e.message, 'error'));
  }
}

async function init() {
  loadUser();
  $('user-name').addEventListener('change', saveUser);
  $('user-role').addEventListener('change', saveUser);
  $('create-form').addEventListener('submit', createRequest);
  $('refresh').addEventListener('click', () => loadWorkRequests());
  $('filter-status').addEventListener('change', () => loadWorkRequests());
  $('filter-line').addEventListener('change', () => loadWorkRequests());
  $('report-form').addEventListener('submit', (e) =>
    loadReport(e).catch((err) => showMessage('โหลดรายงานไม่สำเร็จ: ' + err.message, 'error')));
  for (const btn of document.querySelectorAll('.tab')) {
    btn.addEventListener('click', () => switchView(btn.dataset.view));
  }
  defaultReportRange();

  try {
    await Promise.all([loadMeta(), loadEquipment()]);
    await loadWorkRequests();
  } catch (e) {
    showMessage('โหลดข้อมูลไม่สำเร็จ: ' + e.message, 'error');
  }
}

document.addEventListener('DOMContentLoaded', init);
