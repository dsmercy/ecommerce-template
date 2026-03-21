import { useState, useEffect } from 'react';

/**
 * Delays updating the returned value until `delay` ms have passed
 * since the last change to `value`. Used for search inputs to avoid
 * firing a network request on every keystroke.
 */
export function useDebounce<T>(value: T, delay = 300): T {
  const [debouncedValue, setDebouncedValue] = useState<T>(value);

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedValue(value), delay);
    return () => clearTimeout(timer);
  }, [value, delay]);

  return debouncedValue;
}
