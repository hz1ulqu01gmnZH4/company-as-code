# Company as Code

F# domain-driven design implementation of the Japanese Companies Act (会社法).

## Structure

```
src/
├── CompanyAsCode.SharedKernel/    # Value objects, domain primitives
├── CompanyAsCode.Legal/           # Corporate governance (取締役会, 株主)
├── CompanyAsCode.HR/              # Employment management (雇用契約, 給与)
├── CompanyAsCode.Financial/       # Accounting (仕訳, インボイス制度)
├── CompanyAsCode.Operations/      # Business operations (取引先, 契約)
└── CompanyAsCode.Compliance/      # Regulatory filings (届出, 申告)
```

## Build

```bash
dotnet build
dotnet test
```

## License

WTFPL
