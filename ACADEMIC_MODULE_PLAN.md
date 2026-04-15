# Academic Module — Full Implementation Plan

Admin + Teacher management only.
Student-facing APIs (browse papers, get questions for exam) → Examination module scope.

---

## 1. API Endpoints — Full List

### Reference Data (Admin only)

```
# Streams
POST   /api/academic/streams                              Create stream
GET    /api/academic/streams                              List all streams
PUT    /api/academic/streams/{id}                         Update stream
DELETE /api/academic/streams/{id}                         Delete stream

# Subjects
POST   /api/academic/subjects                             Create subject (with stream assignments)
GET    /api/academic/subjects?streamId={id}               List subjects (optional stream filter)
GET    /api/academic/subjects/{id}                        Subject detail (with streams + topics)
PUT    /api/academic/subjects/{id}                        Update subject
DELETE /api/academic/subjects/{id}                        Delete subject
POST   /api/academic/subjects/{id}/streams/{streamId}     Assign subject to stream
DELETE /api/academic/subjects/{id}/streams/{streamId}     Remove subject from stream

# Topics
POST   /api/academic/topics                               Create topic (+ subtopics in same request)
GET    /api/academic/topics?subjectId={id}                List topics with subtopics
PUT    /api/academic/topics/{id}                          Update topic
DELETE /api/academic/topics/{id}                          Delete topic (cascades subtopics)

# SubTopics
POST   /api/academic/topics/{topicId}/subtopics           Add single subtopic to topic
PUT    /api/academic/subtopics/{id}                       Update subtopic
DELETE /api/academic/subtopics/{id}                       Delete subtopic
```

### Full Academic Tree (Admin + Teacher)

```
GET    /api/academic/tree                                 Full tree: Stream → Subject → Topic → SubTopic
```

---

### Question Bank (Admin + Teacher)

```
# Papers
POST   /api/academic/papers                               Create paper
GET    /api/academic/papers?subjectId=&type=&medium=&year= List papers (with filters)
GET    /api/academic/papers/{id}                          Paper detail
PUT    /api/academic/papers/{id}                          Update paper
DELETE /api/academic/papers/{id}                          Delete paper
PATCH  /api/academic/papers/{id}/visibility               Toggle public/private

# Questions
POST   /api/academic/papers/{paperId}/questions           Add question (+ all 5 options in same request)
GET    /api/academic/papers/{paperId}/questions           List questions with options
PUT    /api/academic/questions/{id}                       Update question text/image/subtopic
DELETE /api/academic/questions/{id}                       Delete question

# Options
PUT    /api/academic/options/{id}                         Update single option

# Explanations
POST   /api/academic/questions/{questionId}/explanation   Add explanation (+ sections in same request)
PUT    /api/academic/explanations/{id}                    Update explanation
DELETE /api/academic/explanations/{id}                    Delete explanation
POST   /api/academic/explanations/{id}/sections           Add single section
PUT    /api/academic/sections/{id}                        Update section
DELETE /api/academic/sections/{id}                        Delete section
```

---

## 2. Request / Response Shapes

### Stream

```json
POST /api/academic/streams
{
  "name": "Physical Science",
  "description": "Physics, Chemistry, Combined Maths"
}
→ { "id": "guid" }

GET /api/academic/streams
→ [
    { "id": "guid", "name": "Physical Science", "description": "..." }
  ]
```

---

### Subject

```json
POST /api/academic/subjects
{
  "name": "Physics",
  "description": "A/L Physics",
  "streamIds": ["guid1", "guid2"]   // assign to streams at creation time
}
→ { "id": "guid" }

GET /api/academic/subjects/{id}
→ {
    "id": "guid",
    "name": "Physics",
    "description": "...",
    "streams": [{ "id": "guid", "name": "Physical Science" }],
    "topicCount": 12
  }
```

---

### Topic — create with SubTopics in same request

```json
POST /api/academic/topics
{
  "subjectId": "guid",
  "topicName": "Mechanics",
  "orderIndex": 1,
  "subTopics": [                      // optional — add subtopics at same time
    { "name": "Newton's Laws",   "orderIndex": 1 },
    { "name": "Kinematics",      "orderIndex": 2 },
    { "name": "Work and Energy", "orderIndex": 3 }
  ]
}
→ { "id": "guid" }

GET /api/academic/topics?subjectId={id}
→ [
    {
      "id": "guid",
      "topicName": "Mechanics",
      "orderIndex": 1,
      "subTopics": [
        { "id": "guid", "name": "Newton's Laws", "orderIndex": 1 },
        { "id": "guid", "name": "Kinematics",    "orderIndex": 2 }
      ]
    }
  ]
```

---

### Paper

