/**
 * Application-wide constants
 * Single Responsibility: Centralized configuration values
 */

// API Configuration
// In development: uses '/api' which Vite proxies to VITE_BACKEND_URL
// In production: uses VITE_API_URL directly (e.g., 'https://api.example.com/api')
export const API_BASE = import.meta.env.VITE_API_URL || '/api';
export const API_TIMEOUT = 30000; // 30 seconds

// Token Storage Keys (using unique prefix for namespace isolation)
export const TOKEN_KEY = 'omega_access_token';
export const REFRESH_TOKEN_KEY = 'omega_refresh_token';
export const USER_KEY = 'omega_user';

// Security Configuration
export const MAX_LOGIN_ATTEMPTS = 5;
export const LOCKOUT_DURATION_MS = 15 * 60 * 1000; // 15 minutes
export const PASSWORD_MIN_LENGTH = 8;
export const PASSWORD_MAX_LENGTH = 128;

// Rate Limiting (client-side awareness)
export const RATE_LIMIT_WINDOW_MS = 60000; // 1 minute
export const MAX_REQUESTS_PER_WINDOW = 100;

// Input Validation Limits
export const MAX_INPUT_LENGTH = {
  email: 254,
  password: 128,
  displayName: 50,
  entityName: 100,
  description: 2000,
  inviteCode: 20,
} as const;

// Transition Timing (for animations)
export const TRANSITION_OUT_DURATION = 400;
export const TRANSITION_IN_DURATION = 500;

// Token Refresh Timing
/** How early (in ms) to proactively refresh a token before it expires */
export const TOKEN_REFRESH_THRESHOLD_MS = 5 * 60 * 1000; // 5 minutes
/** Safety buffer (in ms) to consider a token effectively expired */
export const TOKEN_EXPIRY_BUFFER_MS = 30_000; // 30 seconds

// Terminal Log Limits
/** Default max entries kept in the terminal log */
export const TERMINAL_LOG_DEFAULT = 12;
/** Max entries for pages with a compact log panel */
export const TERMINAL_LOG_COMPACT = 6;
/** Max entries for standard-sized log panels */
export const TERMINAL_LOG_STANDARD = 8;
/** Max entries for expanded log panels */
export const TERMINAL_LOG_LARGE = 10;
/** Max entries for admin/detail-heavy log panels */
export const TERMINAL_LOG_ADMIN = 20;

// Embedding / Document Processing Defaults
/** Default number of documents processed per batch when generating embeddings */
export const EMBEDDING_BATCH_SIZE_DEFAULT = 10;
/** Default maximum total documents to process in one run */
export const EMBEDDING_MAX_DOCUMENTS_DEFAULT = 100;
/** Upper bound for batch size allowed via the admin UI */
export const EMBEDDING_BATCH_SIZE_MAX = 50;
/** Upper bound for max documents allowed via the admin UI */
export const EMBEDDING_MAX_DOCUMENTS_MAX = 1000;

// File Upload Limits
/** Maximum allowed size for PDF manual uploads */
export const PDF_MAX_FILE_SIZE = 50 * 1024 * 1024; // 50 MB
/** Maximum allowed size for image uploads */
export const IMAGE_MAX_FILE_SIZE = 5 * 1024 * 1024; // 5 MB
/** Maximum dimension (px) for uploaded/compressed images */
export const IMAGE_MAX_UPLOAD_WIDTH = 500;
/** JPEG compression quality for uploaded images (0–1) */
export const IMAGE_COMPRESSION_QUALITY = 0.8;
/** MIME types accepted for image uploads */
export const IMAGE_ACCEPTED_FORMATS: string[] = ['image/png', 'image/jpeg', 'image/webp'];

// UX Timing
/** Delay (ms) before clearing a transient "copied to clipboard" indicator */
export const COPY_FEEDBACK_DURATION_MS = 2_000;
/** Delay (ms) before navigating away after a successful generator save */
export const GENERATOR_NAVIGATE_DELAY_MS = 1_000;

// Placeholder Images
export const PLACEHOLDER_IMAGES = {
  character: 'https://images.unsplash.com/photo-1683322001857-f4d932a40672?q=80&w=400&auto=format&fit=crop',
  solarSystem: 'https://images.unsplash.com/photo-1464802686167-b939a6910659?q=80&w=400&auto=format&fit=crop',
  vehicle: 'https://images.unsplash.com/photo-1702499903230-867455db1752?q=80&w=400&auto=format&fit=crop',
} as const;

// Generator Options
export const GENERATOR_OPTIONS = {
  species: [
    { value: 'human', label: 'Humano' },
    { value: 'android', label: 'Androide' },
    { value: 'xenomorph', label: 'Xenomorfo' },
    { value: 'cyber-enhanced', label: 'Cyber-Aumentado' },
  ],
  roles: [
    { value: 'operative', label: 'Operativo' },
    { value: 'hacker', label: 'Netrunner' },
    { value: 'medic', label: 'Médico de Combate' },
    { value: 'bounty-hunter', label: 'Caza-Recompensas' },
  ],
  morphology: ['MASCULINE', 'FEMININE', 'NEUTRAL'],
  spectralClass: [
    { value: 'M', label: 'Red Dwarf' },
    { value: 'G', label: 'Yellow Sun' },
    { value: 'O', label: 'Blue Giant' },
  ],
  vehicleType: [
    { value: 'starship', label: 'Nave Espacial' },
    { value: 'rover', label: 'Rover Terrestre' },
    { value: 'mech', label: 'Mech de Combate' },
  ],
  vehicleClass: [
    { value: 'interceptor', label: 'Interceptor Ligero' },
    { value: 'freighter', label: 'Carguero Pesado' },
    { value: 'explorer', label: 'Explorador de Larga Distancia' },
  ],
} as const;

// Category Configuration
export const CATEGORIES = [
  { id: 'PLANETS', label: 'PLANETAS', icon: 'public' },
  { id: 'CHARACTERS', label: 'PERSONAJES', icon: 'face' },
  { id: 'VEHICLES', label: 'VEHÍCULOS', icon: 'rocket_launch' },
] as const;
