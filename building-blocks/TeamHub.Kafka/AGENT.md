## Purpose
- Shared Kafka producer/consumer bootstrap for Team Hub microservices (`TeamHub.Kafka`).

## Source of truth
- `KafkaOptions.cs` — `Kafka:BootstrapServers`, optional `Kafka:ClientId`
- `ServiceCollectionExtensions.cs` — `AddTeamHubKafkaProducer`, `AddTeamHubKafkaConsumer<TMessage,THandler>`
- `Events/OrganizationMemberAddedEvent.cs` — shared domain event contract
- `KafkaTopics.cs` — topic name constants

## Do
- Use Confluent.Kafka under the hood; keep domain handlers in services.
- Publish JSON with camelCase property names.
- Consumer commits after successful handler; unique business keys for idempotency live in the consumer service.
- Reference this library via `ProjectReference` from producers/consumers.

## Don't
- Do not put service-specific notification or organization business logic here.
- Do not embed Docker/Aspire runtime wiring in this library.