```json
POST /api/academic/papers
{
  "title": "2023 A/L Physics Paper",
  "subjectId": "guid",
  "type": "PastPaper",              // PastPaper | ModelPaper | CommonGeneralTest
  "medium": "Sinhala",              // Sinhala | Tamil | English
  "year": 2023,
  "sitting": "FirstSitting",        // FirstSitting | SecondSitting | null
  "negativeMarkValue": 0.25,        // 0 = no negative marking
  "isPublic": true,
  "officialPaperCode": "PH-2023-01" // optional
}
→ { "id": "guid" }

GET /api/academic/papers
→ [
    {
      "id": "guid",
      "title": "2023 A/L Physics Paper",
      "subjectName": "Physics",
      "type": "PastPaper",
      "medium": "Sinhala",
      "year": 2023,
      "questionCount": 50,
      "isPublic": true,
      "createdAt": "2026-04-13T..."
    }
  ]

GET /api/academic/papers/{id}
→ {
    "id": "guid",
    "title": "...",
    "subjectId": "guid",
    "subjectName": "Physics",
    "type": "PastPaper",
    "medium": "Sinhala",
    "year": 2023,
    "sitting": "FirstSitting",
    "negativeMarkValue": 0.25,
    "isPublic": true,
    "officialPaperCode": "PH-2023-01",
    "questionCount": 50,
    "createdByTeacherId": null       // null = Admin created
  }

PATCH /api/academic/papers/{id}/visibility
{
  "isPublic": false
}
→ 200 OK
```

---

### Question — create with all 5 Options in same request

```json
POST /api/academic/papers/{paperId}/questions
{
  "subTopicId": "guid",
  "questionText": "A body of mass 2kg is moving...",
  "questionImageUrl": null,             // optional
  "orderIndex": 1,
  "options": [                          // exactly 5 options, A/L format
    { "label": "1", "optionText": "5 m/s",  "optionImageUrl": null, "isCorrect": false },
    { "label": "2", "optionText": "10 m/s", "optionImageUrl": null, "isCorrect": true  },
    { "label": "3", "optionText": "15 m/s", "optionImageUrl": null, "isCorrect": false },
    { "label": "4", "optionText": "20 m/s", "optionImageUrl": null, "isCorrect": false },
    { "label": "5", "optionText": "25 m/s", "optionImageUrl": null, "isCorrect": false }
  ]
}
→ { "id": "guid" }

GET /api/academic/papers/{paperId}/questions
→ [
    {
      "id": "guid",
      "orderIndex": 1,
      "questionText": "A body of mass...",
      "questionImageUrl": null,
      "subTopicId": "guid",
      "subTopicName": "Kinematics",
      "options": [
        { "id": "guid", "label": "1", "optionText": "5 m/s",  "isCorrect": false },
        { "id": "guid", "label": "2", "optionText": "10 m/s", "isCorrect": true  }
      ],
      "hasExplanation": true
    }
  ]
```

---

### Explanation — create with Sections in same request

```json
POST /api/academic/questions/{questionId}/explanation
{
  "type": "Text",                        // Text | Video
  "videoUrl": null,                      // required only when type = Video
  "sections": [                          // for Text type
    { "title": "Step 1",  "content": "Apply Newton's second law...", "orderIndex": 1 },
    { "title": "Step 2",  "content": "Substitute values...",         "orderIndex": 2 },
    { "title": "Answer",  "content": "Therefore v = 10 m/s",         "orderIndex": 3 }
  ]
}
→ { "id": "guid" }
```

---

## 3. Commands — Full List

### Reference Data Commands (14 commands)

| Command | Returns | Who |
|---|---|---|
| `CreateStreamCommand(name, description?)` | `Guid` | Admin |
| `UpdateStreamCommand(id, name, description?)` | `void` | Admin |
| `DeleteStreamCommand(id)` | `void` | Admin |
| `CreateSubjectCommand(name, description?, streamIds[])` | `Guid` | Admin |
| `UpdateSubjectCommand(id, name, description?)` | `void` | Admin |
| `DeleteSubjectCommand(id)` | `void` | Admin |
| `AssignSubjectToStreamCommand(subjectId, streamId)` | `void` | Admin |
| `RemoveSubjectFromStreamCommand(subjectId, streamId)` | `void` | Admin |
| `CreateTopicCommand(subjectId, topicName, orderIndex, subTopics[])` | `Guid` | Admin |
| `UpdateTopicCommand(id, topicName, orderIndex)` | `void` | Admin |
| `DeleteTopicCommand(id)` | `void` | Admin |
| `CreateSubTopicCommand(topicId, name, orderIndex)` | `Guid` | Admin |
| `UpdateSubTopicCommand(id, name, orderIndex)` | `void` | Admin |
| `DeleteSubTopicCommand(id)` | `void` | Admin |

### Question Bank Commands (11 commands)

