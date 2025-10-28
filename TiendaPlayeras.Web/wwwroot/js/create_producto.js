// ================================================
// CLASE PRINCIPAL - ProductForm
// ================================================
class ProductForm {
    constructor() {
        this.selectedFiles = [];
        this.initializeElements();
        this.bindEvents();
        this.initializeCounters();
        this.initializeAnimations();
        console.log('✅ Formulario de creación cargado - Sistema mejorado con animaciones');
    }

    // ================================================
    // INICIALIZACIÓN DE ELEMENTOS
    // ================================================
    initializeElements() {
        // Elementos del formulario
        this.productName = document.getElementById('productName');
        this.productDescription = document.getElementById('productDescription');
        this.nameCounter = document.getElementById('nameCounter');
        this.descCounter = document.getElementById('descCounter');
        this.slugPreview = document.getElementById('slugPreview');
        this.slugText = document.getElementById('slugText');
        this.imagesInput = document.getElementById('imagesInput');
        this.imagesPreview = document.getElementById('imagesPreview');
        this.noImagesMessage = document.getElementById('noImagesMessage');
        this.imagesCount = document.getElementById('imagesCount');
        this.uploadStatus = document.getElementById('uploadStatus');
        this.priceInput = document.querySelector('input[name="BasePrice"]');
        this.cancelBtn = document.querySelector('.btn-cancel');
        this.form = document.getElementById('productForm');
        this.uploadArea = document.querySelector('.images-upload-area');
    }

    // ================================================
    // VINCULACIÓN DE EVENTOS
    // ================================================
    bindEvents() {
        // Eventos para el nombre del producto
        if (this.productName) {
            this.productName.addEventListener('input', () => this.handleNameInput());
            this.productName.addEventListener('focus', () => this.handleInputFocus(this.productName));
            this.productName.addEventListener('blur', () => this.handleInputBlur(this.productName));
            this.productName.dispatchEvent(new Event('input'));
        }

        // Eventos para la descripción
        if (this.productDescription) {
            this.productDescription.addEventListener('input', () => this.handleDescriptionInput());
            this.productDescription.addEventListener('focus', () => this.handleInputFocus(this.productDescription));
            this.productDescription.addEventListener('blur', () => this.handleInputBlur(this.productDescription));
            this.productDescription.dispatchEvent(new Event('input'));
        }

        // Eventos para imágenes
        if (this.imagesInput) {
            this.imagesInput.addEventListener('change', (e) => this.handleFiles(e.target.files));
            this.setupDragAndDrop();
        }

        // Eventos para el precio
        if (this.priceInput) {
            this.priceInput.addEventListener('input', () => this.validatePrice());
            this.priceInput.addEventListener('focus', () => this.handleInputFocus(this.priceInput));
            this.priceInput.addEventListener('blur', () => this.handleInputBlur(this.priceInput));
        }

        // Eventos para cancelar
        if (this.cancelBtn && this.form) {
            this.cancelBtn.addEventListener('click', (e) => this.handleCancel(e));
        }

        // Evento para enviar formulario
        if (this.form) {
            this.form.addEventListener('submit', (e) => this.handleSubmit(e));
        }

        // Manejo de tallas disponibles
        this.setupSizeHandling();

        // Animaciones de scroll
        this.setupScrollAnimations();
    }

    // ================================================
    // INICIALIZACIÓN DE CONTADORES Y ANIMACIONES
    // ================================================
    initializeCounters() {
        this.updateImagesCount();
    }

    initializeAnimations() {
        // Animar elementos al cargar
        this.animateOnLoad();
    }

    // ================================================
    // GENERACIÓN DE SLUG
    // ================================================
    generateSlug(text) {
        return text
            .toLowerCase()
            .trim()
            .replace(/[áàäâ]/g, 'a')
            .replace(/[éèëê]/g, 'e')
            .replace(/[íìïî]/g, 'i')
            .replace(/[óòöô]/g, 'o')
            .replace(/[úùüû]/g, 'u')
            .replace(/ñ/g, 'n')
            .replace(/[^a-z0-9\s-]/g, '')
            .replace(/\s+/g, '-')
            .replace(/-+/g, '-');
    }

