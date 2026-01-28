/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** Backend URL for Vite proxy (development only) */
  readonly VITE_BACKEND_URL?: string;
  /** Full API URL for production (e.g., https://api.example.com/api) */
  readonly VITE_API_URL?: string;
  /** Application name */
  readonly VITE_APP_NAME?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
