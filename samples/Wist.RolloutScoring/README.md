# Wist rollout-scoring sample

This is the shortest source-checkout example of the public `UniversalToolchain.Wist` facade.

From the repository root:

```bash ci-timeout=240
dotnet run --project samples/Wist.RolloutScoring/Wist.RolloutScoring.csproj
```

The sample validates a numeric rollout formula, compiles it into a typed delegate, invokes the delegate, and confirms that the restricted preset rejects a statement-style binding.

The host application still owns authorization, persistence, resource isolation, and side effects.
