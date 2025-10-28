// Inicialización de componentes para vista de producto individual
document.addEventListener('DOMContentLoaded', function() {
    initializeProductPage();
});

function initializeProductPage() {
    // Elementos del DOM
    const mainImage = document.getElementById('mainProductImage');
    const thumbnails = document.querySelectorAll('.thumbnail-item');
    const sizeOptions = document.querySelectorAll('.size-option-kaira');
    const quantityInput = document.getElementById('quantity');
    const decreaseBtn = document.getElementById('decreaseQty');
    const increaseBtn = document.getElementById('increaseQty');
    const btnAddToCart = document.getElementById('btnAddToCart');
    const btnBuyNow = document.getElementById('btnBuyNow');
    const btnWishlist = document.getElementById('btnWishlist');
    const wishlistButtons = document.querySelectorAll('.wishlist-btn');
    const addToCartButtons = document.querySelectorAll('.btn-add-to-cart');

    // SOLUCIÓN CORREGIDA: Obtener datos del producto desde data attributes
    function getProductData() {
        // Buscar el elemento que contiene los datos del producto
        const productElement = document.querySelector('[data-product-id]') || 
                              document.getElementById('productData') ||
                              document.querySelector('.product-info-section');
        
        if (!productElement) {
            console.warn('❌ No se encontró elemento con datos del producto');
            return {
                id: 0,
                name: 'Producto',
                price: 0,
                slug: '',
                isActive: true,
                isCustomizable: false
            };
        }

        // Intentar obtener datos de diferentes formas
        const productData = {
            id: parseInt(productElement.dataset.productId || productElement.dataset.id || '0'),
            name: productElement.dataset.productName || productElement.dataset.name || 'Producto',
            price: parseFloat(productElement.dataset.price || '0'),
            slug: productElement.dataset.slug || '',
            isActive: (productElement.dataset.isactive || 'true').toLowerCase() === 'true',
            isCustomizable: (productElement.dataset.iscustomizable || 'false').toLowerCase() === 'true'
        };

        console.log('🛍️ Datos del producto cargados:', productData);
        return productData;
    }

    // Obtener datos del producto
    const productData = getProductData();

    // Galería de imágenes
    function initializeImageGallery() {
        if (thumbnails.length > 0) {
            thumbnails.forEach((thumb) => {
                thumb.addEventListener('click', function() {
                    // Remover clase active de todos los thumbnails
                    thumbnails.forEach(t => t.classList.remove('active'));
                    
                    // Agregar clase active al thumbnail clickeado
                    this.classList.add('active');
                    
                    // Obtener nueva imagen
                    const newImageSrc = this.getAttribute('data-image');
                    if (newImageSrc && newImageSrc !== mainImage.src) {
                        // Efecto de transición suave
                        mainImage.style.opacity = '0.7';
                        mainImage.style.transform = 'scale(0.95)';
                        
                        setTimeout(() => {
                            mainImage.src = newImageSrc;
                            mainImage.style.opacity = '1';
                            mainImage.style.transform = 'scale(1)';
                        }, 150);
                        
                        console.log('🖼️ Imagen cambiada a:', newImageSrc);
                    }
                });
            });
        }
    }

    // Selector de tallas
    function initializeSizeSelector() {
        sizeOptions.forEach(option => {
            option.addEventListener('click', function() {
                // Remover clase selected de todas las opciones
                sizeOptions.forEach(opt => opt.classList.remove('selected'));
                
                // Agregar clase selected a la opción clickeada
                this.classList.add('selected');
                this.querySelector('input').checked = true;
                
                const selectedSize = this.querySelector('input').value;
                console.log('📏 Talla seleccionada:', selectedSize);
                
                // Actualizar UI si es necesario
                updateAddToCartButton();
            });
        });

        // Seleccionar talla por defecto (primera disponible)
        const firstAvailableSize = document.querySelector('.size-option-kaira input');
        if (firstAvailableSize) {
            firstAvailableSize.checked = true;
            firstAvailableSize.closest('.size-option-kaira').classList.add('selected');
        }
    }

    // Control de cantidad
    function initializeQuantitySelector() {
        if (!decreaseBtn || !increaseBtn || !quantityInput) {
            console.warn('❌ Elementos de cantidad no encontrados');
            return;
        }

        decreaseBtn.addEventListener('click', function() {
            let currentValue = parseInt(quantityInput.value);
            if (currentValue > 1) {
                quantityInput.value = currentValue - 1;
                updateAddToCartButton();
            }
        });

        increaseBtn.addEventListener('click', function() {
            let currentValue = parseInt(quantityInput.value);
            let maxValue = parseInt(quantityInput.max);
            if (currentValue < maxValue) {
                quantityInput.value = currentValue + 1;
                updateAddToCartButton();
            }
        });

        quantityInput.addEventListener('change', function() {
            let value = parseInt(this.value);
            if (isNaN(value) || value < 1) {
                this.value = 1;
            } else if (value > 99) {
                this.value = 99;
            }
            updateAddToCartButton();
        });
    }

    // Función para obtener datos del producto seleccionado
    function getSelectedProduct() {
        const selectedSize = document.querySelector('input[name="size"]:checked');
        const quantity = parseInt(quantityInput?.value || 1);

        return {
            ...productData,
            size: selectedSize ? selectedSize.value : null,
            quantity: quantity,
            total: productData.price * quantity,
            image: mainImage?.src || ''
        };
    }

    // Actualizar estado del botón agregar al carrito
    function updateAddToCartButton() {
        if (!btnAddToCart || !btnBuyNow) return;

        const selectedSize = document.querySelector('input[name="size"]:checked');
        const hasSize = selectedSize !== null;
        const isActive = productData.isActive;

        if (!isActive) {
            btnAddToCart.disabled = true;
            btnBuyNow.disabled = true;
            btnAddToCart.innerHTML = '<i class="fas fa-times me-2"></i>Agotado';
        } else if (!hasSize) {
            btnAddToCart.disabled = true;
            btnBuyNow.disabled = true;
        } else {
            btnAddToCart.disabled = false;
            btnBuyNow.disabled = false;
            btnAddToCart.innerHTML = '<i class="fas fa-shopping-cart me-2"></i>Agregar al carrito';
        }
    }

    // Formatear precio
    function formatPrice(amount) {
        return new Intl.NumberFormat('es-MX', {
            style: 'currency',
            currency: 'MXN'
        }).format(amount);
    }

    // Mostrar notificación
    function showNotification(message, type = 'success') {
        // Crear elemento de notificación
        const notification = document.createElement('div');
        notification.className = `alert alert-${type} alert-dismissible fade show`;
        notification.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            z-index: 9999;
            min-width: 300px;
            box-shadow: 0 5px 15px rgba(0,0,0,0.2);
        `;
        notification.innerHTML = `
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;

        document.body.appendChild(notification);

        // Auto-remover después de 5 segundos
        setTimeout(() => {
            if (notification.parentNode) {
                notification.parentNode.removeChild(notification);
            }
        }, 5000);
    }

    // Agregar al carrito
    function initializeAddToCart() {
        if (!btnAddToCart) return;

        btnAddToCart.addEventListener('click', function() {
            const product = getSelectedProduct();
            
            if (!product.size) {
                showNotification('⚠️ Por favor selecciona una talla', 'warning');
                return;
            }

            if (!product.isActive) {
                showNotification('❌ Este producto está agotado', 'danger');
                return;
            }

            console.log('🛒 Agregando al carrito:', product);
            
            // Animación del botón
            const originalText = this.innerHTML;
            const originalBackground = this.style.background;
            
            this.innerHTML = '<i class="fas fa-check me-2"></i> ¡Agregado!';
            this.style.background = '#4CAF50';
            this.disabled = true;
            
            // Simular llamada a API
            setTimeout(() => {
                this.innerHTML = originalText;
                this.style.background = originalBackground;
                this.disabled = false;
                
                // Mostrar notificación de éxito
                showNotification(`
                    ✅ <strong>Producto agregado al carrito</strong><br>
                    <small>${product.name}<br>
                    Talla: ${product.size} • Cantidad: ${product.quantity}<br>
                    Total: ${formatPrice(product.total)}</small>
                `, 'success');
                
                // Actualizar contador del carrito (simulado)
                updateCartCounter(1);
                
            }, 1500);
        });
    }

    // Comprar ahora
    function initializeBuyNow() {
        if (!btnBuyNow) return;

        btnBuyNow.addEventListener('click', function() {
            const product = getSelectedProduct();
            
            if (!product.size) {
                showNotification('⚠️ Por favor selecciona una talla', 'warning');
                return;
            }

            if (!product.isActive) {
                showNotification('❌ Este producto está agotado', 'danger');
                return;
            }

            console.log('⚡ Compra directa:', product);
            
            // Animación del botón
            const originalText = this.innerHTML;
            this.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i> Procesando...';
            this.disabled = true;
            
            // Simular procesamiento de compra
            setTimeout(() => {
                showNotification(`
                    ⚡ <strong>Redirigiendo a checkout</strong><br>
                    <small>Procesando compra de ${product.name}</small>
                `, 'info');
                
                // En una implementación real, redirigir a checkout
                // window.location.href = `/checkout?product=${product.id}&size=${product.size}&quantity=${product.quantity}`;
                
                this.innerHTML = originalText;
                this.disabled = false;
            }, 2000);
        });
    }

    // Lista de deseos
    function initializeWishlist() {
        if (btnWishlist) {
            btnWishlist.addEventListener('click', function() {
                const isActive = this.classList.contains('active');
                
                if (isActive) {
                    // Remover de favoritos
                    this.classList.remove('active');
                    this.innerHTML = '<i class="fas fa-heart"></i>';
                    showNotification('💔 Removido de tu lista de deseos', 'info');
                    console.log('💔 Producto removido de favoritos');
                } else {
                    // Agregar a favoritos
                    this.classList.add('active');
                    this.innerHTML = '<i class="fas fa-heart" style="color: #fff;"></i>';
                    showNotification('💖 Agregado a tu lista de deseos', 'success');
                    console.log('💖 Producto agregado a favoritos');
                }
            });
        }

        // Wishlist para productos relacionados
        wishlistButtons.forEach(button => {
            button.addEventListener('click', function(e) {
                e.preventDefault();
                e.stopPropagation();
                
                this.classList.toggle('active');
                
                if (this.classList.contains('active')) {
                    this.innerHTML = '<i class="fas fa-heart" style="color: #dc3545;"></i>';
                    showNotification('💖 Producto agregado a favoritos', 'success');
                } else {
                    this.innerHTML = '<i class="fas fa-heart"></i>';
                    showNotification('💔 Producto removido de favoritos', 'info');
                }
            });
        });
    }

    // Agregar al carrito para productos relacionados
    function initializeRelatedProducts() {
        addToCartButtons.forEach(button => {
            button.addEventListener('click', function(e) {
                e.preventDefault();
                e.stopPropagation();
                
                const productItem = this.closest('.related-product-item');
                if (!productItem) return;

                const productName = productItem.querySelector('.product-name')?.textContent || 'Producto';
                const productPrice = productItem.querySelector('.product-price')?.textContent || '$0.00';
                
                // Animación del botón
                const originalText = this.innerHTML;
                this.innerHTML = '<i class="fas fa-check me-2"></i> ¡Agregado!';
                this.style.background = '#4CAF50';
                this.disabled = true;
                
                setTimeout(() => {
                    this.innerHTML = originalText;
                    this.style.background = '';
                    this.disabled = false;
                    
                    showNotification(`
                        ✅ <strong>Producto agregado al carrito</strong><br>
                        <small>${productName}<br>
                        ${productPrice}</small>
                    `, 'success');
                    
                    updateCartCounter(1);
                    
                }, 1500);
            });
        });
    }

    // Actualizar contador del carrito (simulado)
    function updateCartCounter(increment = 0) {
        const cartCounter = document.querySelector('.navbar-extra .badge');
        if (cartCounter) {
            let currentCount = parseInt(cartCounter.textContent) || 0;
            currentCount += increment;
            cartCounter.textContent = currentCount;
            
            // Animación
            cartCounter.style.transform = 'scale(1.3)';
            setTimeout(() => {
                cartCounter.style.transform = 'scale(1)';
            }, 300);
        }
    }

    // Manejo de errores en imágenes
    function initializeImageErrorHandling() {
        const images = document.querySelectorAll('img');
        images.forEach(img => {
            img.addEventListener('error', function() {
                console.warn('❌ Error cargando imagen:', this.src);
                this.src = '/images/placeholder.png';
                this.alt = 'Imagen no disponible';
            });
        });
    }

    // Inicializar todas las funcionalidades
    function initializeAll() {
        try {
            initializeImageGallery();
            initializeSizeSelector();
            initializeQuantitySelector();
            initializeAddToCart();
            initializeBuyNow();
            initializeWishlist();
            initializeRelatedProducts();
            initializeImageErrorHandling();
            updateAddToCartButton();
            
            console.log('✅ Vista de producto inicializada correctamente');
        } catch (error) {
            console.error('❌ Error inicializando vista de producto:', error);
        }
    }

    // Inicializar cuando el DOM esté listo
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializeAll);
    } else {
        initializeAll();
    }
}

// Exportar funciones para uso global (si es necesario)
window.productPage = {
    getSelectedProduct: function() {
        const selectedSize = document.querySelector('input[name="size"]:checked');
        const quantity = parseInt(document.getElementById('quantity')?.value || 1);
        
        // Usar la misma lógica segura para obtener datos
        const productElement = document.querySelector('[data-product-id]');
        const productData = {
            id: parseInt(productElement?.dataset.productId || '0'),
            name: productElement?.dataset.productName || 'Producto',
            price: parseFloat(productElement?.dataset.price || '0'),
            size: selectedSize ? selectedSize.value : null,
            quantity: quantity,
            total: parseFloat(productElement?.dataset.price || '0') * quantity
        };
        
        return productData;
    },
    
    addToCart: function() {
        const btn = document.getElementById('btnAddToCart');
        if (btn && !btn.disabled) {
            btn.click();
        }
    },
    
    buyNow: function() {
        const btn = document.getElementById('btnBuyNow');
        if (btn && !btn.disabled) {
            btn.click();
        }
    }
};