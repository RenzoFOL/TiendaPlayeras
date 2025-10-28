// Clase para manejar el formulario de edición de productos
class ProductEditForm {
    constructor() {
        // ✅ CORREGIDO: Obtener ID de manera más robusta
        this.productId = null;
        this.chosenTags = new Map();
        this.selectedNewFiles = [];
        this.existingImages = window.__productImages || [];
        this.hiddenContainer = document.getElementById('hiddenTags');
        
        // Obtener ID del producto de la URL
        const urlMatch = window.location.pathname.match(/\/Products\/Edit\/(\d+)/);
        if (urlMatch) {
            this.productId = urlMatch[1];
            console.log('🎯 Producto ID obtenido de URL:', this.productId);
        } else {
            console.error('❌ No se pudo obtener el ID del producto de la URL');
        }
        
        this.initializeElements();
        this.bindEvents();
        this.initializeForm();
        console.log('🚀 Inicializando sistema de edición para producto ID:', this.productId);
    }

    initializeElements() {
        // Elementos del formulario
        this.imagesCount = document.getElementById('imagesCount');
        this.uploadStatus = document.getElementById('uploadStatus');
        this.currentImagesGrid = document.getElementById('currentImagesGrid');
        this.newImagesInput = document.getElementById('newImagesInput');
        this.newImagesPreview = document.getElementById('newImagesPreview');
        this.tagSearch = document.getElementById('tagSearch');
        this.tagSuggestions = document.getElementById('tagSuggestions');
        this.selectedTags = document.getElementById('selectedTags');
        this.submitBtn = document.getElementById('submitBtn');
        this.editForm = document.getElementById('editForm');
    }

    bindEvents() {
        // Eventos para imágenes
        if (this.newImagesInput) {
            this.newImagesInput.addEventListener('change', (e) => this.handleNewFiles(e.target.files));
            this.setupDragAndDrop();
        }

        // Eventos para etiquetas
        if (this.tagSearch) {
            let searchTimeout;
            this.tagSearch.addEventListener('input', () => {
                clearTimeout(searchTimeout);
                const query = this.tagSearch.value.trim();
                if (query.length < 2) { 
                    this.tagSuggestions.innerHTML = ''; 
                    return; 
                }
                searchTimeout = setTimeout(() => this.searchTags(query), 300);
            });
        }

        // Evento para enviar formulario
        if (this.editForm) {
            this.editForm.addEventListener('submit', (e) => this.handleSubmit(e));
        }

        // Manejo de tallas disponibles
        this.setupSizeHandling();
    }

    async initializeForm() {
        // ✅ CORREGIDO: Obtener el ID del producto de manera segura
        const form = document.getElementById('editForm');
        if (form && form.action) {
            const match = form.action.match(/\/Products\/SaveProductWithTags\/(\d+)/);
            if (match) {
                this.productId = match[1];
                console.log('✅ ID del producto obtenido:', this.productId);
            }
        }
        
        // Si no se pudo obtener del form, intentar de la URL
        if (!this.productId || this.productId === 'Logout') {
            const urlMatch = window.location.pathname.match(/\/Products\/Edit\/(\d+)/);
            if (urlMatch) {
                this.productId = urlMatch[1];
                console.log('✅ ID del producto obtenido de la URL:', this.productId);
            }
        }
        
        // Si aún no tenemos ID, mostrar error
        if (!this.productId || this.productId === 'Logout') {
            console.error('❌ No se pudo obtener el ID del producto');
            this.showUploadStatus('Error: No se pudo cargar el producto', 'error');
            return;
        }

        await this.loadCurrentTags();
        this.updateImagesCount();
        this.renderCurrentImages(); // ✅ Asegurar que las imágenes se rendericen
        console.log('✅ Sistema de edición inicializado correctamente para producto ID:', this.productId);
    }

    // Función para mostrar estado de upload
    showUploadStatus(message, type = 'info') {
        this.uploadStatus.textContent = message;
        this.uploadStatus.className = `upload-status ${type}`;
        
        if (type === 'success' || type === 'error') {
            setTimeout(() => {
                this.uploadStatus.textContent = '';
                this.uploadStatus.className = 'upload-status';
            }, 3000);
        }
    }

