// Inicialización de componentes para la página de inicio
document.addEventListener('DOMContentLoaded', function() {
    initializeHomeComponents();
});

function initializeHomeComponents() {
    // Inicializar Swipers
    initializeSwipers();
    
    // Inicializar animaciones
    initializeAnimations();
    
    // Inicializar efectos de hover
    initializeHoverEffects();
    
    // Inicializar carga perezosa
    initializeLazyLoading();
}

function initializeSwipers() {
    // Swiper de productos
    const productSwipers = document.querySelectorAll('.product-swiper');
    
    productSwipers.forEach(swiperElement => {
        const swiper = new Swiper(swiperElement, {
            slidesPerView: 1,
            spaceBetween: 20,
            loop: true,
            pagination: {
                el: swiperElement.querySelector('.swiper-pagination'),
                clickable: true,
            },
            navigation: {
                nextEl: swiperElement.closest('.product-carousel').querySelector('.icon-arrow-right'),
                prevEl: swiperElement.closest('.product-carousel').querySelector('.icon-arrow-left'),
            },
            breakpoints: {
                576: {
                    slidesPerView: 2,
                },
                768: {
                    slidesPerView: 3,
                },
                1024: {
                    slidesPerView: 4,
                }
            }
        });
    });

    // Swiper de testimonios
    const testimonialSwiper = document.querySelector('.testimonial-swiper');
    if (testimonialSwiper) {
        new Swiper(testimonialSwiper, {
            slidesPerView: 1,
            spaceBetween: 30,
            loop: true,
            pagination: {
                el: '.testimonial-swiper-pagination',
                clickable: true,
            },
            autoplay: {
                delay: 5000,
            },
            breakpoints: {
                768: {
                    slidesPerView: 2,
                },
                1024: {
                    slidesPerView: 3,
                }
            }
        });
    }
}

function initializeAnimations() {
    // Inicializar AOS (Animate On Scroll) si está disponible
    if (typeof AOS !== 'undefined') {
        AOS.init({
            duration: 800,
            once: true,
            offset: 100
        });
    }
    
    // Animaciones personalizadas para elementos
    const animatedElements = document.querySelectorAll('.open-up');
    animatedElements.forEach((element, index) => {
        element.style.animationDelay = `${index * 0.1}s`;
    });
    
    // Preloader
    const preloader = document.querySelector('.preloader');
    if (preloader) {
        window.addEventListener('load', function() {
            setTimeout(() => {
                preloader.classList.add('loaded');
            }, 500);
        });
    }
}

function initializeHoverEffects() {
    // Efectos hover para productos
    const productItems = document.querySelectorAll('.product-item');
    productItems.forEach(item => {
        item.addEventListener('mouseenter', function() {
            this.style.transform = 'translateY(-10px)';
            this.style.transition = 'transform 0.3s ease';
        });
        
        item.addEventListener('mouseleave', function() {
            this.style.transform = 'translateY(0)';
        });
    });

    // Efectos hover para categorías
    const catItems = document.querySelectorAll('.cat-item');
    catItems.forEach(item => {
        item.addEventListener('mouseenter', function() {
            const button = this.querySelector('.btn-common');
            if (button) {
                button.style.transform = 'scale(1.05)';
            }
        });
        
        item.addEventListener('mouseleave', function() {
            const button = this.querySelector('.btn-common');
            if (button) {
                button.style.transform = 'scale(1)';
            }
        });
    });
    
    // Efectos para botones de wishlist
    const wishlistButtons = document.querySelectorAll('.btn-wishlist');
    wishlistButtons.forEach(button => {
        button.addEventListener('click', function(e) {
            e.preventDefault();
            this.classList.toggle('active');
            
            const heartIcon = this.querySelector('svg');
            if (this.classList.contains('active')) {
                heartIcon.style.fill = 'currentColor';
            } else {
                heartIcon.style.fill = 'none';
            }
        });
    });
}

function initializeLazyLoading() {
    const lazyImages = document.querySelectorAll('img[data-src]');
    
    if ('IntersectionObserver' in window) {
        const imageObserver = new IntersectionObserver((entries, observer) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const img = entry.target;
                    img.src = img.dataset.src;
                    img.removeAttribute('data-src');
                    imageObserver.unobserve(img);
                }
            });
        });

        lazyImages.forEach(img => imageObserver.observe(img));
    } else {
        // Fallback para navegadores que no soportan IntersectionObserver
        lazyImages.forEach(img => {
            img.src = img.dataset.src;
        });
    }
}

// Manejar el comportamiento de los enlaces de productos
function setupProductLinks() {
    const productLinks = document.querySelectorAll('.product-content a[href]');
    productLinks.forEach(link => {
        link.addEventListener('click', function(e) {
            if (this.getAttribute('data-after') === 'Agregar al carrito') {
                e.preventDefault();
                addToCart(this);
            }
        });
    });
}

function addToCart(button) {
    const productItem = button.closest('.product-item');
    const productName = productItem.querySelector('h5 a').textContent;
    const productPrice = productItem.querySelector('.product-content a span').textContent;
    
    // Simular agregar al carrito
    console.log(`Producto agregado: ${productName} - ${productPrice}`);
    
    // Mostrar feedback visual
    const originalText = button.querySelector('span').textContent;
    button.querySelector('span').textContent = '¡Agregado!';
    button.style.color = 'var(--primary-color)';
    
    setTimeout(() => {
        button.querySelector('span').textContent = originalText;
        button.style.color = '';
    }, 2000);
}

// Inicializar cuando el DOM esté listo
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initializeHomeComponents);
} else {
    initializeHomeComponents();
}

// Re-inicializar cuando se cargue contenido dinámicamente
document.addEventListener('ajaxComplete', initializeHomeComponents);

// Exportar funciones para uso global
window.HomePage = {
    initialize: initializeHomeComponents,
    refreshSwipers: initializeSwipers
};