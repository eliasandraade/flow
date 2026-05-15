import React, { useState } from 'react';
import {
  View, Text, TouchableOpacity, StyleSheet, ScrollView,
  ActivityIndicator, Alert, TextInput,
} from 'react-native';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { apiFetch } from '../../api/client';
import { ProjectDetail } from '../../types/api';

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
    return <ActivityIndicator style={{ flex: 1 }} size="large" color="#2563EB" />;
  }
  if (error || !project) {
    return <Text style={styles.errorText}>{(error as Error)?.message ?? 'Not found'}</Text>;
  }

  return (
    <ScrollView style={styles.container} contentContainerStyle={{ paddingBottom: 32 }}>
      <Text style={styles.title}>{project.title}</Text>
      <View style={styles.metaRow}>
        <Text style={styles.badge}>{project.status}</Text>
        <Text style={styles.priority}>{project.priority}</Text>
      </View>

      <Text style={styles.sectionLabel}>Description</Text>
      <Text style={styles.body}>{project.description}</Text>

      {project.blockedReason ? (
        <>
          <Text style={styles.sectionLabel}>Blocked Reason</Text>
          <View style={styles.blockedBox}>
            <Text style={styles.blockedText}>{project.blockedReason}</Text>
          </View>
        </>
      ) : null}

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

      {actionLoading && (
        <ActivityIndicator size="large" color="#2563EB" style={{ marginTop: 20 }} />
      )}

      {!actionLoading && project.status === 'Planning' && (
        <TouchableOpacity
          style={[styles.actionBtn, { backgroundColor: '#2563EB' }]}
          onPress={() => callAction(`/projects/${id}/start`)}
        >
          <Text style={styles.actionBtnText}>Start Project</Text>
        </TouchableOpacity>
      )}

      {!actionLoading && project.status === 'InProgress' && (
        <View>
          <TouchableOpacity
            style={[styles.actionBtn, { backgroundColor: '#059669' }]}
            onPress={() => callAction(`/projects/${id}/complete`)}
          >
            <Text style={styles.actionBtnText}>Mark Complete</Text>
          </TouchableOpacity>

          <Text style={styles.sectionLabel}>Block Reason *</Text>
          <TextInput
            style={styles.input}
            value={blockReason}
            onChangeText={setBlockReason}
            placeholder="Why is this project blocked?"
            multiline
            numberOfLines={2}
          />
          <TouchableOpacity
            style={[styles.actionBtn, { backgroundColor: '#DC2626' }]}
            onPress={() => {
              if (!blockReason.trim()) {
                Alert.alert('Validation', 'A block reason is required.');
                return;
              }
              callAction(`/projects/${id}/block`, { reason: blockReason });
            }}
          >
            <Text style={styles.actionBtnText}>Block Project</Text>
          </TouchableOpacity>
        </View>
      )}

      {!actionLoading && project.status === 'Blocked' && (
        <TouchableOpacity
          style={[styles.actionBtn, { backgroundColor: '#D97706' }]}
          onPress={() => callAction(`/projects/${id}/unblock`)}
        >
          <Text style={styles.actionBtnText}>Unblock Project</Text>
        </TouchableOpacity>
      )}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#F9FAFB', padding: 16 },
  title: { fontSize: 20, fontWeight: 'bold', color: '#111827', marginBottom: 8 },
  metaRow: { flexDirection: 'row', gap: 8, marginBottom: 16 },
  badge: {
    backgroundColor: '#EEF2FF', color: '#4338CA',
    paddingHorizontal: 10, paddingVertical: 3,
    borderRadius: 12, fontSize: 13, fontWeight: '500',
  },
  priority: { color: '#6B7280', fontSize: 13, alignSelf: 'center' },
  sectionLabel: { fontSize: 13, fontWeight: '600', color: '#374151', marginTop: 16, marginBottom: 4 },
  body: { fontSize: 15, color: '#374151', lineHeight: 22 },
  blockedBox: { backgroundColor: '#FEF2F2', borderRadius: 6, padding: 10, marginTop: 4 },
  blockedText: { color: '#DC2626', fontSize: 14 },
  meta: { color: '#6B7280', fontSize: 13, marginTop: 6 },
  input: {
    borderWidth: 1, borderColor: '#D1D5DB', borderRadius: 8,
    padding: 12, backgroundColor: '#FFF', fontSize: 15,
    minHeight: 60, textAlignVertical: 'top', marginTop: 4,
  },
  actionBtn: { borderRadius: 8, padding: 14, alignItems: 'center', marginTop: 14 },
  actionBtnText: { color: '#FFF', fontWeight: '600', fontSize: 15 },
  errorText: { textAlign: 'center', color: '#EF4444', marginTop: 48, padding: 16 },
});
