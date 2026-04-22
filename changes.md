# Backend Changes

## 1) CORS configuration added in WebAPI startup
- Added a CORS policy named `AllowAll` in `ScholarFlow.WebAPI/Program.cs`.
- Policy allows:
  - Any origin
  - Any HTTP method
  - Any header
- Enabled the policy in middleware pipeline using `app.UseCors("AllowAll")`.

## 2) Development launch URL updated
- Updated `ScholarFlow.WebAPI/Properties/launchSettings.json`.
- Changed `applicationUrl` from:
  - `https://localhost:7202;http://localhost:5279`
- To:
  - `https://localhost:7202`

## 3) Streams query table-name fix
- Updated `Modules/ScholarFlow.Modules.Academic/Queries/GetAllStreams/GetAllStreamsQueryHandler.cs`.
- Changed SQL source table from `AcademicStreams` to `Streams` to match the actual DB table name.

## 4) Subject summary response extended with streams
- Updated `Modules/ScholarFlow.Modules.Academic/DTOs/SubjectSummaryDto.cs`.
- Added `Streams` collection to each subject summary response.

## 5) Get all subjects query now includes linked streams
- Updated `Modules/ScholarFlow.Modules.Academic/Queries/GetAllSubjects/GetAllSubjectsQueryHandler.cs`.
- Added joins to `SubjectStreams` and `Streams` so each subject can return its connected streams.
- Added de-duplication/aggregation logic when mapping Dapper rows into `SubjectSummaryDto`.

## 6) Academic admin endpoints now allow SuperAdmin too
- Updated `ScholarFlow.WebAPI/Controllers/AcademicController.cs`.
- Stream and subject management endpoints that were `Admin` only now allow both `Admin` and `SuperAdmin`.

## Impact Summary
- Frontend or external clients can call API endpoints without CORS blocking (for allowed environments where this policy is active).
- API local development now runs only on HTTPS for the default profile.
- Stream listing now reads from the correct table in SQL queries.
- Subject listing now includes stream associations in backend response payload.
- SuperAdmin accounts can manage streams and subjects through protected academic endpoints.

---

## 7) Examination module added (full student exam flow)
- New module: `Modules/ScholarFlow.Modules.Examination/`
- New controller: `ScholarFlow.WebAPI/Controllers/ExaminationController.cs`
- Route prefix: `api/examination` — requires `Student` role.
- **Commands added:**
  - `StartExamSession` — starts a paper-based exam session. Body: `{ paperId, isPractice }`. Returns `StartSessionResultDto { sessionId, startTime, isPractice, questionCount }`.
  - `GeneratePersonalizedExam` — AI-driven personalized exam from a subject's question pool. Body: `{ subjectId }`. Returns same `StartSessionResultDto`. Selects 50 questions weighted by weakness tiers (VeryWeak 40%, Weak 30%, Average 20%, Unknown 10%) and difficulty mix per tier.
  - `SubmitAnswer` — records a student's answer. Route: `POST /sessions/{id}/answer`. Body: `{ questionId, selectedOptionId, timeSpentSeconds }`.
  - `FlagQuestion` — marks/unmarks a question for review. Route: `POST /sessions/{id}/flag`. Body: `{ questionId, flagged }`.
  - `EndExamSession` — closes the session and scores it. Route: `POST /sessions/{id}/end`. Returns `EndSessionResultDto { sessionId, obtainedMarks, totalMarks, percentage, isPassing, correctCount, wrongCount, skippedCount, timeTakenSeconds, isPractice }`.
- **Queries added:**
  - `GetAvailablePapers` — `GET /papers` with optional `subjectId`, `type`, `medium`, `year` query params.
  - `GetPaperQuestionsForExam` — `GET /papers/{id}/questions`.
  - `GetMySessions` — `GET /sessions` with optional `paperId`, `isPractice` filters. Returns list of `SessionSummaryDto`.
  - `GetSessionDetail` — `GET /sessions/{id}`. Returns `SessionDetailDto`.
  - `GetSessionReview` — `GET /sessions/{id}/review`. Returns full review with question-by-question breakdown including explanations.
- **DTOs added:** `StartSessionResultDto`, `EndSessionResultDto`, `SessionSummaryDto`, `SessionDetailDto`, `SessionResponseDto`, `ExamQuestionDto`, `ExamOptionDto`, `AvailablePaperDto`, `SessionReviewItemDto`, `ReviewExplanationDto`, `ReviewOptionDto`.
- **Validators added:** `StartExamSessionCommandValidator`, `GeneratePersonalizedExamCommandValidator`, `SubmitAnswerCommandValidator`, `FlagQuestionCommandValidator`, `EndExamSessionCommandValidator`.
- **Public API contract:** `IExaminationApi` — exposes `GetSessionResponsesAsync` for Analytics module to read session responses after completion.
- DI registered in `DependencyInjection.cs`.

## 8) Analytics module added (student performance tracking)
- New module: `Modules/ScholarFlow.Modules.Analytics/`
- New controller: `ScholarFlow.WebAPI/Controllers/AnalyticsController.cs`
- Route prefix: `api/analytics` — requires `Student` role.
- **Queries added:**
  - `GetSubjectPerformance` — `GET /analytics/subjects`. Returns `SubjectPerformanceDto[]` with `{ subjectId, subjectName, totalExams, averageScore, bestScore, totalQuestionsAttempted, overallCorrectPercentage, studyStreakDays, lastStudiedAt }`.
  - `GetSubTopicPerformance` — `GET /analytics/subjects/{subjectId}/subtopics`. Returns `SubTopicPerformanceDto[]` with `{ subTopicId, subTopicName, topicName, totalAttempts, correctCount, correctPercentage, lastUpdated }`.
  - `GetExamHistory` — `GET /analytics/subjects/{subjectId}/history`. Returns `ExamHistoryDto[]` with `{ sessionId, paperTitle, date, score, obtainedMarks, totalMarks }`.