    /// ================================================
// CONFIGURAR MANEJO DE TALLAS - VERSIÓN ACTUALIZADA PARA BOOLEANOS
// ================================================
setupSizeHandling() {
    const updateSizeCounter = () => {
        const selectedCount = Array.from(document.querySelectorAll('input[name^="size"]:checked')).length;
        const counterElement = document.getElementById('sizeCounter');
        
        if (counterElement) {
            counterElement.textContent = `${selectedCount} talla${selectedCount !== 1 ? 's' : ''}`;
            
            // Animación de cambio
            counterElement.style.transform = 'scale(1.2)';
            setTimeout(() => {
                counterElement.style.transform = 'scale(1)';
            }, 300);
        }
        
        console.log('📏 Tallas seleccionadas - S:', document.querySelector('input[name="sizeS"]').checked,
                   'M:', document.querySelector('input[name="sizeM"]').checked,
                   'L:', document.querySelector('input[name="sizeL"]').checked,
                   'XL:', document.querySelector('input[name="sizeXL"]').checked);
    };

    // Inicializar eventos para checkboxes de tallas
    document.querySelectorAll('input[name^="size"]').forEach(checkbox => {
        checkbox.addEventListener('change', updateSizeCounter);
        
        // Agregar efecto hover mejorado
        const label = checkbox.closest('.form-check-size');
        if (label) {
            label.addEventListener('mouseenter', function() {
                if (!this.querySelector('input').checked) {
                    this.style.borderColor = '#667eea';
                    this.style.transform = 'translateY(-1px)';
                }
            });
            
            label.addEventListener('mouseleave', function() {
                if (!this.querySelector('input').checked) {
                    this.style.borderColor = '#e0e7ff';
                    this.style.transform = 'translateY(0)';
                }
            });

            // Efecto al hacer click
            checkbox.addEventListener('click', (e) => {
                this.animateSizeSelection(checkbox);
            });
        }
    });

    // Inicializar al cargar
    updateSizeCounter();
    
    console.log('✅ Sistema de tallas booleanas inicializado');
}

    // ================================================
    // MANEJO DE ENTRADA DE DESCRIPCIÓN
    // ================================================
    handleDescriptionInput() {
        const length = this.productDescription.value.length;
        this.descCounter.textContent = length;

        // Animar el contador
        this.descCounter.style.transform = 'scale(1.2)';
        setTimeout(() => {
            this.descCounter.style.transform = 'scale(1)';
        }, 200);

        if (length > 900) {
            this.descCounter.style.color = '#e53e3e';
            this.descCounter.classList.add('danger');
        } else if (length > 800) {
            this.descCounter.style.color = '#f59e0b';
            this.descCounter.classList.add('warning');
        } else {
            this.descCounter.style.color = '#94a3b8';
            this.descCounter.classList.remove('danger', 'warning');
        }
    }

    // ================================================
    // EFECTOS DE FOCUS Y BLUR EN INPUTS
    // ================================================
    handleInputFocus(element) {
        const parent = element.closest('.col-md-12, .col-md-6');
        if (parent) {
            parent.style.transform = 'scale(1.02)';
            parent.style.transition = 'all 0.3s ease';
        }
    }

    handleInputBlur(element) {
        const parent = element.closest('.col-md-12, .col-md-6');
        if (parent) {
            parent.style.transform = 'scale(1)';
        }
    }

    // ================================================
    // CONFIGURAR DRAG AND DROP
    // ================================================
    setupDragAndDrop() {
        const uploadArea = this.uploadArea;
        
        ['dragover', 'dragenter'].forEach(eventName => {
            uploadArea.addEventListener(eventName, (e) => {
                e.preventDefault();
                e.stopPropagation();
                uploadArea.style.borderColor = '#f55';
                uploadArea.style.background = 'linear-gradient(135deg, #fa709a20, #fee14020)';
                uploadArea.style.transform = 'scale(1.02)';
            });
        });

        ['dragleave', 'dragend'].forEach(eventName => {
            uploadArea.addEventListener(eventName, (e) => {
                e.preventDefault();
                e.stopPropagation();
                uploadArea.style.borderColor = '#fa709a';
                uploadArea.style.background = 'linear-gradient(135deg, #fa709a10, #fee14010)';
                uploadArea.style.transform = 'scale(1)';
            });
        });

        uploadArea.addEventListener('drop', (e) => {
            e.preventDefault();
            e.stopPropagation();
            uploadArea.style.borderColor = '#fa709a';
            uploadArea.style.background = 'linear-gradient(135deg, #fa709a10, #fee14010)';
            uploadArea.style.transform = 'scale(1)';
            
            // Efecto de "drop exitoso"
            this.animateDropSuccess();
            
            this.handleFiles(e.dataTransfer.files);
        });
    }

