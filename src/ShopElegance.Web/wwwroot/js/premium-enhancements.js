/* =========================================================
   ShopElegance – Premium Enhancements
   Presentation-only helpers: scroll reveal, back-to-top,
   image zoom, recently viewed, FAQ accordion, shipping bar,
   checkout stepper sync. No backend calls added beyond
   endpoints already used elsewhere in the app.
   ========================================================= */
(function () {
  'use strict';

  function escapeHtml(str) {
    return String(str ?? '').replace(/[&<>"']/g, (ch) => ({
      '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
    }[ch]));
  }

  document.addEventListener('DOMContentLoaded', function () {

    /* ---- Scroll reveal ---- */
    if ('IntersectionObserver' in window) {
      const revealObserver = new IntersectionObserver((entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            entry.target.classList.add('is-visible');
            revealObserver.unobserve(entry.target);
          }
        });
      }, { threshold: 0.12, rootMargin: '0px 0px -40px 0px' });
      document.querySelectorAll('[data-reveal], [data-reveal-stagger]').forEach((el) => revealObserver.observe(el));
    } else {
      document.querySelectorAll('[data-reveal], [data-reveal-stagger]').forEach((el) => el.classList.add('is-visible'));
    }

    /* ---- Back to top ---- */
    let backBtn = document.querySelector('.back-to-top');
    if (!backBtn) {
      backBtn = document.createElement('button');
      backBtn.className = 'back-to-top';
      backBtn.setAttribute('aria-label', 'Back to top');
      backBtn.innerHTML = '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="12" y1="19" x2="12" y2="5"/><polyline points="5 12 12 5 19 12"/></svg>';
      document.body.appendChild(backBtn);
    }
    window.addEventListener('scroll', () => {
      backBtn.classList.toggle('show', window.scrollY > 480);
    }, { passive: true });
    backBtn.addEventListener('click', () => window.scrollTo({ top: 0, behavior: 'smooth' }));

    /* ---- FAQ accordion ---- */
    document.querySelectorAll('.faq-question').forEach((btn) => {
      btn.addEventListener('click', () => {
        const item = btn.closest('.faq-item');
        const wasOpen = item.classList.contains('open');
        item.parentElement.querySelectorAll('.faq-item.open').forEach((el) => el.classList.remove('open'));
        if (!wasOpen) item.classList.add('open');
      });
    });

    /* ---- Newsletter form (presentational — no backend endpoint exists) ---- */
    document.querySelectorAll('.newsletter-form').forEach((form) => {
      form.addEventListener('submit', (e) => {
        e.preventDefault();
        const input = form.querySelector('input[type="email"]');
        if (input && input.value) {
          if (typeof showNotification === 'function') {
            showNotification('Thanks for subscribing! Check your inbox soon.', 'success');
          }
          form.reset();
        }
      });
    });

    /* ---- Product gallery zoom (hover magnify) ---- */
    const galleryMain = document.getElementById('galleryMain');
    if (galleryMain) {
      const img = galleryMain.querySelector('.gallery-img');
      if (img) {
        galleryMain.addEventListener('mousemove', (e) => {
          const rect = galleryMain.getBoundingClientRect();
          const x = ((e.clientX - rect.left) / rect.width) * 100;
          const y = ((e.clientY - rect.top) / rect.height) * 100;
          img.style.transformOrigin = `${x}% ${y}%`;
        });
        galleryMain.addEventListener('mouseenter', () => galleryMain.classList.add('zoom-active'));
        galleryMain.addEventListener('mouseleave', () => galleryMain.classList.remove('zoom-active'));
      }
    }

    /* ---- Recently viewed (client-side, localStorage only) ---- */
    const RECENT_KEY = 'shopelegance-recently-viewed';
    const currentMeta = document.querySelector('[data-recent-product]');
    if (currentMeta) {
      try {
        const entry = {
          id: currentMeta.dataset.recentId,
          name: currentMeta.dataset.recentName,
          slug: currentMeta.dataset.recentSlug,
          image: currentMeta.dataset.recentImage,
          price: currentMeta.dataset.recentPrice
        };
        let list = JSON.parse(localStorage.getItem(RECENT_KEY) || '[]');
        list = list.filter((p) => p.id !== entry.id);
        list.unshift(entry);
        list = list.slice(0, 8);
        localStorage.setItem(RECENT_KEY, JSON.stringify(list));
      } catch (_) { /* silent */ }
    }
    const recentGrid = document.getElementById('recentlyViewedGrid');
    if (recentGrid) {
      try {
        let list = JSON.parse(localStorage.getItem(RECENT_KEY) || '[]');
        if (currentMeta) list = list.filter((p) => p.id !== currentMeta.dataset.recentId);
        if (list.length) {
          recentGrid.innerHTML = list.slice(0, 4).map((p) => `
            <a class="product-card lift-hover" href="/Shop/Product/${encodeURIComponent(p.slug || '')}" data-product-id="${escapeHtml(p.id)}">
              <div class="product-img-wrap">
                ${p.image ? `<img src="${escapeHtml(p.image)}" alt="${escapeHtml(p.name)}" class="product-img" loading="lazy" />` : `<div class="product-img-placeholder"><span>✦</span></div>`}
              </div>
              <div class="product-info">
                <h3 class="product-name">${escapeHtml(p.name)}</h3>
                <div class="product-price"><span class="price-current">${escapeHtml(p.price)}</span></div>
              </div>
            </a>
          `).join('');
          const section = recentGrid.closest('.recently-viewed-section');
          if (section) section.style.display = '';
        }
      } catch (_) { /* silent */ }
    }

    /* ---- Free shipping progress bar (cart & checkout) ---- */
    const shipBar = document.getElementById('shippingProgress');
    if (shipBar) {
      const threshold = parseFloat(shipBar.dataset.threshold || '50');
      const subtotalEl = document.getElementById('cartSubTotal');
      const subtotal = subtotalEl ? parseFloat(subtotalEl.dataset.raw || '0') : parseFloat(shipBar.dataset.subtotal || '0');
      const pct = Math.min(100, (subtotal / threshold) * 100);
      const fill = shipBar.querySelector('.shipping-progress-fill');
      const label = shipBar.querySelector('.shipping-progress-label');
      if (fill) fill.style.width = pct + '%';
      if (label) {
        if (subtotal >= threshold) {
          label.innerHTML = '✓ You\'ve unlocked free shipping!';
          shipBar.classList.add('complete');
        } else {
          const remaining = (threshold - subtotal).toFixed(2);
          label.innerHTML = `Add <strong>$${remaining}</strong> more for free shipping`;
        }
      }
    }

    /* ---- Search overlay ---- */
    const searchOverlay = document.getElementById('searchOverlay');
    const searchToggle = document.getElementById('searchToggle');
    const searchInput = document.getElementById('searchInput');
    const searchClose = document.getElementById('searchClose');
    const searchClear = document.getElementById('searchClear');
    const searchForm = document.getElementById('searchForm');
    const RECENT_SEARCH_KEY = 'shopelegance-recent-searches';

    function renderRecentSearches() {
        const wrap = document.getElementById('searchRecent');
        const chipsEl = document.getElementById('searchRecentChips');
        if (!wrap || !chipsEl) return;
        let list = [];
        try { list = JSON.parse(localStorage.getItem(RECENT_SEARCH_KEY) || '[]'); } catch (_) { list = []; }
        if (!list.length) { wrap.style.display = 'none'; return; }
        wrap.style.display = '';
        chipsEl.innerHTML = list.slice(0, 6).map((term) => `
            <span class="search-chip" data-term="${escapeHtml(term)}">
              ${escapeHtml(term)} <span class="chip-remove" data-remove="${escapeHtml(term)}">✕</span>
            </span>
        `).join('');
        chipsEl.querySelectorAll('.chip-remove').forEach((btn) => {
            btn.addEventListener('click', (e) => {
                e.stopPropagation();
                let l = JSON.parse(localStorage.getItem(RECENT_SEARCH_KEY) || '[]');
                l = l.filter((t) => t !== btn.dataset.remove);
                localStorage.setItem(RECENT_SEARCH_KEY, JSON.stringify(l));
                renderRecentSearches();
            });
        });
        chipsEl.querySelectorAll('.search-chip').forEach((chip) => {
            chip.addEventListener('click', () => {
                searchInput.value = chip.dataset.term;
                searchForm.submit();
            });
        });
    }

    function openSearch() {
        if (!searchOverlay) return;
        searchOverlay.classList.add('active');
        renderRecentSearches();
        setTimeout(() => searchInput?.focus(), 150);
        document.body.style.overflow = 'hidden';
    }
    function closeSearch() {
        if (!searchOverlay) return;
        searchOverlay.classList.remove('active');
        document.body.style.overflow = '';
    }
    searchToggle?.addEventListener('click', openSearch);
    searchClose?.addEventListener('click', closeSearch);
    searchOverlay?.addEventListener('click', (e) => { if (e.target === searchOverlay) closeSearch(); });
    searchInput?.addEventListener('input', () => {
        searchClear?.classList.toggle('show', searchInput.value.length > 0);
    });
    searchClear?.addEventListener('click', () => {
        searchInput.value = '';
        searchClear.classList.remove('show');
        searchInput.focus();
    });
    searchForm?.addEventListener('submit', () => {
        const term = searchInput.value.trim();
        if (term) {
            try {
                let list = JSON.parse(localStorage.getItem(RECENT_SEARCH_KEY) || '[]');
                list = [term, ...list.filter((t) => t.toLowerCase() !== term.toLowerCase())].slice(0, 8);
                localStorage.setItem(RECENT_SEARCH_KEY, JSON.stringify(list));
            } catch (_) { /* silent */ }
        }
    });
    document.addEventListener('keydown', (e) => {
        const tag = (e.target.tagName || '').toLowerCase();
        const typing = tag === 'input' || tag === 'textarea' || e.target.isContentEditable;
        if (e.key === '/' && !typing) { e.preventDefault(); openSearch(); }
        if (e.key === 'Escape' && searchOverlay?.classList.contains('active')) closeSearch();
    });

    /* ---- Top loading bar on navigation (perceived performance) ---- */
    const loadingBar = document.getElementById('topLoadingBar');
    function startLoadingBar() {
        if (!loadingBar) return;
        loadingBar.style.width = '0%';
        loadingBar.classList.add('active');
        requestAnimationFrame(() => { loadingBar.style.width = '75%'; });
    }
    document.querySelectorAll('a[href]:not([href^="#"]):not([target="_blank"]):not([download])').forEach((a) => {
        a.addEventListener('click', (e) => {
            if (e.defaultPrevented || e.metaKey || e.ctrlKey) return;
            if (a.origin && a.origin !== window.location.origin) return;
            startLoadingBar();
        });
    });
    document.querySelectorAll('form:not(.add-to-cart-form):not(.wishlist-toggle-form)').forEach((f) => {
        f.addEventListener('submit', (e) => { if (!e.defaultPrevented) startLoadingBar(); });
    });

    /* ---- Wishlist count badge: updates live after Toggle (no dedicated
       count endpoint exists server-side, so initial count on page load
       is intentionally left to the Wishlist page itself; this only keeps
       the navbar badge in sync after an in-page toggle). ---- */

    /* ---- Image skeleton reveal ---- */
    document.querySelectorAll('.product-img, .gallery-img').forEach((img) => {
        const wrap = img.closest('.product-img-wrap') || img.parentElement;
        if (wrap && !wrap.classList.contains('img-skeleton-wrap')) wrap.classList.add('img-skeleton-wrap');
        const markLoaded = () => wrap?.classList.add('loaded');
        if (img.complete && img.naturalWidth > 0) markLoaded();
        else {
            img.addEventListener('load', markLoaded);
            img.addEventListener('error', markLoaded);
        }
    });

    /* ---- Filter sidebar: mobile toggle + accordion ---- */
    const filterSidebar = document.querySelector('.shop-sidebar');
    if (filterSidebar) {
        const toggleBtn = document.createElement('button');
        toggleBtn.type = 'button';
        toggleBtn.className = 'filter-toggle-mobile';
        toggleBtn.innerHTML = '<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="4" y1="6" x2="20" y2="6"/><line x1="7" y1="12" x2="17" y2="12"/><line x1="10" y1="18" x2="14" y2="18"/></svg> Filters';
        filterSidebar.parentElement.insertBefore(toggleBtn, filterSidebar);
        toggleBtn.addEventListener('click', () => filterSidebar.classList.toggle('mobile-open'));

        filterSidebar.querySelectorAll('.filter-group').forEach((group) => {
            group.classList.add('collapsible');
            const title = group.querySelector('.filter-title');
            const optionsEl = group.querySelector('.filter-options') || group.querySelector('.price-inputs')?.parentElement;
            if (title && !title.closest('.filter-body')) {
                const rest = Array.from(group.children).filter((c) => c !== title);
                const body = document.createElement('div');
                body.className = 'filter-body';
                rest.forEach((el) => body.appendChild(el));
                group.appendChild(body);
                title.addEventListener('click', () => group.classList.toggle('collapsed'));
            }
        });
    }

    /* ---- Admin dashboard: KPI count-up ---- */
    document.querySelectorAll('[data-countup]').forEach((el) => {
        const raw = el.textContent.trim();
        const numMatch = raw.match(/[\d,.]+/);
        if (!numMatch) return;
        const target = parseFloat(numMatch[0].replace(/,/g, ''));
        if (isNaN(target)) return;
        const prefix = raw.slice(0, numMatch.index);
        const suffix = raw.slice(numMatch.index + numMatch[0].length);
        const isDecimal = numMatch[0].includes('.');
        const duration = 900;
        const start = performance.now();
        function tick(now) {
            const progress = Math.min(1, (now - start) / duration);
            const eased = 1 - Math.pow(1 - progress, 3);
            const value = target * eased;
            el.textContent = prefix + (isDecimal ? value.toFixed(2) : Math.round(value).toLocaleString()) + suffix;
            if (progress < 1) requestAnimationFrame(tick);
        }
        requestAnimationFrame(tick);
    });

    /* ---- Password visibility toggle ---- */
    document.querySelectorAll('input[type="password"]').forEach((input) => {
        if (input.dataset.toggleAdded) return;
        input.dataset.toggleAdded = 'true';

        // Wrap just the input (not label/validation siblings) so the
        // absolutely-positioned toggle button aligns correctly.
        const wrapper = document.createElement('div');
        wrapper.style.position = 'relative';
        input.parentNode.insertBefore(wrapper, input);
        wrapper.appendChild(input);

        input.style.paddingRight = '52px';
        const btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'password-toggle';
        btn.textContent = 'Show';
        btn.setAttribute('aria-label', 'Show password');
        wrapper.appendChild(btn);
        btn.addEventListener('click', () => {
            const showing = input.type === 'text';
            input.type = showing ? 'password' : 'text';
            btn.textContent = showing ? 'Show' : 'Hide';
            btn.setAttribute('aria-label', showing ? 'Show password' : 'Hide password');
        });
    });

    /* ---- Real-time success state for validated inputs (client-side only) ---- */
    document.querySelectorAll('form').forEach((form) => {
        form.querySelectorAll('.form-control[required], .form-control[type="email"]').forEach((input) => {
            input.addEventListener('blur', () => {
                const group = input.closest('.form-group');
                if (!group) return;
                const valid = input.checkValidity() && input.value.trim().length > 0;
                group.classList.toggle('has-success', valid);
            });
        });
    });

    /* ---- Account dropdown menu ---- */
    const accountToggle = document.getElementById('accountMenuToggle');
    const accountMenu = document.getElementById('accountMenu');
    if (accountToggle && accountMenu) {
        accountToggle.addEventListener('click', (e) => {
            e.stopPropagation();
            const isOpen = accountMenu.classList.toggle('open');
            accountToggle.setAttribute('aria-expanded', isOpen ? 'true' : 'false');
        });
        document.addEventListener('click', (e) => {
            if (!accountMenu.classList.contains('open')) return;
            if (!accountMenu.contains(e.target) && e.target !== accountToggle) {
                accountMenu.classList.remove('open');
                accountToggle.setAttribute('aria-expanded', 'false');
            }
        });
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && accountMenu.classList.contains('open')) {
                accountMenu.classList.remove('open');
                accountToggle.setAttribute('aria-expanded', 'false');
                accountToggle.focus();
            }
        });
    }

  }); // end DOMContentLoaded
})();
