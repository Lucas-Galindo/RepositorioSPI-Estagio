using System.Globalization;
using SPI.Domain.Entities;

namespace SPI.Application.Aulas.Services
{
    // specs/064: unico lugar que decide se a chamada de uma aula esta liberada (Principio II).
    // Usada por RegistrarSessaoAsync (recusa com 409) e por Mapear (informa a tela), para a regra
    // nunca divergir. O "agora" e a hora de parede de Sao Paulo (ver RelogioExtensions).
    public static class RegraChamada
    {
        public static DateTime InicioDaAula(Aula aula) => aula.DataInicio.ToDateTime(aula.HoraInicio);

        // Liberada = aula ativa, ainda "Agendada" e o horario atual >= data + hora de inicio,
        // comparados JUNTOS (nao so a data: uma aula de hoje as 15h nao pode ter chamada as 10h).
        // No minuto exato do inicio ja esta liberada. Aula em andamento tambem esta liberada.
        public static bool Liberada(Aula aula, DateTime agora) =>
            AguardaChamada(aula) && agora >= InicioDaAula(aula);

        // Segundos ate o inicio, so quando a unica coisa que falta e o horario chegar; senao nulo.
        public static int? SegundosParaLiberar(Aula aula, DateTime agora)
        {
            if (!AguardaChamada(aula))
            {
                return null;
            }

            var falta = InicioDaAula(aula) - agora;
            return falta > TimeSpan.Zero ? (int)Math.Ceiling(falta.TotalSeconds) : null;
        }

        // So a aula ativa e ainda Agendada pode ter a chamada (realizada, cancelada e excluida nao).
        private static bool AguardaChamada(Aula aula) => aula.Ativo && aula.Status == "Agendada";

        public static string MensagemBloqueio(Aula aula)
        {
            var inicio = InicioDaAula(aula);
            return string.Create(CultureInfo.InvariantCulture,
                $"A chamada ficara disponivel a partir das {inicio:HH:mm} de {inicio:dd/MM/yyyy}.");
        }
    }
}
