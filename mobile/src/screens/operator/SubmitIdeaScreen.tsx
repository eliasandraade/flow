import React, { useState } from 'react';
import { Alert } from 'react-native';
import { useQueryClient } from '@tanstack/react-query';
import { apiFetch } from '../../api/client';
import { IdeaSummary } from '../../types/api';
import { Button } from '../../components/Button';
import { FormInput } from '../../components/FormInput';
import { ScreenContainer } from '../../components/ScreenContainer';

export function SubmitIdeaScreen({ navigation }: any) {
  const [title, setTitle] = useState('');
  const [problem, setProblem] = useState('');
  const [description, setDescription] = useState('');
  const [loading, setLoading] = useState(false);
  const queryClient = useQueryClient();

  async function handleCreate() {
    if (!title.trim() || !problem.trim() || !description.trim()) {
      Alert.alert('Validation', 'Title, problem, and description are all required.');
      return;
    }
    setLoading(true);
    try {
      await apiFetch<IdeaSummary>('/ideas', {
        method: 'POST',
        body: JSON.stringify({ title, problem, description, linkedGuidelineId: null }),
      });
      await queryClient.invalidateQueries({ queryKey: ['ideas'] });
      Alert.alert('Success', 'Idea created.', [
        { text: 'OK', onPress: () => navigation.goBack() },
      ]);
    } catch (err: any) {
      Alert.alert('Error', err.message ?? 'Could not create idea');
    } finally {
      setLoading(false);
    }
  }

  return (
    <ScreenContainer scrollable>
      <FormInput
        label="Title *"
        value={title}
        onChangeText={setTitle}
        placeholder="Brief, descriptive title"
      />
      <FormInput
        label="Problem *"
        value={problem}
        onChangeText={setProblem}
        placeholder="What problem does this idea solve?"
        multiline
        numberOfLines={3}
        textAlignVertical="top"
        inputStyle={{ minHeight: 80 }}
      />
      <FormInput
        label="Description *"
        value={description}
        onChangeText={setDescription}
        placeholder="Describe your idea in detail"
        multiline
        numberOfLines={5}
        textAlignVertical="top"
        inputStyle={{ minHeight: 120 }}
      />
      <Button
        variant="primary"
        size="lg"
        label="Create Idea"
        onPress={handleCreate}
        loading={loading}
      />
    </ScreenContainer>
  );
}
