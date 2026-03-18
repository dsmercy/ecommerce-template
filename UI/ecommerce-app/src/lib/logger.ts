type LogLevel = 'debug' | 'info' | 'warn' | 'error';

interface LogMeta {
  [key: string]: unknown;
}

interface LogEntry {
  level: LogLevel;
  message: string;
  timestamp: string;
  app: string;
  env: string;
  [key: string]: unknown;
}

const APP_LABEL = 'ecommerce-ui';
const ENV_LABEL = (import.meta.env.VITE_APP_ENV as string) ?? 'development';

// Browser logs are forwarded to Loki via the .NET API endpoint POST /api/logs
// VITE_API_BASE_URL is always set — no separate log-receiver service needed
const API_BASE = (import.meta.env.VITE_API_BASE_URL as string) ?? '';

/** Fire-and-forget: POST log entry to .NET API → Serilog → Loki */
function ship(entry: LogEntry): void {
  if (!API_BASE) return;

  fetch(`${API_BASE}/api/logs`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(entry),
    keepalive: true, // survives page unload / navigation
  }).catch(() => {
    // Silently swallow — logging must never crash the app
  });
}

function log(level: LogLevel, message: string, meta?: LogMeta): void {
  const entry: LogEntry = {
    level,
    message,
    timestamp: new Date().toISOString(),
    app: APP_LABEL,
    env: ENV_LABEL,
    ...meta,
  };

  // Always print structured JSON to the browser console in dev
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

  // Ship to .NET API → Serilog → Loki
  ship(entry);
}

export const logger = {
  debug: (message: string, meta?: LogMeta) => log('debug', message, meta),
  info:  (message: string, meta?: LogMeta) => log('info',  message, meta),
  warn:  (message: string, meta?: LogMeta) => log('warn',  message, meta),
  error: (message: string, meta?: LogMeta) => log('error', message, meta),
};
