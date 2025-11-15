// Función para seleccionar un usuario y preparar el formulario de reset de contraseña
function selectUserForReset(userId, userEmail) {
    // Asignar valores a los campos
    document.getElementById('resetUserId').value = userId;
    document.getElementById('resetUserEmail').value = userEmail;
    
    // Habilitar botón de submit
    document.getElementById('resetSubmitBtn').disabled = false;
    
    // Limpiar campos de contraseña
    document.getElementById('newPassword').value = '';
    document.getElementById('confirmPassword').value = '';
    
    // Dar foco al campo de nueva contraseña
    document.getElementById('newPassword').focus();
    
    // Animación visual en la tarjeta de reset
    const resetCard = document.querySelector('.form-card:last-child');
    resetCard.style.animation = 'none';
    setTimeout(() => {
        resetCard.style.animation = 'pulse 0.5s ease';
    }, 10);
}

// Inicialización cuando carga el DOM
document.addEventListener('DOMContentLoaded', function() {
    console.log('✅ Gestión de empleados cargada correctamente');
    
    // Validación en tiempo real para el formulario de crear empleado
    const createForm = document.querySelector('form[action*="Create"]');
    if (createForm) {
        const password = createForm.querySelector('input[type="password"][name*="Password"]:not([name*="Confirm"])');
        const confirmPassword = createForm.querySelector('input[name*="ConfirmPassword"]');
        
        if (password && confirmPassword) {
            confirmPassword.addEventListener('input', function() {
                if (this.value !== password.value) {
                    this.setCustomValidity('Las contraseñas no coinciden');
                } else {
                    this.setCustomValidity('');
                }
            });
            
            password.addEventListener('input', function() {
                if (confirmPassword.value && confirmPassword.value !== this.value) {
                    confirmPassword.setCustomValidity('Las contraseñas no coinciden');
                } else {
                    confirmPassword.setCustomValidity('');
                }
            });
        }
    }
    
    // Validación para el formulario de reset de contraseña
    const resetForm = document.getElementById('resetPasswordForm');
    if (resetForm) {
        const newPassword = document.getElementById('newPassword');
        const confirmPassword = document.getElementById('confirmPassword');
        
        if (newPassword && confirmPassword) {
            confirmPassword.addEventListener('input', function() {
                if (this.value !== newPassword.value) {
                    this.setCustomValidity('Las contraseñas no coinciden');
                } else {
                    this.setCustomValidity('');
                }
            });
            
            newPassword.addEventListener('input', function() {
                if (confirmPassword.value && confirmPassword.value !== this.value) {
                    confirmPassword.setCustomValidity('Las contraseñas no coinciden');
                } else {
                    confirmPassword.setCustomValidity('');
                }
            });
        }
    }
    
    // Auto-cerrar alertas después de 5 segundos
    const alerts = document.querySelectorAll('.alert');
    alerts.forEach(alert => {
        setTimeout(() => {
            alert.style.transition = 'opacity 0.5s ease, transform 0.5s ease';
            alert.style.opacity = '0';
            alert.style.transform = 'translateY(-20px)';
            setTimeout(() => alert.remove(), 500);
        }, 5000);
    });
});