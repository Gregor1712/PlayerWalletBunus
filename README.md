# PlayerWallet API

In-memory player wallet REST API built with .NET 10. Supports deposits, stakes, wins, per-player concurrency control, and transaction idempotency with rejected-to-accepted retry logic.

## Project Structure

```
PlayerWallet/
├── PlayerWallet.Api/            # ASP.NET Web API (controllers, middleware)
├── PlayerWallet.Application/    # Business logic (services, managers, DTOs)
├── PlayerWallet.Domain/         # Domain entities, repositories, EF Core DbContext
├── PlayerWallet.Tests/          # xUnit tests (List-based + EF Core InMemory)
└── Dockerfile
```

## Features

- **Register wallet** for a player (one wallet per player)
- **Deposit / Stake / Win** transactions with balance validation
- **Per-player async locking** — `SemaphoreSlimManager` serializes concurrent requests for the same player using `ConcurrentDictionary<Guid, RefCountedSemaphore>` with automatic eviction
- **Idempotency** — replaying an accepted transaction returns `"accepted"` without double-applying the balance
- **Rejected-to-accepted retry** — a rejected transaction (insufficient funds) is stored; if replayed after the balance increases, it gets updated to accepted

## API Endpoints

Base URL: `http://localhost:5000`

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/players/{playerId}/wallet` | Register a new wallet |
| GET | `/api/players/{playerId}/balance` | Get current balance |
| POST | `/api/players/{playerId}/transactions` | Credit a transaction |
| GET | `/api/players/{playerId}/transactions` | List all transactions |

### Transaction Request Body

```json
{
  "transactionId": "guid",
  "type": "Deposit",
  "amount": 100
}
```

Transaction types: `Deposit`, `Stake`, `Win`

### Error Responses

| Status | Condition |
|--------|-----------|
| 404 | Wallet not found for player |
| 409 | Wallet already exists for player |

## Configuration

In `appsettings.json`:

```json
{
  "UseInMemoryDatabase": false
}
```

- `false` (default) — uses in-process `List<T>` repositories (singleton, no database)
- `true` — uses EF Core InMemory database with scoped repositories

## Running Locally

```bash
dotnet run --project PlayerWallet.Api
```

The API starts on `http://localhost:5000`.

## Running with Docker

```bash
# Build
docker build -t playerwallet-api .

# Run
docker run -d --name playerwallet-api -p 5000:5000 playerwallet-api

# Logs
docker logs playerwallet-api

# Stop
docker stop playerwallet-api
```

## Running Tests

```bash
dotnet test
```

Two test suites:
- **WalletServiceTests** — uses `List<T>` repositories with `Task.Delay(1)` to simulate I/O and expose race conditions
- **WalletServiceInMemoryDatabaseTests** — uses EF Core InMemory database

Key test scenarios:
- Basic CRUD (register, deposit, stake, win, get balance, get transactions)
- Insufficient funds rejection
- Idempotency for accepted and rejected transactions
- Rejected-to-accepted transition after balance top-up
- Concurrent deposits for the same player (proves semaphore necessity)
- Concurrent same `TransactionId` idempotency under load

## Postman Collection

Import `PlayerWallet.postman_collection.json` into Postman. The collection contains step-by-step scenarios including the full rejected-to-accepted idempotency flow.

## Technology

- .NET 10, ASP.NET Core
- EF Core InMemory (optional)
- AutoMapper
- NLog
- xUnit
- Scalar (OpenAPI UI at `/scalar/v1` in Development)

Bonus questions (production grade implementation):

- Data stored in a real database
- Per-player SemaphoreSlim replaced by database optimistic concurrency (RowVersion/concurrency token on the wallet row)
- on conflict, the application catches DbUpdateConcurrencyException and retries the transaction
- Unique index on TransactionId guarantees idempotency at the database level
- accepted transactions return "accepted" on replay without double-applying the balance
- rejected transactions are stored and can transition to accepted if the balance later increases

1. Optimize for heavy reads (much more "get player's balance" API calls than "credit transaction" calls):

	- Use Redis as a distributed cache via IDistributedCache
	- GetBalance reads from cache first, falls back to database on cache miss
	- CreditTransaction updates the database first, then invalidates/updates the cache entry to keep it consistent
	- Set TTL (time-to-live) on cache entries as a safety net — even if invalidation fails, state data expires automatically
	- Reads never hit the database under normal operation, eliminating contention with writes
  
2. Multi-node deployment for distributing load:

	- Implement IDistributedCache using Redis:
	- Redis serves as the shared state across all nodes — any node can serve any players GetBalance from the same cache
	- Database optimistic concurrency (RowVersion) handles write conflicts across nodes — no need for in-process semaphore or sticky sessions
	- Unique index on TransactionId guarantees idempotency regardless of which node processes the request
	- Nodes are stateless and interchangeable behind a load balancer — horizontal scaling by simply adding more instances
	- Consider Redis Sentinel or Redis Cluster for high availability of the cache layer itself