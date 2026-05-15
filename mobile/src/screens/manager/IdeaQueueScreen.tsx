import React from 'react';
import {
  View, Text, FlatList, TouchableOpacity,
  StyleSheet, ActivityIndicator,
} from 'react-native';
import { useQuery } from '@tanstack/react-query';
import { apiFetch } from '../../api/client';
import { IdeaSummary } from '../../types/api';

const STATUS_COLORS: Record<string, string> = {
  Draft: '#6B7280',
  UnderReview: '#D97706',
  Approved: '#059669',
  Rejected: '#DC2626',
};

export function IdeaQueueScreen({ navigation }: any) {
  const { data: ideas, isLoading, isFetching, error, refetch } = useQuery<IdeaSummary[]>({
    queryKey: ['ideas', 'all'],
    queryFn: () => apiFetch<IdeaSummary[]>('/ideas'),
  });

  if (isLoading) {
    return <ActivityIndicator style={{ flex: 1 }} size="large" color="#2563EB" />;
  }
  if (error) {
    return <Text style={styles.errorText}>{(error as Error).message}</Text>;
  }

  // Ideas under review first, then rest
  const pending = (ideas ?? []).filter((i) => i.status === 'UnderReview');
  const rest = (ideas ?? []).filter((i) => i.status !== 'UnderReview');
  const sorted = [...pending, ...rest];

  return (
    <View style={styles.container}>
      <FlatList
        data={sorted}
        keyExtractor={(item) => item.id}
        onRefresh={() => { refetch(); }}
        refreshing={isFetching}
        renderItem={({ item }) => (
          <TouchableOpacity
            style={[styles.card, item.status === 'UnderReview' && styles.cardHighlight]}
            onPress={() => navigation.navigate('ManagerIdeaDetail', { id: item.id })}
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
          <Text style={styles.emptyText}>No ideas submitted yet.</Text>
        }
        contentContainerStyle={{ paddingBottom: 24 }}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#F9FAFB', padding: 16 },
  card: {
    backgroundColor: '#FFF', borderRadius: 8, padding: 14,
    marginBottom: 12, borderWidth: 1, borderColor: '#E5E7EB',
  },
  cardHighlight: { borderColor: '#FCD34D', backgroundColor: '#FFFBEB' },
  cardTitle: { fontSize: 16, fontWeight: '600', color: '#111827', marginBottom: 6 },
  metaRow: { flexDirection: 'row', gap: 8, marginBottom: 4 },
  status: { fontSize: 13, fontWeight: '500' },
  priority: { color: '#6B7280', fontSize: 12, alignSelf: 'center' },
  problem: { color: '#6B7280', fontSize: 13, marginTop: 4 },
  emptyText: { textAlign: 'center', color: '#9CA3AF', marginTop: 48, fontSize: 15 },
  errorText: { textAlign: 'center', color: '#EF4444', marginTop: 48, padding: 16 },
});
