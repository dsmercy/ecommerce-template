import api from '@api/axios';
import type { ApiResponse, AuthResponse } from '@types/api';

// ─── Request shapes ───────────────────────────────────────────────────────────

export interface RegisterRequest {
  name: string;
  email: string;
  password: string;
  phone?: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RefreshRequest {
  accessToken: string | null;
  refreshToken: string;
}

export interface RevokeRequest {
  refreshToken: string;
}

// ─── Endpoint functions ───────────────────────────────────────────────────────

export async function registerUser(
  body: RegisterRequest
): Promise<ApiResponse<AuthResponse>> {
  const { data } = await api.post<ApiResponse<AuthResponse>>('/api/auth/register', body);
  return data;
}

export async function loginUser(
  body: LoginRequest
): Promise<ApiResponse<AuthResponse>> {
  const { data } = await api.post<ApiResponse<AuthResponse>>('/api/auth/login', body);
  return data;
}

export async function refreshToken(
  body: RefreshRequest
): Promise<ApiResponse<AuthResponse>> {
  const { data } = await api.post<ApiResponse<AuthResponse>>('/api/auth/refresh', body);
  return data;
}

export async function revokeToken(
  body: RevokeRequest
): Promise<ApiResponse<null>> {
  const { data } = await api.post<ApiResponse<null>>('/api/auth/revoke', body);
  return data;
}
