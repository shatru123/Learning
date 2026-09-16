using LearningOS.Models;
using Microsoft.EntityFrameworkCore;

namespace LearningOS.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(LearningDbContext db, ILogger logger)
    {
        // For PostgreSQL in production, apply migrations; for SQLite/dev/tests, ensure created
        bool isPostgres = db.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;
        if (isPostgres)
        {
            logger.LogInformation("Checking and applying pending EF Core migrations on PostgreSQL...");
            var pending = await db.Database.GetPendingMigrationsAsync();
            if (pending.Any())
            {
                logger.LogInformation("Applying pending migration(s): {Migrations}", string.Join(", ", pending));
                await db.Database.MigrateAsync();
                logger.LogInformation("EF Core migrations applied successfully to PostgreSQL.");
            }
            else
            {
                logger.LogInformation("PostgreSQL schema is up to date. No pending migrations.");
            }
        }
        else
        {
            await db.Database.EnsureCreatedAsync();
        }

        // Check if curriculum already exists
        if (await db.DayPlans.AnyAsync())
        {
            logger.LogInformation("Database already seeded with learning plan.");
            return;
        }

        logger.LogInformation("Seeding authoritative 100-Day Curriculum into PostgreSQL...");

        // 1. User Settings
        var settings = new UserSettings
        {
            MaxExtraRecoveryMinutesPerDay = 60,
            DailyTargetStudyMinutes = 120,
            RoadmapStartDate = new DateOnly(2026, 9, 21),
            RoadmapEndDate = new DateOnly(2026, 12, 31),
            UpdatedAt = DateTime.UtcNow
        };
        db.UserSettings.Add(settings);

        // 2. Learning Plan & 5 Phases
        var plan = new LearningPlan
        {
            Title = "100-Day Full-Stack & AI Systems Mastery",
            TotalLearningDays = 100,
            StartDate = new DateOnly(2026, 9, 21),
            EndDate = new DateOnly(2026, 12, 31),
            CreatedAt = DateTime.UtcNow
        };
        db.LearningPlans.Add(plan);
        await db.SaveChangesAsync();

        var phases = new List<Phase>
        {
            new() { LearningPlanId = plan.Id, PhaseNumber = 1, Title = "Core .NET/C#, SQL & High-Performance Data Access", Description = "Deep C# internals, memory management, ASP.NET Core internals, SQL execution plans, Dapper & EF Core, and fundamental DSA.", StartDay = 1, EndDay = 25 },
            new() { LearningPlanId = plan.Id, PhaseNumber = 2, Title = "Distributed Systems, Microservices, Cloud & Messaging", Description = "Microservices architecture, RabbitMQ, Refit, YARP reverse proxy, Redis distributed caching, OpenTelemetry & System Design.", StartDay = 26, EndDay = 50 },
            new() { LearningPlanId = plan.Id, PhaseNumber = 3, Title = "AI Engineering, LLMs, Vector DBs & Production Agents", Description = "OpenAI, Claude, Gemini, Ollama, Prompt Engineering, Embeddings, pgvector/Qdrant, RAG architectures, Tool Calling & Multi-Agent systems.", StartDay = 51, EndDay = 75 },
            new() { LearningPlanId = plan.Id, PhaseNumber = 4, Title = "DevOps, Cloud, Observability & Production Architecture", Description = "Docker multi-stage, Kubernetes, Prometheus, Grafana, Jaeger, CI/CD pipelines, Terraform IaC, and end-to-end cloud deployments.", StartDay = 76, EndDay = 90 },
            new() { LearningPlanId = plan.Id, PhaseNumber = 5, Title = "Interview Mastery, Resume/LinkedIn & Job Applications", Description = "System design interview drills, technical deep dives, behavioral STAR stories, resume ATS optimization, and application pipeline.", StartDay = 91, EndDay = 100 }
        };
        db.Phases.AddRange(phases);
        await db.SaveChangesAsync();

        // 3. Seed 100 Exactly Numbered Learning Days
        var dayPlans = Generate100DayPlans(phases, settings.RoadmapStartDate);
        db.DayPlans.AddRange(dayPlans);

        // 4. Seed Trackers: DSA, System Design, Interview Questions, Resources, Goals
        SeedTrackers(db);

        // 5. Initial Audit Log
        db.ActivityAuditLogs.Add(new ActivityAuditLog
        {
            ActionType = "PlanInitialized",
            EntityName = "LearningPlan",
            EntityId = plan.Id.ToString(),
            Description = "100-Day Learning Plan successfully initialized with 100 core curriculum days.",
            Timestamp = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        logger.LogInformation("Successfully initialized LearningOS database with 100 learning days!");
    }

    private static List<DayPlan> Generate100DayPlans(List<Phase> phases, DateOnly startDate)
    {
        var days = new List<DayPlan>();
        var curDate = startDate;

        for (int dayNum = 1; dayNum <= 100; dayNum++)
        {
            var phase = phases.First(p => dayNum >= p.StartDay && dayNum <= p.EndDay);
            int weekNum = ((dayNum - 1) / 7) + 1;

            var (title, theme, tasks) = GetCurriculumForDay(dayNum);

            var dayPlan = new DayPlan
            {
                DayNumber = dayNum,
                IsLearningDay = true,
                CalendarDate = curDate,
                Status = DayStatus.Planned,
                Title = title,
                Theme = theme,
                PhaseId = phase.Id,
                WeekNumber = weekNum,
                CreatedAt = DateTime.UtcNow,
                Tasks = tasks.Select(t => new LearningTask
                {
                    Title = t.Title,
                    Description = t.Description,
                    Category = t.Category,
                    EstimatedMinutes = t.EstimatedMinutes,
                    Priority = t.Priority,
                    Status = "Pending",
                    OriginalDayNumber = dayNum,
                    CurrentDayNumber = dayNum,
                    CreatedAt = DateTime.UtcNow
                }).ToList()
            };

            days.Add(dayPlan);
            curDate = curDate.AddDays(1);
        }

        return days;
    }

    private static (string Title, string Theme, List<(string Title, string Description, string Category, int EstimatedMinutes, string Priority)> Tasks) GetCurriculumForDay(int day)
    {
        return day switch
        {
            // Phase 1: Days 1-25
            1 => ("C# Memory Management & Span<T>", "Advanced .NET & DSA", new() {
                ("Study C# Value Types vs Reference Types, Stack vs Heap", "Deep dive into object headers, sync blocks, and GC generations (Gen 0, 1, 2, LOH, POH)", ".NET/C#", 50, "High"),
                ("Implement Span<T> and ReadOnlySpan<T> parsing", "Create zero-allocation string parsing benchmarks using BenchmarkDotNet", ".NET/C#", 45, "High"),
                ("DSA: Two Sum & Contains Duplicate", "Solve on LeetCode; analyze O(N) time and space complexity with HashSets", "DSA", 40, "Medium")
            }),
            2 => ("Ref Structs, Memory<T> & In-Parameters", "Advanced .NET & DSA", new() {
                ("Master ref struct limitations & Memory<T> async usage", "Understand why ref structs cannot escape to heap or be captured in async lambdas", ".NET/C#", 45, "Medium"),
                ("DSA: Valid Anagram & Group Anagrams", "Solve using frequency arrays vs sorted string hashing", "DSA", 45, "High")
            }),
            3 => ("Async/Await Internals & ValueTask", "Advanced .NET & DSA", new() {
                ("Dissect compiler-generated AsyncStateMachine", "Understand SynchronizationContext, TaskCompletionSource, and ValueTask allocation savings", ".NET/C#", 60, "Critical"),
                ("DSA: Top K Frequent Elements", "Solve with min-heap and bucket sort in O(N)", "DSA", 45, "High")
            }),
            4 => ("Garbage Collector Tuning & Finalization", "Advanced .NET & DSA", new() {
                ("Analyze Server GC vs Workstation GC, Background GC, GCSettings", "Learn how to diagnose GC pauses and memory leaks using dotnet-dump and dotnet-gcdump", ".NET/C#", 50, "High"),
                ("DSA: Product of Array Except Self", "Solve with prefix and suffix pass in O(N) without division", "DSA", 45, "Medium")
            }),
            5 => ("ASP.NET Core Middleware & Kestrel Architecture", "ASP.NET Core & DSA", new() {
                ("Build custom async middleware with RequestDelegate & factory activation", "Examine HttpContext lifecycle and streaming response writing", "ASP.NET Core", 50, "High"),
                ("DSA: Valid Palindrome & Two Sum II (Input Array Is Sorted)", "Two pointers approach", "DSA", 40, "Medium")
            }),
            6 => ("ASP.NET Core Dependency Injection Lifetimes", "ASP.NET Core & DSA", new() {
                ("Deep dive into Transient, Scoped, Singleton & captive dependencies", "Test IServiceScopeFactory in background tasks; inspect IServiceProviderEngine", "ASP.NET Core", 50, "Critical"),
                ("DSA: 3Sum & Container With Most Water", "Sort + two pointers; handle duplicate avoidance", "DSA", 50, "High")
            }),
            7 => ("ASP.NET Core Minimal APIs & Filters", "ASP.NET Core & DSA", new() {
                ("Endpoint routing, IEndpointFilter, TypedResults, and validation", "Compare Minimal APIs vs Controller overhead and source generator speed", "ASP.NET Core", 45, "Medium"),
                ("DSA: Best Time to Buy and Sell Stock", "Sliding window single-pass min-price tracking", "DSA", 40, "Medium")
            }),
            8 => ("SQL Internals: B-Tree Indexing & Clustered Indexes", "SQL & Relational DB", new() {
                ("Inspect PostgreSQL / SQL Server B-Tree page structure", "Analyze fill factor, index leaf pages, clustered vs non-clustered, index fragmentation", "SQL", 55, "Critical"),
                ("DSA: Longest Substring Without Repeating Characters", "Dynamic sliding window with hash map last-seen index", "DSA", 45, "High")
            }),
            9 => ("SQL Execution Plans & Index Tuning", "SQL & Relational DB", new() {
                ("Analyze EXPLAIN (ANALYZE, BUFFERS) in PostgreSQL", "Identify Seq Scans, Index Scans, Bitmap Index Scans, Nested Loops vs Hash Joins", "SQL", 60, "Critical"),
                ("DSA: Longest Repeating Character Replacement", "Sliding window with max-frequency tracking", "DSA", 45, "High")
            }),
            10 => ("SQL Transactions, ACID & Isolation Levels", "SQL & Relational DB", new() {
                ("Simulate Dirty Read, Non-Repeatable Read, Phantom Read, Serialization Anomaly", "Compare Read Committed, Repeatable Read, and Serializable MVCC snapshots in PostgreSQL", "SQL", 60, "Critical"),
                ("DSA: Valid Parentheses & Min Stack", "Stack data structure implementations", "DSA", 40, "Medium")
            }),
            11 => ("EF Core Change Tracking & Optimization", "Dapper/EF Core & DSA", new() {
                ("Snapshot vs Notification tracking, AsNoTrackingIdentityResolution", "Benchmark AsNoTracking queries vs tracked entity graphs", "Dapper/EF Core", 50, "High"),
                ("DSA: Evaluate Reverse Polish Notation & Daily Temperatures", "Monotonic stack pattern", "DSA", 45, "High")
            }),
            12 => ("EF Core Split Queries & Interceptors", "Dapper/EF Core & DSA", new() {
                ("Cartesian explosion prevention using AsSplitQuery()", "Write IDbCommandInterceptor for SQL query auditing and slow query logging", "Dapper/EF Core", 50, "High"),
                ("DSA: Binary Search & Search a 2D Matrix", "Logarithmic search boundaries", "DSA", 45, "Medium")
            }),
            13 => ("High-Performance Data Access with Dapper", "Dapper/EF Core & DSA", new() {
                ("Implement multi-mapping, buffered queries, and async streaming in Dapper", "Benchmark Dapper vs EF Core raw SQL queries with BenchmarkDotNet", "Dapper/EF Core", 55, "High"),
                ("DSA: Find Minimum in Rotated Sorted Array", "Modified binary search with pivot detection", "DSA", 45, "High")
            }),
            14 => ("EF Core Migrations & Production Strategy", "Dapper/EF Core & DevOps", new() {
                ("Idempotent SQL migration scripts (dotnet ef migrations script -i)", "Design zero-downtime database schema deployment pipelines", "Dapper/EF Core", 50, "Critical"),
                ("DSA: Search in Rotated Sorted Array", "Binary search with half-sorted condition branches", "DSA", 45, "High")
            }),
            15 => ("Channels & Concurrency in C#", ".NET/C# & Concurrency", new() {
                ("System.Threading.Channels producer-consumer pipeline", "Build high-throughput async queue with bounded capacity and backpressure", ".NET/C#", 55, "High"),
                ("DSA: Reverse Linked List & Merge Two Sorted Lists", "Pointer manipulation", "DSA", 40, "Medium")
            }),
            16 => ("Distributed Locking & SemaphoreSlim", ".NET/C# & Concurrency", new() {
                ("Thread synchronization with SemaphoreSlim, ReaderWriterLockSlim, and Interlocked", "Compare in-process synchronization with distributed locks", ".NET/C#", 50, "Medium"),
                ("DSA: Reorder List & Remove Nth Node From End of List", "Fast and slow pointer technique", "DSA", 45, "High")
            }),
            17 => ("Introduction to Redis & Cache Patterns", "Redis & Distributed Systems", new() {
                ("Cache-Aside, Write-Through, Write-Behind, Refresh-Ahead patterns", "Set up StackExchange.Redis connection multiplexer and handle timeouts", "Redis", 55, "High"),
                ("DSA: Invert Binary Tree & Maximum Depth of Binary Tree", "Tree traversal recursion", "DSA", 40, "Medium")
            }),
            18 => ("Redis Data Structures & Eviction Policies", "Redis & Distributed Systems", new() {
                ("Strings, Hashes, Sets, Sorted Sets (ZSET), Bitmaps, HyperLogLogs", "Test LRU, LFU, volatile-ttl eviction policies under memory pressure", "Redis", 50, "High"),
                ("DSA: Diameter of Binary Tree & Balanced Binary Tree", "Bottom-up tree height recursion", "DSA", 45, "Medium")
            }),
            19 => ("Redis Distributed Locking with RedLock", "Redis & Distributed Systems", new() {
                ("Implement RedLock algorithm for multi-instance distributed mutual exclusion", "Analyze lock drift, clock skew, and auto-renewal leases", "Redis", 60, "High"),
                ("DSA: Same Tree & Subtree of Another Tree", "Tree isomorphism and recursive matching", "DSA", 45, "Medium")
            }),
            20 => ("Redis Pub/Sub & Redis Streams", "Redis & Distributed Systems", new() {
                ("Consumer groups, ACK mechanisms, pending entries list (PEL) in Redis Streams", "Build persistent event ingestion consumer with Redis Streams in .NET", "Redis", 55, "High"),
                ("DSA: Lowest Common Ancestor of a BST & Binary Tree Level Order Traversal", "BFS queue traversal", "DSA", 50, "High")
            }),
            21 => ("System Design: Distributed Cache Architecture", "System Design & DSA", new() {
                ("Design high-throughput distributed caching cluster", "Consistent hashing, hot-key mitigations, cache stampede protection (mutex / probabilistic early expiry)", "System Design", 60, "Critical"),
                ("DSA: Binary Tree Right Side View & Count Good Nodes in Binary Tree", "DFS/BFS tree problem solving", "DSA", 45, "Medium")
            }),
            22 => ("System Design: Rate Limiter Design", "System Design & DSA", new() {
                ("Token Bucket, Leaky Bucket, Fixed Window, Sliding Window Log, Sliding Window Counter", "Design distributed Redis-backed sliding window rate limiter middleware in ASP.NET Core", "System Design", 60, "Critical"),
                ("DSA: Validate Binary Search Tree & Kth Smallest Element in BST", "In-order traversal properties", "DSA", 50, "High")
            }),
            23 => ("System Design: URL Shortener (TinyURL)", "System Design & DSA", new() {
                ("Base62 encoding, distributed ID generation (Snowflake), database sharding, Redis caching", "Capacity planning: 100M daily writes, 1B daily reads, latency SLAs", "System Design", 60, "Critical"),
                ("DSA: Construct Binary Tree from Preorder and Inorder Traversal", "Recursive hash map divide-and-conquer", "DSA", 50, "High")
            }),
            24 => ("Advanced SQL: CTEs, Window Functions & Partitions", "SQL & DSA", new() {
                ("ROW_NUMBER(), RANK(), DENSE_RANK(), LAG(), LEAD(), declarative table partitioning", "Write analytical queries on high-volume time-series data", "SQL", 55, "High"),
                ("DSA: Binary Tree Maximum Path Sum", "Post-order DFS max gain calculation", "DSA", 50, "Critical")
            }),
            25 => ("Phase 1 Review & Milestone Assessment", "Milestone Review", new() {
                ("Phase 1 comprehensive review: C# Internals, ASP.NET Core, SQL, Dapper/EF, Redis", "Review all 25 days, consolidate notes, and benchmark key projects", "Core", 60, "High"),
                ("DSA: Serialize and Deserialize Binary Tree (Hard)", "BFS / DFS string serialization", "DSA", 55, "Critical")
            }),

            // Phase 2: Days 26-50
            26 => ("Microservices Principles & Bounded Contexts", "Microservices & Distributed Systems", new() {
                ("Domain-Driven Design (DDD) strategic design: Subdomains, Bounded Contexts, Context Mapping", "Deconstruct monolith into services without distributed monolith pitfalls", "Microservices", 55, "High"),
                ("DSA: Number of Islands", "Graph BFS / DFS grid traversal", "DSA", 45, "High")
            }),
            27 => ("API Gateway Architecture with YARP", "Microservices & YARP", new() {
                ("Configure YARP (Yet Another Reverse Proxy) routes, clusters, and health checks", "Implement request transformation, header forwarding, and rate limiting in YARP", "YARP", 60, "Critical"),
                ("DSA: Clone Graph & Max Area of Island", "Graph copy with hash map node tracking", "DSA", 45, "High")
            }),
            28 => ("Resilient Service-to-Service HTTP with Refit & Polly", "Microservices & Refit", new() {
                ("Generate type-safe REST clients with Refit in .NET", "Configure Polly resilience pipelines: Hedging, Circuit Breaker, Timeout, Exponential Backoff", "Refit", 60, "Critical"),
                ("DSA: Pacific Atlantic Water Flow", "Multi-source DFS from ocean borders", "DSA", 50, "High")
            }),
            29 => ("RabbitMQ Core Concepts: Exchanges & Queues", "RabbitMQ & Messaging", new() {
                ("Direct, Topic, Fanout, and Headers exchanges deep dive", "Publisher confirms, consumer acknowledgments (ack/nack), and prefetch count QoS", "RabbitMQ", 60, "Critical"),
                ("DSA: Surrounded Regions & Rotting Oranges", "Multi-source BFS", "DSA", 45, "High")
            }),
            30 => ("RabbitMQ Dead-Letter Queues & MassTransit", "RabbitMQ & Messaging", new() {
                ("Configure DLX (Dead Letter Exchange) and TTL for automatic retry & poison message handling", "Integrate MassTransit in ASP.NET Core for saga coordination and outbox", "RabbitMQ", 60, "Critical"),
                ("DSA: Course Schedule & Course Schedule II", "Topological sort with Kahn's BFS algorithm", "DSA", 50, "Critical")
            }),
            31 => ("The Transactional Outbox Pattern", "Distributed Systems", new() {
                ("Prevent dual-write data inconsistencies between database and message broker", "Implement EF Core Outbox table + background publisher with Polling / CDC", "Distributed Systems", 60, "Critical"),
                ("DSA: Redundant Connection", "Union-Find (Disjoint Set Union) with path compression", "DSA", 50, "High")
            }),
            32 => ("Saga Pattern: Orchestration vs Choreography", "Distributed Systems", new() {
                ("Design distributed transactions with compensating actions", "Implement State Machine Saga using MassTransit and PostgreSQL state storage", "Distributed Systems", 60, "Critical"),
                ("DSA: Word Ladder", "Bidirectional BFS on word mutation graphs", "DSA", 55, "Critical")
            }),
            33 => ("Distributed Idempotency & De-duplication", "Distributed Systems", new() {
                ("Design idempotency keys for distributed payment and order APIs", "Store unique idempotency records in PostgreSQL with conditional unique constraints", "Distributed Systems", 50, "High"),
                ("DSA: Kth Largest Element in an Array", "Quickselect algorithm and Min-Heap comparison", "DSA", 45, "Medium")
            }),
            34 => ("OpenTelemetry in .NET: Distributed Tracing", "OpenTelemetry & Observability", new() {
                ("Configure OpenTelemetry .NET SDK with ActivitySource and TracerProvider", "Trace requests across YARP, ASP.NET Core, EF Core, and RabbitMQ", "OpenTelemetry", 60, "Critical"),
                ("DSA: Task Scheduler", "Greedy max-frequency math and Priority Queue", "DSA", 45, "High")
            }),
            35 => ("Observability Stack: Prometheus, Grafana & Jaeger", "Observability", new() {
                ("Export metrics with OpenTelemetry Prometheus exporter and traces to Jaeger", "Build Grafana dashboard for HTTP request rates, P95/P99 latency, and error rates", "Prometheus/Grafana/Jaeger", 60, "Critical"),
                ("DSA: Design Twitter", "Hash map user graph + min-heap feed aggregator", "DSA", 50, "High")
            }),
            36 => ("System Design: Notification Service", "System Design & DSA", new() {
                ("Design high-scale multi-channel notification engine (SMS, Email, Push)", "Priority queues, user preference filters, rate-limiting, and delivery receipt handling", "System Design", 60, "Critical"),
                ("DSA: Subsets & Subsets II", "Backtracking powerset generation", "DSA", 45, "Medium")
            }),
            37 => ("System Design: High-Throughput Chat System", "System Design & DSA", new() {
                ("WebSocket connections, presence service, Redis Pub/Sub, Cassandra/PostgreSQL message store", "Message ordering, push notifications for offline users, end-to-end encryption", "System Design", 60, "Critical"),
                ("DSA: Combination Sum & Combination Sum II", "Backtracking with target subtraction and duplicate skips", "DSA", 45, "High")
            }),
            38 => ("System Design: Video Streaming System (YouTube/Netflix)", "System Design & DSA", new() {
                ("Chunking, adaptive bitrate streaming (HLS/DASH), CDN edge caching, transcoding pipeline", "Blob storage integration, metadata database sharding, upload resumability", "System Design", 60, "Critical"),
                ("DSA: Permutations & Permutations II", "Backtracking state tracking", "DSA", 45, "Medium")
            }),
            39 => ("CAP Theorem & PACELC in Practice", "Distributed Systems", new() {
                ("Deep dive into Consistency vs Availability vs Partition Tolerance trade-offs", "Analyze DynamoDB, Cassandra, PostgreSQL replication (sync vs async streaming)", "Distributed Systems", 50, "High"),
                ("DSA: Word Search & Letter Combinations of a Phone Number", "Matrix DFS backtracking", "DSA", 45, "High")
            }),
            40 => ("Event Sourcing & CQRS Foundations", "Distributed Systems", new() {
                ("Separating Command and Query models with read-optimized projections", "Store immutable domain events in PostgreSQL event store with optimistic concurrency", "Distributed Systems", 60, "High"),
                ("DSA: Palindrome Partitioning", "Backtracking with DP palindrome check", "DSA", 50, "High")
            }),
            41 => ("Docker Deep Dive for .NET Services", "Docker & DevOps", new() {
                ("Write production multi-stage Dockerfiles with non-root security and layer caching", "Inspect container cgroup memory limits and .NET runtime GC auto-tuning", "Docker", 55, "Critical"),
                ("DSA: Climbing Stairs & Min Cost Climbing Stairs", "1D Dynamic Programming memoization vs tabulation", "DSA", 40, "Medium")
            }),
            42 => ("Docker Compose for Microservice Stacks", "Docker & DevOps", new() {
                ("Compose setup: ASP.NET Core API + PostgreSQL + Redis + RabbitMQ + Jaeger", "Network bridging, healthcheck dependencies, and volume persistence", "Docker", 55, "High"),
                ("DSA: House Robber & House Robber II", "1D DP with circular array constraint", "DSA", 45, "High")
            }),
            43 => ("Kubernetes Architecture: Pods, Deployments & Services", "Kubernetes", new() {
                ("Cluster control plane: kube-apiserver, etcd, scheduler, kubelet, kube-proxy", "Write Deployment and ClusterIP/NodePort/LoadBalancer Service manifests", "Kubernetes", 60, "Critical"),
                ("DSA: Longest Palindromic Substring", "Expand around centers vs DP table", "DSA", 45, "High")
            }),
            44 => ("Kubernetes ConfigMaps, Secrets & Ingress", "Kubernetes", new() {
                ("Decouple environment variables with ConfigMaps and base64/sealed secrets", "Configure NGINX / Traefik Ingress controller with TLS termination and path routing", "Kubernetes", 60, "Critical"),
                ("DSA: Palindromic Substrings & Decode Ways", "1D DP decoding string counting", "DSA", 50, "High")
            }),
            45 => ("Kubernetes Probes & Auto-Scaling (HPA)", "Kubernetes", new() {
                ("Configure Liveness, Readiness, and Startup probes in ASP.NET Core", "Set up Horizontal Pod Autoscaler (HPA) based on CPU/Memory and custom metrics", "Kubernetes", 55, "High"),
                ("DSA: Coin Change & Maximum Product Subarray", "Classic DP optimization", "DSA", 50, "Critical")
            }),
            46 => ("CI/CD with GitHub Actions for .NET", "CI/CD & DevOps", new() {
                ("Build, test, lint, and publish Docker image to GitHub Container Registry (GHCR)", "Implement automated vulnerability scanning with Trivy and automated releases", "CI/CD", 60, "Critical"),
                ("DSA: Word Break", "1D DP with dictionary hash set lookup", "DSA", 45, "High")
            }),
            47 => ("Infrastructure as Code with Terraform", "Terraform & Cloud", new() {
                ("Terraform syntax: Providers, Resources, Variables, Outputs, Remote State (S3/Azure Blob)", "Provision PostgreSQL database instance and container app environment with Terraform", "Terraform", 60, "Critical"),
                ("DSA: Longest Increasing Subsequence (LIS)", "O(N^2) DP vs O(N log N) patience sorting with binary search", "DSA", 50, "Critical")
            }),
            48 => ("Cloud Architecture Patterns (AWS/Azure)", "Cloud Architecture", new() {
                ("Multi-region failover, Managed DB replication, Blob storage, Serverless compute", "Cost optimization: reserved instances, spot instances, egress reduction", "Cloud", 55, "High"),
                ("DSA: Partition Equal Subset Sum (0/1 Knapsack)", "DP boolean subset sum table", "DSA", 50, "High")
            }),
            49 => ("System Design: Distributed Job Scheduler", "System Design & DSA", new() {
                ("Design quartz/hangfire-style distributed cron and delayed task runner", "Worker node heartbeats, leader election with Raft/Zookeeper, at-least-once execution", "System Design", 60, "Critical"),
                ("DSA: Trapping Rain Water", "Two pointers vs monotonic stack (Hard)", "DSA", 60, "Critical")
            }),
            50 => ("Phase 2 Review & Distributed Architecture Benchmark", "Milestone Review", new() {
                ("Phase 2 comprehensive review: Microservices, RabbitMQ, Redis, Kubernetes, Terraform", "Review distributed patterns, verify telemetry dashboards, and test disaster recovery", "Core", 60, "High"),
                ("DSA: Merge K Sorted Lists (Hard)", "Min-heap priority queue k-way merge", "DSA", 55, "Critical")
            }),

            // Phase 3: Days 51-75
            51 => ("AI & LLM Architecture Foundations", "AI/LLM & DSA", new() {
                ("Transformer architecture: Self-Attention, Multi-Head Attention, Feed-Forward, Positional Encoding", "Tokenization (BPE, WordPiece), Context windows, temperature, top_p, frequency penalty", "AI/LLM", 60, "Critical"),
                ("DSA: Unique Paths & Longest Common Subsequence", "2D Dynamic Programming table", "DSA", 50, "High")
            }),
            52 => ("OpenAI, Claude & Gemini API Integrations", "AI/LLM & .NET", new() {
                ("Integrate OpenAI SDK / Microsoft.SemanticKernel in .NET", "Compare OpenAI GPT-4o, Anthropic Claude 3.5 Sonnet, and Google Gemini 1.5 Pro capabilities and pricing", "OpenAI", 60, "Critical"),
                ("DSA: Best Time to Buy and Sell Stock with Cooldown", "State machine dynamic programming", "DSA", 50, "High")
            }),
            53 => ("Local LLM Deployment with Ollama", "AI/LLM & Ollama", new() {
                ("Run Llama 3, Mistral, and DeepSeek locally using Ollama", "Build a local .NET client connecting to Ollama's HTTP API for zero-cost private inference", "Ollama", 55, "High"),
                ("DSA: Coin Change II & Target Sum", "Unbounded vs 0/1 knapsack variants", "DSA", 50, "High")
            }),
            54 => ("Advanced Prompt Engineering & Structured Outputs", "Prompt Engineering", new() {
                ("Few-shot examples, Chain-of-Thought (CoT), Step-Back prompting, ReAct prompt format", "Enforce strict JSON Schema structured outputs with type guarantees in C#", "Prompt Engineering", 55, "Critical"),
                ("DSA: Edit Distance (Hard)", "2D DP matrix minimum edit operations", "DSA", 55, "Critical")
            }),
            55 => ("Text Embeddings & Vector Representations", "Embeddings & Vectors", new() {
                ("Understand vector space, semantic similarity, cosine distance, dot product, Euclidean distance", "Generate embeddings using OpenAI text-embedding-3-small and local all-MiniLM models", "Embeddings", 55, "Critical"),
                ("DSA: Maximum Subarray & Jump Game", "Greedy algorithms (Kadane's)", "DSA", 45, "Medium")
            }),
            56 => ("PostgreSQL pgvector: Vectors in Relational DB", "Vector Databases", new() {
                ("Install and configure pgvector extension in PostgreSQL", "Create vector columns, build IVFFlat and HNSW indexes, write semantic search queries with EF Core raw SQL", "Vector Databases", 60, "Critical"),
                ("DSA: Jump Game II & Gas Station", "Greedy reachability optimization", "DSA", 50, "High")
            }),
            57 => ("Specialized Vector Databases: Qdrant & Pinecone", "Vector Databases", new() {
                ("Compare managed vector DBs: Qdrant (Rust, self-hostable), Pinecone, Milvus", "Implement collection creation, payload metadata filtering, and batch vector upserts", "Vector Databases", 55, "High"),
                ("DSA: Hand of Straights & Merge Triplets to Form Target", "Greedy card groups and target verification", "DSA", 45, "Medium")
            }),
            58 => ("RAG Part 1: Document Ingestion & Chunking", "RAG & AI", new() {
                ("Fixed-size chunking vs Recursive character chunking vs Semantic markdown chunking", "Handle chunk overlap, parent document references, and metadata extraction in C#", "RAG", 60, "Critical"),
                ("DSA: Partition Labels & Valid Parenthesis String", "Greedy intervals and wildcards", "DSA", 45, "High")
            }),
            59 => ("RAG Part 2: Hybrid Search & BM25 Integration", "RAG & AI", new() {
                ("Combine dense semantic vector search with sparse keyword search (BM25 / full-text)", "Implement Reciprocal Rank Fusion (RRF) to merge vector and keyword scores in PostgreSQL", "RAG", 60, "Critical"),
                ("DSA: Insert Interval & Merge Intervals", "Interval overlapping and sorting", "DSA", 45, "High")
            }),
            60 => ("RAG Part 3: Cross-Encoder Reranking & Context Compression", "RAG & AI", new() {
                ("Integrate Cohere Rerank / local cross-encoder models to reorder top retrieved chunks", "Context window compression: trim irrelevant tokens before passing to LLM prompt", "RAG", 55, "High"),
                ("DSA: Non-overlapping Intervals & Minimum Interval to Include Each Query", "Interval scheduling greedy sweeps", "DSA", 50, "High")
            }),
            61 => ("Function / Tool Calling in .NET", "Function/Tool Calling", new() {
                ("Define C# methods as tools via JSON Schema parameter definitions", "Build execution loop: LLM decides tool call -> execute C# method -> return tool output -> final answer", "Function/Tool Calling", 60, "Critical"),
                ("DSA: Meeting Rooms & Meeting Rooms II", "Min-heap / interval sweeps for conference room counts", "DSA", 45, "High")
            }),
            62 => ("Multi-Turn Tool Execution & Error Recovery", "Function/Tool Calling", new() {
                ("Handle nested tool calls, invalid arguments, timeout recovery, and retry prompts", "Build resilient tool registry in ASP.NET Core with permission gates and audit logging", "Function/Tool Calling", 55, "High"),
                ("DSA: Single Number & Number of 1 Bits", "Bit manipulation XOR and Brian Kernighan's algorithm", "DSA", 40, "Medium")
            }),
            63 => ("AI Agents: The ReAct Pattern & State Machines", "AI Agents", new() {
                ("Dissect Thought-Action-Observation loops (Reasoning + Acting)", "Implement autonomous agent state machine with loop termination guards and step budgets", "AI Agents", 60, "Critical"),
                ("DSA: Counting Bits & Reverse Bits", "Bit manipulation DP and bit shifting", "DSA", 40, "Medium")
            }),
            64 => ("Microsoft Semantic Kernel & LangChain", "AI Agents", new() {
                ("Configure Semantic Kernel with Plugins, Memory stores, and Planners in .NET", "Compare orchestration engines: Semantic Kernel vs AutoGen vs LangGraph", "AI Agents", 60, "High"),
                ("DSA: Missing Number & Sum of Two Integers", "Bitwise math without plus operator", "DSA", 45, "Medium")
            }),
            65 => ("Multi-Agent Systems & Specialist Teams", "AI Agents", new() {
                ("Architect multi-agent collaboration: Orchestrator agent, Researcher agent, Coder agent, Critic agent", "Implement inter-agent message passing and consensus resolution protocols", "AI Agents", 60, "Critical"),
                ("DSA: Reverse Integer & Rotate Image", "Math and 2D matrix transformation", "DSA", 45, "Medium")
            }),
            66 => ("AI Security: Prompt Injection & Jailbreak Defense", "AI Security", new() {
                ("Direct vs Indirect prompt injection attacks in RAG and tool outputs", "Implement input sanitation, dual LLM guardrails (evaluator model), and Canary tokens", "AI Security", 55, "Critical"),
                ("DSA: Spiral Matrix & Set Matrix Zeroes", "Matrix traversal and in-place marker optimization", "DSA", 45, "High")
            }),
            67 => ("AI Security: Data Leakage Prevention & Guardrails", "AI Security", new() {
                ("PII detection and masking (emails, credit cards, API keys) before LLM egress", "NeMo Guardrails / Llama Guard integration for topical moderation and safety filters", "AI Security", 50, "High"),
                ("DSA: Happy Number & Plus One", "Cycle detection and big integer simulation", "DSA", 40, "Medium")
            }),
            68 => ("AI Observability: LangSmith & OpenLLMetry", "AI Observability", new() {
                ("Instrument LLM calls with OpenLLMetry / OpenTelemetry GenAI semantic conventions", "Track prompt tokens, completion tokens, costs per user, latency waterfalls, and trace IDs", "AI Observability", 55, "Critical"),
                ("DSA: Pow(x, n) & Multiply Strings", "Binary exponentiation", "DSA", 45, "High")
            }),
            69 => ("Evaluation Frameworks for RAG & Agents", "AI Observability", new() {
                ("RAG Triad evaluation: Faithfulness, Answer Relevance, Context Precision (Ragas framework)", "Automate synthetic test dataset generation and regression testing for prompts", "AI Observability", 55, "High"),
                ("DSA: Subsets of size K & Permutations with constraints", "Backtracking revision", "DSA", 45, "Medium")
            }),
            70 => ("AI Cost & Latency: Semantic Caching", "AI Cost/Latency", new() {
                ("Implement semantic caching using Redis / pgvector: return cached response if cosine similarity > 0.95", "Benchmark cache hits: save 90% latency and API costs for repetitive queries", "AI Cost/Latency Optimization", 55, "Critical"),
                ("DSA: LRU Cache (Hard)", "Doubly linked list + Hash map O(1) get and put", "DSA", 55, "Critical")
            }),
            71 => ("Streaming Responses & Model Routing", "AI Cost/Latency", new() {
                ("Implement Server-Sent Events (SSE) streaming in ASP.NET Core from LLM to frontend", "Dynamic Model Router: send simple tasks to cheap models (GPT-4o-mini/Gemini Flash) and complex reasoning to Claude Sonnet", "AI Cost/Latency Optimization", 60, "Critical"),
                ("DSA: LFU Cache (Hard)", "Frequency buckets + linked lists O(1)", "DSA", 60, "Critical")
            }),
            72 => ("System Design: Enterprise AI RAG Platform", "System Design & AI", new() {
                ("End-to-end architecture: Ingestion pipeline, pgvector, hybrid search, caching, LLM gateway, RBAC", "Multi-tenant tenant isolation, document level permissions, and audit logging", "System Design", 65, "Critical"),
                ("DSA: Median of Two Sorted Arrays (Hard)", "Binary search on partition points O(log(min(N,M)))", "DSA", 60, "Critical")
            }),
            73 => ("System Design: Autonomous Coding Agent Architecture", "System Design & AI", new() {
                ("Sandbox code execution environment, tool sandboxing, AST analysis, git worktree isolation", "Context compaction strategies for long-running multi-turn agent conversations", "System Design", 65, "Critical"),
                ("DSA: Sliding Window Maximum (Hard)", "Monotonic decreasing deque O(N)", "DSA", 55, "Critical")
            }),
            74 => ("Building a Production Full-Stack AI Feature", "AI/LLM & .NET", new() {
                ("Connect ASP.NET Core backend to PostgreSQL pgvector and frontend chat with citations", "Verify end-to-end streaming, cancellation tokens, and tool call confirmations", "AI/LLM", 60, "High"),
                ("DSA: Minimum Window Substring (Hard)", "Sliding window with character frequency map", "DSA", 60, "Critical")
            }),
            75 => ("Phase 3 Review: Modern AI Systems Mastery", "Milestone Review", new() {
                ("Review RAG, Vector DBs, Tool Calling, Agent loops, Security, Observability, and Cost tuning", "Consolidate AI cheat-sheet, benchmark semantic cache, and prepare portfolio documentation", "Core", 60, "High"),
                ("DSA: Largest Rectangle in Histogram (Hard)", "Monotonic stack boundary calculation", "DSA", 60, "Critical")
            }),

            // Phase 4: Days 76-90
            76 => ("Production Portfolio Project: Architecture & Setup", "Portfolio Project", new() {
                ("Architect end-to-end Enterprise AI-Powered Platform with .NET 8, PostgreSQL, Redis, and React/Tailwind", "Initialize git monorepo, define bounded contexts, and setup clean architecture folders", "PortfolioProject", 60, "Critical"),
                ("DSA: Graph Valid Tree & Alien Dictionary", "Topological sort on character graph", "DSA", 50, "High")
            }),
            77 => ("Portfolio Project: Identity & Multi-Tenancy", "Portfolio Project", new() {
                ("Implement JWT authentication, refresh tokens, role-based access control (RBAC), and tenant schema filters", "Add rate-limiting per tenant and API key authorization middleware", "PortfolioProject", 60, "High"),
                ("DSA: Network Delay Time & Cheapest Flights Within K Stops", "Dijkstra and Bellman-Ford", "DSA", 50, "High")
            }),
            78 => ("Portfolio Project: High-Performance Ingestion Pipeline", "Portfolio Project", new() {
                ("Build async background worker using Channels and MassTransit for document parsing", "Batch embedding generation, pgvector storage, and full-text search indexing", "PortfolioProject", 60, "High"),
                ("DSA: Swim in Rising Water & Reconstruct Itinerary", "Dijkstra grid / Eulerian path", "DSA", 50, "High")
            }),
            79 => ("Portfolio Project: Intelligent RAG & Semantic Cache", "Portfolio Project", new() {
                ("Implement hybrid search, reranking, and Redis semantic response cache", "Stream AI answers to client with cited source chunks and confidence metrics", "PortfolioProject", 60, "Critical"),
                ("DSA: Regular Expression Matching (Hard)", "2D DP with star wildcards", "DSA", 60, "Critical")
            }),
            80 => ("Portfolio Project: Observability & Distributed Tracing", "Portfolio Project", new() {
                ("Instrument OpenTelemetry across API, DB, Redis, and AI calls", "Deploy Prometheus, Grafana, and Jaeger dashboard monitoring live project metrics", "PortfolioProject", 60, "High"),
                ("DSA: Burst Balloons (Hard)", "Interval dynamic programming", "DSA", 60, "Critical")
            }),
            81 => ("Portfolio Project: Containerization & Helm Charts", "Docker & Kubernetes", new() {
                ("Package portfolio microservices into optimized distroless Docker images", "Write Helm charts for staging and production Kubernetes deployment", "Kubernetes", 60, "High"),
                ("DSA: Distinct Subsequences (Hard)", "2D DP string matching", "DSA", 55, "High")
            }),
            82 => ("Portfolio Project: Automated CI/CD Deployment", "CI/CD & DevOps", new() {
                ("Build multi-stage GitHub Actions workflow: lint -> unit test -> integration test -> deploy to Render/Cloud", "Configure zero-downtime rolling updates and health check verifications", "CI/CD", 60, "Critical"),
                ("DSA: Longest Increasing Path in a Matrix (Hard)", "DFS with memoization matrix", "DSA", 55, "High")
            }),
            83 => ("Portfolio Project: Performance Benchmarking & Tuning", "Performance & Tuning", new() {
                ("Load test with k6: simulate 1,000 concurrent users", "Identify bottlenecks, tune connection pools, optimize SQL indexes, and reduce GC pauses", "DotNetBackend", 60, "High"),
                ("DSA: Word Search II (Hard)", "Trie data structure + DFS backtracking on board", "DSA", 60, "Critical")
            }),
            84 => ("Portfolio Project: Documentation & Architecture Diagrams", "Documentation", new() {
                ("Create C4 model architecture diagrams (Context, Container, Component, Code) with Mermaid", "Write comprehensive README with live demo link, architecture decisions (ADRs), and benchmarks", "PortfolioProject", 60, "High"),
                ("DSA: Minimum Cost to Cut a Stick & Russian Doll Envelopes", "Hard DP interval & LIS variants", "DSA", 55, "High")
            }),
            85 => ("Portfolio Project: Showcase Video & Live Demo Verification", "Portfolio Project", new() {
                ("Deploy production app to Render with Render PostgreSQL", "Record a crisp 3-minute architectural walkthrough video explaining key engineering decisions", "PortfolioProject", 60, "High"),
                ("DSA: Best Time to Buy and Sell Stock IV (Hard)", "K transactions DP state machine", "DSA", 55, "High")
            }),
            86 => ("System Design: Global Payment Gateway", "System Design & Interview", new() {
                ("Two-phase commit, double-entry ledger database schema, idempotency, PCI-DSS compliance", "Reconciliation engine, webhook delivery with exponential backoff and jitter", "System Design", 65, "Critical"),
                ("DSA: Maximum Profit in Job Scheduling (Hard)", "DP with binary search on intervals", "DSA", 55, "Critical")
            }),
            87 => ("System Design: Metrics & Monitoring System (Datadog/New Relic)", "System Design & Interview", new() {
                ("Time-series database (TSDB) storage engine, downsampling, Gorilla compression algorithm", "Distributed alerting rules engine with sliding window anomaly detection", "System Design", 65, "High"),
                ("DSA: N-Queens & N-Queens II (Hard)", "Classic backtracking with diagonal sets", "DSA", 50, "High")
            }),
            88 => ("Cloud Security, Secrets Management & Zero Trust", "Cloud & Security", new() {
                ("HashiCorp Vault / AWS Secrets Manager / Azure Key Vault rotation", "mTLS between microservices, principle of least privilege, WAF configuration", "Cloud", 50, "High"),
                ("DSA: Sudoku Solver (Hard)", "Constraint propagation and backtracking", "DSA", 55, "High")
            }),
            89 => ("Database Disaster Recovery & Backup Drills", "DevOps & Database", new() {
                ("Point-in-Time Recovery (PITR) with PostgreSQL WAL archiving", "Execute backup restore drill and measure Recovery Time Objective (RTO) and Recovery Point Objective (RPO)", "SQL", 55, "High"),
                ("DSA: Remove Invalid Parentheses (Hard)", "BFS / DFS with minimum removal count", "DSA", 50, "High")
            }),
            90 => ("Phase 4 Review & Production Readiness Checklist", "Milestone Review", new() {
                ("Review end-to-end portfolio codebase, container security, and CI/CD pipelines", "Audit performance metrics, error rates, and verify that all deliverables are production-ready", "Core", 60, "High"),
                ("DSA: Find Median from Data Stream (Hard)", "Two heaps (max-heap + min-heap) in O(1) median query", "DSA", 55, "Critical")
            }),

            // Phase 5: Days 91-100
            91 => ("Resume Transformation & ATS Optimization", "Resume & Career", new() {
                ("Rewrite resume bullets using Google's X-Y-Z formula: Accomplished [X] measured by [Y] by doing [Z]", "Highlight .NET 8, PostgreSQL, Redis, Kubernetes, and AI/RAG architectures; run ATS scan", "Resume/LinkedIn", 60, "Critical"),
                ("Interview Drill: Deep C# Internals & CLR Questions", "Explain GC, Span, Task vs Thread, Boxing/Unboxing, Struct layout to an interviewer", "InterviewPrep", 50, "High")
            }),
            92 => ("LinkedIn Positioning & Personal Branding", "Resume & Career", new() {
                ("Optimize LinkedIn headline, About section, featured projects with GitHub links and live demos", "Write an engaging technical post breaking down an architectural challenge solved in Phase 4", "Resume/LinkedIn", 50, "High"),
                ("Interview Drill: ASP.NET Core & EF Core Under the Hood", "Explain middleware pipelines, scoped service traps, and query optimization strategies", "InterviewPrep", 50, "High")
            }),
            93 => ("Target Company Mapping & Recruiter Outreach", "Job Applications", new() {
                ("Build curated list of 30 top target tech companies (Tier 1 tech, high-growth startups, fintech)", "Draft personalized cold outreach messages to hiring managers and engineering directors", "Job Applications", 60, "Critical"),
                ("Interview Drill: Distributed Systems, Outbox & Saga", "Defend saga vs 2PC trade-offs and message broker ordering guarantees", "InterviewPrep", 50, "High")
            }),
            94 => ("System Design Mock Interview 1: Real-Time Collaborative Doc (Google Docs)", "System Design & Interview", new() {
                ("Operational Transformation (OT) vs Conflict-free Replicated Data Types (CRDTs)", "WebSocket gateway, message deduplication, snapshotting, and presence coordination", "System Design", 65, "Critical"),
                ("Interview Drill: SQL Indexing, Locking & Transactions", "Explain B-Tree search, MVCC, isolation levels, and vacuuming to a staff engineer", "InterviewPrep", 50, "High")
            }),
            95 => ("System Design Mock Interview 2: High-Volume E-Commerce Flash Sale", "System Design & Interview", new() {
                ("Handling 500,000 requests/second without overselling inventory", "Redis pre-allocated stock Lua scripts, message queues, async order settlement, optimistic locks", "System Design", 65, "Critical"),
                ("Interview Drill: AI Systems, RAG & Vector Search", "Explain hybrid search, chunking trade-offs, and semantic caching architecture", "InterviewPrep", 50, "High")
            }),
            96 => ("Behavioral Interview Mastery: The STAR Method", "Behavioral & Leadership", new() {
                ("Prepare 6 polished STAR stories: Leadership, Disagreement with manager, Production outage crisis, Tough technical trade-off, Mentoring, Under-the-gun deadline", "Practice delivery with crisp 2-minute structure focusing on quantifiable impact", "InterviewPrep", 60, "Critical"),
                ("DSA Drill: Top 10 Most Frequent Interview DSA Problems", "Speed run LeetCode 75 favorites", "DSA", 50, "High")
            }),
            97 => ("Live Coding & Technical Problem Solving Drills", "Interview Drills", new() {
                ("Practice thinking out loud, clarifying constraints, discussing edge cases, writing clean code", "Simulate 45-minute timed technical coding round under pressure", "InterviewPrep", 60, "Critical"),
                ("DSA Drill: Binary Search & Monotonic Stack speed drills", "Timed 20-minute solutions", "DSA", 45, "High")
            }),
            98 => ("Job Application Blitz & Referrals", "Job Applications", new() {
                ("Submit 10 tailored job applications with custom cover notes", "Reach out to 5 1st/2nd-degree connections for internal referrals at target companies", "Job Applications", 60, "Critical"),
                ("Interview Drill: Microservices, Containers & Kubernetes", "Discuss Pod lifecycles, Ingress routing, and zero-downtime deployments", "InterviewPrep", 50, "High")
            }),
            99 => ("Offer Negotiation & Compensation Strategy", "Career Strategy", new() {
                ("Learn compensation negotiation levers: Base salary, equity/RSUs, sign-on bonus, performance bonus", "Prepare negotiation scripts and evaluate total compensation packages across offers", "Job Applications", 55, "High"),
                ("Full Mock Interview: 1-hour combined System Design + Coding + Behavioral", "Comprehensive end-to-end dress rehearsal", "InterviewPrep", 60, "Critical")
            }),
            100 => ("The 100-Day Graduation & Continuous Growth Plan", "Milestone & Graduation", new() {
                ("Celebrate completion of the 100-Day Learning Journey!", "Reflect on transformation: from fundamentals to advanced distributed systems and modern AI architectures", "Core", 60, "Critical"),
                ("Establish lifelong continuous learning cadence: weekly deep dives, open source contributions, and industry writing", "Review all 100 days of history and archive graduation state", "Core", 60, "Critical")
            }),

            _ => ($"Day {day} Learning & Mastery", "Continuous Engineering Excellence", new() {
                ($"Master Day {day} Core Engineering Objectives", "Complete structured readings and exercises", "Core", 60, "Medium"),
                ("DSA Problem Solving Practice", "Solve medium-level problem on target topic", "DSA", 45, "Medium")
            })
        };
    }

    private static void SeedTrackers(LearningDbContext db)
    {
        // DSA Problems
        db.DSAProblems.AddRange(new List<DSAProblem>
        {
            new() { Title = "Two Sum", Difficulty = "Easy", Platform = "LeetCode", Pattern = "Arrays & Hashing", Status = "Planned" },
            new() { Title = "Group Anagrams", Difficulty = "Medium", Platform = "LeetCode", Pattern = "Arrays & Hashing", Status = "Planned" },
            new() { Title = "Top K Frequent Elements", Difficulty = "Medium", Platform = "LeetCode", Pattern = "Heap / Bucket Sort", Status = "Planned" },
            new() { Title = "Longest Substring Without Repeating Characters", Difficulty = "Medium", Platform = "LeetCode", Pattern = "Sliding Window", Status = "Planned" },
            new() { Title = "Valid Parentheses", Difficulty = "Easy", Platform = "LeetCode", Pattern = "Stack", Status = "Planned" },
            new() { Title = "Daily Temperatures", Difficulty = "Medium", Platform = "LeetCode", Pattern = "Monotonic Stack", Status = "Planned" },
            new() { Title = "Search in Rotated Sorted Array", Difficulty = "Medium", Platform = "LeetCode", Pattern = "Binary Search", Status = "Planned" },
            new() { Title = "LRU Cache", Difficulty = "Hard", Platform = "LeetCode", Pattern = "Doubly Linked List + Hash Map", Status = "Planned" },
            new() { Title = "Number of Islands", Difficulty = "Medium", Platform = "LeetCode", Pattern = "Graph BFS/DFS", Status = "Planned" },
            new() { Title = "Course Schedule", Difficulty = "Medium", Platform = "LeetCode", Pattern = "Topological Sort", Status = "Planned" },
            new() { Title = "Coin Change", Difficulty = "Medium", Platform = "LeetCode", Pattern = "Dynamic Programming", Status = "Planned" },
            new() { Title = "Trapping Rain Water", Difficulty = "Hard", Platform = "LeetCode", Pattern = "Two Pointers", Status = "Planned" }
        });

        // System Design Topics
        db.SystemDesignTopics.AddRange(new List<SystemDesignTopic>
        {
            new() { TopicName = "Distributed Caching & Rate Limiting", ArchitectureSummary = "Consistent hashing, multi-tier cache, sliding window Redis rate limiter", Status = "Planned" },
            new() { TopicName = "URL Shortener (TinyURL)", ArchitectureSummary = "Base62 encoding, Snowflake ID generation, distributed sharding, high read scalability", Status = "Planned" },
            new() { TopicName = "Real-Time Chat Application", ArchitectureSummary = "WebSockets, Redis Pub/Sub, Cassandra/PostgreSQL persistence, presence tracking", Status = "Planned" },
            new() { TopicName = "Adaptive Video Streaming (Netflix/YouTube)", ArchitectureSummary = "Chunking, HLS/DASH protocols, CDN edge distribution, distributed transcoding", Status = "Planned" },
            new() { TopicName = "Enterprise AI RAG Architecture", ArchitectureSummary = "Ingestion pipeline, hybrid search (BM25 + pgvector), reranking, semantic cache, LLM gateway", Status = "Planned" },
            new() { TopicName = "High-Throughput Payment Gateway", ArchitectureSummary = "Double-entry ledger, 2PC / Saga pattern, idempotency keys, distributed reconciliation", Status = "Planned" }
        });

        // Interview Questions
        db.InterviewQuestions.AddRange(new List<InterviewQuestion>
        {
            new() { Question = "Explain how the Garbage Collector works in .NET, including generations and LOH/POH.", Category = ".NET/C#", ConfidenceLevel = "Medium" },
            new() { Question = "What is the difference between Task and ValueTask, and when should you use ValueTask?", Category = ".NET/C#", ConfidenceLevel = "Medium" },
            new() { Question = "Explain the Transactional Outbox Pattern and why it is critical in distributed systems.", Category = "Distributed Systems", ConfidenceLevel = "Medium" },
            new() { Question = "How do you defend against prompt injection in production RAG systems?", Category = "AI/LLM", ConfidenceLevel = "Medium" },
            new() { Question = "Explain the difference between B-Tree indexes and HNSW vector indexes.", Category = "SQL/DB", ConfidenceLevel = "Medium" }
        });

        // Resources
        db.LearningResources.AddRange(new List<LearningResource>
        {
            new() { Title = "Microsoft .NET Documentation & Performance Best Practices", Url = "https://learn.microsoft.com/en-us/dotnet/", Category = "Documentation", IsCompleted = false },
            new() { Title = "Designing Data-Intensive Applications (Martin Kleppmann)", Url = "https://dataintensive.net/", Category = "Book", IsCompleted = false },
            new() { Title = "OpenTelemetry .NET Specification & Architecture", Url = "https://opentelemetry.io/docs/languages/net/", Category = "Documentation", IsCompleted = false },
            new() { Title = "PostgreSQL pgvector Official Repository & Indexing Guide", Url = "https://github.com/pgvector/pgvector", Category = "Repository", IsCompleted = false }
        });

        // Career Goals
        db.Goals.AddRange(new List<Goal>
        {
            new() { Title = "Complete All 100 Learning Days & DSA Problems", Category = "Learning", ProgressPercent = 0, Status = "Active" },
            new() { Title = "Deploy Production AI-Powered Portfolio Project to Render with PostgreSQL", Category = "Portfolio", ProgressPercent = 0, Status = "Active" },
            new() { Title = "Secure Senior / Lead Software Engineer Offer in Distributed Systems or AI", Category = "Career", ProgressPercent = 0, Status = "Active" }
        });
    }
}
