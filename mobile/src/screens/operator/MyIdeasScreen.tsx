import React from 'react';
import {
  View, Text, FlatList, TouchableOpacity,
  StyleSheet, ActivityIndicator,
} from 'react-native';
import { useQuery } from '@tanstack/react-query';
import { apiFetch, logout } from '../../api/client';
import { useAuthStore } from '../../store/authStore';
import { IdeaSummary } from '../../types/api';

const STATUS_COLORS: Record<string, string> = {
  Draft: '#6B7280',
  UnderReview: '#D97706',
  Approved: '#059669',
  Rejected: '#DC2626',
};

export function MyIdeasScreen({ navigation }: any) {
  const session = useAuthStore((s) => s.session);

  const { data: ideas, isLoading, error, refetch } = useQuery<IdeaSummary[]>({
    queryKey: ['ideas'],
    queryFn: () => apiFetch<IdeaSummary[]>('/ideas'),
  });

  if (isLoading) {
    return <ActivityIndicator style={{ flex: 1 }} size="large" color="#2563EB" />;
  }
  if (error) {
    return <Text style={styles.errorText}>{(error as Error).message}</Text>;
  }

  return (
    <View style={styles.container}>
      <TouchableOpacity
        style={styles.newButton}
        onPress={() => navigation.navigate('SubmitIdea')}
      >
        <Text style={styles.newButtonText}>+ New Idea</Text>
      </TouchableOpacity>

      <FlatList
        data={ideas ?? []}
        keyExtractor={(item) => item.id}
        onRefresh={refetch}
        refreshing={isLoading}
        renderItem={({ item }) => (
          <TouchableOpacity
            style={styles.card}
            onPress={() => navigation.navigate('IdeaDetail', { id: item.id })}
          >
            <Text style={styles.cardTitle}>{item.title}</Text>
            <View style={styles.metaRow}>
              <Text style={[styles.status, { color: STATUS_COLORS[item.status] ?? '#6B7280' }]}>
                {item.status}
              </Text>
              <Text style={styles.priority}>{item.priority}</Text>
            </View>
            <Text style={styles.problem} numberOfLines={2}>{item.problem}</Text>
          </TouchableOpacity>
        )}
        ListEmptyComponent={
          <Text style={styles.emptyText}>No ideas yet. Tap "+ New Idea" to get started.</Text>
        }
        contentContainerStyle={{ paddingBottom: 24 }}
      />

      <TouchableOpacity
        style={styles.logoutBtn}
        onPress={() => logout(session?.refreshToken ?? '')}
      >
        <Text style={styles.logoutText}>Sign Out</Text>
      </TouchableOpacity>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#F9FAFB', padding: 16 },
  newButton: {
    backgroundColor: '#2563EB', borderRadius: 8,
    padding: 12, alignItems: 'center', marginBottom: 16,
  },
  newButtonText: { color: '#FFF', fontWeight: '600', fontSize: 15 },
  card: {
    backgroundColor: '#FFF', borderRadius: 8, padding: 14,
    marginBottom: 12, borderWidth: 1, borderColor: '#E5E7EB',
  },
  cardTitle: { fontSize: 16, fontWeight: '600', color: '#111827', marginBottom: 6 },
  metaRow: { flexDirection: 'row', gap: 8, marginBottom: 4 },
  status: { fontSize: 13, fontWeight: '500' },
  priority: { color: '#6B7280', fontSize: 12, alignSelf: 'center' },
  problem: { color: '#6B7280', fontSize: 13, marginTop: 4 },
  emptyText: { textAlign: 'center', color: '#9CA3AF', marginTop: 48, fontSize: 15, lineHeight: 22 },
  errorText: { textAlign: 'center', color: '#EF4444', marginTop: 48, padding: 16 },
  logoutBtn: { alignItems: 'center', padding: 14, marginTop: 8 },
  logoutText: { color: '#6B7280', fontSize: 14 },
});
