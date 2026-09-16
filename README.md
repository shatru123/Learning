# LearningOS — 100-Day Career & AI Engineering Mastery

> **"The roadmap can change. The schedule can change. A day can be missed. But my learning history should never disappear."**

**Created by**: **Shatrughna Ambhore** • ✉️ [ambhoreshatrughna@gmail.com](mailto:ambhoreshatrughna@gmail.com) • 📞 [+91 9604466334](tel:+919604466334)

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

### 3. Interactive 3D Cosmic Roadmap (WebGL / Three.js)
- **New Dedicated Tab**: Explore the 100-day journey in an immersive, interactive 3D WebGL space.
- **Helical Momentum Highway**: 100 color-coded nodes plotted along a 3D spiral curve with animated pulsing energy streams.
- **5 Demarcated Phase Sectors**: Visual color zones, floating canvas milestone badges, and phase boundary markers.
- **Interactive Controls & Inspector**:
  - Orbit, pan, and smooth zoom controls with auto-rotation toggle.
  - Hover tooltips and click raycasting.
  - Floating glassmorphism inspector drawer: toggle task completion, inspect day objectives, or jump directly into the full day plan.
  - Camera presets: Full Galaxy, Today's Focus, and 1-click jumps to Phases 1–5.
  - Filter 3D nodes by status (Completed, In Progress, Planned, Missed).

### 4. Missed Day & Smart Recovery Manager
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

### 7. Working-Hours-Aware Scheduling (Workday Protection)
- Dedicated settings panel for professional software engineers:
  - **Workday Window**: e.g., 09:00 to 18:00 (editable).
  - **Study Window**: e.g., 20:00 to 22:30.
  - **Daily Available Minutes**: Weekday (120 mins) vs. Weekend (240 mins).
  - **Protect Working Hours**: Recovery tasks and study sessions are strictly scheduled outside your work hours.

### 8. Learning Analytics & Velocity Dashboard
- Real-time tracking of your 100-day journey:
  - **Active Consistency Rate**: Completed + rest days vs total elapsed.
  - **Estimated Total Study Hours**: Calculated dynamically from completed tasks.
  - **5-Phase Progress Breakdown**: Visual progress bars across all 5 curriculum phases.
  - **Workload by Domain**: Distribution across C# Internals, Microservices, AI/LLM, System Design, DSA, and DevOps.

### 9. Backup & Restore Center
- **Export Everything**: Download full JSON backup (`LearningOS_Backup_YYYY-MM-DD.json`) and CSV summaries.
- **Restore Backup**: Validates schema and entity counts before asking for explicit confirmation. Never silently overwrites data.

---

## 📚 100-Day Curriculum Highlights

The system comes pre-seeded with an authoritative 100-day curriculum across 5 phases:

1. **Phase 1 (Days 1–25)**: Advanced C# Internals (Memory, Span/Memory, GC Tuning, AsyncStateMachine), ASP.NET Core Internals (Middleware, DI Lifetimes, Minimal APIs), SQL Internals (B-Tree, Execution Plans, MVCC, Isolation Levels), Dapper & EF Core Optimization, Redis Caching & Distributed Locks, and Fundamental DSA.
2. **Phase 2 (Days 26–50)**: Microservices Architecture, API Gateways (YARP), Resilient HTTP (Refit & Polly), Event-Driven Messaging (RabbitMQ, Dead-Letter Queues, MassTransit), Outbox & Saga Patterns, Distributed Idempotency, OpenTelemetry, Prometheus, Grafana, Jaeger, Docker, Kubernetes, CI/CD, Terraform, and Graph/DP DSA.
3. **Phase 3 (Days 51–75)**: AI/LLM Engineering (Transformers, Tokenization, OpenAI, Claude, Gemini, Ollama), Advanced Prompt Engineering, Embeddings, PostgreSQL pgvector, Qdrant/Pinecone, Hybrid Search (BM25 + Dense RRF), Reranking, Function/Tool Calling, AI Agents (ReAct, Semantic Kernel), AI Security, Observability (LangSmith, OpenLLMetry), Semantic Caching, and Cost/Latency Optimization.
4. **Phase 4 (Days 76–90)**: Production Enterprise AI Portfolio Project, Multi-Tenancy, Automated CI/CD Pipelines, Helm, Load Testing (k6), Architecture Documentation (C4 Model, ADRs), and Disaster Recovery.
5. **Phase 5 (Days 91–100)**: System Design Interview Drills (Flash Sale, Collab Doc, Video Streaming), Technical CLR/.NET & Distributed Deep Dives, Behavioral STAR Stories, Resume/LinkedIn ATS Optimization, and Job Application Pipeline Tracking.

