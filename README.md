# Controle de Gastos — Projeto Completo
file:///C:/Users/EdyM2/ControleDeGastos-Completo/frontend/index.html

Projeto completo: API (.NET 8 + PostgreSQL + segurança contra invasão) e Dashboard web (HTML/CSS/JS puro).

```
ControleDeGastos-Completo/
├── ControleDeGastos.slnx          → solução do Visual Studio
├── ControleDeGastos.API/          → backend (API)
│   ├── Controllers/GastosController.cs
│   ├── Models/                    → Gasto, TipoGasto, FormaPagamento
│   ├── DTOs/GastoDtos.cs
│   ├── Data/                      → AppDbContext, SeedData
│   ├── Security/AntiInvasaoMiddleware.cs  → rate limiting, bloqueio de IP, detecção de invasão
│   ├── Program.cs
│   ├── appsettings.json           → connection string do PostgreSQL
│   └── ControleDeGastos.API.csproj
└── frontend/
    └── dashboard.html             → dashboard web (abrir direto no navegador)
```

## 1. Banco de dados (PostgreSQL)

Suba um PostgreSQL local ou via Docker:

```bash
docker run --name pg-gastos -e POSTGRES_PASSWORD=suasenha -p 5432:5432 -d postgres
```

Edite `ControleDeGastos.API/appsettings.json` com usuário/senha/banco reais:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=controle_gastos;Username=postgres;Password=suasenha"
}
```

> Em produção, não deixe a senha em texto puro no `appsettings.json` — use `dotnet user-secrets` em dev ou variáveis de ambiente em produção.

## 2. Rodar a API

Dentro de `ControleDeGastos.API/`:

```bash
dotnet restore
dotnet ef migrations add InitialCreate
dotnet ef database update
dotnet run
```

Na primeira execução, a API aplica a migration e popula dados de exemplo automaticamente. Anote a porta exibida no console (ex: `https://localhost:7123`) — o Swagger fica em `/swagger`.

## 3. Abrir o dashboard

Abra `frontend/dashboard.html` no navegador. No rodapé da página, ajuste o campo **API** para a URL/porta da sua API e clique em **Reconectar**.

## 4. Segurança já incluída

- `Security/AntiInvasaoMiddleware.cs`: rate limiting por IP, bloqueio temporário após atividade suspeita, detecção de padrões de SQL Injection/XSS/path traversal/command injection, e cabeçalhos HTTP de segurança (CSP, HSTS, X-Frame-Options, etc.)
- CORS restrito por configuração (`Cors:AllowedOrigins` no `appsettings.json`) — vazio libera qualquer origem, útil em dev
- Tratamento de erro genérico em produção (não vaza stack trace)
- Validação de entrada via DataAnnotations nos DTOs

## Próximos passos sugeridos

- Autenticação (JWT) para proteger os endpoints de escrita
- `dotnet user-secrets` para a connection string em desenvolvimento
- Deploy da API (ex: Render, Railway, Azure) e do PostgreSQL gerenciado
