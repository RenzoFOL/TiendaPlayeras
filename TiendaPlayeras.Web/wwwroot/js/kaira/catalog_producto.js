// =============================================
// FUNCIONES GLOBALES SIMPLIFICADAS
// =============================================

// Función para mostrar notificaciones
function showNotification(message, type = 'info') {
    const toast = document.createElement('div');
    toast.className = `alert alert-${type} alert-dismissible fade show position-fixed`;
    toast.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 300px;';
    toast.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
    document.body.appendChild(toast);
    
    setTimeout(() => {
        if (toast.parentNode) toast.remove();
    }, 3000);
}

// Función para actualizar contador del carrito
async function updateCartCount() {
    try {
        const response = await fetch('/Cart/GetCartSummary');
        if (response.ok) {
            const result = await response.json();
            if (result.success) {
                document.querySelectorAll('.cart-count').forEach(el => {
                    el.textContent = `(${result.data.totalItems})`;
                });
            }
        }
    } catch (error) {
        console.error('Error actualizando carrito:', error);
    }
}

// Función para actualizar contador de wishlist
async function updateWishlistCount() {
    try {
        const response = await fetch('/Wishlist/GetWishlistCount');
        if (response.ok) {
            const result = await response.json();
            document.querySelectorAll('.wishlist-count').forEach(el => {
                el.textContent = `(${result.count})`;
            });
        }
    } catch (error) {
        console.error('Error actualizando wishlist:', error);
    }
}

// =============================================
// INICIALIZACIÓN RÁPIDA
// =============================================

document.addEventListener('DOMContentLoaded', function() {
    console.log('🛍️ Inicializando tienda...');
    initializeStore();
});

function initializeStore() {
    // 1. GALERÍA DE IMÁGENES
    const thumbnails = document.querySelectorAll('.thumbnail-item');
    const mainImage = document.getElementById('mainProductImage');
    
    if (thumbnails.length > 0 && mainImage) {
        thumbnails.forEach(thumb => {
            thumb.addEventListener('click', function() {
                const imageUrl = this.getAttribute('data-image');
                if (imageUrl) {
                    thumbnails.forEach(t => t.classList.remove('active'));
                    this.classList.add('active');
                    mainImage.src = imageUrl;
                }
            });
        });
    }

    // 2. SELECTOR DE CANTIDAD
    const decreaseBtn = document.getElementById('decreaseQty');
    const increaseBtn = document.getElementById('increaseQty');
    const quantityInput = document.getElementById('quantity');
    
    if (decreaseBtn && increaseBtn && quantityInput) {
        decreaseBtn.addEventListener('click', () => {
            let currentQty = parseInt(quantityInput.value);
            if (currentQty > 1) quantityInput.value = currentQty - 1;
        });
        
        increaseBtn.addEventListener('click', () => {
            let currentQty = parseInt(quantityInput.value);
            if (currentQty < 99) quantityInput.value = currentQty + 1;
        });
    }

    // 3. BOTÓN AGREGAR AL CARRITO
    const btnAddToCart = document.getElementById('btnAddToCart');
    if (btnAddToCart) {
        btnAddToCart.addEventListener('click', async function() {
            await addToCartSimple(false, this);
        });
    }

    // 4. BOTÓN COMPRAR AHORA
    const btnBuyNow = document.getElementById('btnBuyNow');
    if (btnBuyNow) {
        btnBuyNow.addEventListener('click', async function() {
            await addToCartSimple(true, this);
        });
    }

    // 5. BOTÓN WISHLIST
    const btnWishlist = document.getElementById('btnWishlist');
    if (btnWishlist) {
        btnWishlist.addEventListener('click', async function() {
            await toggleWishlistSimple(this);
        });
    }

    // Inicializar contadores
    updateCartCount();
    updateWishlistCount();
    
    console.log('✅ Tienda inicializada correctamente');
}

// =============================================
// FUNCIONES PRINCIPALES OPTIMIZADAS
// =============================================

