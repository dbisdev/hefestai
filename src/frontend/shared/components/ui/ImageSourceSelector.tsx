/**
 * ImageSourceSelector Component
 * 
 * A reusable component for selecting image source in entity generators.
 * Provides three modes:
 * - none: Use placeholder image (no generation/upload)
 * - generate: Let AI generate the image
 * - upload: Allow user to upload their own image
 * 
 * Features:
 * - Drag and drop file upload
 * - Image preview
 * - Base64 conversion for uploaded images
 * - Cyberpunk terminal aesthetic styling
 */

import React, { useState, useRef, useCallback } from 'react';

/** Available image source modes */
export type ImageSourceMode = 'none' | 'generate' | 'upload';

/** Configuration for the ImageSourceSelector component */
export interface ImageSourceSelectorProps {
  /** Currently selected image source mode */
  mode: ImageSourceMode;
  /** Callback when mode changes */
  onModeChange: (mode: ImageSourceMode) => void;
  /** Callback when an image is uploaded (provides base64 data) */
  onImageUpload?: (imageBase64: string | null) => void;
  /** Currently uploaded image as base64 (for controlled component) */
  uploadedImage?: string | null;
  /** Maximum file size in bytes (default: 5MB) */
  maxFileSizeBytes?: number;
  /** Accepted image formats */
  acceptedFormats?: string[];
  /** Whether the component is disabled */
  disabled?: boolean;
  /** Custom class name for container */
  className?: string;
}

/** Configuration for each image source option */
interface ImageSourceOption {
  value: ImageSourceMode;
  label: string;
  icon: string;
  description: string;
}

const IMAGE_SOURCE_OPTIONS: ImageSourceOption[] = [
  {
    value: 'none',
    label: 'SIN IMAGEN',
    icon: 'hide_image',
    description: 'Usar placeholder por defecto'
  },
  {
    value: 'generate',
    label: 'GENERAR IA',
    icon: 'auto_awesome',
    description: 'Generación automática con IA'
  },
  {
    value: 'upload',
    label: 'SUBIR',
    icon: 'upload_file',
    description: 'Cargar imagen personalizada'
  }
];

const DEFAULT_MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB
const DEFAULT_ACCEPTED_FORMATS = ['image/png', 'image/jpeg', 'image/webp'];
const MAX_UPLOAD_WIDTH = 500;
const COMPRESSION_QUALITY = 0.8;

/**
 * ImageSourceSelector Component
 * 
 * Provides a unified interface for selecting how entity images should be sourced:
 * placeholder, AI-generated, or user-uploaded.
 */
