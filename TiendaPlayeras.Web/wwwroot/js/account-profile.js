(function () {
  const root = document.getElementById('accountRoot');
  if (!root) return;

  const menu = document.getElementById('accountMenu');
  const content = document.getElementById('accountContent');
  const originalHTML = content.innerHTML;

  const urls = {
    update: root.dataset.urlUpdate,
    changePwd: root.dataset.urlChangepwd,
    deleteAcc: root.dataset.urlDelete,
    ordersPartial: root.dataset.urlOrdersPartial
  };

  // token antifalsificación
  function getToken() {
    const tokenInput = content.querySelector('input[name="__RequestVerificationToken"]')
                    || document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenInput ? tokenInput.value : '';
  }

  // alertas
  function renderAlert(type, message, timeout = 4000) {
    const host = document.getElementById('alertPlaceholder');
    if (!host) return;
    const wrap = document.createElement('div');
    wrap.innerHTML = `
      <div class="alert alert-${type} alert-dismissible fade show shadow-sm" role="alert">
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Cerrar"></button>
      </div>`;
    const el = wrap.firstElementChild;
    host.appendChild(el);
    if (timeout > 0) {
      setTimeout(() => { try { bootstrap.Alert.getOrCreateInstance(el).close(); } catch { } }, timeout);
    }
  }
  const showOk = (m) => renderAlert('success', m || 'Guardado correctamente.');
  const showErr = (m) => renderAlert('danger', m || 'Ocurrió un error.');

  function setActive(btn) {
    menu.querySelectorAll('.list-group-item').forEach(b => b.classList.remove('active'));
    btn.classList.add('active');
  }

  function setReadonly(input, ro) {
    if (!input) return;
    input.readOnly = ro;
    input.classList.toggle('is-valid', !ro);
  }

  async function postJson(url, body) {
    const res = await fetch(url, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'RequestVerificationToken': getToken()
      },
      body: JSON.stringify(body || {})
    });
    try { return await res.json(); } catch { return { ok: false, error: 'Respuesta inválida' }; }
  }

  // === Handlers del contenido "Editar perfil" ===
  function bindProfileHandlers() {
    // editar/guardar por campo
    content.querySelectorAll('.btn-toggle').forEach(btn => {
      btn.addEventListener('click', async (e) => {
        e.preventDefault();
        const field = btn.getAttribute('data-field');
        const inputId = field === 'FirstName' ? 'txtFirstName'
          : field === 'LastNames' ? 'txtLastNames'
            : field === 'Email' ? 'txtEmail'
              : 'txtPhone';
        const input = content.querySelector('#' + inputId);

        if (btn.textContent.trim().toLowerCase() === 'editar') {
          setReadonly(input, false);
          if (input) input.focus();
          btn.textContent = 'Guardar';
          btn.classList.remove('btn-outline-secondary');
          btn.classList.add('btn-success');
          return;
        }

        const value = input ? input.value : '';
        const data = await postJson(urls.update, { field, value });
        if (data && data.ok) {
          setReadonly(input, true);
          btn.textContent = 'Editar';
          btn.classList.remove('btn-success');
          btn.classList.add('btn-outline-secondary');
          showOk('Guardado correctamente.');
        } else {
          showErr((data && data.error) || 'No se pudo guardar.');
        }
      });
    });

    // cambiar contraseña
    const btnSavePwd = content.querySelector('#btnSavePwd');
    if (btnSavePwd) {
      btnSavePwd.addEventListener('click', async () => {
        const current = content.querySelector('#pwdCurrent')?.value || '';
        const n = content.querySelector('#pwdNew')?.value || '';
        const c = content.querySelector('#pwdConfirm')?.value || '';

        const data = await postJson(urls.changePwd, {
          currentPassword: current, newPassword: n, confirmNewPassword: c
        });

        if (data && data.ok) {
          ['pwdCurrent', 'pwdNew', 'pwdConfirm'].forEach(id => { const el = content.querySelector('#' + id); if (el) el.value = ''; });
          const modalEl = document.getElementById('pwdModal');
          if (modalEl) bootstrap.Modal.getOrCreateInstance(modalEl).hide();
          showOk('Contraseña actualizada.');
        } else {
          showErr((data && data.error) || 'No se pudo actualizar la contraseña.');
        }
      });
    }

    // confirmar eliminación
    const chk = content.querySelector('#chkConfirmDelete');
    const txt = content.querySelector('#txtConfirmWord');
    const btnConfirm = content.querySelector('#btnConfirmDelete');

    function validateDeleteReady() {
      if (!chk || !txt || !btnConfirm) return;
      const okWord = (txt.value || '').trim().toUpperCase() === 'ELIMINAR';
      btnConfirm.disabled = !(chk.checked && okWord);
    }
    if (chk && txt) {
      chk.addEventListener('change', validateDeleteReady);
      txt.addEventListener('input', validateDeleteReady);
      validateDeleteReady();
    }

    if (btnConfirm) {
      btnConfirm.addEventListener('click', async () => {
        btnConfirm.disabled = true;
        btnConfirm.innerHTML = 'Eliminando...';
        const data = await postJson(urls.deleteAcc, {});
        if (data && data.ok) {
          window.location.href = data.redirectUrl || '/';
          return;
        }
        btnConfirm.innerHTML = 'Eliminar definitivamente';
        validateDeleteReady && validateDeleteReady();
        showErr((data && data.error) || 'No se pudo eliminar la cuenta.');
      });
    }
  }

  // === Cargar "Mis pedidos" ===
  async function loadOrders(btn) {
    setActive(btn);
    content.innerHTML = `
      <div class="py-5 text-center">
        <div class="spinner-border" role="status" aria-hidden="true"></div>
        <div class="mt-2">Cargando pedidos...</div>
      </div>`;
    try {
      const resp = await fetch(urls.ordersPartial, {
        headers: { 
          'X-Requested-With': 'fetch',
          'RequestVerificationToken': getToken()
        },
        cache: 'no-store'
      });
      if (!resp.ok) throw new Error('HTTP ' + resp.status);
      const html = await resp.text();
      content.innerHTML = html;
    } catch (err) {
      console.error(err);
      content.innerHTML = `
        <div class="alert alert-danger">
          <h5>Error al cargar pedidos</h5>
          <p class="mb-0">No se pudieron cargar tus pedidos. Intenta más tarde.</p>
        </div>`;
    }
  }

  // === Cargar contenido del perfil ===
  function loadProfile(btn) {
    setActive(btn);
    content.innerHTML = originalHTML;
    bindProfileHandlers(); // re-enlazar eventos
  }

  // === Manejo del menú lateral ===
  menu.addEventListener('click', function (e) {
    const btn = e.target.closest('.list-group-item');
    if (!btn || btn.classList.contains('disabled')) return;

    const action = btn.getAttribute('data-action');
    if (action === 'orders') {
      loadOrders(btn);
    } else if (action === 'profile') {
      loadProfile(btn);
    }
    // Los otros botones (addresses, payments) están disabled
  });

  // Inicializar
  bindProfileHandlers();

})();