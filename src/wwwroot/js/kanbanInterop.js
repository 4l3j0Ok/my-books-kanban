// Carga única: tema + clipboard + scroll suave.
// No contiene reglas de negocio.

const STORAGE_KEY = "mbk.theme";

export function getStoredTheme() {
  try {
    return localStorage.getItem(STORAGE_KEY);
  } catch {
    return null;
  }
}

export function setStoredTheme(theme) {
  try {
    localStorage.setItem(STORAGE_KEY, theme);
  } catch {
    /* ignore quota / private mode */
  }
}

export function applyTheme(theme) {
  const root = document.documentElement;
  if (theme === "dark" || theme === "light") {
    root.setAttribute("data-theme", theme);
  } else {
    root.removeAttribute("data-theme");
  }
}

export async function copyToClipboard(text) {
  try {
    if (navigator.clipboard && window.isSecureContext) {
      await navigator.clipboard.writeText(text);
      return true;
    }
    const area = document.createElement("textarea");
    area.value = text;
    area.setAttribute("aria-hidden", "true");
    area.style.position = "fixed";
    area.style.opacity = "0";
    document.body.appendChild(area);
    area.select();
    const ok = document.execCommand("copy");
    document.body.removeChild(area);
    return ok;
  } catch {
    return false;
  }
}

export function scrollToSelector(selector) {
  const el = document.querySelector(selector);
  if (!el) return false;
  el.scrollIntoView({ behavior: "smooth", block: "start" });
  return true;
}

export function scrollToTop() {
  window.scrollTo({ top: 0, behavior: "smooth" });
}

const BOOK_DETAIL_TRANSITION = "selected-book-detail";

function getBookSpine(bookId) {
  return document.querySelector(`[data-book-id="${CSS.escape(String(bookId))}"]`);
}

export function openBookDetailWithTransition(bookId) {
  if (!document.startViewTransition) return false;

  const spine = getBookSpine(bookId);
  const drawer = document.querySelector(".drawer");
  const panel = document.querySelector(".drawer-panel");
  if (!spine || !drawer || !panel) return false;

  spine.style.viewTransitionName = BOOK_DETAIL_TRANSITION;
  spine.style.contain = "layout";

  const transition = document.startViewTransition(() => {
    spine.style.viewTransitionName = "none";
    drawer.classList.remove("is-closed");
    drawer.classList.add("is-open");
    drawer.setAttribute("aria-hidden", "false");
    panel.style.viewTransitionName = BOOK_DETAIL_TRANSITION;
    panel.style.contain = "layout";
    panel.classList.add("animate__animated", "animate__flipInY");
  });

  transition.finished
    .finally(() => {
      spine.style.viewTransitionName = "";
      spine.style.contain = "";
      panel.style.viewTransitionName = "";
      panel.style.contain = "";
    })
    .catch(() => {});

  return true;
}

export function closeBookDetailWithTransition(bookId) {
  if (!document.startViewTransition) return false;

  const drawer = document.querySelector(".drawer.is-open");
  const panel = document.querySelector(".drawer-panel");
  if (!drawer || !panel) return false;

  panel.style.viewTransitionName = BOOK_DETAIL_TRANSITION;
  panel.style.contain = "layout";

  let spine = null;
  const transition = document.startViewTransition(() => {
    panel.style.viewTransitionName = "none";
    drawer.classList.remove("is-open");
    drawer.classList.add("is-closed");
    drawer.setAttribute("aria-hidden", "true");
    panel.classList.remove("animate__animated", "animate__flipInY");
    spine = getBookSpine(bookId);

    if (spine) {
      spine.style.viewTransitionName = BOOK_DETAIL_TRANSITION;
      spine.style.contain = "layout";
    }
  });

  transition.finished
    .finally(() => {
      if (spine) {
        spine.style.viewTransitionName = "";
        spine.style.contain = "";
      }
      panel.style.viewTransitionName = "";
      panel.style.contain = "";
    })
    .catch(() => {});

  return true;
}

// Devuelve el índice de inserción dentro de `container` para un punto Y
// (clientY del puntero). El contenedor expone hijos con el atributo
// `data-book-id` en orden; el índice retornado es 0..N (N = append al final).
// Si el puntero está en la mitad superior de un hijo, se inserta antes;
// si está en la mitad inferior, se inserta después.
export function findDropIndex(container, clientY) {
  if (!container) return 0;
  const cards = container.querySelectorAll("[data-book-id]");
  for (let i = 0; i < cards.length; i++) {
    const rect = cards[i].getBoundingClientRect();
    const midY = rect.top + rect.height / 2;
    if (clientY < midY) return i;
  }
  return cards.length;
}
