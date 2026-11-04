using System.Collections.Concurrent;
using SPI.Application.Common;

namespace SPI.Application.Aulas.Services
{
    // specs/067: ver ILinkAulaTemporario. Dicionario concorrente (singleton, usado por requisicoes e
    // pela rotina de lembretes ao mesmo tempo); a expiracao e conferida na leitura e as entradas
    // vencidas saem a cada gravacao/leitura (volume minimo: uma professora). Este tipo nao tem log
    // de proposito: o link pode conter senha de reuniao.
    public class LinkAulaTemporario : ILinkAulaTemporario
    {
        private readonly ConcurrentDictionary<int, (string Link, DateTimeOffset ExpiraEm)> _links = new();
        private readonly TimeProvider _relogio;

        public LinkAulaTemporario(TimeProvider relogio)
        {
            _relogio = relogio;
        }

        public string? Obter(int aulaId)
        {
            RemoverVencidos();
            return _links.TryGetValue(aulaId, out var entrada) ? entrada.Link : null;
        }

        public void Definir(int aulaId, string link, DateTime fimDaAula)
        {
            RemoverVencidos();

            var expiraEm = RelogioExtensions.ParaInstanteUtc(fimDaAula);
            if (expiraEm <= _relogio.GetUtcNow())
            {
                _links.TryRemove(aulaId, out _);
                return;
            }

            _links[aulaId] = (link, expiraEm);
        }

        public void Remover(int aulaId) => _links.TryRemove(aulaId, out _);

        private void RemoverVencidos()
        {
            var agora = _relogio.GetUtcNow();
            foreach (var (aulaId, entrada) in _links)
            {
                if (entrada.ExpiraEm <= agora)
                {
                    _links.TryRemove(aulaId, out _);
                }
            }
        }
    }
}
