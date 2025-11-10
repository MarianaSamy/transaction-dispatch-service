** Overview : 
The Transaction Dispatch Microservice asynchronously queues and processes file-based dispatch jobs, streaming eligible files from local storage and sending them to Kafka with configurable parallelism and reliability.
It follows a clean layered architecture with strong separation of concerns, high scalability, and testable, options-driven components.

** Dispatch Microservice — Layered Architecture

1- Domain Layer
   -Defines the core business models and logic, including DispatchJob, DispatchFile, and JobStatus.
   -Enums: Including JobStatus and ProcessingOutcome enums.

2- Application Layer
  Defines the coordination logic of the microservice. It contains the core services (DispatchService, JobProcessor, FileProcessor) that manage job creation and execution.
  It also includes the supporting DTOs, interfaces, and options classes that structure data flow, configuration, and Abstraction. 

Additionally, the Application Layer serves as the microservice’s orchestration hub. It applies business rules, enforces workflow consistency, and bridges the Domain and Infrastructure layers through well-defined interfaces.
It ensures testability, maintainability, and configurability by isolating logic from implementation details, allowing the system to evolve or swap technologies (e.g., Kafka, file I/O, or DB) without impacting the core business flow.

3- Infrastructure Layer
   Implements all system integrations and operational capabilities:
   - I/O – LocalFileProvider handles file enumeration, validation, reading, and deletion.
   - Background – Hosts the background task queue and worker responsible for dequeuing and processing jobs asynchronously.
   - Kafka – ConfluentKafkaProducer manages message dispatch to Kafka with configurable batching, compression, and acknowledgment behavior.
   - Persistence (Database) – Stores and updates job metadata through IJobRepository, providing durable tracking of job progress and state.

4- API Layer
Exposes REST endpoints to initiate new dispatch jobs and retrieve their current status.

**The service relies on configuration values defined in appsettings.json, including database connection details, Kafka producer settings, and file type options.
These parameters should be provided per environment.

** Future Plans
If more time were available, the Application Layer would be extended to persist failed files for retry and to implement a per-file audit trail using bulk insert, enabling efficient recovery and detailed tracking of dispatch operations.




