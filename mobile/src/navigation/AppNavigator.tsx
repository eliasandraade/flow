import React from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { useAuthStore } from '../store/authStore';
import { LoginScreen } from '../screens/auth/LoginScreen';
import { MyIdeasScreen } from '../screens/operator/MyIdeasScreen';
import { SubmitIdeaScreen } from '../screens/operator/SubmitIdeaScreen';
import { IdeaDetailScreen } from '../screens/operator/IdeaDetailScreen';
import { IdeaQueueScreen } from '../screens/manager/IdeaQueueScreen';
import { ManagerIdeaDetailScreen } from '../screens/manager/ManagerIdeaDetailScreen';
import { ProjectListScreen } from '../screens/manager/ProjectListScreen';
import { ProjectDetailScreen } from '../screens/manager/ProjectDetailScreen';
import { PlaceholderLeadershipScreen } from '../screens/leadership/PlaceholderLeadershipScreen';

const AuthStack = createNativeStackNavigator();
const OperatorStack = createNativeStackNavigator();
const ManagerIdeasStack = createNativeStackNavigator();
const ManagerProjectsStack = createNativeStackNavigator();
const ManagerTabs = createBottomTabNavigator();
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

function ManagerIdeasNavigator() {
  return (
    <ManagerIdeasStack.Navigator>
      <ManagerIdeasStack.Screen
        name="IdeaQueue"
        component={IdeaQueueScreen}
        options={{ title: 'Ideas' }}
      />
      <ManagerIdeasStack.Screen
        name="ManagerIdeaDetail"
        component={ManagerIdeaDetailScreen}
        options={{ title: 'Idea Review' }}
      />
    </ManagerIdeasStack.Navigator>
  );
}

function ManagerProjectsNavigator() {
  return (
    <ManagerProjectsStack.Navigator>
      <ManagerProjectsStack.Screen
        name="ProjectList"
        component={ProjectListScreen}
        options={{ title: 'Projects' }}
      />
      <ManagerProjectsStack.Screen
        name="ProjectDetail"
        component={ProjectDetailScreen}
        options={{ title: 'Project' }}
      />
    </ManagerProjectsStack.Navigator>
  );
}

function ManagerNavigator() {
  return (
    <ManagerTabs.Navigator>
      <ManagerTabs.Screen
        name="Ideas"
        component={ManagerIdeasNavigator}
        options={{ headerShown: false }}
      />
      <ManagerTabs.Screen
        name="Projects"
        component={ManagerProjectsNavigator}
        options={{ headerShown: false }}
      />
    </ManagerTabs.Navigator>
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