// Función para agregar al carrito - VERSIÓN FINAL
async function addToCartSimple(redirectToCheckout = false, buttonElement = null) {
    console.log('🛒 Agregando al carrito...');
    
    // Obtener datos del producto
    const productElement = document.querySelector('.product-info-section');
    if (!productElement) {
        showNotification('❌ Error: No se encontró el producto', 'danger');
        return;
    }

    const productId = parseInt(productElement.dataset.productId);
    const productName = productElement.dataset.productName || 'Producto';
    
    // Obtener talla (M por defecto si no hay selección)
    let size = 'M';
    const selectedSize = document.querySelector('input[name="size"]:checked');
    if (selectedSize) size = selectedSize.value;
    
    // Obtener cantidad
    const quantityInput = document.getElementById('quantity');
    const quantity = quantityInput ? parseInt(quantityInput.value) : 1;

    console.log('📦 Datos:', { productId, productName, size, quantity });

    // Si no se pasó el botón, buscarlo
    if (!buttonElement) {
        buttonElement = redirectToCheckout ? 
            document.getElementById('btnBuyNow') : 
            document.getElementById('btnAddToCart');
    }

    // Guardar estado original del botón
    const originalHTML = buttonElement ? buttonElement.innerHTML : null;
    const originalBackground = buttonElement ? buttonElement.style.background : '';
    let success = false;

    try {
        // Obtener token anti-forgery
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        if (!token) {
            showNotification('❌ Error de seguridad. Recarga la página.', 'danger');
            return;
        }

        // Animación del botón
        if (buttonElement) {
            buttonElement.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Procesando...';
            buttonElement.disabled = true;
        }

        // Llamar al servidor
        const response = await fetch('/Cart/AddToCart', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify({ 
                productId: productId, 
                size: size, 
                quantity: quantity 
            })
        });
        
        console.log('📥 Respuesta del servidor:', response.status);
        
        if (response.ok) {
            const result = await response.json();
            console.log('📊 Resultado:', result);
            
            if (result.success) {
                // ÉXITO
                await updateCartCount();
                showNotification(`✅ <strong>${productName}</strong> agregado al carrito`, 'success');
                success = true;
                
                // Animación de éxito
                if (buttonElement) {
                    buttonElement.innerHTML = '<i class="fas fa-check me-2"></i>¡Agregado!';
                    buttonElement.style.background = '#28a745';
                }
                
                // Redirigir si es "Comprar ahora"
                if (redirectToCheckout && success) {
                    setTimeout(() => {
                        window.location.href = '/Cart';
                    }, 1000);
                }
                
            } else {
                throw new Error(result.error || 'Error del servidor');
            }
        } else {
            const errorText = await response.text();
            console.error('❌ Error del servidor:', errorText);
            throw new Error(`Error HTTP ${response.status}`);
        }
        
    } catch (error) {
        console.error('❌ Error completo:', error);
        showNotification('❌ Error al agregar al carrito: ' + error.message, 'danger');
        success = false;
    } finally {
        // SIEMPRE restaurar el botón
        if (buttonElement && originalHTML) {
            if (success) {
                // Si fue éxito, mantener el estado de éxito por 2 segundos
                setTimeout(() => {
                    buttonElement.innerHTML = originalHTML;
                    buttonElement.style.background = originalBackground;
                    buttonElement.disabled = false;
                }, 2000);
            } else {
                // Si fue error, restaurar inmediatamente
                buttonElement.innerHTML = originalHTML;
                buttonElement.style.background = originalBackground;
                buttonElement.disabled = false;
            }
        }
    }
}

// Función para wishlist - VERSIÓN FINAL
async function toggleWishlistSimple(buttonElement = null) {
    console.log('💖 Alternando wishlist...');
    
    const productElement = document.querySelector('.product-info-section');
    if (!productElement) return;

    const productId = parseInt(productElement.dataset.productId);
    
    // Si no se pasó el botón, buscarlo
    if (!buttonElement) {
        buttonElement = document.getElementById('btnWishlist');
    }

    // Guardar estado original
    const originalHTML = buttonElement ? buttonElement.innerHTML : null;
    let success = false;

    try {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        if (!token) {
            showNotification('❌ Error de seguridad', 'danger');
            return;
        }

        // Animación
        if (buttonElement) {
            buttonElement.innerHTML = '<i class="fas fa-spinner fa-spin"></i>';
            buttonElement.disabled = true;
        }

        const response = await fetch('/Wishlist/ToggleWishlist', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify({ productId: productId })
        });
        
        if (response.ok) {
            const result = await response.json();
            
            if (result.success) {
                success = true;
                
                if (buttonElement) {
                    if (result.added) {
                        buttonElement.innerHTML = '<i class="fas fa-heart" style="color: #dc3545;"></i>';
                        buttonElement.classList.add('active');
                        showNotification('💖 Agregado a favoritos', 'success');
                    } else {
                        buttonElement.innerHTML = '<i class="fas fa-heart"></i>';
                        buttonElement.classList.remove('active');
                        showNotification('💔 Removido de favoritos', 'info');
                    }
                }
                
                await updateWishlistCount();
            }
        }
        
    } catch (error) {
        console.error('❌ Error wishlist:', error);
        showNotification('❌ Error en favoritos', 'danger');
    } finally {
        // Restaurar botón en caso de error
        if (buttonElement && originalHTML && !success) {
            buttonElement.innerHTML = originalHTML;
            buttonElement.disabled = false;
        }
    }
}