    // Función para actualizar contador de imágenes
    updateImagesCount() {
        const totalImages = this.existingImages.length + this.selectedNewFiles.length;
        this.imagesCount.textContent = `${totalImages} imagen${totalImages !== 1 ? 'es' : ''}`;
    }

    // Configurar drag and drop
    setupDragAndDrop() {
        const uploadArea = document.querySelector('.images-upload-area');
        
        uploadArea.addEventListener('dragover', (e) => {
            e.preventDefault();
            e.stopPropagation();
            uploadArea.style.borderColor = '#667eea';
            uploadArea.style.background = 'linear-gradient(135deg, #667eea20, #764ba220)';
        });

        uploadArea.addEventListener('dragleave', (e) => {
            e.preventDefault();
            e.stopPropagation();
            uploadArea.style.borderColor = '#667eea';
            uploadArea.style.background = 'linear-gradient(135deg, #667eea10, #764ba210)';
        });

        uploadArea.addEventListener('drop', (e) => {
            e.preventDefault();
            e.stopPropagation();
            uploadArea.style.borderColor = '#667eea';
            uploadArea.style.background = 'linear-gradient(135deg, #667eea10, #764ba210)';
            this.handleNewFiles(e.dataTransfer.files);
        });
    }

    // Manejar nuevas imágenes
    handleNewFiles(files) {
        const validTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/webp', 'image/gif'];
        let validFiles = 0;
        
        Array.from(files).forEach(file => {
            if (file.size > 10 * 1024 * 1024) {
                this.showUploadStatus(`❌ ${file.name} es demasiado grande. Máximo 10MB.`, 'error');
                return;
            }

            if (!validTypes.includes(file.type)) {
                this.showUploadStatus(`❌ ${file.name} no es un formato válido.`, 'error');
                return;
            }

            // Verificar si ya existe
            const exists = this.selectedNewFiles.some(f => f.name === file.name && f.size === file.size);
            if (!exists) {
                this.selectedNewFiles.push(file);
                validFiles++;
            }
        });

        if (validFiles > 0) {
            this.showUploadStatus(`✅ ${validFiles} imagen(es) agregada(s) correctamente`, 'success');
        }

        this.renderNewImagesPreview();
        this.updateNewFilesInput();
        this.updateImagesCount();
    }

    // Renderizar vista previa de nuevas imágenes
    renderNewImagesPreview() {
        this.newImagesPreview.innerHTML = '';
        
        if (this.selectedNewFiles.length === 0) {
            return;
        }

        this.selectedNewFiles.forEach((file, index) => {
            const reader = new FileReader();
            reader.onload = (e) => {
                const div = document.createElement('div');
                div.className = 'image-preview-item';
                div.innerHTML = `
                    <img src="${e.target.result}" alt="Nueva imagen ${index + 1}">
                    <button type="button" class="remove-image" data-index="${index}">
                        <i class="fas fa-times"></i>
                    </button>
                    <div class="image-info">
                        <div>${file.name}</div>
                        <div>${(file.size / 1024 / 1024).toFixed(2)} MB</div>
                    </div>
                `;
                this.newImagesPreview.appendChild(div);

                div.querySelector('.remove-image').addEventListener('click', () => {
                    this.removeNewImage(parseInt(div.querySelector('.remove-image').getAttribute('data-index')));
                });
            };
            reader.readAsDataURL(file);
        });
    }

    // Remover nueva imagen
    removeNewImage(index) {
        const removedFile = this.selectedNewFiles[index];
        this.selectedNewFiles.splice(index, 1);
        this.renderNewImagesPreview();
        this.updateNewFilesInput();
        this.updateImagesCount();
        this.showUploadStatus(`🗑️ Imagen "${removedFile.name}" removida`, 'info');
    }

    // Actualizar input de archivos
    updateNewFilesInput() {
        const dataTransfer = new DataTransfer();
        this.selectedNewFiles.forEach(file => dataTransfer.items.add(file));
        this.newImagesInput.files = dataTransfer.files;
    }

