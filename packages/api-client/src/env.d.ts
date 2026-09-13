// Ambient declaration so this package type-checks standalone (without depending on the
// `vite` package just for `ImportMetaEnv`). Consuming apps' own Vite-provided types
// take over at build time; this is only for `tsc --noEmit` here.
interface ImportMetaEnv {
  readonly VITE_API_BASE_URL?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
