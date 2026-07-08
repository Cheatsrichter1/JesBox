import React, { useState } from 'react';
import { useLanguage } from '../i18n.jsx';

export default function JoinScreen({ onJoin, error }) {
  const { t } = useLanguage();
  const [name, setName] = useState('');
  const [code, setCode] = useState('');

  const canSubmit = name.trim().length > 0 && code.trim().length === 4;

  const submit = (e) => {
    e.preventDefault();
    if (!canSubmit) return;
    onJoin(name.trim(), code.trim());
  };

  return (
    <div className="screen">
      <div className="cross">✝</div>
      <h1 className="brand">JesBox</h1>
      <p className="subtitle">{t('join.subtitle')}</p>

      <form className="field" onSubmit={submit} style={{ alignItems: 'center' }}>
        <label htmlFor="code">{t('join.roomCodeLabel')}</label>
        <input
          id="code"
          type="text"
          inputMode="text"
          autoCapitalize="characters"
          maxLength={4}
          placeholder="ABCD"
          value={code}
          onChange={(e) => setCode(e.target.value.toUpperCase().replace(/[^A-Z0-9]/g, ''))}
        />

        <label htmlFor="name">{t('join.nameLabel')}</label>
        <input
          id="name"
          type="text"
          maxLength={20}
          placeholder={t('join.namePlaceholder')}
          value={name}
          onChange={(e) => setName(e.target.value)}
        />

        <div className="error-text">{error}</div>

        <button type="submit" className="btn" disabled={!canSubmit}>
          {t('join.button')}
        </button>
      </form>
    </div>
  );
}