    // Eliminar imagen existente - VERSIÓN CORREGIDA
    async deleteImage(imageId) {
        try {
            console.log('🗑️ Solicitando eliminar imagen:', imageId);
            
            if (!confirm('¿Estás seguro de que quieres eliminar esta imagen? Esta acción no se puede deshacer.')) {
                return;
            }

            // Mostrar loading
            this.showUploadStatus('🔄 Eliminando imagen...', 'info');

            const response = await fetch(`/Products/DeleteImage?id=${imageId}`, { 
                method: 'POST',
                headers: {
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                }
            });
            
            if (!response.ok) {
                const errorText = await response.text();
                throw new Error(`HTTP ${response.status}: ${errorText}`);
            }
            
            const result = await response.json();
            
            if (result.success) {
                console.log('✅ Imagen eliminada correctamente');
                
                // Remover la imagen del array local
                this.existingImages = this.existingImages.filter(img => img.id !== imageId);
                
                // Re-renderizar las imágenes actuales
                this.renderCurrentImages();
                
                // Actualizar contador
                this.updateImagesCount();
                
                this.showUploadStatus('✅ Imagen eliminada correctamente', 'success');
                
                // Recargar la página después de 1 segundo para asegurar sincronización
                setTimeout(() => {
                    window.location.reload();
                }, 1000);
                
            } else {
                throw new Error(result.error || 'Error desconocido al eliminar la imagen');
            }
        } catch(error) {
            console.error('❌ Error eliminando imagen:', error);
            this.showUploadStatus('❌ Error al eliminar la imagen: ' + error.message, 'error');
        }
    }

    // Marcar imagen como principal
    async setMainImage(imageId) {
        try {
            console.log('⭐ Marcando imagen como principal:', imageId);
            
            const formData = new FormData();
            formData.append('__RequestVerificationToken', document.querySelector('input[name="__RequestVerificationToken"]').value);
            
            const response = await fetch(`/Products/SetMainImage?id=${imageId}`, { 
                method: 'POST',
                body: formData
            });
            
            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            
            const result = await response.json();
            if (result.success) {
                console.log('✅ Imagen marcada como principal');
                // Actualizar estado local
                this.existingImages.forEach(img => {
                    img.isMain = img.id === imageId;
                });
                this.renderCurrentImages();
                this.showUploadStatus('✅ Imagen marcada como principal', 'success');
            } else {
                throw new Error(result.error || 'Error desconocido');
            }
        } catch(error) {
            console.error('❌ Error marcando imagen como principal:', error);
            this.showUploadStatus('❌ Error al marcar la imagen como principal: ' + error.message, 'error');
        }
    }

    // Renderizar imágenes actuales - CON EVENTOS CORRECTOS
    renderCurrentImages() {
        this.currentImagesGrid.innerHTML = '';
        
        if (this.existingImages.length === 0) {
            this.currentImagesGrid.innerHTML = `
                <div class="no-images-message">
                    <i class="fas fa-image me-2"></i>
                    No hay imágenes cargadas para este producto
                </div>
            `;
            return;
        }

        this.existingImages.forEach((img, index) => {
            const div = document.createElement('div');
            div.className = 'current-image-item';
            
            div.innerHTML = `
                <img src="${img.path}" alt="Imagen ${index + 1}" 
                     onerror="this.src='https://via.placeholder.com/150?text=Error+Imagen'">
                <button type="button" class="delete-image" data-id="${img.id}">
                    <i class="fas fa-times"></i>
                </button>
                ${img.isMain ? '<span class="main-badge"><i class="fas fa-star me-1"></i>Principal</span>' : ''}
                ${!img.isMain ? `
                    <button type="button" class="set-main-btn" data-id="${img.id}" title="Marcar como principal">
                        <i class="fas fa-star"></i>
                    </button>
                ` : ''}
                <div class="image-info">
                    <div>Orden: ${img.displayOrder}</div>
                    ${img.isMain ? '<div><strong>Principal</strong></div>' : ''}
                </div>
            `;
            this.currentImagesGrid.appendChild(div);

            // Botón para eliminar - CON LOGS DE DEBUG
            const deleteBtn = div.querySelector('.delete-image');
            deleteBtn.addEventListener('click', () => {
                console.log('🖱️ Click en botón eliminar, ID:', deleteBtn.getAttribute('data-id'));
                this.deleteImage(deleteBtn.getAttribute('data-id'));
            });

            // Botón para marcar como principal
            const setMainBtn = div.querySelector('.set-main-btn');
            if (setMainBtn) {
                setMainBtn.addEventListener('click', () => {
                    console.log('⭐ Click en botón principal, ID:', setMainBtn.getAttribute('data-id'));
                    this.setMainImage(setMainBtn.getAttribute('data-id'));
                });
            }
        });
    }