- **Event handler added:** `ExamSessionCompletedDomainEventHandler` — listens to the `ExamSessionCompletedDomainEvent` domain event. When a non-practice exam completes:
  1. Updates `StudentQuestionHistory` per question (timesAttempted, correctCount, lastAnswerCorrect, lastSeenAt).
  2. Groups responses by subtopic and upserts `StudentSubTopicPerformance` (totalAttempts, correctCount, correctPercentage).
  3. After accumulating enough data, triggers `IAcademicApi.UpdateSystemDifficultyAsync` to auto-recalculate question difficulty.
- **Public API contract:** `IAnalyticsApi` — exposes `GetSubTopicPerformancesAsync`, `GetRecentlySeenQuestionIdsAsync` for Examination module's personalized exam generation.
- DI registered in `DependencyInjection.cs`.

## 9) Academic module — Question management commands added
- Added `AddQuestionToPaper` command under `Modules/ScholarFlow.Modules.Academic/Commands/Papers/AddQuestionToPaper/`.
  - Body: `{ paperId, subTopicId, questionText, questionImageUrl?, orderIndex, manualDifficulty?, options[] }` where each option is `{ label, optionText, optionImageUrl?, isCorrect }`.
  - Returns: `Guid` (new question ID).
- Added `UpdateQuestion` command under `Modules/ScholarFlow.Modules.Academic/Commands/Papers/UpdateQuestion/`.
  - Body: `{ id, subTopicId, questionText, questionImageUrl?, orderIndex, manualDifficulty? }`.
  - Updates question fields; options are managed separately.

## 10) Academic Public API extended
- Updated `Modules/ScholarFlow.Modules.Academic/Public/IAcademicApi.cs`.
- Added methods consumed by Examination and Analytics modules:
  - `PaperExistsAsync(paperId)` — checks paper existence.
  - `GetPaperSummaryAsync(paperId)` — returns `AcademicPaperSummary { id, isPublic, negativeMarkValue, createdByTeacherId, questionCount }`.
  - `GetQuestionsForExamAsync(paperId)` — returns `AcademicQuestionSummary[]` with `{ questionId, correctOptionId }` for scoring.
  - `GetQuestionPoolAsync(subjectId)` — returns full `QuestionPoolItem[]` with `{ questionId, subTopicId, topicId, subjectId, manualDifficulty, systemDifficulty }` and computed `EffectiveDifficulty` property. Used by personalized exam generation.
  - `UpdateSystemDifficultyAsync(questionId, level)` — called by Analytics after enough student responses to auto-set system difficulty.
- `QuestionPoolItem.EffectiveDifficulty` maps `ManualDifficulty` → `SystemDifficultyLevel` (DirectRecall→Easy, Conceptual/Calculation→Medium, Analytical→Hard), falling back to `SystemDifficulty`.

## 11) Domain — new enums and entities
- Added `ScholarFlow.Domain/Enums/DifficultyLevel.cs` — cognitive/manual difficulty: `DirectRecall=1`, `Conceptual=2`, `Calculation=3`, `Analytical=4`.
- Added `ScholarFlow.Domain/Enums/SystemDifficultyLevel.cs` — performance-based auto difficulty: `Easy=1` (>70% correct rate), `Medium=2` (40–70%), `Hard=3` (<40%).
- Updated `ScholarFlow.Domain/Entities/Question.cs` — added `ManualDifficulty` (DifficultyLevel?), `SystemDifficulty` (SystemDifficultyLevel?), `UserResponses` navigation collection, `UpdateSystemDifficulty(level)` method.
- Added `ScholarFlow.Domain/Entities/UserResponse.cs` — records a student's answer per question in a session.
- Added `ScholarFlow.Domain/Interfaces/IAnalyticsApi.cs` and `IExaminationApi.cs` — cross-module contracts in Domain layer.
- Added `ScholarFlow.Domain/Interfaces/Repositories/IAnalyticsRepository.cs` and `IExamSessionRepository.cs`.

## 12) Infrastructure — new repositories and EF migration
- Added `ScholarFlow.Infrastructure/Persistence/Repositories/EfExamSessionRepository.cs` — EF implementation of `IExamSessionRepository`.
- Added `ScholarFlow.Infrastructure/Persistence/Repositories/EfAnalyticsRepository.cs` — EF implementation of `IAnalyticsRepository`.
- Added EF migration `20260419145602_InitialCreate` — creates all tables for Examination and Analytics domain entities (ExamSessions, UserResponses, ExamSessionQuestions, StudentQuestionHistory, StudentSubTopicPerformance, etc.).

## 13) UserProfiles Public API extended
- Updated `Modules/ScholarFlow.Modules.UserProfiles/Public/IUserProfilesApi.cs` and `UserProfilesApi.cs`.
- Exposed methods needed by Examination/Analytics modules to resolve student identity from JWT user ID.

## Impact Summary (Examination & Analytics)
- Students can now start paper-based exams (`POST /examination/sessions/start`) or request personalized AI-generated exams (`POST /examination/sessions/generate`).
- Answers are submitted per-question in real time; questions can be flagged for review.
- Ending a session returns a full score breakdown.
- Session history and per-question review with explanations are available via GET endpoints.
- Analytics endpoints provide per-subject and per-subtopic performance aggregates, study streak, and exam history.
- After every non-practice exam, the system automatically recalculates question difficulty based on aggregated student performance data.
- All Examination and Analytics endpoints are restricted to authenticated `Student` role users.
