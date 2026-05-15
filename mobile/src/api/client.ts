import * as SecureStore from 'expo-secure-store';
import { useAuthStore } from '../store/authStore';

// Android emulator: http://10.0.2.2:5153/api/v1
// iOS simulator / local machine: http://localhost:5153/api/v1
// Real device on LAN: replace with your machine's LAN IP
export const API_BASE = 'http://10.0.2.2:5153/api/v1';

const STORE_KEYS = ['accessToken', 'refreshToken', 'userId', 'name', 'email', 'role'] as const;

export async function apiFetch<T>(
  path: string,
  options: RequestInit = {}
): Promise<T> {
  const token = await SecureStore.getItemAsync('accessToken');

  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(options.headers as Record<string, string>),
  };
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  const response = await fetch(`${API_BASE}${path}`, { ...options, headers });

  if (response.status === 401) {
    for (const key of STORE_KEYS) {
      await SecureStore.deleteItemAsync(key);
    }
    useAuthStore.getState().clearSession();
    throw new Error('Session expired. Please log in again.');
  }

  if (!response.ok) {
    let message = `HTTP ${response.status}`;
    try {
      const body = await response.json();
      message = body?.detail ?? body?.title ?? message;
    } catch {
      // non-JSON error body
    }
    throw new Error(message);
  }

  if (response.status === 204) {
    return undefined as unknown as T;
  }

  return response.json() as Promise<T>;
}

export async function logout(refreshToken: string): Promise<void> {
  try {
    await apiFetch('/auth/logout', {
      method: 'POST',
      body: JSON.stringify({ refreshToken }),
    });
  } catch {
    // best-effort
  }
  for (const key of STORE_KEYS) {
    await SecureStore.deleteItemAsync(key);
  }
  useAuthStore.getState().clearSession();
}
