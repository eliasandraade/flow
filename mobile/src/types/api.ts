export interface AuthResult {
  accessToken: string;
  refreshToken: string;
  userId: string;
  name: string;
  email: string;
  role: 'Operator' | 'Manager' | 'Leadership';
}

export interface IdeaSummary {
  id: string;
  title: string;
  problem: string;
  status: string;
  priority: string;
  submittedBy: string;
  linkedGuidelineId: string | null;
  createdAt: string;
}

export interface IdeaDetail {
  id: string;
  title: string;
  description: string;
  problem: string;
  status: string;
  priority: string;
  submittedBy: string;
  managerComment: string | null;
  linkedGuidelineId: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface ProjectSummary {
  id: string;
  title: string;
  status: string;
  priority: string;
  ownerId: string;
  sourceIdeaId: string | null;
  deadline: string | null;
  blockedReason: string | null;
  createdAt: string;
}

export interface ProjectDetail {
  id: string;
  title: string;
  description: string;
  status: string;
  priority: string;
  ownerId: string;
  sourceIdeaId: string | null;
  estimatedCost: number | null;
  actualCost: number | null;
  startDate: string | null;
  deadline: string | null;
  completedAt: string | null;
  blockedReason: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface BlockedProject {
  id: string;
  title: string;
  ownerId: string;
  blockedReason: string;
  daysBlocked: number;
}

export interface DashboardSummary {
  totalIdeas: number;
  approvedIdeas: number;
  rejectedIdeas: number;
  pendingIdeas: number;
  conversionRate: number;
  activeProjects: number;
  blockedProjects: number;
  completedProjects: number;
  totalRoi: number;
  averageCompletionDays: number;
  bottleneckIndex: number;
  blockedProjectList: BlockedProject[];
}