    // ================================================
    // ANIMACIÓN DE DROP EXITOSO
    // ================================================
    animateDropSuccess() {
        this.uploadArea.style.background = 'linear-gradient(135deg, #48bb7820, #38a16920)';
        setTimeout(() => {
            this.uploadArea.style.background = 'linear-gradient(135deg, #fa709a10, #fee14010)';
        }, 300);
    }

    // ================================================
    // MANEJAR ARCHIVOS SELECCIONADOS
    // ================================================
    handleFiles(files) {
        const validTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/webp', 'image/gif'];
        let validFiles = 0;
        let invalidFiles = 0;
        
        Array.from(files).forEach(file => {
            if (file.size > 10 * 1024 * 1024) {
                this.showUploadStatus(`❌ ${file.name} es demasiado grande. Máximo 10MB.`, 'error');
                invalidFiles++;
                return;
            }

            if (!validTypes.includes(file.type)) {
                this.showUploadStatus(`❌ ${file.name} no es un formato válido.`, 'error');
                invalidFiles++;
                return;
            }

            // Verificar si ya existe
            const exists = this.selectedFiles.some(f => f.name === file.name && f.size === file.size);
            if (!exists) {
                this.selectedFiles.push(file);
                validFiles++;
            }
        });

        if (validFiles > 0) {
            this.showUploadStatus(`✅ ${validFiles} imagen(es) agregada(s) correctamente`, 'success');
            this.animateImagesCount();
        }

        this.updateImagesPreview();
        this.updateFileInput();
        this.updateImagesCount();
    }

    // ================================================
    // MOSTRAR ESTADO DE CARGA
    // ================================================
    showUploadStatus(message, type = 'info') {
        this.uploadStatus.textContent = message;
        this.uploadStatus.className = `upload-status ${type} show`;
        
        if (type === 'success' || type === 'error') {
            setTimeout(() => {
                this.uploadStatus.classList.remove('show');
                setTimeout(() => {
                    this.uploadStatus.textContent = '';
                    this.uploadStatus.className = 'upload-status';
                }, 300);
            }, 3000);
        }
    }

    // ================================================
    // ACTUALIZAR VISTA PREVIA DE IMÁGENES
    // ================================================
    updateImagesPreview() {
        this.imagesPreview.innerHTML = '';
        
        if (this.selectedFiles.length === 0) {
            this.imagesPreview.appendChild(this.noImagesMessage);
            this.noImagesMessage.style.display = 'flex';
            return;
        }

        this.noImagesMessage.style.display = 'none';

        this.selectedFiles.forEach((file, index) => {
            const reader = new FileReader();
            reader.onload = (e) => {
                const div = document.createElement('div');
                div.className = 'image-preview-item';
                div.style.animationDelay = `${index * 0.1}s`;
                div.innerHTML = `
                    <img src="${e.target.result}" alt="Imagen ${index + 1}">
                    <button type="button" class="remove-image" data-index="${index}" title="Eliminar imagen">
                        <i class="fas fa-times"></i>
                    </button>
                    ${index === 0 ? '<span class="main-badge"><i class="fas fa-star me-1"></i>Principal</span>' : ''}
                    <div class="image-info">
                        <div title="${file.name}">${file.name}</div>
                        <div>${(file.size / 1024 / 1024).toFixed(2)} MB</div>
                    </div>
                `;
                this.imagesPreview.appendChild(div);

                // Evento para remover imagen
                div.querySelector('.remove-image').addEventListener('click', (e) => {
                    e.stopPropagation();
                    this.removeImage(index);
                });

                // Efecto hover en la imagen
                div.addEventListener('mouseenter', () => {
                    div.style.zIndex = '10';
                });
                div.addEventListener('mouseleave', () => {
                    div.style.zIndex = '1';
                });
            };
            reader.readAsDataURL(file);
        });
    }

    // ================================================
    // REMOVER IMAGEN
    // ================================================
    removeImage(index) {
        const removedFile = this.selectedFiles[index];
        
        // Animar salida
        const imageItem = this.imagesPreview.children[index];
        if (imageItem) {
            imageItem.style.animation = 'scaleOut 0.3s ease';
            setTimeout(() => {
                this.selectedFiles.splice(index, 1);
                this.updateImagesPreview();
                this.updateFileInput();
                this.updateImagesCount();
                this.showUploadStatus(`🗑️ Imagen "${removedFile.name}" removida`, 'info');
            }, 300);
        }
    }

