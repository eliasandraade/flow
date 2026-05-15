import React from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { useAuthStore } from '../store/authStore';
import { LoginScreen } from '../screens/auth/LoginScreen';
import { MyIdeasScreen } from '../screens/operator/MyIdeasScreen';
import { SubmitIdeaScreen } from '../screens/operator/SubmitIdeaScreen';
import { IdeaDetailScreen } from '../screens/operator/IdeaDetailScreen';
import { PlaceholderManagerScreen } from '../screens/manager/PlaceholderManagerScreen';
import { PlaceholderLeadershipScreen } from '../screens/leadership/PlaceholderLeadershipScreen';

const AuthStack = createNativeStackNavigator();
const OperatorStack = createNativeStackNavigator();
const ManagerStack = createNativeStackNavigator();
const LeadershipStack = createNativeStackNavigator();

function AuthNavigator() {
  return (
    <AuthStack.Navigator screenOptions={{ headerShown: false }}>
      <AuthStack.Screen name="Login" component={LoginScreen} />
    </AuthStack.Navigator>
  );
}

function OperatorNavigator() {
  return (
    <OperatorStack.Navigator>
      <OperatorStack.Screen
        name="MyIdeas"
        component={MyIdeasScreen}
        options={{ title: 'My Ideas' }}
      />
      <OperatorStack.Screen
        name="SubmitIdea"
        component={SubmitIdeaScreen}
        options={{ title: 'Submit Idea' }}
      />
      <OperatorStack.Screen
        name="IdeaDetail"
        component={IdeaDetailScreen}
        options={{ title: 'Idea' }}
      />
    </OperatorStack.Navigator>
  );
}

function ManagerNavigator() {
  return (
    <ManagerStack.Navigator>
      <ManagerStack.Screen
        name="ManagerHome"
        component={PlaceholderManagerScreen}
        options={{ title: 'Manager' }}
      />
    </ManagerStack.Navigator>
  );
}

function LeadershipNavigator() {
  return (
    <LeadershipStack.Navigator>
      <LeadershipStack.Screen
        name="LeadershipHome"
        component={PlaceholderLeadershipScreen}
        options={{ title: 'Dashboard' }}
      />
    </LeadershipStack.Navigator>
  );
}

export function AppNavigator() {
  const session = useAuthStore((s) => s.session);

  return (
    <NavigationContainer>
      {!session ? (
        <AuthNavigator />
      ) : session.role === 'Operator' ? (
        <OperatorNavigator />
      ) : session.role === 'Manager' ? (
        <ManagerNavigator />
      ) : (
        <LeadershipNavigator />
      )}
    </NavigationContainer>
  );
}
