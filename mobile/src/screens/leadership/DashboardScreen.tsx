import React from 'react';
import {
  View, Text, StyleSheet, ScrollView,
  ActivityIndicator, TouchableOpacity,
} from 'react-native';
import { useQuery } from '@tanstack/react-query';
import { apiFetch, logout } from '../../api/client';
import { useAuthStore } from '../../store/authStore';
import { DashboardSummary, BlockedProject } from '../../types/api';

export function DashboardScreen() {
  const session = useAuthStore((s) => s.session);

  const { data, isLoading, error } = useQuery<DashboardSummary>({
    queryKey: ['dashboard'],
    queryFn: () => apiFetch<DashboardSummary>('/dashboard/summary'),
    refetchInterval: 60_000,
  });

  if (isLoading) {
    return <ActivityIndicator style={{ flex: 1 }} size="large" color="#2563EB" />;
  }
  if (error || !data) {
    return (
      <Text style={styles.errorText}>
        {(error as Error)?.message ?? 'Failed to load dashboard'}
      </Text>
    );
  }

  const sortedBlocked = [...data.blockedProjectList].sort(
    (a, b) => b.daysBlocked - a.daysBlocked
  );

  return (
    <ScrollView style={styles.container} contentContainerStyle={{ paddingBottom: 32 }}>
      <Text style={styles.heading}>Innovation Overview</Text>

      <Text style={styles.sectionTitle}>Ideas</Text>
      <View style={styles.row}>
        <MetricCard label="Total" value={data.totalIdeas} />
        <MetricCard label="Approved" value={data.approvedIdeas} color="#059669" />
        <MetricCard label="Rejected" value={data.rejectedIdeas} color="#DC2626" />
      </View>
      <View style={styles.row}>
        <MetricCard label="Pending Review" value={data.pendingIdeas} color="#D97706" />
        <MetricCard
          label="Conversion"
          value={`${data.conversionRate.toFixed(1)}%`}
          color="#2563EB"
        />
      </View>

      <Text style={styles.sectionTitle}>Projects</Text>
      <View style={styles.row}>
        <MetricCard label="Active" value={data.activeProjects} color="#2563EB" />
        <MetricCard label="Blocked" value={data.blockedProjects} color="#DC2626" />
        <MetricCard label="Completed" value={data.completedProjects} color="#059669" />
      </View>
      <View style={styles.row}>
        <MetricCard
          label="Avg Completion"
          value={`${data.averageCompletionDays}d`}
        />
        <MetricCard
          label="Bottleneck"
          value={`${data.bottleneckIndex.toFixed(1)}%`}
          color={data.bottleneckIndex > 30 ? '#DC2626' : '#059669'}
        />
        <MetricCard
          label="Total ROI"
          value={`${data.totalRoi.toFixed(0)}%`}
          color="#2563EB"
        />
      </View>

      {sortedBlocked.length > 0 && (
        <>
          <Text style={styles.sectionTitle}>Blocked Projects</Text>
          {sortedBlocked.map((p: BlockedProject) => (
            <View key={p.id} style={styles.blockedCard}>
              <View style={styles.blockedHeader}>
                <Text style={styles.blockedTitle} numberOfLines={1}>{p.title}</Text>
                <Text style={styles.blockedDays}>
                  {p.daysBlocked}d
                </Text>
              </View>
              <Text style={styles.blockedReason}>{p.blockedReason}</Text>
            </View>
          ))}
        </>
      )}

      <TouchableOpacity
        style={styles.logoutBtn}
        onPress={() => logout(session?.refreshToken ?? '')}
      >
        <Text style={styles.logoutText}>Sign Out</Text>
      </TouchableOpacity>
    </ScrollView>
  );
}

function MetricCard({
  label,
  value,
  color = '#111827',
}: {
  label: string;
  value: string | number;
  color?: string;
}) {
  return (
    <View style={cardStyles.card}>
      <Text style={[cardStyles.value, { color }]}>{value}</Text>
      <Text style={cardStyles.label}>{label}</Text>
    </View>
  );
}

const cardStyles = StyleSheet.create({
  card: {
    flex: 1, backgroundColor: '#FFF', borderRadius: 8,
    padding: 14, alignItems: 'center',
    borderWidth: 1, borderColor: '#E5E7EB',
  },
  value: { fontSize: 22, fontWeight: 'bold', marginBottom: 4 },
  label: { fontSize: 11, color: '#6B7280', textAlign: 'center' },
});

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#F9FAFB', padding: 16 },
  heading: { fontSize: 22, fontWeight: 'bold', color: '#1E3A5F', marginBottom: 4 },
  sectionTitle: {
    fontSize: 14, fontWeight: '700', color: '#374151',
    marginTop: 20, marginBottom: 10,
  },
  row: { flexDirection: 'row', gap: 8, marginBottom: 8 },
  blockedCard: {
    backgroundColor: '#FEF2F2', borderRadius: 8, padding: 12,
    marginBottom: 8, borderWidth: 1, borderColor: '#FECACA',
  },
  blockedHeader: {
    flexDirection: 'row', justifyContent: 'space-between',
    alignItems: 'center', marginBottom: 4,
  },
  blockedTitle: { fontSize: 15, fontWeight: '600', color: '#111827', flex: 1, marginRight: 8 },
  blockedDays: { fontSize: 13, color: '#DC2626', fontWeight: '600' },
  blockedReason: { fontSize: 13, color: '#7F1D1D', lineHeight: 18 },
  logoutBtn: {
    marginTop: 32, borderRadius: 8, borderWidth: 1,
    borderColor: '#D1D5DB', padding: 14, alignItems: 'center',
  },
  logoutText: { color: '#374151', fontWeight: '600' },
  errorText: { textAlign: 'center', color: '#EF4444', marginTop: 48, padding: 16 },
});