    // ================================================
    // ACTUALIZAR INPUT DE ARCHIVOS
    // ================================================
    updateFileInput() {
        const dataTransfer = new DataTransfer();
        this.selectedFiles.forEach(file => dataTransfer.items.add(file));
        this.imagesInput.files = dataTransfer.files;
    }

    // ================================================
    // ACTUALIZAR CONTADOR DE IMÁGENES
    // ================================================
    updateImagesCount() {
        const count = this.selectedFiles.length;
        this.imagesCount.textContent = `${count} imagen${count !== 1 ? 'es' : ''}`;
    }

    // ================================================
    // ANIMAR CONTADOR DE IMÁGENES
    // ================================================
    animateImagesCount() {
        this.imagesCount.style.transform = 'scale(1.3)';
        this.imagesCount.style.transition = 'all 0.3s ease';
        setTimeout(() => {
            this.imagesCount.style.transform = 'scale(1)';
        }, 300);
    }

    // ================================================
    // VALIDAR PRECIO
    // ================================================
    validatePrice() {
        const value = parseFloat(this.priceInput.value);
        if (value < 0) this.priceInput.value = 0;
        if (value > 999999) this.priceInput.value = 999999;

        // Efecto visual al cambiar precio
        const priceGroup = this.priceInput.closest('.price-input-group');
        if (priceGroup) {
            priceGroup.style.transform = 'scale(1.05)';
            setTimeout(() => {
                priceGroup.style.transform = 'scale(1)';
            }, 200);
        }
    }

    // ================================================
    // CONFIGURAR MANEJO DE TALLAS
    // ================================================
    // Configurar manejo de tallas - VERSIÓN MEJORADA
setupSizeHandling() {
    const updateAvailableSizes = () => {
        const selectedSizes = Array.from(document.querySelectorAll('input[name="availableSizes"]:checked'))
            .map(checkbox => checkbox.value)
            .join(',');
        
        document.getElementById('availableSizesHidden').value = selectedSizes;
        console.log('📏 Tallas seleccionadas:', selectedSizes);
        
        // Mostrar feedback visual mejorado
        const sizeLabels = document.querySelectorAll('.form-check-size');
        sizeLabels.forEach(label => {
            const checkbox = label.querySelector('input');
            if (checkbox.checked) {
                label.style.background = 'linear-gradient(135deg, #667eea20, #764ba220)';
                label.style.borderColor = '#667eea';
                label.style.transform = 'translateY(-2px)';
                label.style.boxShadow = '0 5px 15px rgba(102, 126, 234, 0.2)';
            } else {
                label.style.background = 'linear-gradient(135deg, #667eea10, #764ba210)';
                label.style.borderColor = '#e0e7ff';
                label.style.transform = 'translateY(0)';
                label.style.boxShadow = 'none';
            }
        });
        
        // Actualizar contador visual
        this.updateSizeCounter(selectedSizes);
    };

    // Actualizar contador de tallas seleccionadas
    this.updateSizeCounter = (selectedSizes) => {
        const count = selectedSizes ? selectedSizes.split(',').filter(size => size.length > 0).length : 0;
        let counterElement = document.getElementById('sizeCounter');
        
        if (!counterElement) {
            counterElement = document.createElement('div');
            counterElement.id = 'sizeCounter';
            counterElement.className = 'size-counter';
            document.querySelector('.size-options-grid').parentNode.appendChild(counterElement);
        }
        
        counterElement.innerHTML = `
            <i class="fas fa-check-circle me-1"></i>
            ${count} talla${count !== 1 ? 's' : ''} seleccionada${count !== 1 ? 's' : ''}
        `;
        
        // Animación de cambio
        counterElement.style.opacity = '0';
        setTimeout(() => {
            counterElement.style.opacity = '1';
            counterElement.style.transition = 'opacity 0.3s ease';
        }, 50);
    };

    // Inicializar eventos para checkboxes de tallas
    document.querySelectorAll('input[name="availableSizes"]').forEach(checkbox => {
        checkbox.addEventListener('change', updateAvailableSizes);
        
        // Agregar efecto hover mejorado
        const label = checkbox.closest('.form-check-size');
        if (label) {
            label.addEventListener('mouseenter', function() {
                if (!this.querySelector('input').checked) {
                    this.style.borderColor = '#667eea';
                    this.style.transform = 'translateY(-1px)';
                }
            });
            
            label.addEventListener('mouseleave', function() {
                if (!this.querySelector('input').checked) {
                    this.style.borderColor = '#e0e7ff';
                    this.style.transform = 'translateY(0)';
                }
            });
        }
    });

    // Inicializar al cargar
    updateAvailableSizes();
    
    console.log('✅ Sistema de tallas inicializado');
}

