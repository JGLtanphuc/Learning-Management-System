export interface DashboardStats {
  totalUsers: number;
  totalRoles: number;
  totalMenus: number;
  totalMenuItems: number;
  activeUsers: number;
  activeRoles: number;
  activeMenus: number;
  totalClasses?: number;
  totalStudents?: number;
  activeMockProjects?: number;
  totalAllocations?: number;
}

export interface DashboardFilter {
  startDate?: string;
  endDate?: string;
}
