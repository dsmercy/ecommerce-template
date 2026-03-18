import { onCLS, onINP, onLCP, onFCP, onTTFB, type Metric } from 'web-vitals';
import { logger } from '@lib/logger';

function sendToAnalytics(metric: Metric): void {
  logger.info('web-vital', {
    name: metric.name,
    value: Math.round(metric.value),
    id: metric.id,
    rating: metric.rating, // 'good' | 'needs-improvement' | 'poor'
  });
}

export function reportWebVitals(): void {
  onCLS(sendToAnalytics);
  onINP(sendToAnalytics);
  onLCP(sendToAnalytics);
  onFCP(sendToAnalytics);
  onTTFB(sendToAnalytics);
}