| Command | Returns | Who |
|---|---|---|
| `CreatePaperCommand(title, subjectId, type, medium, year, sitting?, negativeMarkValue, isPublic, officialPaperCode?)` | `Guid` | Admin / Teacher |
| `UpdatePaperCommand(id, title, year, type, medium, sitting?, negativeMarkValue, officialPaperCode?)` | `void` | Owner only |
| `DeletePaperCommand(id)` | `void` | Owner only |
| `TogglePaperVisibilityCommand(id, isPublic)` | `void` | Owner only |
| `AddQuestionToPaperCommand(paperId, subTopicId, questionText, questionImageUrl?, orderIndex, options[])` | `Guid` | Owner only |
| `UpdateQuestionCommand(id, subTopicId, questionText, questionImageUrl?, orderIndex)` | `void` | Owner only |
| `DeleteQuestionCommand(id)` | `void` | Owner only |
| `UpdateOptionCommand(id, optionText, optionImageUrl?, isCorrect)` | `void` | Owner only |
| `AddExplanationToQuestionCommand(questionId, type, videoUrl?, sections[])` | `Guid` | Owner only |
| `UpdateExplanationCommand(id, type, videoUrl?)` | `void` | Owner only |
| `DeleteExplanationCommand(id)` | `void` | Owner only |

Total: **25 Commands**

---

## 4. Queries — Full List

| Query | Returns | Filter params |
|---|---|---|
| `GetAllStreamsQuery` | `StreamDto[]` | — |
| `GetAllSubjectsQuery` | `SubjectSummaryDto[]` | `streamId?` |
| `GetSubjectDetailQuery(id)` | `SubjectDetailDto` | — |
| `GetTopicsBySubjectQuery(subjectId)` | `TopicWithSubTopicsDto[]` | — |
| `GetAcademicTreeQuery` | `AcademicStreamTreeDto[]` | — (full tree, no params) |
| `GetPapersQuery` | `PaperSummaryDto[]` | `subjectId?, type?, medium?, year?, isPublic?` |
| `GetPaperDetailQuery(id)` | `PaperDetailDto` | — |
| `GetPaperQuestionsQuery(paperId)` | `QuestionWithOptionsDto[]` | — |

Total: **8 Queries**

---

## 5. Repositories Needed

| Interface | Owns | Methods |
|---|---|---|
| `IStreamRepository` | `AcademicStream` | GetAll, GetById, Add, Update, Delete, SaveChanges |
| `ISubjectRepository` | `Subject`, `SubjectStream` | GetAll, GetById, GetWithStreams, Add, Update, Delete, SubjectStreamExists, AddSubjectStream, RemoveSubjectStream, SaveChanges |
| `ITopicRepository` | `Topic`, `SubTopic` | GetBySubjectId, GetById, Add, Update, Delete, AddSubTopic, SaveChanges |
| `IPaperRepository` | `Paper` | GetAll, GetById, GetByTitle, Add, Update, Delete, SaveChanges |
| `IQuestionRepository` | `Question`, `Option`, `Explanation`, `ExplanationSection` | GetByPaperId, GetById, Add, Update, Delete, GetWithOptions, SaveChanges |

---

## 6. Folder Structure — Academic Module

```
Modules/ScholarFlow.Modules.Academic/
│
├── Commands/
│   ├── Streams/
│   │   ├── CreateStream/
│   │   │   ├── CreateStreamCommand.cs
│   │   │   ├── CreateStreamCommandHandler.cs
│   │   │   └── CreateStreamCommandValidator.cs
│   │   ├── UpdateStream/
│   │   └── DeleteStream/
│   │
│   ├── Subjects/
│   │   ├── CreateSubject/
│   │   ├── UpdateSubject/
│   │   ├── DeleteSubject/
│   │   ├── AssignSubjectToStream/
│   │   └── RemoveSubjectFromStream/
│   │
│   ├── Topics/
│   │   ├── CreateTopic/           ← includes SubTopics[] in command
│   │   ├── UpdateTopic/
│   │   ├── DeleteTopic/
│   │   ├── CreateSubTopic/
│   │   ├── UpdateSubTopic/
│   │   └── DeleteSubTopic/
│   │
│   └── Papers/
│       ├── CreatePaper/
│       ├── UpdatePaper/
│       ├── DeletePaper/
│       ├── TogglePaperVisibility/
│       ├── AddQuestionToPaper/    ← includes Options[] in command
│       ├── UpdateQuestion/
│       ├── DeleteQuestion/
│       ├── UpdateOption/
│       ├── AddExplanationToQuestion/  ← includes Sections[] in command
│       ├── UpdateExplanation/
│       └── DeleteExplanation/
│
├── Queries/
│   ├── GetAcademicTree/           ← full tree, single Dapper query
│   ├── GetAllStreams/
│   ├── GetAllSubjects/
│   ├── GetSubjectDetail/
│   ├── GetTopicsBySubject/
│   ├── GetPapers/
│   ├── GetPaperDetail/
│   └── GetPaperQuestions/
│
├── DTOs/
│   ├── AcademicStreamTreeDto.cs   ← tree response (Stream → Subject → Topic → SubTopic)
│   ├── StreamDto.cs
│   ├── SubjectSummaryDto.cs
│   ├── SubjectDetailDto.cs
│   ├── TopicWithSubTopicsDto.cs
│   ├── PaperSummaryDto.cs
│   ├── PaperDetailDto.cs
│   └── QuestionWithOptionsDto.cs
│
├── Public/                        ← Module Public API (for Examination module)
│   ├── IAcademicApi.cs
│   └── AcademicApi.cs
│
└── DependencyInjection.cs
```

