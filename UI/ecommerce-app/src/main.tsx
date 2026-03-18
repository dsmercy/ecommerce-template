// wdyr MUST be the very first import in dev — patches React before any component loads
if (import.meta.env.DEV) await import('./wdyr');

import React from 'react';
import ReactDOM from 'react-dom/client';
import App from './App';
import { reportWebVitals } from '@lib/vitals';
import { logger } from '@lib/logger';
import './index.css';

// Global error listeners — catch errors that escape React error boundaries
window.addEventListener('error', (event) => {
  logger.error('Uncaught global error', {
    message: event.message,
    filename: event.filename,
    lineno: event.lineno,
    colno: event.colno,
  });
});

window.addEventListener('unhandledrejection', (event) => {
  logger.error('Unhandled promise rejection', {
    reason: event.reason instanceof Error ? event.reason.message : String(event.reason),
  });
});

// Development-only: turn React warnings into thrown errors
if (import.meta.env.DEV) {
  const originalError = console.error;
  console.error = (...args: unknown[]) => {
    const message = typeof args[0] === 'string' ? args[0] : '';
    if (message.includes('Warning:') && !message.includes('act(')) {
      throw new Error(args.join(' '));
    }
    originalError(...args);
  };
}

const rootEl = document.getElementById('root');
if (!rootEl) throw new Error('Root element #root not found in index.html');

ReactDOM.createRoot(rootEl).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);

reportWebVitals();
