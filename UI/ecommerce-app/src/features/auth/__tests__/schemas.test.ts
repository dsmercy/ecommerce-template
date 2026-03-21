import { describe, it, expect } from 'vitest';
import { registerSchema, loginSchema } from '@features/auth/schemas/authSchemas';

// ─── registerSchema ───────────────────────────────────────────────────────────

describe('registerSchema', () => {
  const valid = {
    name: 'Alice Smith',
    email: 'alice@example.com',
    password: 'Password1',
    phone: '07700900000',
  };

  it('accepts a fully valid payload', () => {
    expect(registerSchema.safeParse(valid).success).toBe(true);
  });

  it('accepts when phone is omitted (optional field)', () => {
    const { phone: _p, ...noPhone } = valid;
    expect(registerSchema.safeParse(noPhone).success).toBe(true);
  });

  it('accepts when phone is an empty string (treated as optional)', () => {
    expect(registerSchema.safeParse({ ...valid, phone: '' }).success).toBe(true);
  });

  // name
  it('rejects empty name', () => {
    const result = registerSchema.safeParse({ ...valid, name: '' });
    expect(result.success).toBe(false);
    if (!result.success) {
      const fields = result.error.flatten().fieldErrors;
      expect(fields.name).toBeDefined();
    }
  });

  it('rejects name longer than 150 characters', () => {
    const result = registerSchema.safeParse({ ...valid, name: 'A'.repeat(151) });
    expect(result.success).toBe(false);
  });

  // email
  it('rejects invalid email format', () => {
    const result = registerSchema.safeParse({ ...valid, email: 'not-an-email' });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.flatten().fieldErrors.email).toBeDefined();
    }
  });

  it('rejects email without TLD', () => {
    expect(registerSchema.safeParse({ ...valid, email: 'user@domain' }).success).toBe(false);
  });

  // password length
  it('rejects password shorter than 8 characters', () => {
    const result = registerSchema.safeParse({ ...valid, password: 'Abc1' });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.flatten().fieldErrors.password).toBeDefined();
    }
  });

  // password uppercase rule
  it('rejects password with no uppercase letter', () => {
    const result = registerSchema.safeParse({ ...valid, password: 'password1' });
    expect(result.success).toBe(false);
    if (!result.success) {
      const msgs = result.error.flatten().fieldErrors.password ?? [];
      expect(msgs.some((m) => m.toLowerCase().includes('uppercase'))).toBe(true);
    }
  });

  // password digit rule
  it('rejects password with no digit', () => {
    const result = registerSchema.safeParse({ ...valid, password: 'PasswordNoDigit' });
    expect(result.success).toBe(false);
    if (!result.success) {
      const msgs = result.error.flatten().fieldErrors.password ?? [];
      expect(msgs.some((m) => m.toLowerCase().includes('digit'))).toBe(true);
    }
  });

  it('accepts password that is exactly 8 characters with upper + digit', () => {
    expect(registerSchema.safeParse({ ...valid, password: 'Abc1Xyz!' }).success).toBe(true);
  });

  // phone length
  it('rejects phone longer than 20 characters', () => {
    const result = registerSchema.safeParse({ ...valid, phone: '1'.repeat(21) });
    expect(result.success).toBe(false);
  });

  it('accepts phone exactly 20 characters', () => {
    expect(registerSchema.safeParse({ ...valid, phone: '1'.repeat(20) }).success).toBe(true);
  });
});

// ─── loginSchema ──────────────────────────────────────────────────────────────

describe('loginSchema', () => {
  const valid = { email: 'alice@example.com', password: 'any-value-123' };

  it('accepts valid login payload', () => {
    expect(loginSchema.safeParse(valid).success).toBe(true);
  });

  it('rejects invalid email', () => {
    const result = loginSchema.safeParse({ ...valid, email: 'bad-email' });
    expect(result.success).toBe(false);
  });

  it('rejects empty password', () => {
    const result = loginSchema.safeParse({ ...valid, password: '' });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.flatten().fieldErrors.password).toBeDefined();
    }
  });

  it('does NOT enforce password complexity on login (only min length 1)', () => {
    // Login schema deliberately has no complexity rules — the API validates
    expect(loginSchema.safeParse({ ...valid, password: 'simple' }).success).toBe(true);
  });

  it('rejects missing email field entirely', () => {
    const result = loginSchema.safeParse({ password: 'Password1' });
    expect(result.success).toBe(false);
  });

  it('rejects missing password field entirely', () => {
    const result = loginSchema.safeParse({ email: 'a@b.com' });
    expect(result.success).toBe(false);
  });
});
