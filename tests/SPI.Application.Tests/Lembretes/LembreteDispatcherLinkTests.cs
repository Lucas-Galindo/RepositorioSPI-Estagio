using SPI.Application.Aulas.Services;
using SPI.Application.Common;
using SPI.Application.Tests.Aulas;
using SPI.Domain.Entities;
using SPI.Domain.Repositories;
using SPI.Infrastructure.BackgroundServices;
using Xunit;

namespace SPI.Application.Tests.Lembretes
{
    // specs/067 (US2): o e-mail do lembrete leva o link da PROXIMA aula da turma, quando existe e ainda
    // vale. Sem link, o corpo e o de sempre; a regra de envio (status, tentativas, destinatarios) nao muda.
    public class LembreteDispatcherLinkTests
    {
        private static readonly DateTime Agora = new(2026, 11, 10, 12, 0, 0);
        private const string Link = "https://meet.exemplo.com/teste-067";

        // Corpo de antes da spec 067 (turma 1, lembrete das 11:55), com a quebra de linha normalizada.
        private const string CorpoSemLink =
            "<p>Ol&aacute;!</p>\n<p>Este &eacute; um lembrete da turma <b>Turma 1</b>: sua pr&oacute;xima aula est&aacute; se aproximando.</p>\n<p>Data/hora do lembrete: 10/11/2026 11:55";

        private sealed class RepositorioComAula : ILembreteRepository
        {
            public List<Lembrete> Lembretes { get; } = new();
            public Aula? ProximaAula { get; set; }
            public bool FalharAoBuscarAula { get; set; }
            public int BuscasDeAula { get; private set; }

            public Task<List<Lembrete>> ListarPendentesVencidosAsync(DateTime agora, CancellationToken cancellationToken = default) =>
                Task.FromResult(Lembretes.Where(l => l.Ativo && l.Status == "Pendente" && l.HoraProgramada <= agora).OrderBy(l => l.HoraProgramada).ToList());

            public Task<Aula?> ObterProximaAulaDaTurmaAsync(int turmaId, CancellationToken cancellationToken = default)
            {
                BuscasDeAula++;
                if (FalharAoBuscarAula)
                {
                    throw new InvalidOperationException("Falha simulada de banco.");
                }

                return Task.FromResult(ProximaAula);
            }

