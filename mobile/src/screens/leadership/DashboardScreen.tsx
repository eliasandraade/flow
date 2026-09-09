import React from 'react';
import { ActivityIndicator, StyleSheet, Text, View } from 'react-native';
import { useQuery } from '@tanstack/react-query';
import { apiFetch, logout } from '../../api/client';
import { useAuthStore } from '../../store/authStore';
import { BlockedProject, DashboardSummary } from '../../types/api';
import { theme } from '../../theme';
import { normalizeStatus } from '../../utils/normalizeStatus';
import { Button } from '../../components/Button';
import { Card } from '../../components/Card';
import { ScreenContainer } from '../../components/ScreenContainer';
import { StatusBadge } from '../../components/StatusBadge';

export function DashboardScreen() {
  const session = useAuthStore((s) => s.session);

  const { data, isLoading, error } = useQuery<DashboardSummary>({
    queryKey: ['dashboard'],
    queryFn: () => apiFetch<DashboardSummary>('/dashboard/summary'),
    refetchInterval: 60_000,
  });

  if (isLoading) {
    return <ActivityIndicator style={{ flex: 1 }} size="large" color={theme.colors.primary} />;
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
    <ScreenContainer scrollable>
      <Text style={styles.heading}>Innovation Overview</Text>

      <Text style={styles.sectionTitle}>Ideas</Text>
      <View style={styles.row}>
        <KpiCard label="Total"         value={data.totalIdeas} />
        <KpiCard label="Approved"      value={data.approvedIdeas}  color={theme.colors.success} />
        <KpiCard label="Rejected"      value={data.rejectedIdeas}  color={theme.colors.danger} />
      </View>
      <View style={styles.row}>
        <KpiCard label="Under Review"  value={data.pendingIdeas}   color={theme.colors.status.underReview.text} />
        <KpiCard label="Conversion"    value={`${data.conversionRate.toFixed(1)}%`} color={theme.colors.primary} />
      </View>

      <Text style={styles.sectionTitle}>Projects</Text>
      <View style={styles.row}>
        <KpiCard label="Active"        value={data.activeProjects}    color={theme.colors.primary} />
        <KpiCard label="Blocked"       value={data.blockedProjects}   color={theme.colors.status.blocked.text} />
        <KpiCard label="Completed"     value={data.completedProjects} color={theme.colors.success} />
      </View>
      <View style={styles.row}>
        <KpiCard label="Avg Completion" value={`${data.averageCompletionDays}d`} />
        <KpiCard
          label="Bottleneck"
          value={`${data.bottleneckIndex.toFixed(1)}%`}
          color={data.bottleneckIndex > 30 ? theme.colors.danger : theme.colors.success}
        />
        <KpiCard label="Total ROI"     value={`${data.totalRoi.toFixed(0)}%`} color={theme.colors.primary} />
      </View>

      {sortedBlocked.length > 0 && (
        <View style={styles.blockedSection}>
          <Text style={styles.sectionTitle}>Blocked Projects</Text>
          {sortedBlocked.map((p: BlockedProject) => (
            <Card key={p.id} style={styles.blockedCard} padding={theme.spacing.lg}>
              <View style={styles.blockedHeader}>
                <Text style={styles.blockedTitle} numberOfLines={1}>{p.title}</Text>
                <View style={styles.blockedMeta}>
                  <Text style={styles.blockedDays}>{p.daysBlocked}d</Text>
                  <StatusBadge status={normalizeStatus('Blocked')} />
                </View>
              </View>
              <Text style={styles.blockedReason}>{p.blockedReason}</Text>
            </Card>
          ))}
        </View>
      )}

      <Button
        variant="secondary"
        label="Sign Out"
        onPress={() => logout(session?.refreshToken ?? '')}
        style={styles.logoutBtn}
      />
    </ScreenContainer>
  );
}

function KpiCard({
  label,
  value,
  color = theme.colors.text.primary,
}: {
  label: string;
  value: string | number;
  color?: string;
}) {
  return (
    <Card style={styles.kpiCard} padding={theme.spacing.xl}>
      <Text style={[styles.kpiValue, { color }]}>{value}</Text>
      <Text style={styles.kpiLabel}>{label}</Text>
    </Card>
  );
}

const styles = StyleSheet.create({
  heading: {
    ...theme.typography.heading,
    color: theme.colors.text.primary,
    marginBottom: theme.spacing.xs,
  },
  sectionTitle: {
    ...theme.typography.title,
    color: theme.colors.text.primary,
    marginTop: theme.spacing.xxl,
    marginBottom: theme.spacing.md,
  },
  row: {
    flexDirection: 'row',
    gap: theme.spacing.sm,
    marginBottom: theme.spacing.sm,
    alignItems: 'stretch',
  },
  kpiCard: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
  },
  kpiValue: {
    ...theme.typography.kpi,
    marginBottom: theme.spacing.xs,
    textAlign: 'center',
  },
  kpiLabel: {
    ...theme.typography.label,
    color: theme.colors.text.secondary,
    textAlign: 'center',
  },
  blockedSection: { marginTop: theme.spacing.xxl },
  blockedCard: {
    marginBottom: theme.spacing.md,
    backgroundColor: theme.colors.status.blocked.bg,
    borderColor: theme.colors.status.blocked.border,
  },
  blockedHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: theme.spacing.xs,
  },
  blockedTitle: {
    ...theme.typography.title,
    color: theme.colors.text.primary,
    flex: 1,
    marginRight: theme.spacing.sm,
  },
  blockedMeta: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: theme.spacing.sm,
  },
  blockedDays: {
    ...theme.typography.label,
    color: theme.colors.status.blocked.text,
    fontWeight: '700',
  },
  blockedReason: {
    ...theme.typography.label,
    color: theme.colors.text.secondary,
    lineHeight: 18,
  },
  logoutBtn: { marginTop: theme.spacing.xxl },
  errorText: {
    ...theme.typography.body,
    textAlign: 'center',
    color: theme.colors.status.rejected.text,
    marginTop: theme.spacing.xxxl,
    padding: theme.spacing.lg,
  },
});
