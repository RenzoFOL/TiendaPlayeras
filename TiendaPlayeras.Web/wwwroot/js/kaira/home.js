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
}

function initializeSwipers() {
    // Swiper principal
    const mainSwiper = new Swiper('.main-swiper', {
        slidesPerView: 1,
        spaceBetween: 30,
        loop: true,
        pagination: {
            el: '.swiper-pagination',
            clickable: true,
        },
        navigation: {
            nextEl: '.icon-arrow-right',
            prevEl: '.icon-arrow-left',
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

    // Swiper de productos
    const productSwiper = new Swiper('.product-swiper', {
        slidesPerView: 1,
        spaceBetween: 20,
        loop: true,
        pagination: {
            el: '.swiper-pagination',
            clickable: true,
        },
        navigation: {
            nextEl: '.icon-arrow-right',
            prevEl: '.icon-arrow-left',
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

    // Swiper de testimonios
    const testimonialSwiper = new Swiper('.testimonial-swiper', {
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
    });
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
}

// Función para manejar la carga perezosa de imágenes
function initializeLazyLoading() {
    const lazyImages = document.querySelectorAll('img[data-src]');
    
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
}

// Inicializar cuando el DOM esté listo
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initializeHomeComponents);
} else {
    initializeHomeComponents();
}