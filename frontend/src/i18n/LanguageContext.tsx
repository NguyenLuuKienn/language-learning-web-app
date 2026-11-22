import React, { createContext, useState, useEffect, useMemo, useCallback, type ReactNode } from 'react';
import { en } from './translations/en';
import { vi } from './translations/vi';

export type SupportedLanguage = 'en' | 'vi';

type TranslationSchema = typeof en;

interface LanguageContextType {
  language: SupportedLanguage;
  setLanguage: (lang: SupportedLanguage) => void;
  toggleLanguage: () => void;
  t: (path: string, params?: Record<string, string | number>) => string;
  translations: TranslationSchema;
}

const dictionaries: Record<SupportedLanguage, TranslationSchema> = {
  en,
  vi
};

export const LanguageContext = createContext<LanguageContextType | undefined>(undefined);

export const LanguageProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [language, setLanguageState] = useState<SupportedLanguage>(() => {
    const saved = localStorage.getItem('app_language') as SupportedLanguage;
    return saved === 'vi' || saved === 'en' ? saved : 'en';
  });

  const setLanguage = useCallback((lang: SupportedLanguage) => {
    setLanguageState(lang);
    localStorage.setItem('app_language', lang);
  }, []);

  const toggleLanguage = useCallback(() => {
    setLanguageState((current) => {
      const next = current === 'en' ? 'vi' : 'en';
      localStorage.setItem('app_language', next);
      return next;
    });
  }, []);

  useEffect(() => {
    document.documentElement.lang = language;
  }, [language]);

  const activeDictionary = useMemo(() => dictionaries[language] || en, [language]);

  const t = useCallback((path: string, params?: Record<string, string | number>): string => {
    const parts = path.split('.');
    let current: unknown = activeDictionary;

    for (const part of parts) {
      if (current && typeof current === 'object' && part in current) {
        current = (current as Record<string, unknown>)[part];
      } else {
        let fallback: unknown = en;
        for (const fbPart of parts) {
          if (fallback && typeof fallback === 'object' && fbPart in fallback) {
            fallback = (fallback as Record<string, unknown>)[fbPart];
          } else {
            fallback = path;
            break;
          }
        }
        current = fallback;
        break;
      }
    }

    if (typeof current !== 'string') {
      return path;
    }

    let result = current;
    if (params) {
      Object.entries(params).forEach(([k, v]) => {
        result = result.replace(new RegExp(`\\{${k}\\}`, 'g'), String(v));
      });
    }

    return result;
  }, [activeDictionary]);

  const contextValue = useMemo(() => ({
    language,
    setLanguage,
    toggleLanguage,
    t,
    translations: activeDictionary
  }), [language, setLanguage, toggleLanguage, t, activeDictionary]);

  return (
    <LanguageContext.Provider value={contextValue}>
      {children}
    </LanguageContext.Provider>
  );
};
