import React, { useState } from 'react';
import {
  View, Text, TouchableOpacity, StyleSheet, ScrollView,
  ActivityIndicator, Alert, TextInput,
} from 'react-native';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { apiFetch } from '../../api/client';
import { IdeaDetail } from '../../types/api';

export function ManagerIdeaDetailScreen({ route }: any) {
  const { id } = route.params as { id: string };
  const queryClient = useQueryClient();
  const [comment, setComment] = useState('');
  const [actionLoading, setActionLoading] = useState(false);

  const { data: idea, isLoading, error } = useQuery<IdeaDetail>({
    queryKey: ['idea', id],
    queryFn: () => apiFetch<IdeaDetail>(`/ideas/${id}`),
  });

  async function invalidate() {
    await queryClient.invalidateQueries({ queryKey: ['idea', id] });
    await queryClient.invalidateQueries({ queryKey: ['ideas', 'all'] });
  }

  async function handleApprove() {
    setActionLoading(true);
    try {
      await apiFetch(`/ideas/${id}/approve`, {
        method: 'POST',
        body: JSON.stringify({ managerComment: comment.trim() || null }),
      });
      await invalidate();
      Alert.alert('Approved', 'The idea has been approved.');
    } catch (err: any) {
      Alert.alert('Error', err.message ?? 'Could not approve idea');
    } finally {
      setActionLoading(false);
    }
  }

  async function handleReject() {
    if (!comment.trim()) {
      Alert.alert('Validation', 'A rejection comment is required.');
      return;
    }
    setActionLoading(true);
    try {
      await apiFetch(`/ideas/${id}/reject`, {
        method: 'POST',
        body: JSON.stringify({ managerComment: comment }),
      });
      await invalidate();
      Alert.alert('Rejected', 'The idea has been rejected.');
    } catch (err: any) {
      Alert.alert('Error', err.message ?? 'Could not reject idea');
    } finally {
      setActionLoading(false);
    }
  }

  if (isLoading) {
    return <ActivityIndicator style={{ flex: 1 }} size="large" color="#2563EB" />;
  }
  if (error || !idea) {
    return <Text style={styles.errorText}>{(error as Error)?.message ?? 'Not found'}</Text>;
  }

  const canAct = idea.status === 'UnderReview';

  return (
    <ScrollView style={styles.container} contentContainerStyle={{ paddingBottom: 32 }}>
      <Text style={styles.title}>{idea.title}</Text>
      <View style={styles.metaRow}>
        <Text style={styles.badge}>{idea.status}</Text>
        <Text style={styles.priority}>{idea.priority}</Text>
      </View>

      <Text style={styles.sectionLabel}>Problem</Text>
      <Text style={styles.body}>{idea.problem}</Text>

      <Text style={styles.sectionLabel}>Description</Text>
      <Text style={styles.body}>{idea.description}</Text>

      {idea.managerComment ? (
        <>
          <Text style={styles.sectionLabel}>Manager Comment</Text>
          <Text style={styles.body}>{idea.managerComment}</Text>
        </>
      ) : null}

      {canAct && (
        <>
          <Text style={styles.sectionLabel}>Comment (required for rejection)</Text>
          <TextInput
            style={styles.input}
            value={comment}
            onChangeText={setComment}
            placeholder="Add a comment..."
            multiline
            numberOfLines={3}
          />
          {actionLoading ? (
            <ActivityIndicator size="large" color="#2563EB" style={{ marginTop: 16 }} />
          ) : (
            <View style={styles.actionsRow}>
              <TouchableOpacity style={[styles.actionBtn, styles.approveBtn]} onPress={handleApprove}>
                <Text style={styles.actionBtnText}>Approve</Text>
              </TouchableOpacity>
              <TouchableOpacity style={[styles.actionBtn, styles.rejectBtn]} onPress={handleReject}>
                <Text style={styles.actionBtnText}>Reject</Text>
              </TouchableOpacity>
            </View>
          )}
        </>
      )}

      {!canAct && (
        <Text style={styles.resolvedNote}>
          This idea has already been {idea.status.toLowerCase()}.
        </Text>
      )}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#F9FAFB', padding: 16 },
  title: { fontSize: 20, fontWeight: 'bold', color: '#111827', marginBottom: 8 },
  metaRow: { flexDirection: 'row', gap: 8, marginBottom: 16 },
  badge: {
    backgroundColor: '#FEF3C7', color: '#92400E',
    paddingHorizontal: 10, paddingVertical: 3,
    borderRadius: 12, fontSize: 13, fontWeight: '500',
  },
  priority: { color: '#6B7280', fontSize: 13, alignSelf: 'center' },
  sectionLabel: { fontSize: 13, fontWeight: '600', color: '#374151', marginTop: 16, marginBottom: 4 },
  body: { fontSize: 15, color: '#374151', lineHeight: 22 },
  input: {
    borderWidth: 1, borderColor: '#D1D5DB', borderRadius: 8,
    padding: 12, backgroundColor: '#FFF', fontSize: 15,
    minHeight: 72, textAlignVertical: 'top', marginTop: 4,
  },
  actionsRow: { flexDirection: 'row', gap: 12, marginTop: 16 },
  actionBtn: { flex: 1, padding: 14, borderRadius: 8, alignItems: 'center' },
  approveBtn: { backgroundColor: '#059669' },
  rejectBtn: { backgroundColor: '#DC2626' },
  actionBtnText: { color: '#FFF', fontWeight: '600', fontSize: 15 },
  resolvedNote: { marginTop: 24, textAlign: 'center', color: '#6B7280', fontSize: 14 },
  errorText: { textAlign: 'center', color: '#EF4444', marginTop: 48, padding: 16 },
});
