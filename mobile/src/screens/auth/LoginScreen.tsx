import React, { useState } from 'react';
import {
  View,
  Text,
  TextInput,
  TouchableOpacity,
  StyleSheet,
  Alert,
  ActivityIndicator,
} from 'react-native';
import * as SecureStore from 'expo-secure-store';
import { API_BASE } from '../../api/client';
import { useAuthStore } from '../../store/authStore';
import { AuthResult } from '../../types/api';

export function LoginScreen() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const setSession = useAuthStore((s) => s.setSession);

  async function handleLogin() {
    if (!email.trim() || !password.trim()) {
      Alert.alert('Validation', 'Email and password are required.');
      return;
    }
    setLoading(true);
    try {
      const response = await fetch(`${API_BASE}/auth/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password }),
      });
      if (!response.ok) {
        const body = await response.json().catch(() => ({}));
        throw new Error(body?.detail ?? body?.title ?? 'Invalid credentials');
      }
      const data: AuthResult = await response.json();

      await SecureStore.setItemAsync('accessToken', data.accessToken);
      await SecureStore.setItemAsync('refreshToken', data.refreshToken);
      await SecureStore.setItemAsync('userId', data.userId);
      await SecureStore.setItemAsync('name', data.name);
      await SecureStore.setItemAsync('email', data.email);
      await SecureStore.setItemAsync('role', data.role);

      setSession({
        accessToken: data.accessToken,
        refreshToken: data.refreshToken,
        userId: data.userId,
        name: data.name,
        email: data.email,
        role: data.role,
      });
    } catch (err: any) {
      Alert.alert('Login Failed', err.message ?? 'Unknown error');
    } finally {
      setLoading(false);
    }
  }

  return (
    <View style={styles.container}>
      <Text style={styles.title}>Flow</Text>
      <Text style={styles.subtitle}>Innovation Management</Text>
      <TextInput
        style={styles.input}
        placeholder="Email"
        autoCapitalize="none"
        keyboardType="email-address"
        value={email}
        onChangeText={setEmail}
      />
      <TextInput
        style={styles.input}
        placeholder="Password"
        secureTextEntry
        value={password}
        onChangeText={setPassword}
      />
      {loading ? (
        <ActivityIndicator size="large" color="#2563EB" style={{ marginTop: 8 }} />
      ) : (
        <TouchableOpacity style={styles.button} onPress={handleLogin}>
          <Text style={styles.buttonText}>Sign In</Text>
        </TouchableOpacity>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, justifyContent: 'center', padding: 24, backgroundColor: '#F9FAFB' },
  title: { fontSize: 36, fontWeight: 'bold', color: '#1E3A5F', textAlign: 'center' },
  subtitle: { fontSize: 14, color: '#6B7280', textAlign: 'center', marginBottom: 36 },
  input: {
    borderWidth: 1, borderColor: '#D1D5DB', borderRadius: 8,
    padding: 12, marginBottom: 12, backgroundColor: '#FFF', fontSize: 16,
  },
  button: {
    backgroundColor: '#2563EB', borderRadius: 8,
    padding: 14, alignItems: 'center', marginTop: 8,
  },
  buttonText: { color: '#FFF', fontWeight: '600', fontSize: 16 },
});
