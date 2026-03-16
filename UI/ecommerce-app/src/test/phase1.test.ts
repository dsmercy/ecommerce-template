import { describe, it, expect } from 'vitest';
import { formatPrice, formatDate, cn } from '@lib/utils';
import { logger } from '@lib/logger';

describe('Phase 1 — alias resolution', () => {
  it('resolves @lib/utils via path alias', () => {
    expect(typeof formatPrice).toBe('function');
  });

  it('resolves @lib/logger via path alias', () => {
    expect(typeof logger.info).toBe('function');
  });
});

describe('formatPrice', () => {
  it('formats a number as USD currency', () => {
    expect(formatPrice(19.99)).toBe('$19.99');
  });

  it('formats zero correctly', () => {
    expect(formatPrice(0)).toBe('$0.00');
  });

  it('formats large amounts with commas', () => {
    expect(formatPrice(1234.5)).toBe('$1,234.50');
  });
});

describe('formatDate', () => {
  it('formats an ISO date string', () => {
    const result = formatDate('2026-03-16T00:00:00.000Z');
    expect(result).toMatch(/Mar\s+1[56],\s+2026/); // handles UTC offset
  });

  it('accepts a Date object', () => {
    const result = formatDate(new Date('2026-01-01'));
    expect(result).toContain('2026');
  });
});

describe('cn (classname merger)', () => {
  it('merges simple class names', () => {
    expect(cn('foo', 'bar')).toBe('foo bar');
  });

  it('resolves Tailwind conflicts (last class wins)', () => {
    const result = cn('p-2', 'p-4');
    expect(result).toBe('p-4');
  });

  it('handles conditional classes', () => {
    const active = true;
    expect(cn('base', active && 'active')).toBe('base active');
  });

  it('ignores falsy values', () => {
    expect(cn('base', false, undefined, null)).toBe('base');
  });
});
