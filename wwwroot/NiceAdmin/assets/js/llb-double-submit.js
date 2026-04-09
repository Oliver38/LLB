(() => {
  "use strict";

  if (window.__llbDoubleSubmitInitialized === true) {
    return;
  }

  window.__llbDoubleSubmitInitialized = true;

  const actionCooldownMs = 1800;
  const lockedClassName = "llb-action-locked";
  const submitSelector = 'button[type="submit"], input[type="submit"], input[type="image"]';
  const actionSelector = 'button, input[type="button"], input[type="submit"], input[type="image"], a.btn, .btn';
  const protectionSelector = "[data-prevent-double-submit='true']";

  const shouldAllowMultiple = (element) => {
    return element?.closest("[data-allow-multiple-click='true'], [data-allow-multiple-submit='true']") !== null;
  };

  const shouldProtect = (element) => {
    return element?.matches(protectionSelector) || element?.closest(protectionSelector) !== null;
  };

  const isBootstrapToggle = (element) => {
    return element.hasAttribute("data-bs-toggle")
      || element.hasAttribute("data-bs-dismiss")
      || element.getAttribute("role") === "tab";
  };

  const isSubmitControl = (element) => {
    if (!element) {
      return false;
    }

    if (element.matches('input[type="submit"], input[type="image"]')) {
      return true;
    }

    if (element.tagName === "BUTTON") {
      const type = (element.getAttribute("type") || "").trim().toLowerCase();
      return type === "" || type === "submit";
    }

    return false;
  };

  const shouldIgnoreActionLock = (element) => {
    if (!element || shouldAllowMultiple(element) || element.disabled) {
      return true;
    }

    if (isBootstrapToggle(element)) {
      return true;
    }

    if (element.tagName === "BUTTON") {
      const type = (element.getAttribute("type") || "").trim().toLowerCase();
      if (type === "reset") {
        return true;
      }

      if ((type === "" || type === "submit") && element.form) {
        return true;
      }
    }

    const href = element.getAttribute("href");
    if (element.tagName === "A" && (!href || href === "#" || href.startsWith("#") || href.startsWith("javascript:"))) {
      return true;
    }

    return false;
  };

  const setLockedState = (element, locked) => {
    if (!element) {
      return;
    }

    element.classList.toggle(lockedClassName, locked);

    if (locked) {
      element.setAttribute("aria-disabled", "true");
    } else {
      element.removeAttribute("aria-disabled");
    }

    if (element.tagName === "BUTTON" || element.tagName === "INPUT") {
      if (locked) {
        if (!element.disabled) {
          element.dataset.llbTemporaryDisabled = "true";
          element.disabled = true;
        }
      } else if (element.dataset.llbTemporaryDisabled === "true") {
        element.disabled = false;
        delete element.dataset.llbTemporaryDisabled;
      }
    }
  };

  const clearActionLock = (element) => {
    if (!element) {
      return;
    }

    delete element.dataset.llbClickLocked;
    delete element.dataset.llbClickToken;
    setLockedState(element, false);
  };

  const lockActionTemporarily = (element, clickToken) => {
    if (!element) {
      return;
    }

    element.dataset.llbClickLocked = "true";
    element.dataset.llbClickToken = clickToken;
    setLockedState(element, true);
    window.setTimeout(() => clearActionLock(element), actionCooldownMs);
  };

  const getAssociatedSubmitControls = (form) => {
    const controls = new Set(Array.from(form.querySelectorAll(submitSelector)));

    if (form.id) {
      document.querySelectorAll(`${submitSelector}[form="${form.id}"]`).forEach((element) => controls.add(element));
    }

    return Array.from(controls);
  };

  const disableFormSubmitControls = (form) => {
    getAssociatedSubmitControls(form).forEach((element) => {
      element.dataset.llbSubmitLocked = "true";
      setLockedState(element, true);
    });
  };

  const resetFormState = (form) => {
    if (!form) {
      return;
    }

    delete form.dataset.llbSubmitting;
    delete form.dataset.llbSubmitToken;
    form.classList.remove("llb-is-submitting");

    getAssociatedSubmitControls(form).forEach((element) => {
      delete element.dataset.llbSubmitLocked;
      clearActionLock(element);
    });
  };

  document.addEventListener("click", (event) => {
    const actionElement = event.target.closest(actionSelector);
    if (!actionElement || !shouldProtect(actionElement) || event.defaultPrevented || shouldIgnoreActionLock(actionElement)) {
      return;
    }

    const clickToken = String(event.timeStamp);
    if (actionElement.dataset.llbClickLocked === "true" && actionElement.dataset.llbClickToken !== clickToken) {
      event.preventDefault();
      event.stopImmediatePropagation();
      return;
    }

    lockActionTemporarily(actionElement, clickToken);
  });

  document.addEventListener("submit", (event) => {
    const form = event.target;
    if (!(form instanceof HTMLFormElement) || !shouldProtect(form) || shouldAllowMultiple(form) || event.defaultPrevented) {
      return;
    }

    const submitToken = String(event.timeStamp);
    if (form.dataset.llbSubmitting === "true" && form.dataset.llbSubmitToken !== submitToken) {
      event.preventDefault();
      event.stopImmediatePropagation();
      return;
    }

    form.dataset.llbSubmitting = "true";
    form.dataset.llbSubmitToken = submitToken;
    form.classList.add("llb-is-submitting");
    window.setTimeout(() => disableFormSubmitControls(form), 0);
  });

  window.addEventListener("pageshow", () => {
    document.querySelectorAll("form.llb-is-submitting").forEach((form) => resetFormState(form));
    document.querySelectorAll(`.${lockedClassName}`).forEach((element) => clearActionLock(element));
  });
})();