    // Cargar etiquetas actuales - VERSIÓN CORREGIDA
    async loadCurrentTags() {
        try {
            // ✅ VERIFICACIÓN: Asegurar que tenemos un ID válido
            if (!this.productId || isNaN(parseInt(this.productId))) {
                console.error('❌ ID de producto inválido:', this.productId);
                this.showUploadStatus('Error: ID de producto inválido', 'error');
                return;
            }

            console.log('🔄 Cargando etiquetas para producto ID:', this.productId);
            
            const response = await fetch(`/Products/GetTags?id=${this.productId}`);
            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            
            const data = await response.json();
            console.log('🏷️ Respuesta de etiquetas:', data);
            
            // ✅ MANEJO MEJORADO DE ERRORES
            if (data && data.error) {
                throw new Error(data.error);
            }
            
            if (!Array.isArray(data)) {
                console.warn('⚠️ Respuesta de etiquetas no es un array:', data);
                this.renderSelectedTags(); // Renderizar vacío
                return;
            }
            
            this.chosenTags.clear();
            data.forEach(tag => {
                const tagId = (tag.tagId || tag.id || '').toString();
                const name = tag.name || tag.Name || '';
                const category = tag.category || tag.Category || '';
                
                if (tagId && name) {
                    this.chosenTags.set(tagId, { name, category });
                }
            });
            
            this.renderSelectedTags();
            
        } catch(error) {
            console.error('❌ Error cargando tags:', error);
            this.showUploadStatus('❌ Error al cargar las etiquetas: ' + error.message, 'error');
            
            // Renderizar vacío en caso de error
            this.renderSelectedTags();
        }
    }

    // Renderizar tags seleccionados
    renderSelectedTags() {
        this.selectedTags.innerHTML = '';
        this.hiddenContainer.innerHTML = '';
        
        if (this.chosenTags.size === 0) {
            this.selectedTags.innerHTML = '<span class="empty-tags"><i class="fas fa-tag me-2"></i>No hay etiquetas seleccionadas</span>';
            return;
        }

        this.chosenTags.forEach((tagInfo, tagId) => {
            const badge = document.createElement('span');
            badge.className = 'badge';
            badge.innerHTML = `${tagInfo.name} <i class="fas fa-times-circle"></i>`;
            badge.title = `${tagInfo.category} - Click para quitar`;
            badge.onclick = () => { 
                this.chosenTags.delete(tagId); 
                this.renderSelectedTags();
            };
            this.selectedTags.appendChild(badge);

            const input = document.createElement('input');
            input.type = 'hidden';
            input.name = 'tagIds';
            input.value = tagId;
            this.hiddenContainer.appendChild(input);
        });
    }

    // Buscar tags
    async searchTags(query) {
        try {
            if (!query || query.length < 2) {
                this.tagSuggestions.innerHTML = '';
                return;
            }

            console.log('🔍 Buscando tags:', query);
            const response = await fetch(`/CategoryTags/FindTags?q=${encodeURIComponent(query)}`);
            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            
            const suggestions = await response.json();
            this.tagSuggestions.innerHTML = '';
            
            if (!suggestions || suggestions.length === 0) {
                this.tagSuggestions.innerHTML = `
                    <div class="list-group-item text-center">
                        <i class="fas fa-search me-2 text-muted"></i>
                        <span class="text-muted">No se encontraron etiquetas</span>
                    </div>
                `;
                return;
            }

            suggestions.forEach(tag => {
                const tagId = (tag.id || tag.Id).toString();
                const tagName = tag.name || tag.Name;
                const categoryName = tag.category || tag.Category || 'Sin categoría';
                const isSelected = this.chosenTags.has(tagId);
                
                const item = document.createElement('a');
                item.className = `list-group-item list-group-item-action ${isSelected ? 'active' : ''}`;
                item.innerHTML = `
                    <i class="fas fa-tag me-2"></i>
                    <strong>${tagName}</strong> 
                    <span class="text-muted">— ${categoryName}</span>
                    ${isSelected ? '<span class="float-end"><i class="fas fa-check"></i></span>' : ''}
                `;
                item.href = '#';
                item.onclick = (e) => { 
                    e.preventDefault(); 
                    if (this.chosenTags.has(tagId)) {
                        this.chosenTags.delete(tagId);
                    } else {
                        this.chosenTags.set(tagId, { name: tagName, category: categoryName });
                    }
                    this.renderSelectedTags();
                    this.searchTags(query);
                };
                this.tagSuggestions.appendChild(item);
            });
        } catch(error) {
            console.error('❌ Error buscando tags:', error);
            this.showUploadStatus('❌ Error al buscar etiquetas: ' + error.message, 'error');
        }
    }

