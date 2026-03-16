// web-vitals v5: FID replaced by INP (Interaction to Next Paint)
import { onCLS, onINP, onLCP, onFCP, onTTFB, type Metric } from 'web-vitals';
import * as Sentry from '@sentry/react';
import { logger } from '@lib/logger';

function sendToAnalytics(metric: Metric): void {
  logger.info('Web vital', {
    name: metric.name,
    value: Math.round(metric.value),
    id: metric.id,
  });

  // Forward to Sentry as a custom tag — setMeasurement API varies by SDK version
  Sentry.getCurrentScope().setTag(`vital.${metric.name.toLowerCase()}`, Math.round(metric.value));
}

export function reportWebVitals(): void {
  onCLS(sendToAnalytics);  // Cumulative Layout Shift
  onINP(sendToAnalytics);  // Interaction to Next Paint (replaces FID in v5)
  onLCP(sendToAnalytics);  // Largest Contentful Paint
  onFCP(sendToAnalytics);  // First Contentful Paint
  onTTFB(sendToAnalytics); // Time to First Byte
}
