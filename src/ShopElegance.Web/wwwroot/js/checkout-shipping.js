(function () {
  'use strict';

  document.addEventListener('DOMContentLoaded', function () {
    var select = document.querySelector('[data-shipping-select]');
    var cityInput = document.getElementById('cityInput');
    if (!select) return;

    var costDisplay = document.querySelector('[data-shipping-cost-display]');
    var hiddenCost = document.getElementById('hiddenShippingCost');
    var etaNote = document.querySelector('[data-shipping-eta]');
    var freeLocalNote = document.getElementById('freeLocalDeliveryNote');
    var subTotalEl = document.getElementById('cartSubTotal');
    var discountEl = document.getElementById('hiddenDiscount');

    function formatCurrency(amount) {
      return 'EGP ' + amount.toFixed(2);
    }

    function fetchShipping() {
      var governorate = select.value;
      if (!governorate) return;

      var formData = new FormData();
      formData.append('governorate', governorate);
      formData.append('city', cityInput ? cityInput.value : '');

      fetch('/Checkout/CalculateShipping', {
        method: 'POST',
        body: formData,
        headers: { 'X-Requested-With': 'XMLHttpRequest', 'X-CSRF-TOKEN': window.getCsrfToken() },
        credentials: 'same-origin'
      })
        .then(function (r) { return r.json(); })
        .then(function (data) {
          if (!data.success) {
            if (etaNote) { etaNote.textContent = data.message || ''; etaNote.style.color = '#c0392b'; }
            return;
          }

          if (costDisplay) costDisplay.textContent = data.shippingCostFormatted;
          if (hiddenCost) hiddenCost.value = data.shippingCost;
          if (etaNote) {
            etaNote.style.color = '';
            etaNote.textContent = data.shippingCost === 0
              ? 'Free shipping · estimated ' + data.estimatedDeliveryDays + ' day(s)'
              : 'Estimated delivery: ' + data.estimatedDeliveryDays + ' day(s)';
          }
          if (freeLocalNote) {
            freeLocalNote.style.display = data.isFreeLocalDelivery ? 'flex' : 'none';
          }

          recalculateTotal(data.shippingCost);
        })
        .catch(function () {
          if (etaNote) { etaNote.textContent = 'Could not calculate shipping. Please try again.'; etaNote.style.color = '#c0392b'; }
        });
    }

    select.addEventListener('change', fetchShipping);

    // Free local delivery depends on the City text field too, so recalculate
    // shortly after the customer finishes typing there (debounced).
    if (cityInput) {
      var cityDebounce;
      cityInput.addEventListener('input', function () {
        clearTimeout(cityDebounce);
        cityDebounce = setTimeout(function () {
          if (select.value) fetchShipping();
        }, 500);
      });
    }

    function recalculateTotal(shippingCost) {
      if (!subTotalEl) return;
      var subTotal = parseFloat(subTotalEl.dataset.raw || '0');
      var discount = discountEl ? parseFloat(discountEl.value || '0') : 0;
      // No tax — total is simply subtotal - discount + shipping.
      var total = subTotal - discount + shippingCost;

        var totalEl = document.getElementById('grandTotalDisplay') || document.querySelector('.summary-row.total span:last-child');
      if (totalEl) totalEl.textContent = formatCurrency(total);
    }
  });
})();
