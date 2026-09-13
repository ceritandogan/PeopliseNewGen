import en from "./locales/en.json";
import tr from "./locales/tr.json";

/** react-i18next `resources` shape — pass straight into `i18n.init({ resources })`. */
export const resources = {
  en: { translation: en },
  tr: { translation: tr },
} as const;

export const supportedLanguages = ["en", "tr"] as const;
export type SupportedLanguage = (typeof supportedLanguages)[number];

export const defaultLanguage: SupportedLanguage = "tr";

/** The shape of one locale file — every locale must satisfy this so keys stay in sync. */
export type TranslationResource = typeof en;