Infrastructure additions:
```
Infrastructure/Persistence/Repositories/
  ├── EfStreamRepository.cs
  ├── EfSubjectRepository.cs
  ├── EfTopicRepository.cs
  ├── EfPaperRepository.cs
  └── EfQuestionRepository.cs

Domain/Interfaces/Repositories/
  ├── IStreamRepository.cs
  ├── ISubjectRepository.cs
  ├── ITopicRepository.cs
  ├── IPaperRepository.cs
  └── IQuestionRepository.cs
```

---

## 7. Authorization Rules

| Action | Admin | Teacher | Notes |
|---|---|---|---|
| Stream CRUD | ✅ | ❌ | Reference data — Admin only |
| Subject CRUD | ✅ | ❌ | Reference data — Admin only |
| Topic / SubTopic CRUD | ✅ | ❌ | Reference data — Admin only |
| Create Paper | ✅ | ✅ | Admin → IsPublic=true default, Teacher → IsPublic=false default |
| Update / Delete Paper | ✅ own | ✅ own | Must be paper owner (CreatedByTeacherId == currentUser or Admin) |
| Toggle Visibility | ✅ | ✅ own | Teacher can only toggle own private papers |
| Add / Update / Delete Question | ✅ own | ✅ own | Must own the paper |
| Add Explanation | ✅ own | ✅ own | Must own the paper |

---

## 8. Business Rules (Validators + Handler checks)

| Rule | Where enforced |
|---|---|
| Stream name must be unique | CreateStreamCommandHandler — ConflictException |
| Subject name must be unique | CreateSubjectCommandHandler — ConflictException |
| SubjectStream must not already exist (no duplicate assign) | AssignSubjectToStreamCommandHandler |
| Topic orderIndex must be unique within subject | Validator — range check |
| Question must have exactly 5 options | CreateQuestionCommandValidator |
| Question must have exactly 1 correct option | CreateQuestionCommandValidator |
| Option label must be "1"–"5" | CreateQuestionCommandValidator |
| Explanation: if type=Video, videoUrl required | Validator |
| Explanation: if type=Text, sections must not be empty | Validator |
| NegativeMarkValue must be between 0 and 1 | Domain — DomainException in Paper.Create() |
| Paper update/delete only by owner | Handler — ForbiddenException |

---

## 9. Module Public API (IAcademicApi)

Examination module will use this to validate paper/question data without touching Academic tables directly.

```csharp
public interface IAcademicApi
{
    Task<bool> PaperExistsAsync(Guid paperId, CancellationToken ct);
    Task<PaperSummary?> GetPaperSummaryAsync(Guid paperId, CancellationToken ct);
    Task<IReadOnlyList<QuestionSummary>> GetQuestionsForExamAsync(Guid paperId, CancellationToken ct);
}

public sealed record PaperSummary(
    Guid Id,
    bool IsPublic,
    decimal NegativeMarkValue,
    Guid? CreatedByTeacherId,
    int QuestionCount);

public sealed record QuestionSummary(
    Guid QuestionId,
    Guid CorrectOptionId,
    decimal NegativeMarkValue);
```

---

## 10. Implementation Order

Build in this order so each step is independently testable:

1. **Repositories** — 5 interfaces + 5 EF implementations + DbSet entries
2. **Reference Data commands** — Stream → Subject → Topic (with SubTopics)
3. **Reference Data queries** — GetAllStreams, GetAllSubjects, GetTopicsBySubject
4. **Paper commands** — CreatePaper, UpdatePaper, DeletePaper, ToggleVisibility
5. **Question commands** — AddQuestion (with options), UpdateQuestion, UpdateOption, DeleteQuestion
6. **Explanation commands** — AddExplanation (with sections), Update, Delete
7. **Paper + Question queries** — GetPapers, GetPaperDetail, GetPaperQuestions
8. **AcademicApi** — Module Public API for Examination module
9. **Controller** — AcademicController wiring all endpoints
10. **Migration** — if any new columns needed
