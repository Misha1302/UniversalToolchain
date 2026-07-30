# UniversalToolchain.Templates

Installs `dotnet new ut-language`, a standalone non-Wist language template based on the
strongly typed `UniversalToolchain.LanguageAuthoring` API.

```bash ci-run=false
dotnet new install UniversalToolchain.Templates@0.3.0-alpha.3
dotnet new ut-language -n Contoso.RuleLanguage
```

The generated language ID, artifact IDs, contribution IDs and runtime-provider ID are derived
from the requested project name; no `Acme.Pricing` or Wist identifiers are embedded.
