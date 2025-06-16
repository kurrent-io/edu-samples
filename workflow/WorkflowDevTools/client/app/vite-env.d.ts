/// <reference types="vite/client" />

interface ViteTypeOptions {
  strictImportMetaEnv: unknown
}

interface ImportMetaEnv {
  readonly VITE_CODESPACES?: string
  readonly VITE_CODESPACE_NAME?: string
  readonly VITE_GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
