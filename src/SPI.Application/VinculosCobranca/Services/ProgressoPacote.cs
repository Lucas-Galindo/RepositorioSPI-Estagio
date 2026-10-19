using SPI.Application.VinculosCobranca.Dtos;
using SPI.Domain.Entities;

namespace SPI.Application.VinculosCobranca.Services
{
    // specs/058: unico calculo de progresso de pacote -- usado pela ficha do aluno
    // e pela Home, para as duas mostrarem sempre os mesmos numeros (FR-004).
    // Decisao do usuario (2026-10-19): aulas dadas = presencas registradas no
    // pacote; restantes = total - dadas. O saldo (SaldoAulas) continua sendo o que
    // bloqueia a presenca (specs/041), mas nao entra aqui, exceto para estimar o
    // total de pacotes antigos que nunca tiveram total informado.
    public static class ProgressoPacote
    {
        public const string EmDia = "EmDia";
        public const string Atencao = "Atencao";
        public const string Esgotado = "Esgotado";
        public const string SemTotal = "SemTotal";

        public static ProgressoPacoteResponse Calcular(VinculoCobranca pacote, int aulasDadas)
        {
            var estimado = !pacote.TotalAulasPacote.HasValue && pacote.SaldoAulas.HasValue;
            var total = pacote.TotalAulasPacote ?? (estimado ? aulasDadas + pacote.SaldoAulas!.Value : null);

            // Total editado para baixo antes desta regra pode ficar menor que as
            // dadas: restante nunca e negativo.
            int? restantes = total.HasValue ? Math.Max(0, total.Value - aulasDadas) : null;

            var estado = restantes switch
            {
                null => SemTotal,
                0 => Esgotado,
                <= VinculoCobranca.LimiteAlertaPacote => Atencao,
                _ => EmDia,
            };

            return new ProgressoPacoteResponse
            {
                Total = total,
                TotalEstimado = estimado,
                AulasDadas = aulasDadas,
                AulasRestantes = restantes,
                Estado = estado,
            };
        }
    }
}
