import React from 'react';
import { StyleSheet, Text } from 'react-native';
import { theme } from '../theme';
import { StatusKey } from '../utils/normalizeStatus';

const DISPLAY_LABELS: Record<StatusKey, string> = {
  draft:       'Draft',
  underReview: 'Under Review',
  approved:    'Approved',
  rejected:    'Rejected',
  inProgress:  'In Progress',
  blocked:     'Blocked',
  completed:   'Completed',
  cancelled:   'Cancelled',
};

interface Props {
  status: StatusKey;
}

export function StatusBadge({ status }: Props) {
  const colors = theme.colors.status[status] ?? theme.colors.status.draft;
  const label  = DISPLAY_LABELS[status] ?? 'Unknown';

  return (
    <Text style={[styles.badge, { backgroundColor: colors.bg, color: colors.text }]}>
      {label}
    </Text>
  );
}

const styles = StyleSheet.create({
  badge: {
    ...theme.typography.label,
    paddingVertical: 4,
    paddingHorizontal: 10,
    borderRadius: theme.radius.full,
    alignSelf: 'flex-start',
    overflow: 'hidden',
  },
});
