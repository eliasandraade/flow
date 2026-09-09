import React from 'react';
import {
  ActivityIndicator,
  StyleSheet,
  Text,
  TouchableOpacity,
  ViewStyle,
} from 'react-native';
import { theme } from '../theme';

type Variant = 'primary' | 'success' | 'danger' | 'secondary';
type Size = 'sm' | 'md' | 'lg';

interface Props {
  variant: Variant;
  label: string;
  onPress: () => void;
  size?: Size;
  loading?: boolean;
  disabled?: boolean;
  style?: ViewStyle;
}

const VARIANT_STYLES: Record<Variant, { bg: string; text: string; border?: string }> = {
  primary:   { bg: theme.colors.primary,      text: theme.colors.text.inverse },
  success:   { bg: theme.colors.success,      text: theme.colors.text.inverse },
  danger:    { bg: theme.colors.danger,       text: theme.colors.text.inverse },
  secondary: { bg: theme.colors.surface.card, text: theme.colors.text.primary, border: theme.colors.surface.border },
};

const SIZE_STYLES: Record<Size, { paddingVertical: number; paddingHorizontal: number }> = {
  sm: { paddingVertical: 8,  paddingHorizontal: 12 },
  md: { paddingVertical: 12, paddingHorizontal: 20 },
  lg: { paddingVertical: 14, paddingHorizontal: 24 },
};

export function Button({
  variant,
  label,
  onPress,
  size = 'md',
  loading = false,
  disabled = false,
  style,
}: Props) {
  const v = VARIANT_STYLES[variant];
  const s = SIZE_STYLES[size];

  function handlePress() {
    if (loading) return;
    onPress();
  }

  return (
    <TouchableOpacity
      onPress={handlePress}
      disabled={disabled || loading}
      activeOpacity={0.8}
      style={[
        styles.base,
        { backgroundColor: v.bg, ...s },
        v.border ? { borderWidth: 1, borderColor: v.border } : null,
        disabled && styles.disabled,
        style,
      ]}
    >
      <Text style={[styles.label, { color: v.text, opacity: loading ? 0 : 1 }]}>
        {label}
      </Text>
      {loading && (
        <ActivityIndicator style={StyleSheet.absoluteFill} color={v.text} size="small" />
      )}
    </TouchableOpacity>
  );
}

const styles = StyleSheet.create({
  base: {
    borderRadius: theme.radius.md,
    alignItems: 'center',
    justifyContent: 'center',
    overflow: 'hidden',
  },
  label: {
    fontSize: theme.typography.body.fontSize,
    fontWeight: '600',
  },
  disabled: { opacity: 0.5 },
});