    // ================================================
    // ANIMAR SELECCIÓN DE TALLA
    // ================================================
    animateSizeSelection(checkbox) {
        const sizeItem = checkbox.closest('.form-check-size');
        if (sizeItem) {
            if (checkbox.checked) {
                // Efecto de selección
                sizeItem.style.animation = 'none';
                setTimeout(() => {
                    sizeItem.style.animation = 'pulse 0.5s ease';
                }, 10);
            } else {
                // Efecto de deselección
                sizeItem.style.animation = 'shake 0.5s ease';
            }
        }
    }

    // ================================================
    // MANEJAR CANCELACIÓN
    // ================================================
    handleCancel(e) {
        const hasData = this.productName.value.trim() !== '' || 
                       this.productDescription.value.trim() !== '' || 
                       (this.priceInput && this.priceInput.value !== '') ||
                       this.selectedFiles.length > 0;
        
        if (hasData) {
            if (!confirm('¿Estás seguro de que deseas cancelar? Los datos ingresados se perderán.')) {
                e.preventDefault();
            }
        }
    }

    // ================================================
    // MANEJAR ENVÍO DEL FORMULARIO
    // ================================================
    handleSubmit(e) {
        // Validación adicional antes de enviar
        if (this.selectedFiles.length === 0) {
            if (!confirm('⚠️ No has seleccionado ninguna imagen. ¿Deseas continuar sin imágenes?')) {
                e.preventDefault();
                return;
            }
        }

        const submitBtn = this.form.querySelector('.btn-submit');
        const originalHTML = submitBtn.innerHTML;
        
        submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Creando Producto...';
        submitBtn.disabled = true;
        
        // Animar el botón
        submitBtn.style.animation = 'pulse 1s infinite';
        
        // Mostrar información de lo que se va a guardar
        console.log('📦 Creando producto con:');
        console.log('   - Nombre:', this.productName.value);
        console.log('   - Descripción:', this.productDescription.value.length, 'caracteres');
        console.log('   - Precio:', this.priceInput.value);
        console.log('   - Imágenes:', this.selectedFiles.length);
        
        // Si hay un error, restaurar el botón
        setTimeout(() => {
            if (this.form.checkValidity && !this.form.checkValidity()) {
                submitBtn.innerHTML = originalHTML;
                submitBtn.disabled = false;
                submitBtn.style.animation = '';
            }
        }, 100);
    }

    // ================================================
    // ANIMACIONES AL CARGAR
    // ================================================
    animateOnLoad() {
        // Animar las secciones del formulario
        const sections = document.querySelectorAll('.form-section');
        sections.forEach((section, index) => {
            section.style.opacity = '0';
            section.style.transform = 'translateY(30px)';
            setTimeout(() => {
                section.style.transition = 'all 0.6s ease';
                section.style.opacity = '1';
                section.style.transform = 'translateY(0)';
            }, index * 100);
        });
    }

    // ================================================
    // CONFIGURAR ANIMACIONES DE SCROLL
    // ================================================
    setupScrollAnimations() {
        const observerOptions = {
            threshold: 0.1,
            rootMargin: '0px 0px -100px 0px'
        };

        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.style.opacity = '1';
                    entry.target.style.transform = 'translateY(0)';
                }
            });
        }, observerOptions);

        // Observar elementos que necesitan animación
        const elementsToAnimate = document.querySelectorAll('.form-section, .form-actions');
        elementsToAnimate.forEach(el => {
            observer.observe(el);
        });
    }
}

// ================================================
// ESTILOS DINÁMICOS PARA ANIMACIONES
// ================================================
const style = document.createElement('style');
style.textContent = `
    @keyframes scaleOut {
        from {
            opacity: 1;
            transform: scale(1);
        }
        to {
            opacity: 0;
            transform: scale(0.8);
        }
    }
`;
document.head.appendChild(style);

// ================================================
// INICIALIZAR CUANDO EL DOM ESTÉ LISTO
// ================================================
document.addEventListener('DOMContentLoaded', function() {
    const productForm = new ProductForm();
    
    // Log de bienvenida con estilo
    console.log('%c🎨 Sistema de Creación de Productos', 'color: #667eea; font-size: 16px; font-weight: bold;');
    console.log('%c✨ Con animaciones y efectos mejorados', 'color: #764ba2; font-size: 12px;');
});