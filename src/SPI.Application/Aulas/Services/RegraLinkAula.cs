using SPI.Domain.Entities;

namespace SPI.Application.Aulas.Services
{
    // specs/067: unico lugar que decide se uma aula aceita (e mostra) link. Funcao pura, como
    // RegraChamada. A tela so exibe o resultado (Principio II).
    public static class RegraLinkAula
    {
        public static DateTime FimDaAula(Aula aula) => aula.DataInicio.ToDateTime(aula.HoraFim);

        // Aula ativa, nao cancelada e que ainda nao terminou. Uma aula ja Realizada que ainda esta em
        // andamento continua aceitando: a professora pode precisar do link depois de abrir a chamada.
        public static bool AceitaLink(Aula aula, DateTime agora) =>
            aula.Ativo && aula.Status != "Cancelada" && agora < FimDaAula(aula);
    }
}