// =============================================
// FUNCIONES PARA PRODUCTOS RELACIONADOS
// =============================================

// Manejo de eventos para productos relacionados
document.addEventListener('click', async function(e) {
    // Botones "Agregar" en productos relacionados
    if (e.target.closest('.btn-add-to-cart')) {
        e.preventDefault();
        const button = e.target.closest('.btn-add-to-cart');
        const productItem = button.closest('.related-product-item');
        
        if (productItem) {
            const productId = productItem.dataset.productId;
            const productName = productItem.querySelector('.product-name')?.textContent || 'Producto';
            
            if (productId) {
                await addRelatedToCart(parseInt(productId), productName, button);
            }
        }
    }
    
    // Wishlist en productos relacionados
    if (e.target.closest('.wishlist-btn')) {
        e.preventDefault();
        const button = e.target.closest('.wishlist-btn');
        const productItem = button.closest('.related-product-item');
        
        if (productItem) {
            const productId = productItem.dataset.productId;
            if (productId) {
                await toggleRelatedWishlist(parseInt(productId), button);
            }
        }
    }
});

// Función para agregar productos relacionados
async function addRelatedToCart(productId, productName, button) {
    try {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        if (!token) return;

        const originalHTML = button.innerHTML;
        const originalBackground = button.style.background;
        
        button.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>';
        button.disabled = true;

        const response = await fetch('/Cart/AddToCart', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify({ 
                productId: productId, 
                size: 'M',
                quantity: 1 
            })
        });
        
        if (response.ok) {
            const result = await response.json();
            if (result.success) {
                await updateCartCount();
                button.innerHTML = '<i class="fas fa-check me-2"></i>¡Agregado!';
                button.style.background = '#28a745';
                
                setTimeout(() => {
                    button.innerHTML = originalHTML;
                    button.style.background = originalBackground;
                    button.disabled = false;
                }, 1500);
                
                showNotification(`✅ <strong>${productName}</strong> agregado al carrito`, 'success');
            }
        }
    } catch (error) {
        console.error('❌ Error:', error);
        button.innerHTML = '<i class="fas fa-exclamation me-2"></i>Error';
        setTimeout(() => {
            button.innerHTML = '<i class="fas fa-shopping-cart me-2"></i>Agregar';
            button.disabled = false;
        }, 2000);
    }
}

// Función para wishlist de productos relacionados
async function toggleRelatedWishlist(productId, button) {
    try {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        if (!token) return;

        const originalHTML = button.innerHTML;
        button.innerHTML = '<i class="fas fa-spinner fa-spin"></i>';
        button.disabled = true;

        const response = await fetch('/Wishlist/ToggleWishlist', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify({ productId: productId })
        });
        
        if (response.ok) {
            const result = await response.json();
            if (result.success) {
                if (result.added) {
                    button.innerHTML = '<i class="fas fa-heart" style="color: #dc3545;"></i>';
                    showNotification('💖 Agregado a favoritos', 'success');
                } else {
                    button.innerHTML = '<i class="fas fa-heart"></i>';
                    showNotification('💔 Removido de favoritos', 'info');
                }
                await updateWishlistCount();
            }
        }
    } catch (error) {
        console.error('❌ Error:', error);
        button.innerHTML = originalHTML;
    } finally {
        button.disabled = false;
    }
}

// =============================================
// FUNCIONES DE CONVENIENCIA
// =============================================

// Exportar para uso global
window.store = {
    addToCart: addToCartSimple,
    toggleWishlist: toggleWishlistSimple,
    updateCartCount: updateCartCount,
    updateWishlistCount: updateWishlistCount
};