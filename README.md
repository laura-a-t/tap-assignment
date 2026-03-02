# Tap Assignment - Texas Hold'em CLI

Minimal heads-up Texas Hold'em decision engine implemented as a .NET 10 CLI app.

## Requirements

- .NET SDK 10.0 (for local run/tests)
- Docker (for containerized run)

## Run Locally

```bash
dotnet build
dotnet run
```

## Run Tests

```bash
dotnet test
```

## Run With Docker Compose

From `/Users/lauraturcanu/RiderProjects/tap-assignment`:

```bash
docker compose build
docker compose run --rm tap-assignment-cli
```

## Notes

- The app runs one hand per execution.
- Hero action is chosen by the decision engine.
- Villain action is entered in the CLI (`fold`, `check`, `call`, `raise`).