            public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task<Lembrete?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<List<Lembrete>> ListarAsync(int? turmaId, string? status, bool? ativo, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<List<Lembrete>> ListarAtivosPorTurmaAsync(int turmaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task AdicionarAsync(Lembrete lembrete, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        }

        private sealed class EmailCapturaCorpo : IEmailSender
        {
            public List<(string Destinatario, string Assunto, string Corpo)> Enviados { get; } = new();

            public Task EnviarAsync(string destinatario, string assunto, string corpoHtml, CancellationToken cancellationToken = default)
            {
                Enviados.Add((destinatario, assunto, corpoHtml));
                return Task.CompletedTask;
            }
        }

        private static Aula ProximaAulaDaTurma() => new()
        {
            Id = 77, TurmaId = 1, Ativo = true, Status = "Agendada",
            DataInicio = new DateOnly(2026, 11, 10), HoraInicio = new TimeOnly(15, 0), HoraFim = new TimeOnly(16, 0),
        };

        // Um lembrete vencido (turma de 2 alunos) e uma proxima aula, com relogio as 12h.
        private static (RepositorioComAula Repo, Lembrete Lembrete, LinkAulaTemporario Links) Cenario()
        {
            var repo = new RepositorioComAula { ProximaAula = ProximaAulaDaTurma() };
            var lembrete = LembreteTestHelper.CriarLembretePendente(id: 1, horaProgramada: new DateTime(2026, 11, 10, 11, 55, 0), quantidadeAlunos: 2);
            repo.Lembretes.Add(lembrete);
            return (repo, lembrete, new LinkAulaTemporario(new RelogioFalso(Agora)));
        }

        private static Task Processar(RepositorioComAula repo, EmailCapturaCorpo email, ILinkAulaTemporario? links) =>
            LembreteDispatcherService.ProcessarLembretesVencidosAsync(repo, email, maxEmailsPorCiclo: 10, logger: null, agora: Agora, cancellationToken: default, links: links);

        private static string Normalizar(string corpo) => corpo.Replace("\r\n", "\n");

        [Fact]
        public async Task Com_link_vigente_o_corpo_traz_o_link_clicavel_para_todos_os_destinatarios()
        {
            var (repo, _, links) = Cenario();
            links.Definir(77, Link, new DateTime(2026, 11, 10, 16, 0, 0));
            var email = new EmailCapturaCorpo();

            await Processar(repo, email, links);

            Assert.Equal(2, email.Enviados.Count);
            Assert.All(email.Enviados, e =>
            {
                Assert.Contains($"<a href=\"{Link}\">{Link}</a>", e.Corpo);
                Assert.Contains("Link da aula", e.Corpo);
            });
        }

        [Fact]
        public async Task Sem_link_o_corpo_e_identico_ao_de_antes()
        {
            var (repo, _, links) = Cenario();
            var email = new EmailCapturaCorpo();

            await Processar(repo, email, links);

            Assert.All(email.Enviados, e =>
            {
                Assert.StartsWith(CorpoSemLink, Normalizar(e.Corpo));
                Assert.DoesNotContain("Link da aula", e.Corpo);
                Assert.DoesNotContain("href", e.Corpo);
            });
        }

        [Fact]
        public async Task Sem_o_armazenamento_nao_busca_a_aula_e_o_corpo_e_o_de_antes()
        {
            var (repo, _, _) = Cenario();
            var email = new EmailCapturaCorpo();

            await Processar(repo, email, links: null);

            Assert.Equal(0, repo.BuscasDeAula);
            Assert.All(email.Enviados, e => Assert.DoesNotContain("Link da aula", e.Corpo));
        }

        [Fact]
        public async Task Link_expirado_ou_perdido_sai_sem_link_e_sem_erro()
        {
            var (repo, lembrete, links) = Cenario();
            links.Definir(77, Link, new DateTime(2026, 11, 10, 16, 0, 0));
            var relogioDoArmazenamento = new RelogioFalso(new DateTime(2026, 11, 10, 17, 0, 0)); // depois do fim
            var expirado = new LinkAulaTemporario(relogioDoArmazenamento); // armazenamento vazio, como depois de um reinicio
            var email = new EmailCapturaCorpo();

            await Processar(repo, email, expirado);

            Assert.Equal(2, email.Enviados.Count);
            Assert.All(email.Enviados, e => Assert.DoesNotContain("Link da aula", e.Corpo));
            Assert.Equal("Enviado", lembrete.Status);
        }

        [Fact]
        public async Task Link_de_outra_aula_nao_e_usado()
        {
            var (repo, _, links) = Cenario();
            links.Definir(999, Link, new DateTime(2026, 11, 10, 16, 0, 0)); // outra aula, nao a proxima da turma
            var email = new EmailCapturaCorpo();

            await Processar(repo, email, links);

            Assert.All(email.Enviados, e => Assert.DoesNotContain("Link da aula", e.Corpo));
        }

        [Fact]
        public async Task Sem_proxima_aula_sai_sem_link()
        {
            var (repo, _, links) = Cenario();
            repo.ProximaAula = null;
            var email = new EmailCapturaCorpo();

            await Processar(repo, email, links);

            Assert.Equal(2, email.Enviados.Count);
            Assert.All(email.Enviados, e => Assert.DoesNotContain("Link da aula", e.Corpo));
        }

        [Fact]
        public async Task Falha_ao_buscar_a_aula_so_tira_o_link_o_envio_segue()
        {
            var (repo, lembrete, links) = Cenario();
            links.Definir(77, Link, new DateTime(2026, 11, 10, 16, 0, 0));
            repo.FalharAoBuscarAula = true;
            var email = new EmailCapturaCorpo();

            await Processar(repo, email, links);

            Assert.Equal(2, email.Enviados.Count);
            Assert.All(email.Enviados, e => Assert.DoesNotContain("Link da aula", e.Corpo));
            Assert.Equal("Enviado", lembrete.Status);
        }

        [Fact]
        public async Task Caracteres_especiais_do_link_saem_protegidos_no_texto_e_no_href()
        {
            var (repo, _, links) = Cenario();
            const string estranho = "https://exemplo.com/sala?a=1&b=\"2\"<script>x</script>";
            links.Definir(77, estranho, new DateTime(2026, 11, 10, 16, 0, 0));
            var email = new EmailCapturaCorpo();

            await Processar(repo, email, links);

            var corpo = email.Enviados[0].Corpo;
            Assert.Contains("a=1&amp;b=&quot;2&quot;&lt;script&gt;x&lt;/script&gt;", corpo);
            Assert.DoesNotContain("<script>", corpo);
        }

        // FR-008: o link nao muda nada da regra de envio.
        [Fact]
        public async Task A_regra_de_envio_e_a_mesma_com_ou_sem_link()
        {
            var (repoCom, lembreteCom, linksCom) = Cenario();
            linksCom.Definir(77, Link, new DateTime(2026, 11, 10, 16, 0, 0));
            var (repoSem, lembreteSem, _) = Cenario();

            var emailCom = new EmailCapturaCorpo();
            var emailSem = new EmailCapturaCorpo();
            await Processar(repoCom, emailCom, linksCom);
            await Processar(repoSem, emailSem, links: null);

            Assert.Equal(lembreteSem.Status, lembreteCom.Status);
            Assert.Equal(lembreteSem.EnviosComSucesso, lembreteCom.EnviosComSucesso);
            Assert.Equal(lembreteSem.DestinatariosTentados, lembreteCom.DestinatariosTentados);
            Assert.Equal(emailSem.Enviados.Select(e => (e.Destinatario, e.Assunto)), emailCom.Enviados.Select(e => (e.Destinatario, e.Assunto)));
        }

        [Fact]
        public async Task Uma_busca_de_aula_por_lembrete_e_nao_por_destinatario()
        {
            var (repo, _, links) = Cenario();
            var email = new EmailCapturaCorpo();

            await Processar(repo, email, links);

            Assert.Equal(2, email.Enviados.Count);
            Assert.Equal(1, repo.BuscasDeAula);
        }
    }
}
