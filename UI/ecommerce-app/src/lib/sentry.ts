import * as Sentry from '@sentry/react';

export function initSentry(): void {
  const dsn = import.meta.env.VITE_SENTRY_DSN as string | undefined;

  if (!dsn) {
    console.warn('[Sentry] VITE_SENTRY_DSN not set — error monitoring disabled');
    return;
  }

  const isProd = import.meta.env.PROD as boolean;

  Sentry.init({
    dsn,
    environment: (import.meta.env.VITE_APP_ENV as string) ?? 'development',
    tracesSampleRate: isProd ? 0.2 : 0,
    replaysSessionSampleRate: 0.1,
    replaysOnErrorSampleRate: 1.0,
    integrations: [Sentry.browserTracingIntegration(), Sentry.replayIntegration()],
    beforeSend(event) {
      // Strip PII — never send raw passwords
      if (event.request?.data && typeof event.request.data === 'object') {
        const data = event.request.data as Record<string, unknown>;
        delete data['password'];
        delete data['refreshToken'];
      }
      return event;
    },
  });
}