export const ImageSourceSelector: React.FC<ImageSourceSelectorProps> = ({
  mode,
  onModeChange,
  onImageUpload,
  uploadedImage,
  maxFileSizeBytes = DEFAULT_MAX_FILE_SIZE,
  acceptedFormats = DEFAULT_ACCEPTED_FORMATS,
  disabled = false,
  className = ''
}) => {
  const [dragActive, setDragActive] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(uploadedImage || null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  /**
   * Formats file size for human-readable display
   */
  const formatFileSize = (bytes: number): string => {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  };

  /**
   * Validates the uploaded file against size and format constraints
   */
  const validateFile = useCallback((file: File): string | null => {
    if (!acceptedFormats.includes(file.type)) {
      return `Formato no soportado. Usa: ${acceptedFormats.map(f => f.split('/')[1].toUpperCase()).join(', ')}`;
    }
    if (file.size > maxFileSizeBytes) {
      return `Archivo muy grande. Máximo: ${formatFileSize(maxFileSizeBytes)}`;
    }
    return null;
  }, [acceptedFormats, maxFileSizeBytes]);

  /**
   * Converts a file to base64 string
   */
  const fileToBase64 = (file: File): Promise<string> => {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = () => {
        const result = reader.result as string;
        const base64 = result.split(',')[1];
        resolve(base64);
      };
      reader.onerror = () => reject(new Error('Error al leer el archivo'));
      reader.readAsDataURL(file);
    });
  };

  /**
   * Compresses an image to WebP format using Canvas API
   */
  const compressImageToWebP = (file: File): Promise<string> => {
    return new Promise((resolve, reject) => {
      const img = new Image();
      img.onload = () => {
        const canvas = document.createElement('canvas');
        let width = img.width;
        let height = img.height;

        // Resize if wider than max width while maintaining aspect ratio
        if (width > MAX_UPLOAD_WIDTH) {
          height = Math.round((height * MAX_UPLOAD_WIDTH) / width);
          width = MAX_UPLOAD_WIDTH;
        }

        canvas.width = width;
        canvas.height = height;

        const ctx = canvas.getContext('2d');
        if (!ctx) {
          reject(new Error('Canvas not supported'));
          return;
        }

        ctx.drawImage(img, 0, 0, width, height);

        // Convert to WebP
        const dataUrl = canvas.toDataURL('image/webp', COMPRESSION_QUALITY);
        const base64 = dataUrl.split(',')[1];
        resolve(base64);
      };
      img.onerror = () => reject(new Error('Error loading image'));
      img.src = URL.createObjectURL(file);
    });
  };

  /**
   * Processes the selected/dropped file
   */
  const processFile = useCallback(async (file: File) => {
    setError(null);

    const validationError = validateFile(file);
    if (validationError) {
      setError(validationError);
      return;
    }

    try {
      // Compress and convert to WebP
      const webpBase64 = await compressImageToWebP(file);
      const webpDataUrl = `data:image/webp;base64,${webpBase64}`;
      
      setPreviewUrl(webpDataUrl);
      onImageUpload?.(webpBase64);
    } catch (err) {
      setError('Error al procesar la imagen');
      console.error('File processing error:', err);
    }
  }, [validateFile, onImageUpload]);

  /**
   * Handles file input change event
   */
  const handleFileSelect = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      processFile(file);
    }
    // Reset input so same file can be selected again
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  }, [processFile]);

  /**
   * Handles drag enter event
   */
  const handleDragEnter = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    if (!disabled) {
      setDragActive(true);
    }
  }, [disabled]);

  /**
   * Handles drag leave event
   */
  const handleDragLeave = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setDragActive(false);
  }, []);

  /**
   * Handles drag over event
   */
  const handleDragOver = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
  }, []);

  /**
   * Handles file drop event
   */
  const handleDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setDragActive(false);

    if (disabled) return;

    const file = e.dataTransfer.files?.[0];
    if (file) {
      processFile(file);
    }
  }, [disabled, processFile]);

  /**
   * Opens the file browser dialog
   */
  const handleBrowseClick = useCallback(() => {
    if (!disabled) {
      fileInputRef.current?.click();
    }
  }, [disabled]);

  /**
   * Clears the uploaded image
   */
  const handleClearImage = useCallback(() => {
    setPreviewUrl(null);
    setError(null);
    onImageUpload?.(null);
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  }, [onImageUpload]);

  /**
   * Handles mode selection change
   */
  const handleModeChange = useCallback((newMode: ImageSourceMode) => {
    if (disabled) return;
    
    onModeChange(newMode);
    
    // Clear uploaded image data when switching away from upload mode
    if (newMode !== 'upload') {
      setPreviewUrl(null);
      setError(null);
      onImageUpload?.(null);
    }
  }, [disabled, onModeChange, onImageUpload]);

  return (
    <div className={`space-y-3 ${className}`}>
      {/* Mode Label */}
      <label className="text-primary text-[10px] uppercase tracking-widest flex items-center gap-2">
        <span className="material-icons text-sm">image</span> Fuente de Imagen
      </label>

      {/* Mode Selection Buttons */}
      <div className="grid grid-cols-3 gap-2">
        {IMAGE_SOURCE_OPTIONS.map((option) => (
          <button
            key={option.value}
            type="button"
            onClick={() => handleModeChange(option.value)}
            disabled={disabled}
            className={`p-2 border text-left transition-all ${
              mode === option.value
                ? 'border-primary bg-primary/20 text-primary'
                : 'border-primary/30 hover:border-primary/60 text-primary/60'
            } disabled:opacity-50 disabled:cursor-not-allowed`}
          >
            <div className="flex items-center gap-2 mb-1">
              <span className="material-icons text-sm">{option.icon}</span>
              <span className="text-[10px] font-bold uppercase">{option.label}</span>
            </div>
            <span className="text-[8px] text-primary/40 block">{option.description}</span>
          </button>
        ))}
      </div>

      {/* Upload Area - Only shown when upload mode is selected */}
      {mode === 'upload' && (
        <div className="space-y-3 animate-in fade-in duration-200">
          {/* Error Message */}
          {error && (
            <div className="bg-danger/20 border border-danger/50 p-2 text-danger text-[10px] flex items-center gap-2">
              <span className="material-icons text-sm">error</span>
              {error}
            </div>
          )}

          {/* Preview or Drop Zone */}
          {previewUrl ? (
            /* Image Preview */
            <div className="relative border border-primary/30 bg-black p-1">
              <img
                src={previewUrl}
                alt="Preview"
                className="w-full h-32 object-contain bg-black"
              />
              <button
                type="button"
                onClick={handleClearImage}
                disabled={disabled}
                className="absolute top-2 right-2 bg-black/80 border border-primary/50 p-1 hover:bg-primary/20 transition-colors disabled:opacity-50"
                aria-label="Eliminar imagen"
              >
                <span className="material-icons text-primary text-sm">close</span>
              </button>
              <div className="absolute bottom-0 left-0 right-0 bg-black/80 p-1 text-[9px] text-primary/60 text-center">
                Imagen cargada correctamente
              </div>
            </div>
          ) : (
            /* Drop Zone */
            <div
              onDragEnter={handleDragEnter}
              onDragLeave={handleDragLeave}
              onDragOver={handleDragOver}
              onDrop={handleDrop}
              onClick={handleBrowseClick}
              className={`border-2 border-dashed p-4 text-center cursor-pointer transition-all ${
                dragActive
                  ? 'border-primary bg-primary/10'
                  : 'border-primary/30 hover:border-primary/60 hover:bg-primary/5'
              } ${disabled ? 'opacity-50 cursor-not-allowed' : ''}`}
            >
              <input
                ref={fileInputRef}
                type="file"
                accept={acceptedFormats.join(',')}
                onChange={handleFileSelect}
                className="hidden"
                disabled={disabled}
              />
              <span className="material-icons text-3xl text-primary/40 mb-2 block">
                {dragActive ? 'download' : 'cloud_upload'}
              </span>
              <p className="text-[10px] text-primary/60 mb-1">
                {dragActive ? (
                  'Suelta el archivo aquí'
                ) : (
                  <>
                    Arrastra una imagen o{' '}
                    <span className="text-primary underline">haz clic para seleccionar</span>
                  </>
                )}
              </p>
              <p className="text-[8px] text-primary/40">
                PNG, JPG, WEBP • Máx. {formatFileSize(maxFileSizeBytes)}
              </p>
            </div>
          )}
        </div>
      )}

      {/* Helper Text for other modes */}
      {mode === 'none' && (
        <p className="text-[9px] text-white/50 flex items-center gap-1">
          <span className="material-icons text-xs">info</span>
          Se usará una imagen de placeholder por defecto
        </p>
      )}
      {mode === 'generate' && (
        <p className="text-[9px] text-white/50 flex items-center gap-1">
          <span className="material-icons text-xs">info</span>
          La IA generará una imagen basada en los parámetros
        </p>
      )}
    </div>
  );
};

export default ImageSourceSelector;
