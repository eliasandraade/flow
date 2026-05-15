import React, { useState } from 'react';
import { ActivityIndicator, Alert, StyleSheet, Text, View } from 'react-native';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { apiFetch } from '../../api/client';
import { ProjectDetail } from '../../types/api';
import { theme } from '../../theme';
import { normalizeStatus } from '../../utils/normalizeStatus';
import { Button } from '../../components/Button';
import { FormInput } from '../../components/FormInput';
import { ScreenContainer } from '../../components/ScreenContainer';
import { StatusBadge } from '../../components/StatusBadge';

export function ProjectDetailScreen({ route }: any) {
  const { id } = route.params as { id: string };
  const queryClient = useQueryClient();
  const [blockReason, setBlockReason] = useState('');
  const [actionLoading, setActionLoading] = useState(false);

  const { data: project, isLoading, error } = useQuery<ProjectDetail>({
    queryKey: ['project', id],
    queryFn: () => apiFetch<ProjectDetail>(`/projects/${id}`),
  });

  async function callAction(path: string, body?: object) {
    setActionLoading(true);
    try {
      await apiFetch(path, {
        method: 'POST',
        body: body ? JSON.stringify(body) : undefined,
      });
      await queryClient.invalidateQueries({ queryKey: ['project', id] });
      await queryClient.invalidateQueries({ queryKey: ['projects'] });
    } catch (err: any) {
      Alert.alert('Error', err.message ?? 'Action failed');
    } finally {
      setActionLoading(false);
    }
  }

  if (isLoading) {
    return <ActivityIndicator style={{ flex: 1 }} size="large" color={theme.colors.primary} />;
  }
  if (error || !project) {
    return <Text style={styles.errorText}>{(error as Error)?.message ?? 'Not found'}</Text>;
  }

  return (
    <ScreenContainer scrollable>
      <Text style={styles.title}>{project.title}</Text>
      <View style={styles.metaRow}>
        <StatusBadge status={normalizeStatus(project.status)} />
        <Text style={styles.priority}>{project.priority}</Text>
      </View>

      <Text style={styles.sectionLabel}>Description</Text>
      <Text style={styles.body}>{project.description}</Text>

      {project.blockedReason ? (
        <View style={styles.section}>
          <Text style={styles.sectionLabel}>Blocked Reason</Text>
          <View style={styles.blockedBox}>
            <Text style={styles.blockedText}>{project.blockedReason}</Text>
          </View>
        </View>
      ) : null}

      <View style={styles.section}>
        <Text style={styles.sectionLabel}>Details</Text>
        {project.estimatedCost != null && (
          <Text style={styles.meta}>Estimated Cost: ${project.estimatedCost.toLocaleString()}</Text>
        )}
        {project.deadline && (
          <Text style={styles.meta}>Deadline: {new Date(project.deadline).toLocaleDateString()}</Text>
        )}
        {project.startDate && (
          <Text style={styles.meta}>Started: {new Date(project.startDate).toLocaleDateString()}</Text>
        )}
        {project.completedAt && (
          <Text style={styles.meta}>Completed: {new Date(project.completedAt).toLocaleDateString()}</Text>
        )}
      </View>

      <View style={styles.actionsSection}>
        {project.status === 'Planning' && (
          <Button
            variant="primary"
            size="lg"
            label="Start Project"
            onPress={() => callAction(`/projects/${id}/start`)}
            loading={actionLoading}
          />
        )}

        {project.status === 'InProgress' && (
          <View style={styles.inProgressActions}>
            <Button
              variant="success"
              size="lg"
              label="Mark Complete"
              onPress={() => callAction(`/projects/${id}/complete`)}
              loading={actionLoading}
            />
            <View style={styles.blockSection}>
              <FormInput
                label="Block Reason *"
                value={blockReason}
                onChangeText={setBlockReason}
                placeholder="Why is this project blocked?"
                multiline
                numberOfLines={2}
                textAlignVertical="top"
                inputStyle={{ minHeight: 60 }}
              />
              <Button
                variant="danger"
                label="Block Project"
                onPress={() => {
                  if (!blockReason.trim()) {
                    Alert.alert('Validation', 'A block reason is required.');
                    return;
                  }
                  callAction(`/projects/${id}/block`, { reason: blockReason });
                }}
                loading={actionLoading}
              />
            </View>
          </View>
        )}

        {project.status === 'Blocked' && (
          <Button
            variant="success"
            size="lg"
            label="Unblock Project"
            onPress={() => callAction(`/projects/${id}/unblock`)}
            loading={actionLoading}
          />
        )}
      </View>
    </ScreenContainer>
  );
}

const styles = StyleSheet.create({
  title: {
    ...theme.typography.title,
    color: theme.colors.text.primary,
    marginBottom: theme.spacing.sm,
  },
  metaRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: theme.spacing.sm,
    marginBottom: theme.spacing.xl,
  },
  priority: {
    ...theme.typography.label,
    color: theme.colors.text.secondary,
  },
  sectionLabel: {
    ...theme.typography.label,
    color: theme.colors.text.secondary,
    marginBottom: theme.spacing.xs,
  },
  body: {
    ...theme.typography.body,
    color: theme.colors.text.primary,
    lineHeight: 22,
  },
  section: { marginTop: theme.spacing.xxl },
  blockedBox: {
    backgroundColor: theme.colors.status.blocked.bg,
    borderRadius: theme.radius.md,
    padding: theme.spacing.md,
  },
  blockedText: {
    ...theme.typography.body,
    color: theme.colors.status.blocked.text,
  },
  meta: {
    ...theme.typography.label,
    color: theme.colors.text.secondary,
    marginTop: theme.spacing.sm,
  },
  actionsSection: { marginTop: theme.spacing.xxl },
  inProgressActions: { gap: theme.spacing.lg },
  blockSection: { marginTop: theme.spacing.lg, gap: theme.spacing.sm },
  errorText: {
    textAlign: 'center',
    color: '#EF4444',
    marginTop: 48,
    padding: theme.spacing.lg,
  },
});
