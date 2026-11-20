import { createBaseApiService } from "@/api/base-service.api";
import axiosInstance from "@/api/axios";
import type {
  InterviewTopicDto,
  InterviewTopicFilter,
  InterviewQuestionDto,
  InterviewQuestionFilter,
  InterviewAssignmentDto,
  InterviewAssignmentFilter,
  InterviewSessionDto,
  SaveSessionAnswerRequest,
  ReviewSessionRequest,
  InterviewSessionSubmissionSummaryDto,
} from "@/types";

export const interviewTopicApi = createBaseApiService<InterviewTopicDto, InterviewTopicFilter>({
  path: "/interview-topics",
});

export const interviewQuestionApi = createBaseApiService<InterviewQuestionDto, InterviewQuestionFilter>({
  path: "/interview-questions",
});

const assignmentBase = createBaseApiService<InterviewAssignmentDto, InterviewAssignmentFilter>({
  path: "/interview-assignments",
});

export const interviewAssignmentApi = Object.assign({}, assignmentBase, {
  getMyAssignments: async (): Promise<InterviewAssignmentDto[]> => {
    const response = await axiosInstance.get<InterviewAssignmentDto[]>("/interview-assignments/my-assignments");
    return response.data;
  },

  startSession: async (assignmentId: number): Promise<InterviewSessionDto> => {
    const response = await axiosInstance.post<InterviewSessionDto>(
      `/interview-assignments/${assignmentId}/start-session`
    );
    return response.data;
  },

  getSession: async (sessionId: number): Promise<InterviewSessionDto> => {
    const response = await axiosInstance.get<InterviewSessionDto>(
      `/interview-assignments/sessions/${sessionId}`
    );
    return response.data;
  },

  saveAnswer: async (sessionId: number, request: SaveSessionAnswerRequest): Promise<{ success: boolean }> => {
    const response = await axiosInstance.post<{ success: boolean }>(
      `/interview-assignments/sessions/${sessionId}/save-answer`,
      request
    );
    return response.data;
  },

  submitSession: async (sessionId: number): Promise<InterviewSessionDto> => {
    const response = await axiosInstance.post<InterviewSessionDto>(
      `/interview-assignments/sessions/${sessionId}/submit`
    );
    return response.data;
  },

  getSubmissions: async (assignmentId: number): Promise<InterviewSessionSubmissionSummaryDto[]> => {
    const response = await axiosInstance.get<InterviewSessionSubmissionSummaryDto[]>(
      `/interview-assignments/${assignmentId}/submissions`
    );
    return response.data;
  },

  reviewSession: async (sessionId: number, request: ReviewSessionRequest): Promise<InterviewSessionDto> => {
    const response = await axiosInstance.post<InterviewSessionDto>(
      `/interview-assignments/sessions/${sessionId}/review`,
      request
    );
    return response.data;
  },
});
