/* =========================================================
   ShopElegance – admin.js
   ========================================================= */
(function () {
  'use strict';

  const THEME_KEY = 'shopelegance-theme';
  function applyTheme(t) {
    document.documentElement.setAttribute('data-theme', t);
    const icon = document.querySelector('#themeToggle');
    if (icon) icon.textContent = t === 'dark' ? '☽' : '☀';
    localStorage.setItem(THEME_KEY, t);
  }
  const saved = localStorage.getItem(THEME_KEY)
    || (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
  applyTheme(saved);

  document.addEventListener('DOMContentLoaded', function () {

    /* ---- Theme ---- */
    const themeBtn = document.getElementById('themeToggle');
    if (themeBtn) {
      themeBtn.addEventListener('click', () => {
        applyTheme(document.documentElement.getAttribute('data-theme') === 'dark' ? 'light' : 'dark');
      });
    }

    /* ---- Sidebar toggle (mobile) ---- */
    const sidebarToggle = document.getElementById('sidebarToggle');
    const sidebar = document.getElementById('adminSidebar');
    if (sidebarToggle && sidebar) {
      sidebarToggle.addEventListener('click', () => sidebar.classList.toggle('open'));
      document.addEventListener('click', e => {
        if (!sidebar.contains(e.target) && !sidebarToggle.contains(e.target))
          sidebar.classList.remove('open');
      });
    }

    /* ---- Auto-dismiss alerts ---- */
    document.querySelectorAll('.alert').forEach(el => {
      setTimeout(() => {
        el.style.opacity = '0'; el.style.transition = '0.5s ease';
        setTimeout(() => el.remove(), 500);
      }, 5000);
    });

    /* ---- Confirm delete actions ---- */
    document.querySelectorAll('[data-confirm]').forEach(el => {
      el.addEventListener('click', function (e) {
        if (!confirm(this.dataset.confirm || 'Are you sure?')) e.preventDefault();
      });
    });

    /* ---- Image preview for file inputs ---- */
    document.querySelectorAll('input[type=file][data-preview]').forEach(input => {
      input.addEventListener('change', function () {
        const previewContainer = document.getElementById(this.dataset.preview);
        if (!previewContainer) return;
        previewContainer.innerHTML = '';
        Array.from(this.files).forEach(file => {
          if (!file.type.startsWith('image/')) return;
          const reader = new FileReader();
          reader.onload = e => {
            const div = document.createElement('div');
            div.className = 'image-preview-item';
            div.innerHTML = `<img src="${e.target.result}" alt="Preview" />`;
            previewContainer.appendChild(div);
          };
          reader.readAsDataURL(file);
        });
      });
    });

    /* ---- Drag & drop image upload ---- */
    document.querySelectorAll('.image-upload-area').forEach(area => {
      area.addEventListener('dragover', e => { e.preventDefault(); area.style.borderColor = '#6c5ce7'; });
      area.addEventListener('dragleave', () => area.style.borderColor = '');
      area.addEventListener('drop', e => {
        e.preventDefault(); area.style.borderColor = '';
        const input = area.querySelector('input[type=file]');
        if (input) { input.files = e.dataTransfer.files; input.dispatchEvent(new Event('change')); }
      });
      area.addEventListener('click', () => area.querySelector('input[type=file]')?.click());
    });

    /* ---- Date range picker shortcut ---- */
    const fromDate = document.getElementById('fromDate');
    const toDate = document.getElementById('toDate');
    document.querySelectorAll('[data-range]').forEach(btn => {
      btn.addEventListener('click', function () {
        const now = new Date();
        const fmt = d => d.toISOString().split('T')[0];
        const range = this.dataset.range;
        if (range === 'today' && fromDate && toDate) { fromDate.value = toDate.value = fmt(now); }
        else if (range === 'week' && fromDate && toDate) {
          const start = new Date(now); start.setDate(now.getDate() - 7);
          fromDate.value = fmt(start); toDate.value = fmt(now);
        } else if (range === 'month' && fromDate && toDate) {
          fromDate.value = fmt(new Date(now.getFullYear(), now.getMonth(), 1));
          toDate.value = fmt(now);
        }
        this.closest('form')?.submit();
      });
    });

    /* ---- Table row click to navigate ---- */
    document.querySelectorAll('tr[data-href]').forEach(row => {
      row.style.cursor = 'pointer';
      row.addEventListener('click', function (e) {
        if (!e.target.closest('button, a, input')) window.location.href = this.dataset.href;
      });
    });

    /* ---- Select all checkbox ---- */
    const selectAll = document.getElementById('selectAll');
    if (selectAll) {
      selectAll.addEventListener('change', function () {
        document.querySelectorAll('.row-checkbox').forEach(cb => cb.checked = this.checked);
      });
    }

    /* ---- Character counter for textareas ---- */
    document.querySelectorAll('textarea[maxlength]').forEach(ta => {
      const counter = document.createElement('small');
      counter.className = 'char-counter help-text';
      const update = () => counter.textContent = `${ta.value.length} / ${ta.maxLength}`;
      ta.addEventListener('input', update);
      ta.parentNode.insertBefore(counter, ta.nextSibling);
      update();
    });

    /* ---- Slug auto-generate from product name ---- */
    const nameInput = document.getElementById('productName');
    const slugPreview = document.getElementById('slugPreview');
    if (nameInput && slugPreview) {
      nameInput.addEventListener('input', function () {
        const slug = this.value.toLowerCase()
          .replace(/[^a-z0-9\s-]/g, '')
          .replace(/\s+/g, '-').replace(/-+/g, '-');
        slugPreview.textContent = slug || '—';
      });
    }

    /* ---- Rich text editor init (using native contenteditable) ---- */
    document.querySelectorAll('[data-rich-editor]').forEach(textarea => {
      const editorId = textarea.dataset.richEditor;
      const editor = document.getElementById(editorId);
      if (!editor) return;
      editor.innerHTML = textarea.value || '';
      editor.addEventListener('input', () => textarea.value = editor.innerHTML);

      // Toolbar buttons
      document.querySelectorAll(`[data-editor="${editorId}"]`).forEach(toolBtn => {
        toolBtn.addEventListener('click', e => {
          e.preventDefault();
          document.execCommand(toolBtn.dataset.cmd, false, toolBtn.dataset.val || null);
          editor.focus();
          textarea.value = editor.innerHTML;
        });
      });
    });

  });
})();
