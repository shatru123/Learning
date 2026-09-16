# LearningOS — 100-Day Career & AI Engineering Mastery

> **"The roadmap can change. The schedule can change. A day can be missed. But my learning history should never disappear."**

LearningOS is a production-grade, full-stack learning operating system designed for real-life software engineering learning journeys. Built with **ASP.NET Core 8.0**, **Entity Framework Core**, and **PostgreSQL** as the authoritative persistent datastore, LearningOS protects against unrealistic workloads, preserves your complete audit trail, and provides smart recovery strategies for missed or skipped days.

---

## 🏗️ Architecture

```
                    Render Cloud
                         │
        ┌────────────────┴────────────────┐
        │                                 │
        ▼                                 ▼
 ASP.NET Core Web Service (Stateless)   PostgreSQL (Authoritative Persistent DB)
        │                                 │
        └────────────────┬────────────────┘
                         ▼
                   Browser SPA
```

### Critical Persistence Invariant
- **PostgreSQL is the ONLY authoritative production datastore**.
- The web service filesystem on Render is ephemeral and stateless.
- If PostgreSQL is unreachable in production, the service fails fast with an explicit error rather than silently falling back to local files, memory, or SQLite.
- Every task completion, reschedule, missed day reason, note, journal entry, and review is permanently retained with full audit history.

---

## 🎯 Core Features

### 1. Exactly 100 Learning Days
- Curriculum covers exactly 100 numbered learning days (Day 1 through Day 100).
- Planned Rest Days and Leave Days are scheduled calendar events separate from the 100 curriculum days, preventing day numbering ambiguity.

### 2. 8 Visually Distinct Day Statuses
- 📅 **Planned**: Scheduled future days.
- ⏳ **In Progress**: Active day with pulsing indicator.
- ✅ **Completed**: All tasks finished.
- 🌓 **Partially Completed**: Active progress recorded.
- ⏭️ **Skipped**: Deliberately skipped day.
- ❌ **Missed**: Day missed with recorded reason and notes.
- 🌴 **Rest Day**: Planned rest day bridging active days (preserves streak!).
- 🏖️ **Leave Day**: Sick leave, vacation, wedding, travel, or personal work (preserves streak!).

### 3. Missed Day & Smart Recovery Manager
When a day is missed:
- Click **"Mark Day as Missed"** and select a reason (*Work, Health, Family, Travel, Personal, Workload, Lack of time, Other*) with optional notes.
- **Original tasks and history remain completely visible and intact.**
- **Smart Recovery Strategies**:
  - **Option A — Move Tasks to Tomorrow**: Workload-protected! Respects your configurable daily cap (default 60 mins). Excess workload automatically rolls over to the following day.
  - **Option B — Reschedule**: Choose a specific date to carry forward incomplete work.
  - **Option C — Compress**: Distribute incomplete tasks evenly across the next 3 to 7 days.
  - **Option D — Skip**: Permanently mark incomplete tasks as skipped while advancing the roadmap.
  - **Extend Roadmap**: Calculates unfinished learning days and moves the schedule forward by N days upon explicit confirmation.

### 4. Intelligent Streak System
- Planned Rest Days (🌴) and recorded Leave Days (🏖️) **do not break your streak**.
- Only unexplained missed days interrupt active streaks.

### 5. Daily Review & Reflection
- End-of-day flow: Rate today (*Completed everything, Completed most, Completed some, Missed, Rest day*), record what happened, carry forward notes, and define tomorrow's #1 priority.

### 6. Outage & Offline Safety (Draft Protection)
- Client-side network interceptor detects 5xx errors or network drops.
- Warns: *"LearningOS is temporarily unavailable. Your local changes have not been confirmed as saved."*
- Form inputs (notes, review, journal) are cached in temporary browser draft storage until confirmed saved by PostgreSQL with `200 OK`.

### 7. Backup & Restore Center
- **Export Everything**: Download full JSON backup (`LearningOS_Backup_YYYY-MM-DD.json`) and CSV summaries.
- **Restore Backup**: Validates schema and entity counts before asking for explicit confirmation. Never silently overwrites data.

---

## 📚 100-Day Curriculum Highlights

The system comes pre-seeded with a comprehensive curriculum across 5 phases:

1. **Phase 1 (Days 1–25)**: Advanced C# Internals (Memory, Span/Memory, GC Tuning, AsyncStateMachine), ASP.NET Core Internals (Middleware, DI Lifetimes, Minimal APIs), SQL Internals (B-Tree, Execution Plans, MVCC, Isolation Levels), Dapper & EF Core Optimization, Redis Caching & Distributed Locks, and Fundamental DSA.
2. **Phase 2 (Days 26–50)**: Microservices Architecture, API Gateways (YARP), Resilient HTTP (Refit & Polly), Event-Driven Messaging (RabbitMQ, Dead-Letter Queues, MassTransit), Outbox & Saga Patterns, Distributed Idempotency, OpenTelemetry, Prometheus, Grafana, Jaeger, Docker, Kubernetes, CI/CD, Terraform, and Graph/DP DSA.
3. **Phase 3 (Days 51–75)**: AI/LLM Engineering (Transformers, Tokenization, OpenAI, Claude, Gemini, Ollama), Advanced Prompt Engineering, Embeddings, PostgreSQL pgvector, Qdrant/Pinecone, Hybrid Search (BM25 + Dense RRF), Reranking, Function/Tool Calling, AI Agents (ReAct, Semantic Kernel), AI Security, Observability (LangSmith, OpenLLMetry), Semantic Caching, and Cost/Latency Optimization.
4. **Phase 4 (Days 76–90)**: Production Enterprise AI Portfolio Project, Multi-Tenancy, Automated CI/CD Pipelines, Helm, Load Testing (k6), Architecture Documentation (C4 Model, ADRs), and Disaster Recovery.
5. **Phase 5 (Days 91–100)**: System Design Interview Drills (Flash Sale, Collab Doc, Video Streaming), Technical CLR/.NET & Distributed Deep Dives, Behavioral STAR Stories, Resume/LinkedIn ATS Optimization, and Job Application Pipeline Tracking.

---

## 🚀 Running Locally

### Prerequisites
- .NET 8.0 SDK or .NET 9.0 SDK
- (Optional) PostgreSQL running locally, or runs in offline development mode using SQLite.

### Commands
```bash
# Build the solution
dotnet build LearningOS.sln

# Run all automated unit and data survival tests
dotnet run --project tests/LearningOS.Tests

# Start the web service
dotnet run --project src/LearningOS
```

Open `http://localhost:5000` or `http://localhost:8080` in your browser.

---

## ☁️ Deployment on Render

### Using render.yaml (Blueprint)
1. Push your repository to GitHub.
2. Log into [Render Dashboard](https://dashboard.render.com).
3. Click **New +** -> **Blueprint**.
4. Select your repository. Render will automatically provision:
   - `learning-os-postgres`: Managed Render PostgreSQL database.
   - `learning-os-web`: Docker web service with health check at `/health` and `DATABASE_URL` linked to the database.
5. Click **Apply**.

### Environment Variables
- `DATABASE_URL`: PostgreSQL connection string (automatically set by Render PostgreSQL).
- `ASPNETCORE_ENVIRONMENT`: `Production`
