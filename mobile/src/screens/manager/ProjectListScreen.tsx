import React from 'react';
import {
  View, Text, FlatList, TouchableOpacity,
  StyleSheet, ActivityIndicator,
} from 'react-native';
import { useQuery } from '@tanstack/react-query';
import { apiFetch, logout } from '../../api/client';
import { useAuthStore } from '../../store/authStore';
import { ProjectSummary } from '../../types/api';

const STATUS_COLORS: Record<string, string> = {
  Planning: '#6B7280',
  InProgress: '#2563EB',
  Blocked: '#DC2626',
  Completed: '#059669',
  Cancelled: '#9CA3AF',
};

export function ProjectListScreen({ navigation }: any) {
  const session = useAuthStore((s) => s.session);

  const { data: projects, isLoading, isFetching, error, refetch } = useQuery<ProjectSummary[]>({
    queryKey: ['projects'],
    queryFn: () => apiFetch<ProjectSummary[]>('/projects'),
  });

  if (isLoading) {
    return <ActivityIndicator style={{ flex: 1 }} size="large" color="#2563EB" />;
  }
  if (error) {
    return <Text style={styles.errorText}>{(error as Error).message}</Text>;
  }

  return (
    <View style={styles.container}>
      <FlatList
        data={projects ?? []}
        keyExtractor={(item) => item.id}
        onRefresh={() => { refetch(); }}
        refreshing={isFetching}
        renderItem={({ item }) => (
          <TouchableOpacity
            style={[styles.card, item.status === 'Blocked' && styles.cardBlocked]}
            onPress={() => navigation.navigate('ProjectDetail', { id: item.id })}
          >
            <Text style={styles.cardTitle}>{item.title}</Text>
            <View style={styles.metaRow}>
              <Text style={[styles.status, { color: STATUS_COLORS[item.status] ?? '#6B7280' }]}>
                {item.status}
              </Text>
              <Text style={styles.priority}>{item.priority}</Text>
            </View>
            {item.blockedReason && (
              <Text style={styles.blockedReason} numberOfLines={1}>
                ⚠ {item.blockedReason}
              </Text>
            )}
          </TouchableOpacity>
        )}
        ListEmptyComponent={
          <Text style={styles.emptyText}>No projects yet.</Text>
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
  card: {
    backgroundColor: '#FFF', borderRadius: 8, padding: 14,
    marginBottom: 12, borderWidth: 1, borderColor: '#E5E7EB',
  },
  cardBlocked: { borderColor: '#FECACA', backgroundColor: '#FEF2F2' },
  cardTitle: { fontSize: 16, fontWeight: '600', color: '#111827', marginBottom: 6 },
  metaRow: { flexDirection: 'row', gap: 8 },
  status: { fontSize: 13, fontWeight: '600' },
  priority: { color: '#6B7280', fontSize: 12, alignSelf: 'center' },
  blockedReason: { color: '#DC2626', fontSize: 12, marginTop: 6 },
  emptyText: { textAlign: 'center', color: '#9CA3AF', marginTop: 48, fontSize: 15 },
  errorText: { textAlign: 'center', color: '#EF4444', marginTop: 48, padding: 16 },
  logoutBtn: { alignItems: 'center', padding: 14, marginTop: 8 },
  logoutText: { color: '#6B7280', fontSize: 14 },
});
