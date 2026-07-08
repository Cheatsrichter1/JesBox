import React from 'react';
import { useLanguage } from '../i18n.jsx';

export default function LanguageToggle() {
  const { lang, setLang } = useLanguage();

  return (
    <div className="language-toggle">
      <button
        className={`language-toggle-btn${lang === 'en' ? ' selected' : ''}`}
        onClick={() => setLang('en')}
      >
        EN
      </button>
      <button
        className={`language-toggle-btn${lang === 'de' ? ' selected' : ''}`}
        onClick={() => setLang('de')}
      >
        DE
      </button>
    </div>
  );
}
