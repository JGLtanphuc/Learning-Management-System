export type InterviewDifficulty = "EASY" | "MEDIUM" | "HARD";
export type InterviewAnswerMode = "TEXT" | "VOICE";
export type InterviewQuestionType = "WRITTEN" | "MCQ";

export interface InterviewTopicDto {
  id: number;
  name: string;
  description?: string;
  displayOrder: number;
  questionCount: number;
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string;
}

export interface InterviewTopicFilter {
  search?: string;
  isActive?: boolean;
}

export interface InterviewQuestionOption {
  id: string;
  text: string;
}

export interface InterviewQuestionDto {
  id: number;
  topicId: number;
  topicName: string;
  questionText: string;
  sampleAnswer?: string;
  keywords?: string;
  difficulty: InterviewDifficulty;
  questionType: InterviewQuestionType;
  optionsJson?: string;
  correctOptionId?: string;
  displayOrder: number;
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string;
}

export interface InterviewQuestionFilter {
  search?: string;
  isActive?: boolean;
  topicId?: number;
  difficulty?: InterviewDifficulty;
  questionType?: InterviewQuestionType;
}

export interface InterviewAssignmentDto {
  id: number;
  topicId: number;
  topicName: string;
  classId?: number | null;
  classCode?: string;
  className?: string;
  title: string;
  description?: string;
  durationMinutes: number;
  totalQuestions: number;
  easyCount: number;
  mediumCount: number;
  hardCount: number;
  passingScore: number;
  startTime?: string;
  endTime?: string;
  status: "DRAFT" | "PUBLISHED" | "CLOSED";
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string;

  // Dashboard Aggregates
  totalSubmissions: number;
  pendingReviewCount: number;
  averageScore?: number | null;

  // Student specific
  mySessionStatus?: "NOT_STARTED" | "IN_PROGRESS" | "SUBMITTED" | "REVIEWED" | "PUBLISHED";
  mySessionId?: number | null;
  myTotalScore?: number | null;
}

export interface InterviewAssignmentFilter {
  search?: string;
  topicId?: number;
  classId?: number;
  status?: string;
  isActive?: boolean;
}

export interface InterviewSessionQuestionDto {
  questionId: number;
  displayOrder: number;
  questionText: string;
  difficulty: InterviewDifficulty;
  questionType: InterviewQuestionType;
  optionsJson?: string;
  sampleAnswer?: string;
  correctOptionId?: string;

  // Candidate input
  answerText?: string;
  selectedOptionId?: string;
  answerMode: InterviewAnswerMode;
  isFlagged: boolean;

  // Evaluation
  isCorrect?: boolean | null;
  score?: number | null;
  aiScore?: number | null;
  aiVerdict?: string;
  aiFeedback?: string;
  teacherScore?: number | null;
  teacherNote?: string;
}

export interface InterviewSessionDto {
  sessionId: number;
  assignmentId: number;
  assignmentTitle: string;
  topicName: string;
  durationMinutes: number;
  totalQuestions: number;
  passingScore: number;
  userId: number;
  studentName: string;
  studentCode: string;
  className?: string;

  startedAt: string;
  submittedAt?: string;
  remainingSeconds: number;
  status: "IN_PROGRESS" | "SUBMITTED" | "REVIEWED" | "PUBLISHED";

  totalScore?: number | null;
  aiAverageScore?: number | null;
  teacherScore?: number | null;
  overallVerdict?: string;
  overallSummary?: string;
  teacherFeedback?: string;
  reviewedBy?: number | null;
  reviewedByName?: string;
  reviewedAt?: string;

  questions: InterviewSessionQuestionDto[];
  report?: CandidateInterviewReportDto;
}

export interface CandidateInterviewReportDto {
  totalScorePercentage: number;
  completionRate: number;
  totalQuestions: number;
  answeredQuestions: number;

  proficiencyRank: "ADVANCED" | "PROFICIENT" | "COMPETENT" | "DEVELOPING" | "NOVICE";
  proficiencyRankTitle: string;
  proficiencySummary: string;
  ojtRecommendation: string;

  easyCount: number;
  easyScorePercent: number;
  mediumCount: number;
  mediumScorePercent: number;
  hardCount: number;
  hardScorePercent: number;

  mcqTotal: number;
  mcqCorrect: number;
  mcqAccuracyRate: number;

  subjectiveTotal: number;
  subjectiveScorePercent: number;

  strengths: string[];
  areasForImprovement: string[];
  recommendations: string[];
}

export interface SaveSessionAnswerRequest {
  questionId: number;
  answerText?: string;
  selectedOptionId?: string;
  answerMode?: InterviewAnswerMode;
  isFlagged?: boolean;
}

export interface ReviewQuestionScoreItem {
  questionId: number;
  teacherScore?: number;
  teacherNote?: string;
}

export interface ReviewSessionRequest {
  sessionTeacherScore?: number;
  teacherFeedback?: string;
  overallVerdict?: string;
  publishResult?: boolean;
  questionScores?: ReviewQuestionScoreItem[];
}

export interface InterviewSessionSubmissionSummaryDto {
  sessionId: number;
  assignmentId: number;
  assignmentTitle: string;
  userId: number;
  studentCode: string;
  studentName: string;
  studentEmail: string;
  className?: string;
  startedAt: string;
  submittedAt?: string;
  status: string;
  totalScore?: number | null;
  aiAverageScore?: number | null;
  teacherScore?: number | null;
  overallVerdict?: string;
  reviewedAt?: string;
  reviewedByName?: string;
}
