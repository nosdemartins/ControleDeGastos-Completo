using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace ControleDeGastos.API.Security;

/// <summary>
/// Middleware responsável por detectar e mitigar tentativas de invasão:
/// - Rate limiting simples por IP (janela de 1 minuto)
/// - Bloqueio temporário de IPs após atividade suspeita repetida
/// - Detecção de padrões comuns de SQL Injection, XSS, path traversal e command injection
/// - Cabeçalhos HTTP de segurança em todas as respostas
///
/// IMPORTANTE: isto é uma camada extra de defesa (defesa em profundidade).
/// Não substitui boas práticas já existentes na API, como o uso de EF Core
/// (que parametriza consultas e já protege contra SQL Injection real) e a
/// validação de modelos via DataAnnotations nos DTOs.
/// </summary>
public class AntiInvasaoMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AntiInvasaoMiddleware> _logger;

    private const int LimiteRequisicoesPorMinuto = 100;
    private const int LimiteAtividadesSuspeitasParaBloqueio = 3;
    private const int MinutosDeBloqueio = 15;

    private static readonly ConcurrentDictionary<string, InfoAcesso> _acessos = new();

    // Padrões heurísticos de ataques comuns. Não são infalíveis, mas pegam
    // a grande maioria de tentativas automatizadas e scanners.
    private static readonly Regex[] PadroesSuspeitos =
    {
        new(@"(\%27)|(')|(--)|(\%23)|(#)|(\bor\b\s+\d+\s*=\s*\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\b(union(\s|\%20)+select|select(\s|\%20)+.*from|insert(\s|\%20)+into|drop(\s|\%20)+table|exec(\s|\%20)+xp_)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"<script[^>]*>|javascript:|onerror\s*=|onload\s*=|<iframe", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\.\.(/|\\)", RegexOptions.Compiled),
        new(@";\s*(rm|del|shutdown|format|wget|curl)\s", RegexOptions.IgnoreCase | RegexOptions.Compiled)
    };

    public AntiInvasaoMiddleware(RequestDelegate next, ILogger<AntiInvasaoMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var ip = ObterIpCliente(context);
        var info = _acessos.GetOrAdd(ip, _ => new InfoAcesso());

        // 1) IP já bloqueado por comportamento suspeito?
        if (info.BloqueadoAte is { } bloqueadoAte && bloqueadoAte > DateTime.UtcNow)
        {
            _logger.LogWarning("Requisição recusada: IP {Ip} está bloqueado até {Ate}.", ip, bloqueadoAte);
            await ResponderEBloquear(context, StatusCodes.Status403Forbidden, "Acesso temporariamente bloqueado por atividade suspeita.");
            return;
        }

        // 2) Rate limiting por janela de 1 minuto
        bool excedeuLimite;
        lock (info)
        {
            if ((DateTime.UtcNow - info.InicioJanela).TotalMinutes >= 1)
            {
                info.InicioJanela = DateTime.UtcNow;
                info.RequisicoesNaJanela = 0;
            }

            info.RequisicoesNaJanela++;
            excedeuLimite = info.RequisicoesNaJanela > LimiteRequisicoesPorMinuto;
        }

        if (excedeuLimite)
        {
            _logger.LogWarning("IP {Ip} excedeu o limite de {Limite} requisições por minuto.", ip, LimiteRequisicoesPorMinuto);
            await ResponderEBloquear(context, StatusCodes.Status429TooManyRequests, "Muitas requisições em pouco tempo. Aguarde um instante.");
            return;
        }

        // 3) Verifica padrões suspeitos na URL, query string e corpo da requisição
        if (await ConteudoSuspeitoAsync(context))
        {
            info.AtividadesSuspeitas++;
            _logger.LogWarning(
                "Padrão suspeito detectado (possível tentativa de invasão) do IP {Ip} em {Metodo} {Caminho}. Ocorrência {Numero}.",
                ip, context.Request.Method, context.Request.Path, info.AtividadesSuspeitas);

            if (info.AtividadesSuspeitas >= LimiteAtividadesSuspeitasParaBloqueio)
            {
                info.BloqueadoAte = DateTime.UtcNow.AddMinutes(MinutosDeBloqueio);
                _logger.LogWarning("IP {Ip} bloqueado por {Minutos} minutos após atividade suspeita repetida.", ip, MinutosDeBloqueio);
            }

            await ResponderEBloquear(context, StatusCodes.Status400BadRequest, "Requisição rejeitada por conter conteúdo potencialmente malicioso.");
            return;
        }

        // 4) Cabeçalhos de segurança em toda resposta
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["X-XSS-Protection"] = "1; mode=block";
            headers["Content-Security-Policy"] = "default-src 'self'";
            if (context.Request.IsHttps)
            {
                headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
            }
            return Task.CompletedTask;
        });

        await _next(context);
    }

    private static string ObterIpCliente(HttpContext context)
    {
        // Respeita cabeçalho de proxy reverso (ex: quando atrás de Nginx/Load Balancer)
        var encaminhadoPor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(encaminhadoPor))
        {
            return encaminhadoPor.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
    }

    private async Task<bool> ConteudoSuspeitoAsync(HttpContext context)
    {
        var alvo = context.Request.Path + context.Request.QueryString;
        if (PadroesSuspeitos.Any(padrao => padrao.IsMatch(alvo)))
        {
            return true;
        }

        if (context.Request.Method is "POST" or "PUT" && context.Request.ContentLength is > 0)
        {
            context.Request.EnableBuffering();
            using var leitor = new StreamReader(context.Request.Body, leaveOpen: true);
            var corpo = await leitor.ReadToEndAsync();
            context.Request.Body.Position = 0;

            if (PadroesSuspeitos.Any(padrao => padrao.IsMatch(corpo)))
            {
                return true;
            }
        }

        return false;
    }

    private static async Task ResponderEBloquear(HttpContext context, int statusCode, string mensagem)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { mensagem });
    }

    private class InfoAcesso
    {
        public DateTime InicioJanela { get; set; } = DateTime.UtcNow;
        public int RequisicoesNaJanela { get; set; }
        public int AtividadesSuspeitas { get; set; }
        public DateTime? BloqueadoAte { get; set; }
    }
}

public static class AntiInvasaoMiddlewareExtensions
{
    public static IApplicationBuilder UseAntiInvasao(this IApplicationBuilder app)
    {
        return app.UseMiddleware<AntiInvasaoMiddleware>();
    }
}
