## Purpose
- Shared Kafka producer/consumer bootstrap for Team Hub microservices (`TeamHub.Kafka`).

## Source of truth
- `KafkaOptions.cs` — `Kafka:BootstrapServers`, optional `Kafka:ClientId`
- `ServiceCollectionExtensions.cs` — `AddTeamHubKafkaProducer`, `AddTeamHubKafkaConsumer<TMessage,THandler>`
- `IKafkaProducer` — `ProduceAsync<T>` (JSON) and `ProduceRawAsync` (pre-serialized outbox payloads)
- `Events/OrganizationMemberAddedEvent.cs` — shared domain event contract (`organizationName` included for notification copy)
- `KafkaTopics.cs` — topic name constants

## Do
- Use Confluent.Kafka under the hood; keep domain handlers in services.
- Publish JSON with camelCase property names.
- Consumer commits after successful handler; unique business keys for idempotency live in the consumer service.
- Create producer/consumer spans via `ActivitySource("TeamHub.Kafka")` (`kafka produce {topic}` / `kafka consume {topic}`).
- Reference this library via `ProjectReference` from producers/consumers.

## Don't
- Do not put service-specific notification or organization business logic here.
- Do not embed Docker/Aspire runtime wiring in this library.
- Do not implement transactional outbox here (lives in organization service).
