(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', init);

    function init() {
        var root = document.querySelector('[data-wc-customizer]');
        if (!root) return;

        var dataEl = document.getElementById('wc-data');
        if (!dataEl) return;

        var data;
        try {
            data = JSON.parse(dataEl.textContent);
        } catch (e) {
            console.error('White Coat customizer: could not parse product data.', e);
            return;
        }

        var state = {
            productId: data.productId,
            basePrice: data.basePrice,
            variants: data.variants || [],
            fonts: (data.fonts || []).filter(function (f) { return f.isActive; }),
            logos: (data.logos || []).filter(function (l) { return l.isActive; }),
            selected: { size: null, color: null, fabric: null, sleeve: null, gender: null, fit: null },
            currentVariant: null,
            previewTimer: null,
            uploadedLogoUrl: null
        };

        var els = {
            size: root.querySelector('#wc-size'),
            color: root.querySelector('#wc-color'),
            fabric: root.querySelector('#wc-fabric'),
            sleeve: root.querySelector('#wc-sleeve'),
            gender: root.querySelector('#wc-gender'),
            fit: root.querySelector('#wc-fit'),
            embroideryToggle: root.querySelector('[data-wc-embroidery-toggle]'),
            embroideryFields: root.querySelector('[data-wc-embroidery-fields]'),
            font: root.querySelector('#wc-font'),
            logoUploadInput: root.querySelector('[data-wc-logo-upload]'),
            customLogoPreview: root.querySelector('[data-wc-custom-logo-preview]'),
            baseImage: root.querySelector('[data-wc-base-image]'),
            logoImage: root.querySelector('[data-wc-logo-image]'),
            textOverlay: root.querySelector('[data-wc-text-overlay]'),
            previewLoading: root.querySelector('[data-wc-preview-loading]'),
            gallery: root.querySelector('[data-wc-gallery]'),
            finalPrice: root.querySelector('[data-wc-final-price]'),
            feeNote: root.querySelector('[data-wc-fee-note]'),
            stockMessage: root.querySelector('[data-wc-stock-message]'),
            quantity: root.querySelector('[data-wc-quantity]'),
            addToCartBtn: root.querySelector('[data-wc-add-to-cart]')
        };

        populateFontSelect(els.font, state.fonts);
        populateLogoSelect(els.logoCatalogSelect, state.logos, null);

        // Map hardcoded fields to state
        ['coatType', 'size', 'fabric', 'color'].forEach(function (field) {
            var el = root.querySelector('[data-wc-field="' + field + '"]');
            if (!el) return;
            state.selected[field] = el.value;
            el.addEventListener('change', function () {
                state.selected[field] = el.value;
                state.currentVariant = findMatchingVariant();
                onSelectionChanged();
            });
        });

        els.embroideryToggle.addEventListener('change', function () {
            toggle(els.embroideryFields, els.embroideryToggle.checked);
            onSelectionChanged();
        });

        root.querySelectorAll('[data-wc-embroidery-fields] [data-wc-field], [data-wc-embroidery-fields] select').forEach(function (el) {
            el.addEventListener('input', debounced(onSelectionChanged));
            el.addEventListener('change', onSelectionChanged);
        });

        // Removed logo type selection logic
        els.logoUploadInput.addEventListener('change', function () {
            var file = els.logoUploadInput.files && els.logoUploadInput.files[0];
            if (!file) return;
            uploadCustomLogo(file);
        });

        els.quantity.addEventListener('change', onSelectionChanged);
        els.addToCartBtn.addEventListener('click', handleAddToCart);

        // Initial render
        state.currentVariant = findMatchingVariant();
        onSelectionChanged();

        // ---- subtle parallax tilt on the preview stage (pseudo-3D depth, not a real 3D model) ----
        var tiltStage = root.querySelector('[data-wc-tilt-stage]');
        if (tiltStage && window.matchMedia('(hover: hover) and (pointer: fine)').matches) {
            tiltStage.addEventListener('mousemove', function (e) {
                var rect = tiltStage.getBoundingClientRect();
                var px = (e.clientX - rect.left) / rect.width;   // 0..1
                var py = (e.clientY - rect.top) / rect.height;   // 0..1
                var maxTilt = 9; // degrees
                var rotateY = (px - 0.5) * 2 * maxTilt;
                var rotateX = (0.5 - py) * 2 * maxTilt;
                tiltStage.style.setProperty('--wc-tilt-x', rotateY.toFixed(2) + 'deg');
                tiltStage.style.setProperty('--wc-tilt-y', rotateX.toFixed(2) + 'deg');
                tiltStage.style.setProperty('--wc-tilt-scale', '1.03');
            });
            tiltStage.addEventListener('mouseleave', function () {
                tiltStage.style.setProperty('--wc-tilt-x', '0deg');
                tiltStage.style.setProperty('--wc-tilt-y', '0deg');
                tiltStage.style.setProperty('--wc-tilt-scale', '1');
            });
        }

        // ---- functions ----

        function distinctValues(variants, key) {
            var seen = {};
            var result = [];
            variants.forEach(function (v) {
                var val = v[key];
                if (!seen[val]) { seen[val] = true; result.push(val); }
            });
            return result;
        }

        function populateAttributeSelect(select, values, attr) {
            if (!select) return;
            select.innerHTML = '';
            values.forEach(function (v) {
                var opt = document.createElement('option');
                opt.value = v;
                opt.textContent = humanize(v);
                select.appendChild(opt);
            });
        }

        function humanize(value) {
            if (!value) return value;
            return value.replace(/([a-z])([A-Z])/g, '$1 $2');
        }

        function populateFontSelect(select, fonts) {
            if (!select) return;
            select.innerHTML = '';
            var placeholder = document.createElement('option');
            placeholder.value = '';
            placeholder.textContent = '— Select a font —';
            select.appendChild(placeholder);
            fonts.forEach(function (f) {
                var opt = document.createElement('option');
                opt.value = f.id;
                opt.textContent = f.name;
                opt.style.fontFamily = f.cssFontFamily;
                select.appendChild(opt);
            });
        }

        function populateLogoSelect(select, logos, type) {
            if (!select) return;
            select.innerHTML = '';
            var placeholder = document.createElement('option');
            placeholder.value = '';
            placeholder.textContent = '— Select a logo —';
            select.appendChild(placeholder);
            logos
                .filter(function (l) { return !type || l.type === type; })
                .forEach(function (l) {
                    var opt = document.createElement('option');
                    opt.value = l.id;
                    opt.textContent = l.name;
                    select.appendChild(opt);
                });
        }

        function toggle(el, show) {
            if (!el) return;
            el.classList.toggle('wc-hidden', !show);
        }

        function findMatchingVariant() {
            var s = state.selected;
            return state.variants.find(function (v) {
                return v.size === s.size && v.color === s.color && v.fabric === s.fabric &&
                    v.sleeve === s.sleeve && v.gender === s.gender && v.fit === s.fit;
            }) || null;
        }

        function updateGallery(variant) {
            if (!els.gallery) return;
            els.gallery.innerHTML = '';
            var images = (variant && variant.images) || [];
            if (!images.length) {
                var fallback = (data.product && data.product.primaryImageUrl) || '/images/default-coat.jpg';
                els.baseImage.src = fallback;
                els.baseImage.alt = 'Preview image';
                return;
            }
            var primary = images.find(function (i) { return i.isPrimary; }) || images[0];
            els.baseImage.src = primary.url;
            els.baseImage.alt = variant.size + ' ' + variant.color + ' variant';

            images.forEach(function (img) {
                var thumb = document.createElement('img');
                thumb.src = img.url;
                thumb.alt = img.altText || '';
                thumb.className = 'wc-thumb' + (img.url === primary.url ? ' active' : '');
                thumb.tabIndex = 0;
                thumb.setAttribute('role', 'listitem');
                thumb.addEventListener('click', function () {
                    els.baseImage.src = img.url;
                    els.gallery.querySelectorAll('.wc-thumb').forEach(function (t) { t.classList.remove('active'); });
                    thumb.classList.add('active');
                });
                els.gallery.appendChild(thumb);
            });
        }

        function updateStockMessage(variant) {
            if (!variant) {
                show(els.stockMessage, 'Please select a size, color, fabric, sleeve, gender, and fit to see availability.');
                els.addToCartBtn.disabled = true;
                return;
            }
            if (!variant.isActive || variant.isOutOfStock) {
                show(els.stockMessage, 'This exact combination is currently out of stock.');
                els.addToCartBtn.disabled = true;
            } else if (variant.isLowStock) {
                show(els.stockMessage, 'Only ' + variant.stockQuantity + ' left in stock — order soon!');
                els.addToCartBtn.disabled = false;
            } else {
                hide(els.stockMessage);
                els.addToCartBtn.disabled = false;
            }
        }

        function show(el, text) {
            if (!el) return;
            el.textContent = text;
            el.classList.remove('wc-hidden');
        }
        function hide(el) {
            if (!el) return;
            el.classList.add('wc-hidden');
        }

        function buildCustomizationPayload() {
            var studentName = val('#wc-student-name');
            var doctorName = val('#wc-doctor-name');
            var faculty = val('#wc-faculty');
            var department = val('#wc-department');
            var gradYear = val('#wc-grad-year');
            var fontId = val('#wc-font');
            var textColor = val('#wc-text-color');
            var position = val('#wc-position');

            // Treat as enabled if ANY field has content, regardless of the visual toggle
            var embroideryEnabled = els.embroideryToggle.checked || !!(studentName || doctorName || faculty || department || gradYear || fontId || (textColor && textColor !== '#1a1a1a') || position);

            return {
                whiteCoatVariantId: state.currentVariant ? state.currentVariant.id : 0,
                embroideryEnabled: embroideryEnabled,
                studentName: studentName,
                doctorName: doctorName,
                faculty: faculty,
                department: department,
                graduationYear: gradYear ? parseInt(gradYear, 10) : null,
                embroideryTextColor: textColor,
                embroideryFontId: fontId ? parseInt(fontId, 10) : null,
                embroideryPosition: position,
                logoType: 'Custom',
                customLogoUrl: state.uploadedLogoUrl,
                coatType: val('[data-wc-field="coatType"]'),
                size: val('[data-wc-field="size"]'),
                fabric: val('[data-wc-field="fabric"]'),
                color: val('[data-wc-field="color"]'),
                requiresSpecialPackaging: root.querySelector('#wc-special-packaging').checked,
                isFragile: root.querySelector('#wc-is-fragile').checked
            };
        }

        function val(selector) {
            var el = root.querySelector(selector);
            return el ? el.value : null;
        }

        function clientSideValidate(payload) {
            clearErrors();
            var valid = true;

            if (payload.embroideryEnabled) {
                if (!payload.studentName && !payload.doctorName) {
                    setError('name', 'Enter a student name or doctor name.');
                    valid = false;
                }
                if (!payload.embroideryFontId) {
                    setError('font', 'Please choose a font.');
                    valid = false;
                }
                if (!payload.embroideryPosition) {
                    setError('position', 'Please choose a position.');
                    valid = false;
                }
            }

            if (payload.logoType === 'University' || payload.logoType === 'Hospital') {
                if (!payload.logoId) {
                    setError('logo', 'Please choose a logo.');
                    valid = false;
                }
            }
            if (payload.logoType === 'Custom' && !payload.customLogoUrl) {
                setError('customLogo', 'Please upload a logo image.');
                valid = false;
            }

            return valid;
        }

        function setError(key, message) {
            var el = root.querySelector('[data-wc-error="' + key + '"]');
            if (el) { el.textContent = message; el.classList.remove('wc-hidden'); }
        }
        function clearErrors() {
            root.querySelectorAll('[data-wc-error]').forEach(function (el) { el.classList.add('wc-hidden'); });
        }

        function onSelectionChanged() {
            var payload = buildCustomizationPayload();
            
            // Check all embroidery fields for content, regardless of toggle
            var hasEmbroidery = val('#wc-student-name') || val('#wc-doctor-name') || val('#wc-faculty') || 
                val('#wc-department') || val('#wc-grad-year') || val('#wc-font') || 
                (val('#wc-text-color') && val('#wc-text-color') !== '#1a1a1a') || val('#wc-position');
            
            var hasLogo = (payload.logoType === 'Custom' && payload.customLogoUrl) || 
                          (payload.logoId && payload.logoId > 0);
            
            var isCustomized = hasEmbroidery || hasLogo;
            
            var customizationFee = isCustomized ? 150 : 0;
            var total = state.basePrice + customizationFee;
            
            els.finalPrice.textContent = formatCurrency(total);
            
            // Toggle the unified fee row
            var feeRow = root.querySelector('[data-wc-customization-fee-row]');
            toggle(feeRow, isCustomized);
            
            debouncedPreviewFetch();
        }

        function debounced(fn) {
            var t;
            return function () {
                clearTimeout(t);
                t = setTimeout(fn, 300);
            };
        }

        var previewDebounceTimer = null;
        function debouncedPreviewFetch() {
            clearTimeout(previewDebounceTimer);
            previewDebounceTimer = setTimeout(fetchPreview, 250);
        }

        function fetchPreview() {
            var payload = buildCustomizationPayload();

            if (!state.currentVariant) {
                var hasEmbroidery = payload.studentName || payload.doctorName || payload.faculty || 
                    payload.department || payload.graduationYear || payload.embroideryFontId || 
                    (payload.embroideryTextColor && payload.embroideryTextColor !== '#1a1a1a') || payload.embroideryPosition;
                var hasLogo = payload.customLogoUrl || (payload.logoId && payload.logoId > 0);
                var isCustomized = hasEmbroidery || hasLogo;
                var total = state.basePrice + (isCustomized ? 150 : 0);
                
                els.finalPrice.textContent = formatCurrency(total);
                
                toggle(els.logoImage, false);
                toggle(els.textOverlay, false);
                return;
            }

            show(els.previewLoading, 'Updating preview…');

            fetch('/WhiteCoat/Preview', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': window.getCsrfToken() },
                body: JSON.stringify({
                    whiteCoatVariantId: payload.whiteCoatVariantId,
                    embroideryEnabled: payload.embroideryEnabled,
                    studentName: payload.studentName,
                    doctorName: payload.doctorName,
                    faculty: payload.faculty,
                    department: payload.department,
                    graduationYear: payload.graduationYear,
                    embroideryTextColor: payload.embroideryTextColor,
                    embroideryFontId: payload.embroideryFontId,
                    embroideryPosition: payload.embroideryPosition,
                    logoType: payload.logoType,
                    logoId: payload.logoId,
                    customLogoUrl: payload.customLogoUrl
                })
            })
                .then(function (r) { return r.json(); })
                .then(function (result) {
                    hide(els.previewLoading);
                    if (!result.success) return;

                    els.finalPrice.textContent = result.finalPriceFormatted;
                    els.finalPrice.classList.remove('wc-price-pulse');
                    void els.finalPrice.offsetWidth; // restart animation
                    els.finalPrice.classList.add('wc-price-pulse');
                    if (result.customizationFee > 0) {
                        show(els.feeNote, '(includes ' + result.customizationFeeFormatted + ' customization fee)');
                    } else {
                        hide(els.feeNote);
                    }

                    if (result.logoImageUrl) {
                        els.logoImage.src = result.logoImageUrl;
                        els.logoImage.className = 'wc-preview-logo wc-logo-pos-' + (result.embroideryPosition || 'LeftChest').toLowerCase();
                        toggle(els.logoImage, true);
                    } else {
                        toggle(els.logoImage, false);
                    }

                    var name = payload.studentName || payload.doctorName;
                    if (payload.embroideryEnabled && name) {
                        els.textOverlay.textContent = name;
                        els.textOverlay.style.color = payload.embroideryTextColor || '#1a1a1a';
                        var font = state.fonts.find(function (f) { return f.id === payload.embroideryFontId; });
                        els.textOverlay.style.fontFamily = font ? font.cssFontFamily : 'inherit';
                        els.textOverlay.className = 'wc-preview-text wc-logo-pos-' + (payload.embroideryPosition || 'LeftChest').toLowerCase();
                        toggle(els.textOverlay, true);
                    } else {
                        toggle(els.textOverlay, false);
                    }
                })
                .catch(function () { hide(els.previewLoading); });
        }

        function uploadCustomLogo(file) {
            var formData = new FormData();
            formData.append('file', file);

            show(els.previewLoading, 'Uploading logo…');
            fetch('/WhiteCoat/UploadLogo', {
                method: 'POST',
                body: formData,
                headers: { 'X-CSRF-TOKEN': window.getCsrfToken() }
            })
                .then(function (r) { return r.json(); })
                .then(function (result) {
                    hide(els.previewLoading);
                    if (!result.success) {
                        setError('customLogo', result.message || 'Could not upload that image.');
                        return;
                    }
                    state.uploadedLogoUrl = result.url;
                    els.customLogoPreview.src = result.url;
                    toggle(els.customLogoPreview, true);
                    onSelectionChanged();
                })
                .catch(function () {
                    hide(els.previewLoading);
                    setError('customLogo', 'Upload failed. Please try again.');
                });
        }

        function formatCurrency(n) {
            return 'EGP ' + Number(n).toFixed(2);
        }

        function handleAddToCart() {
            var payload = buildCustomizationPayload();
            if (!clientSideValidate(payload)) return;

            els.addToCartBtn.disabled = true;
            els.addToCartBtn.textContent = 'Adding…';

            fetch('/WhiteCoat/AddToCart', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': window.getCsrfToken() },
                body: JSON.stringify({
                    productId: state.productId,
                    quantity: parseInt(els.quantity.value, 10) || 1,
                    customization: payload
                })
            })
                .then(function (r) { return r.json(); })
                .then(function (result) {
                    els.addToCartBtn.disabled = false;
                    els.addToCartBtn.textContent = '🧵 Add Customized Coat to Cart';

                    if (!result.success) {
                        (result.errors || []).forEach(function (e) {
                            setError(mapFieldToErrorKey(e.field), e.message);
                        });
                        if (!result.errors || !result.errors.length) {
                            setError('general', 'Something went wrong. Please try again.');
                        }
                        return;
                    }

                    var cartCountEl = document.getElementById('cartCount');
                    if (cartCountEl) {
                        cartCountEl.textContent = result.itemCount;
                        cartCountEl.style.display = result.itemCount > 0 ? 'flex' : 'none';
                    }

                    setError('general', '');
                    show(els.stockMessage, (result.message || 'Added to cart!') +
                        ' You can keep customizing another coat, or head to your cart to check out.');
                    els.stockMessage.classList.remove('wc-alert-error');
                    els.stockMessage.classList.add('wc-alert-success');

                    var goToCartLink = document.getElementById('wcGoToCart');
                    if (!goToCartLink && els.stockMessage) {
                        goToCartLink = document.createElement('a');
                        goToCartLink.id = 'wcGoToCart';
                        goToCartLink.href = '/Cart';
                        goToCartLink.className = 'btn btn-primary';
                        goToCartLink.style.marginTop = '12px';
                        goToCartLink.style.display = 'inline-block';
                        goToCartLink.textContent = 'Go to Cart & Checkout →';
                        els.stockMessage.insertAdjacentElement('afterend', goToCartLink);
                    }
                })
                .catch(function () {
                    els.addToCartBtn.disabled = false;
                    els.addToCartBtn.textContent = '🧵 Add Customized Coat to Cart';
                    setError('general', 'Network error. Please try again.');
                });
        }

        function mapFieldToErrorKey(field) {
            var map = {
                StudentName: 'name', DoctorName: 'name', EmbroideryFontId: 'font',
                EmbroideryPosition: 'position', LogoId: 'logo', CustomLogoUrl: 'customLogo',
                WhiteCoatVariantId: 'variant'
            };
            return map[field] || 'general';
        }
    }
})();