---

## 🧪 Automated Verification Test Suite (25 Tests)

The repository includes a comprehensive 25-test verification suite covering:

- **Data Survival Tests (1–6)**: Verifies that notes, task completions, missed day records, journal entries, resources, and multi-day progress survive application restarts, container redeployments, and host restarts.
- **Streak Calculation Tests (7–10)**: Verifies completed days increment streaks, planned rest days protect streaks, recorded leave days protect streaks, and unexplained missed days break streaks.
- **Recovery Logic Tests (11–13)**: Verifies workload-capped smart recovery, multi-day compression, and roadmap extension.
- **Backup & Restore Tests (14–16)**: Verifies JSON backup export, validated restore with explicit confirmation, and safety failure without confirmation.
- **Controller Integration Tests (17–21)**: Verifies `/health` endpoint (100 days healthy), dashboard metrics, missed day marking, task history audit trail, and daily review submission.
- **Curriculum Integrity Tests (22–25)**: Verifies exact 100 learning days without gaps or duplicates, rest and leave days stored separately from learning days, working-hours schedule persistence in user settings, and coverage of all 5 curriculum phases.

Run the test suite:
```bash
dotnet run --project tests/LearningOS.Tests
```

---

## 🚀 Running Locally

### Prerequisites
- .NET 8.0 SDK or .NET 9.0 SDK
- (Optional) PostgreSQL running locally, or runs in offline development mode using SQLite.

### Commands
```bash
# Build the application
dotnet build src/LearningOS/LearningOS.csproj

# Run all 25 automated tests
dotnet run --project tests/LearningOS.Tests

# Start the web service
dotnet run --project src/LearningOS
```

Open `http://localhost:5000` in your browser.

---

## ☁️ Deployment on Render

### Using render.yaml (Blueprint)
1. Push your repository to GitHub.
2. Log into [Render Dashboard](https://dashboard.render.com).
3. Click **New +** -> **Blueprint**.
4. Select your repository. Render will automatically provision:
   - `learning-os-postgres`: Managed Render PostgreSQL database (`starter` plan).
   - `learning-os-web`: Docker web service (`starter` plan) with health check at `/health` and `DATABASE_URL` linked to the PostgreSQL database.
5. Click **Apply**.

### Dynamic Port & Health Checks
- Render binds web services dynamically using the `$PORT` environment variable. `Program.cs` automatically detects `$PORT` and binds Kestrel to `http://0.0.0.0:$PORT`.
- Health check path is set to `/health` with a 200 OK response required before routing traffic.
- If PostgreSQL is unreachable on startup in Production, the container fails fast and reports unhealthy to Render.

---

## 👨‍💻 Creator & Contact

<p align="left">
  <img src="src/LearningOS/wwwroot/images/shatrughna.jpg" width="120" height="120" style="border-radius: 50%; object-fit: cover;" alt="Shatrughna Ambhore" />
</p>

- **Created by**: **Shatrughna Ambhore**
- **Email**: [ambhoreshatrughna@gmail.com](mailto:ambhoreshatrughna@gmail.com)
- **Phone**: [+91 9604466334](tel:+919604466334)
- **GitHub Repository**: [shatru123/Learning](https://github.com/shatru123/Learning)
- **License**: MIT


