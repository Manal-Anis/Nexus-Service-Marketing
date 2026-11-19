/* =============================================================================================
   NEXUS SERVICE MARKETING SYSTEM - site wide behaviour
   ============================================================================================= */
(function () {
    "use strict";

    const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    /* --------------------------------------------------------------- reveal on scroll */
    function initReveal() {
        const items = document.querySelectorAll("[data-reveal]");
        if (!items.length) return;

        if (reducedMotion || !("IntersectionObserver" in window)) {
            items.forEach(el => el.classList.add("revealed"));
            return;
        }

        const observer = new IntersectionObserver(entries => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const delay = parseInt(entry.target.dataset.reveal || "0", 10);
                    setTimeout(() => entry.target.classList.add("revealed"), delay);
                    observer.unobserve(entry.target);
                }
            });
        }, { threshold: 0.12, rootMargin: "0px 0px -60px 0px" });

        items.forEach(el => observer.observe(el));
    }

    /* --------------------------------------------------------------- animated counters */
    function animateCounter(el) {
        const target = parseFloat(el.dataset.count);
        const suffix = el.dataset.suffix || "";
        const decimals = parseInt(el.dataset.decimals || "0", 10);
        const duration = parseInt(el.dataset.duration || "1800", 10);

        if (isNaN(target)) return;

        if (reducedMotion) {
            el.textContent = target.toFixed(decimals) + suffix;
            return;
        }

        const start = performance.now();
        function frame(now) {
            const progress = Math.min((now - start) / duration, 1);
            const eased = 1 - Math.pow(1 - progress, 3);
            el.textContent = (target * eased).toFixed(decimals) + suffix;
            if (progress < 1) requestAnimationFrame(frame);
        }
        requestAnimationFrame(frame);
    }

    function initCounters() {
        const counters = document.querySelectorAll(".counter");
        if (!counters.length) return;

        if (!("IntersectionObserver" in window)) {
            counters.forEach(animateCounter);
            return;
        }

        const observer = new IntersectionObserver(entries => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    animateCounter(entry.target);
                    observer.unobserve(entry.target);
                }
            });
        }, { threshold: 0.4 });

        counters.forEach(el => observer.observe(el));
    }

    /* --------------------------------------------------------------- progress bars */
    function initProgressBars() {
        const bars = document.querySelectorAll(".progress-fill[data-value]");
        if (!bars.length) return;

        const apply = bar => { bar.style.width = Math.min(parseFloat(bar.dataset.value) || 0, 100) + "%"; };

        if (!("IntersectionObserver" in window)) {
            bars.forEach(apply);
            return;
        }

        const observer = new IntersectionObserver(entries => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    setTimeout(() => apply(entry.target), 120);
                    observer.unobserve(entry.target);
                }
            });
        }, { threshold: 0.3 });

        bars.forEach(el => observer.observe(el));
    }

    /* --------------------------------------------------------------- glass tabs */
    function initTabs() {
        document.querySelectorAll("[data-tab-group]").forEach(group => {
            const buttons = group.querySelectorAll("[data-tab-target]");
            const panes = document.querySelectorAll("[data-tab-pane]");

            buttons.forEach(button => {
                button.addEventListener("click", () => {
                    const target = button.dataset.tabTarget;

                    buttons.forEach(b => b.classList.remove("active"));
                    button.classList.add("active");

                    panes.forEach(pane => {
                        if (pane.dataset.tabPane === target) {
                            pane.classList.add("active");
                            pane.style.display = "";
                        } else {
                            pane.classList.remove("active");
                            pane.style.display = "none";
                        }
                    });

                    try { sessionStorage.setItem("tab:" + target, "1"); } catch (e) { /* ignore */ }
                });
            });
        });
    }

    /* --------------------------------------------------------------- FAQ accordion */
    function initFaq() {
        document.querySelectorAll(".faq-question").forEach(question => {
            question.addEventListener("click", () => {
                const answer = question.nextElementSibling;
                const open = question.getAttribute("aria-expanded") === "true";

                document.querySelectorAll(".faq-question").forEach(other => {
                    if (other !== question) {
                        other.setAttribute("aria-expanded", "false");
                        if (other.nextElementSibling) other.nextElementSibling.style.maxHeight = null;
                    }
                });

                question.setAttribute("aria-expanded", open ? "false" : "true");
                if (answer) {
                    answer.style.maxHeight = open ? null : answer.scrollHeight + "px";
                }
            });
        });
    }

    /* --------------------------------------------------------------- toasts */
    const ICONS = {
        success: "bi-check-circle-fill",
        error: "bi-x-circle-fill",
        warning: "bi-exclamation-triangle-fill",
        info: "bi-info-circle-fill"
    };

    function showToast(message, type) {
        type = type || "success";

        let stack = document.querySelector(".toast-stack");
        if (!stack) {
            stack = document.createElement("div");
            stack.className = "toast-stack";
            document.body.appendChild(stack);
        }

        const toast = document.createElement("div");
        toast.className = "toast-nexus " + type;
        toast.setAttribute("role", "status");

        const icon = document.createElement("i");
        icon.className = "toast-icon bi " + (ICONS[type] || ICONS.info);

        const body = document.createElement("div");
        body.textContent = message;

        toast.appendChild(icon);
        toast.appendChild(body);
        stack.appendChild(toast);

        setTimeout(() => {
            toast.classList.add("hide");
            setTimeout(() => toast.remove(), 350);
        }, 4200);
    }

    window.nexusToast = showToast;

    /* --------------------------------------------------------------- password toggle */
    function initPasswordToggles() {
        document.querySelectorAll(".password-toggle").forEach(button => {
            button.addEventListener("click", () => {
                const input = button.parentElement.querySelector("input");
                if (!input) return;
                const showing = input.type === "text";
                input.type = showing ? "password" : "text";
                button.querySelector("i").className = showing ? "bi bi-eye" : "bi bi-eye-slash";
            });
        });
    }

    /* --------------------------------------------------------------- ripple on buttons */
    function initRipple() {
        document.addEventListener("click", event => {
            const button = event.target.closest(".btn-gold, .btn-silver");
            if (!button || reducedMotion) return;

            const rect = button.getBoundingClientRect();
            const size = Math.max(rect.width, rect.height);
            const ripple = document.createElement("span");
            ripple.className = "ripple";
            ripple.style.width = ripple.style.height = size + "px";
            ripple.style.left = (event.clientX - rect.left - size / 2) + "px";
            ripple.style.top = (event.clientY - rect.top - size / 2) + "px";
            button.appendChild(ripple);
            setTimeout(() => ripple.remove(), 620);
        });
    }

    /* --------------------------------------------------------------- 3D tilt */
    function initTilt() {
        if (reducedMotion || !window.matchMedia("(hover: hover)").matches) return;

        document.querySelectorAll(".tilt").forEach(card => {
            card.addEventListener("mousemove", event => {
                const rect = card.getBoundingClientRect();
                const x = (event.clientX - rect.left) / rect.width - 0.5;
                const y = (event.clientY - rect.top) / rect.height - 0.5;
                card.style.transform = "perspective(900px) rotateX(" + (-y * 7) + "deg) rotateY(" + (x * 9) + "deg) translateZ(6px)";
            });
            card.addEventListener("mouseleave", () => { card.style.transform = ""; });
        });
    }

    /* --------------------------------------------------------------- back to top */
    function initBackToTop() {
        const button = document.querySelector(".back-to-top");
        if (!button) return;

        window.addEventListener("scroll", () => {
            button.classList.toggle("show", window.scrollY > 420);
        }, { passive: true });

        button.addEventListener("click", () => window.scrollTo({ top: 0, behavior: reducedMotion ? "auto" : "smooth" }));
    }

    /* --------------------------------------------------------------- sidebar toggle (mobile) */
    function initSidebarToggle() {
        document.querySelectorAll("[data-sidebar-toggle]").forEach(button => {
            button.addEventListener("click", () => {
                const sidebar = document.querySelector(".glass-sidebar");
                if (sidebar) sidebar.classList.toggle("open");
            });
        });

        document.querySelectorAll(".sidebar-overlay").forEach(overlay => {
            overlay.addEventListener("click", () => {
                const sidebar = document.querySelector(".glass-sidebar");
                if (sidebar) sidebar.classList.remove("open");
            });
        });
    }

    /* --------------------------------------------------------------- confirm modals */
    function initConfirmForms() {
        document.querySelectorAll("form[data-confirm]").forEach(form => {
            form.addEventListener("submit", event => {
                if (form.dataset.confirmed === "yes") {
                    form.dataset.confirmed = "";
                    return;
                }

                event.preventDefault();
                const message = form.dataset.confirm || "Are you sure?";
                if (window.confirm(message)) {
                    form.dataset.confirmed = "yes";
                    form.submit();
                }
            });
        });
    }

    /* --------------------------------------------------------------- loading button state */
    function initLoadingButtons() {
        document.querySelectorAll("form").forEach(form => {
            form.addEventListener("submit", () => {
                const button = form.querySelector("button[type=submit]");
                if (!button || button.dataset.busy === "1") return;

                button.dataset.busy = "1";
                const original = button.innerHTML;
                button.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>' +
                    (button.dataset.busyText || "Please wait");
                button.disabled = true;

                setTimeout(() => {
                    button.disabled = false;
                    button.innerHTML = original;
                    button.dataset.busy = "0";
                }, 12000);
            });
        });
    }

    /* --------------------------------------------------------------- order wizard */
    function initStepper() {
        document.querySelectorAll("[data-stepper]").forEach(stepper => {
            // The scope holds the steps, the panes and the navigation buttons. It is the stepper
            // itself when the markup wraps the form, and the form when the buttons sit inside it.
            const scope = stepper.closest("form") ?? stepper;
            const steps = Array.from(scope.querySelectorAll(".step"));
            const panes = Array.from(scope.querySelectorAll(".step-pane"));
            let index = 0;

            function render() {
                steps.forEach((step, i) => {
                    step.classList.toggle("active", i === index);
                    step.classList.toggle("done", i < index);
                });
                panes.forEach((pane, i) => pane.classList.toggle("active", i === index));

                const submit = scope.querySelector("[data-step-submit]");
                if (submit) {
                    submit.classList.toggle("d-none", index !== panes.length - 1);
                }
            }

            scope.addEventListener("click", event => {
                const next = event.target.closest("[data-step-next]");
                const prev = event.target.closest("[data-step-prev]");
                if (!next && !prev) return;

                if (next) {
                    const pane = panes[index];
                    if (pane && !validatePane(pane)) return;
                    index = Math.min(index + 1, panes.length - 1);
                } else {
                    index = Math.max(index - 1, 0);
                }
                render();
            });

            render();
        });
    }

    function validatePane(pane) {
        let valid = true;
        pane.querySelectorAll("input[required], select[required], textarea[required]").forEach(field => {
            if (!field.checkValidity()) {
                field.classList.add("is-invalid");
                field.reportValidity();
                valid = false;
            } else {
                field.classList.remove("is-invalid");
            }
        });
        return valid;
    }

    /* --------------------------------------------------------------- live order summary */
    function initWizardSummary() {
        document.querySelectorAll("[data-wizard-root]").forEach(root => {
            const quantity = root.querySelector('[data-wizard="quantity-input"]');
            const equipment = root.querySelector('[data-wizard="equipment-input"]');
            if (!quantity) return;

            const depositUnit = parseFloat(root.dataset.wizardDeposit || "0");
            const priceUnit = parseFloat(root.dataset.wizardPrice || "0");
            const taxPercent = parseFloat(root.dataset.wizardTax || "0");

            let tiers = [];
            try {
                tiers = JSON.parse(root.dataset.wizardTiers || "[]");
            } catch (error) {
                tiers = [];
            }

            function write(name, value) {
                root.querySelectorAll('[data-wizard="' + name + '"]').forEach(node => {
                    if (node.tagName === "INPUT" || node.tagName === "SELECT" || node.tagName === "TEXTAREA") return;
                    node.textContent = value;
                });
            }

            function money(value) {
                return value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
            }

            function refresh() {
                const count = Math.max(1, parseInt(quantity.value || "1", 10) || 1);

                let unitPrice = 0;
                if (equipment && equipment.selectedOptions.length > 0) {
                    unitPrice = parseFloat(equipment.selectedOptions[0].dataset.price || "0");
                }

                // The plan can change while the form is open, therefore the unit prices are read
                // from the data attributes on every run.
                const deposit = parseFloat(root.dataset.wizardDeposit || String(depositUnit)) || 0;
                const price = parseFloat(root.dataset.wizardPrice || String(priceUnit)) || 0;
                const taxRate = parseFloat(root.dataset.wizardTax || String(taxPercent)) || 0;

                const grossDeposit = deposit * count;
                const grossAdvance = price * count;
                const equipmentAmount = unitPrice * count;

                const tier = tiers.find(t => count >= t.min && (t.max === null || count <= t.max));
                const percent = tier ? tier.pct : 0;
                const factor = 1 - percent / 100;

                const netDeposit = grossDeposit * factor;
                const netAdvance = grossAdvance * factor;
                const taxable = netDeposit + netAdvance + equipmentAmount;
                const tax = taxable * taxRate / 100;
                const total = taxable + tax;

                write("quantity", String(count));
                write("gross-deposit", money(grossDeposit));
                write("gross-advance", money(grossAdvance));
                write("discount", String(Math.round(percent)));
                write("deposit", money(netDeposit));
                write("advance", money(netAdvance));
                write("equipment", money(equipmentAmount));
                write("tax", money(tax));
                write("total", money(total));
                write("total-round", Math.round(total).toLocaleString());
            }

            quantity.addEventListener("input", refresh);
            if (equipment) {
                equipment.addEventListener("change", refresh);
            }
            refresh();
        });
    }

    /* --------------------------------------------------------------- charts */
    const CHART_COLORS = {
        gold: "#d4af37",
        goldLight: "#f3df8a",
        goldDeep: "#8a6d1d",
        silver: "#c9ccd3",
        silverMuted: "#8b8d97",
        grid: "rgba(255,255,255,0.07)"
    };

    function chartGradient(ctx, area, from, to) {
        const gradient = ctx.createLinearGradient(0, area.top, 0, area.bottom);
        gradient.addColorStop(0, from);
        gradient.addColorStop(1, to);
        return gradient;
    }

    /**
     * Creates a Chart.js chart that uses the black / gold / silver palette of the theme.
     * The caller only supplies the data, the defaults are merged in here.
     */
    window.nexusChart = function (target, config) {
        if (!target || !window.Chart) return null;

        const canvas = typeof target === "string" ? document.getElementById(target) : target;
        if (!canvas) return null;

        const merged = Object.assign({}, config);
        merged.options = Object.assign({
            responsive: true,
            maintainAspectRatio: false,
            interaction: { mode: "index", intersect: false },
            plugins: {
                legend: {
                    labels: { color: CHART_COLORS.silver, font: { family: "Inter, sans-serif", size: 12 }, boxWidth: 10, usePointStyle: true, padding: 16 }
                },
                tooltip: {
                    backgroundColor: "rgba(14,14,18,0.95)",
                    borderColor: "rgba(212,175,55,0.35)",
                    borderWidth: 1,
                    titleColor: "#f3df8a",
                    bodyColor: "#e8e9ed",
                    padding: 10,
                    cornerRadius: 8
                }
            },
            scales: {
                x: { ticks: { color: CHART_COLORS.silverMuted, font: { family: "Inter, sans-serif" } }, grid: { color: CHART_COLORS.grid, drawBorder: false } },
                y: { ticks: { color: CHART_COLORS.silverMuted, font: { family: "Inter, sans-serif" } }, grid: { color: CHART_COLORS.grid, drawBorder: false }, beginAtZero: true }
            }
        }, config.options || {});

        return new window.Chart(canvas, merged);
    };

    window.nexusChartColors = CHART_COLORS;
    window.nexusChartGradient = chartGradient;

    /* --------------------------------------------------------------- auto submit filters */
    function initAutoSubmit() {
        document.querySelectorAll("[data-auto-submit]").forEach(element => {
            element.addEventListener("change", () => {
                const form = element.closest("form");
                if (form) form.submit();
            });
        });
    }

    /* --------------------------------------------------------------- boot */
    document.addEventListener("DOMContentLoaded", () => {
        initReveal();
        initCounters();
        initProgressBars();
        initTabs();
        initFaq();
        initPasswordToggles();
        initRipple();
        initTilt();
        initBackToTop();
        initSidebarToggle();
        initConfirmForms();
        initLoadingButtons();
        initStepper();
        initWizardSummary();
        initAutoSubmit();
    });
})();
