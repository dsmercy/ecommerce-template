import * as Sentry from '@sentry/react';

type LogLevel = 'debug' | 'info' | 'warn' | 'error';
type SentryLevel = 'debug' | 'info' | 'warning' | 'error';

interface LogMeta {
  [key: string]: unknown;
}

function toSentryLevel(level: LogLevel): SentryLevel {
  return level === 'warn' ? 'warning' : level;
}

function log(level: LogLevel, message: string, meta?: LogMeta): void {
  const entry = {
    level,
    message,
    timestamp: new Date().toISOString(),
    ...meta,
  };

  Sentry.addBreadcrumb({
    category: 'log',
    message,
    level: toSentryLevel(level),
    data: meta,
  });

  if (import.meta.env.DEV) {
    const serialized = JSON.stringify(entry, null, 2);
    if (level === 'warn') {
      console.warn('[WARN]', serialized);
    } else if (level === 'error') {
      console.error('[ERROR]', serialized);
    } else {
      console.log(`[${level.toUpperCase()}]`, serialized);
    }
  }

  if (level === 'error') {
    Sentry.captureMessage(message, { level: 'error', extra: meta });
  }
}

export const logger = {
  debug: (message: string, meta?: LogMeta) => log('debug', message, meta),
  info:  (message: string, meta?: LogMeta) => log('info',  message, meta),
  warn:  (message: string, meta?: LogMeta) => log('warn',  message, meta),
  error: (message: string, meta?: LogMeta) => log('error', message, meta),
};
