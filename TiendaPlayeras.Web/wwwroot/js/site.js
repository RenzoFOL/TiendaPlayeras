(function () {
  const token = document.querySelector('meta[name="RequestVerificationToken"]')?.content || '';

  function get(url) {
    return fetch(url, { credentials: 'same-origin' }).then(r => r.json());
  }
  function post(url, body = {}) {
    return fetch(url, {
      method: 'POST',
      credentials: 'same-origin',
      headers: {
        'Content-Type': 'application/json',
        'RequestVerificationToken': token
      },
      body: JSON.stringify(body)
    }).then(r => r.json());
  }

  async function refreshWishlistCount() {
    try {
      const { count } = await get('/Wishlist/Count');
      const badge = document.getElementById('js-wishlist-count');
      if (badge) badge.textContent = count ?? 0;
    } catch { /* noop */ }
  }

  async function openWishlistDrawer() {
    const modalContent = document.getElementById('wishlist-modal-content');
    const modalEl = document.getElementById('wishlist-modal');
    if (!modalContent || !modalEl) return;

    // Cargamos HTML parcial
    const html = await fetch('/Wishlist/Drawer', { credentials: 'same-origin' }).then(r => r.text());
    modalContent.innerHTML = html;

    // Bind quitar
    modalContent.querySelectorAll('.js-wishlist-remove').forEach(btn => {
      btn.addEventListener('click', async () => {
        const pid = parseInt(btn.dataset.productId, 10);
        const res = await post('/Wishlist/Remove', { productId: pid });
        if (res?.ok) {
          await openWishlistDrawer(); // recarga la vista
          await refreshWishlistCount();
        }
      });
    });

    // Mostrar modal (Bootstrap 5)
    const bsModal = bootstrap.Modal.getOrCreateInstance(modalEl);
    bsModal.show();
  }

  async function addToWishlist(productId) {
    const res = await post('/Wishlist/Add', { productId });
    if (res?.ok) {
      await refreshWishlistCount();
      // pequeño feedback (opcional)
      try { toast('Agregado a tu wishlist'); } catch {}
    } else {
      try { toast('No se pudo agregar'); } catch {}
    }
  }

  // Click en el icono de wishlist en el banner
  document.addEventListener('click', (e) => {
    const openBtn = e.target.closest('#js-wishlist-open');
    if (openBtn) {
      e.preventDefault();
      openWishlistDrawer();
    }

    const favBtn = e.target.closest('.js-add-wishlist');
    if (favBtn) {
      e.preventDefault();
      const pid = parseInt(favBtn.dataset.productId, 10);
      if (!isNaN(pid)) addToWishlist(pid);
    }
  });

  // Inicializa el contador al cargar
  document.addEventListener('DOMContentLoaded', refreshWishlistCount);
})();
