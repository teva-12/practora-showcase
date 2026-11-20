/* =========================================================
   ShopElegance – site.js
   ========================================================= */
(function () {
  'use strict';

  /* ---- CSRF token helper for fetch() calls ----
     The server enforces AutoValidateAntiforgeryTokenAttribute globally on unsafe HTTP
     methods once an antiforgery cookie exists (which it does as soon as any page renders
     @Html.AntiForgeryToken(), see _Layout.cshtml). Regular <form> posts get this for free
     via the form tag helper; fetch()-based AJAX calls must attach it manually as the
     X-CSRF-TOKEN header (configured in Program.cs's AddAntiforgery options). */
  window.getCsrfToken = function () {
    var input = document.querySelector('input[name="__RequestVerificationToken"]');
    return input ? input.value : '';
  };

  /* ---- Theme ---- */
  const THEME_KEY = 'shopelegance-theme';
  function applyTheme(t) {
    document.documentElement.setAttribute('data-theme', t);
    const icon = document.querySelector('.theme-icon');
    if (icon) icon.textContent = t === 'dark' ? '☽' : '☀';
    localStorage.setItem(THEME_KEY, t);
  }
  const savedTheme = localStorage.getItem(THEME_KEY)
    || (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
  applyTheme(savedTheme);

  document.addEventListener('DOMContentLoaded', function () {

    /* ---- Theme Toggle ---- */
    document.querySelectorAll('.theme-toggle, #themeToggle').forEach(btn => {
      btn.addEventListener('click', () => {
        const current = document.documentElement.getAttribute('data-theme');
        applyTheme(current === 'dark' ? 'light' : 'dark');
      });
    });

    /* ---- Navbar scroll effect ---- */
    const nav = document.getElementById('mainNav');
    if (nav) {
      const onScroll = () => nav.classList.toggle('scrolled', window.scrollY > 10);
      window.addEventListener('scroll', onScroll, { passive: true });
      onScroll();
    }

    /* ---- Mobile nav toggle ---- */
    const toggler = document.getElementById('navToggler');
    const navMenu = document.getElementById('navMenu');
    if (toggler && navMenu) {
      toggler.addEventListener('click', () => navMenu.classList.toggle('open'));
      document.addEventListener('click', e => {
        if (!toggler.contains(e.target) && !navMenu.contains(e.target))
          navMenu.classList.remove('open');
      });
    }

    /* ---- Cart badge ---- */
    async function updateCartBadge() {
      try {
        const res = await fetch('/Cart/Count', { credentials: 'same-origin' });
        if (res.ok) {
          const data = await res.json();
          document.querySelectorAll('#cartCount').forEach(el => {
            el.textContent = data.count || 0;
            el.style.display = data.count > 0 ? 'flex' : 'none';
          });
        }
      } catch (_) { /* silent */ }
    }
    updateCartBadge();

    /* ---- AJAX Add to Cart ---- */
    document.querySelectorAll('.add-to-cart-form').forEach(form => {
      form.addEventListener('submit', async function (e) {
        e.preventDefault();
        const btn = form.querySelector('button[type=submit]');
        const original = btn.innerHTML;
        btn.innerHTML = '…'; btn.disabled = true;

        try {
          const res = await fetch(form.action, {
            method: 'POST',
            body: new FormData(form),
            headers: { 'X-Requested-With': 'XMLHttpRequest' },
            credentials: 'same-origin'
          });
          const data = await res.json();
          if (data.success) {
            showNotification(data.message || 'Added to cart!', 'success');
            updateCartBadge();
          } else {
            showNotification(data.message || 'Error adding to cart.', 'error');
          }
        } catch (_) {
          showNotification('Network error. Please try again.', 'error');
        } finally {
          btn.innerHTML = original; btn.disabled = false;
        }
      });
    });

    /* ---- Wishlist AJAX toggle ---- */
    document.querySelectorAll('.wishlist-toggle-form').forEach(form => {
      form.addEventListener('submit', async function (e) {
        e.preventDefault();
        try {
          const res = await fetch(form.action, {
            method: 'POST', body: new FormData(form),
            headers: { 'X-Requested-With': 'XMLHttpRequest' },
            credentials: 'same-origin'
          });
          const data = await res.json();
          if (data.success) {
            const btn = form.querySelector('button');
            if (btn) {
              btn.classList.toggle('in-wishlist', data.inWishlist);
              if (btn.dataset.labelSaved || btn.dataset.labelDefault) {
                btn.textContent = data.inWishlist
                  ? (btn.dataset.labelSaved || '❤ Saved')
                  : (btn.dataset.labelDefault || '♡ Wishlist');
              }
            }
            document.querySelectorAll('#wishlistCount').forEach(el => {
              el.textContent = data.count || 0;
              el.style.display = data.count > 0 ? 'flex' : 'none';
            });
            showNotification(data.inWishlist ? 'Added to wishlist!' : 'Removed from wishlist.', data.inWishlist ? 'success' : 'info');
          }
        } catch (_) { /* silent */ }
      });
    });

    /* ---- Cart quantity update (cart page) ---- */
    document.querySelectorAll('.cart-qty-input').forEach(input => {
      let timer;
      input.addEventListener('change', function () {
        clearTimeout(timer);
        timer = setTimeout(() => updateCartItem(this.dataset.itemId, this.value), 400);
      });
    });

    document.querySelectorAll('.cart-qty-btn').forEach(btn => {
      btn.addEventListener('click', function () {
        const input = this.closest('.cart-qty').querySelector('.cart-qty-input');
        const delta = this.dataset.delta === '-1' ? -1 : 1;
        const newVal = Math.max(1, parseInt(input.value || 1) + delta);
        input.value = newVal;
        updateCartItem(input.dataset.itemId, newVal);
      });
    });

    async function updateCartItem(itemId, qty) {
      try {
        const fd = new FormData();
        fd.append('cartItemId', itemId);
        fd.append('quantity', qty);
        const res = await fetch('/Cart/Update', {
          method: 'POST', body: fd,
          headers: { 'X-Requested-With': 'XMLHttpRequest', 'X-CSRF-TOKEN': window.getCsrfToken() },
          credentials: 'same-origin'
        });
        const data = await res.json();
        if (data.success) {
          updateCartBadge();
          const subEl = document.getElementById('cartSubTotal');
          if (subEl) subEl.textContent = data.subTotal;
          data.items?.forEach(item => {
            const el = document.querySelector(`[data-item-total="${item.id}"]`);
            if (el) el.textContent = item.totalPrice;
          });
        }
      } catch (_) { /* silent */ }
    }

    /* ---- Coupon apply ---- */
    const couponBtn = document.getElementById('applyCoupon');
    if (couponBtn) {
      couponBtn.addEventListener('click', async function () {
        const code = document.getElementById('couponCode').value.trim();
        const subTotalEl = document.getElementById('cartSubTotal');
        const subTotal = subTotalEl ? parseFloat(subTotalEl.dataset.raw || 0) : 0;
        if (!code) return showNotification('Please enter a coupon code.', 'error');

        const fd = new FormData();
        fd.append('couponCode', code);
        fd.append('orderTotal', subTotal);
        try {
          const res = await fetch('/Cart/ApplyCoupon', {
            method: 'POST', body: fd,
            headers: { 'X-Requested-With': 'XMLHttpRequest', 'X-CSRF-TOKEN': window.getCsrfToken() },
            credentials: 'same-origin'
          });
          const data = await res.json();
          showNotification(data.message, data.success ? 'success' : 'error');
          if (data.success) {
            const discountEl = document.getElementById('discountAmount');
            if (discountEl) discountEl.textContent = '-EGP ' + data.discountAmount.toFixed(2);
            const hiddenCode = document.getElementById('hiddenCouponCode');
            if (hiddenCode) hiddenCode.value = code;
            const hiddenDiscount = document.getElementById('hiddenDiscount');
            if (hiddenDiscount) hiddenDiscount.value = data.discountAmount;
          }
        } catch (_) { showNotification('Error applying coupon.', 'error'); }
      });
    }

    /* ---- Notification System v2 ---- */
    const TOAST_DURATION = 4500;
    const MAX_TOASTS = 4;
    const titles = { success: 'Success', error: 'Error', info: 'Notice', warning: 'Warning' };
    const icons = { success: '✓', error: '✕', info: 'ℹ', warning: '!' };

    function escapeHtml(str) {
      return String(str ?? '').replace(/[&<>"']/g, (ch) => ({
        '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
      }[ch]));
    }

    window.showNotification = function (message, type = 'info') {
      let container = document.querySelector('.notification-container');
      if (!container) {
        container = document.createElement('div');
        container.className = 'notification-container';
        document.body.appendChild(container);
      }

      // Cap stack: remove oldest if at limit
      const existing = container.querySelectorAll('.notification');
      if (existing.length >= MAX_TOASTS) dismissToast(existing[0]);

      const notif = document.createElement('div');
      notif.className = `notification ${type}`;
      notif.setAttribute('role', 'status');
      notif.innerHTML = `
        <div class="notification-icon">${icons[type] || 'ℹ'}</div>
        <div class="notification-body">
          <div class="notification-title">${titles[type] || 'Notice'}</div>
          <div class="notification-message">${escapeHtml(message)}</div>
        </div>
        <button type="button" class="notification-close" aria-label="Dismiss">✕</button>
        <div class="notification-progress" style="animation-duration:${TOAST_DURATION}ms"></div>
      `;
      container.appendChild(notif);

      let remaining = TOAST_DURATION;
      let startTime = Date.now();
      let timer = setTimeout(() => dismissToast(notif), remaining);

      notif.addEventListener('mouseenter', () => {
        notif.classList.add('paused');
        clearTimeout(timer);
        remaining -= (Date.now() - startTime);
      });
      notif.addEventListener('mouseleave', () => {
        notif.classList.remove('paused');
        startTime = Date.now();
        timer = setTimeout(() => dismissToast(notif), Math.max(remaining, 600));
      });
      notif.querySelector('.notification-close').addEventListener('click', () => {
        clearTimeout(timer);
        dismissToast(notif);
      });
    };

    function dismissToast(notif) {
      if (!notif || notif.classList.contains('hide')) return;
      notif.classList.add('hide');
      setTimeout(() => notif.remove(), 250);
    }

    /* ---- Flash messages from TempData (rendered as safe data-* attrs by _Notifications.cshtml) ---- */
    const ssrSuccess = document.getElementById('ssrSuccessMsg');
    if (ssrSuccess) showNotification(ssrSuccess.dataset.message, 'success');
    const ssrError = document.getElementById('ssrErrorMsg');
    if (ssrError) showNotification(ssrError.dataset.message, 'error');
    const ssrInfo = document.getElementById('ssrInfoMsg');
    if (ssrInfo) showNotification(ssrInfo.dataset.message, 'info');

    /* ---- Image lazy load enhancement ---- */
    if ('IntersectionObserver' in window) {
      const observer = new IntersectionObserver(entries => {
        entries.forEach(e => {
          if (e.isIntersecting) {
            const img = e.target;
            if (img.dataset.src) { img.src = img.dataset.src; observer.unobserve(img); }
          }
        });
      }, { rootMargin: '100px' });
      document.querySelectorAll('img[data-src]').forEach(img => observer.observe(img));
    }

    /* ---- Smooth anchor scroll ---- */
    document.querySelectorAll('a[href^="#"]').forEach(a => {
      a.addEventListener('click', e => {
        const target = document.querySelector(a.getAttribute('href'));
        if (target) { e.preventDefault(); target.scrollIntoView({ behavior: 'smooth', block: 'start' }); }
      });
    });

    /* ---- Auto-dismiss alerts ---- */
    document.querySelectorAll('.alert[data-auto-dismiss]').forEach(el => {
      setTimeout(() => {
        el.style.opacity = '0'; el.style.transition = '0.5s ease';
        setTimeout(() => el.remove(), 500);
      }, 4000);
    });

  }); // end DOMContentLoaded
})();