    // Configurar manejo de tallas - VERSIÓN ACTUALIZADA PARA BOOLEANOS
    setupSizeHandling() {
        console.log('📏 Configurando manejo de tallas booleanas...');
        
        const updateSizeCounter = () => {
            // Contar checkboxes marcados
            const selectedCount = document.querySelectorAll('input[name^="size"]:checked').length;
            const counterElement = document.getElementById('sizeCounter');
            
            if (counterElement) {
                counterElement.textContent = `${selectedCount} talla${selectedCount !== 1 ? 's' : ''}`;
                
                // Animación de cambio
                counterElement.style.transform = 'scale(1.2)';
                setTimeout(() => {
                    counterElement.style.transform = 'scale(1)';
                }, 300);
            }
            
            // Log para debugging
            console.log('📏 Tallas seleccionadas - S:', document.querySelector('input[name="sizeS"]')?.checked,
                       'M:', document.querySelector('input[name="sizeM"]')?.checked,
                       'L:', document.querySelector('input[name="sizeL"]')?.checked,
                       'XL:', document.querySelector('input[name="sizeXL"]')?.checked);
            
            // Actualizar feedback visual
            this.updateSizeVisualFeedback();
        };

        // Inicializar eventos para checkboxes de tallas
        document.querySelectorAll('input[name^="size"]').forEach(checkbox => {
            checkbox.addEventListener('change', updateSizeCounter);
            console.log('🔗 Evento agregado a checkbox:', checkbox.name, '=', checkbox.checked);
        });

        // Ejecutar una vez al cargar para establecer estado inicial
        console.log('🎯 Estado inicial de las tallas:');
        document.querySelectorAll('input[name^="size"]').forEach(checkbox => {
            console.log('   ', checkbox.name, '=', checkbox.checked);
        });
        
        // Inicializar contador y feedback visual
        setTimeout(() => {
            updateSizeCounter();
            console.log('✅ Manejo de tallas booleanas configurado correctamente');
        }, 100);
    }

    // Agrega este método para feedback visual
    updateSizeVisualFeedback() {
        const sizeLabels = document.querySelectorAll('.form-check-size');
        sizeLabels.forEach(label => {
            const checkbox = label.querySelector('input');
            if (checkbox && checkbox.checked) {
                label.style.background = 'linear-gradient(135deg, #667eea30, #764ba230)';
                label.style.borderColor = '#667eea';
                label.style.color = '#667eea';
                label.style.fontWeight = '700';
                label.style.transform = 'scale(1.05)';
            } else {
                label.style.background = 'linear-gradient(135deg, #667eea10, #764ba210)';
                label.style.borderColor = '#e0e7ff';
                label.style.color = '#64748b';
                label.style.fontWeight = '600';
                label.style.transform = 'scale(1)';
            }
        });
    }

    // Manejar envío del formulario
    handleSubmit(e) {
        this.submitBtn.innerHTML = '<div class="spinner me-2"></div> Guardando...';
        this.submitBtn.disabled = true;
        this.submitBtn.classList.add('loading');
    }
}

// Inicializar cuando el DOM esté listo
document.addEventListener('DOMContentLoaded', function() {
    new ProductEditForm();
});