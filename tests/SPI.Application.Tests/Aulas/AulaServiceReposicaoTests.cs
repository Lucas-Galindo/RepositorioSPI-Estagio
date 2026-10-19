using SPI.Application.Aulas.Dtos;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using SPI.Domain.Exceptions;
using Xunit;

namespace SPI.Application.Tests.Aulas
{
    // specs/056 (US3): reposicao -- a professora coloca um aluno de fora da turma
    // numa aula agendada (FR-015 a FR-019).
    public class AulaServiceReposicaoTests
    {
        private const int MatriculadoId = 1;
        private const int VisitanteId = 2;
        private const int InativoId = 3;
        private const int TurmaB = 20;

        private readonly CenarioCadastroAula _cenario;

        public AulaServiceReposicaoTests()
        {
            _cenario = new CenarioCadastroAula(CenarioCadastroAula.NovoAluno(MatriculadoId, "Ana"));

            // O visitante existe e esta ativo, mas nao e da turma da aula.
            var visitante = CenarioCadastroAula.NovoAluno(VisitanteId, "Bruno");
            _cenario.AlunosRepo.Alunos[VisitanteId] = visitante;
            _cenario.Aulas.AlunosConhecidos[VisitanteId] = visitante;

            var inativo = CenarioCadastroAula.NovoAluno(InativoId, "Caio");
            inativo.Ativo = false;
            _cenario.AlunosRepo.Alunos[InativoId] = inativo;
        }

        private static AulaRequest Request(int? turmaId = CenarioCadastroAula.TurmaId, int? alunoId = null) => new()
        {
            MateriaId = 1,
            TurmaId = turmaId,
            AlunoId = alunoId,
            DataInicio = DateOnly.FromDateTime(DateTime.Now).AddDays(7),
            HoraInicio = new TimeOnly(10, 0),
            HoraFim = new TimeOnly(11, 0),
        };

        private async Task<int> NovaAulaAgendadaAsync() => (await _cenario.Servico.CadastrarAsync(1, Request())).Id;

        private Aula Aula(int id) => _cenario.Aulas.Aulas.Single(a => a.Id == id);

        // ---------- Adicionar ----------

        [Fact]
        public async Task Adiciona_aluno_de_fora_da_turma_marcado_como_reposicao()
        {
            var aulaId = await NovaAulaAgendadaAsync();

            var resposta = await _cenario.Servico.AdicionarParticipanteAsync(aulaId, VisitanteId);

            var visitante = resposta.Alunos.Single(a => a.AlunoId == VisitanteId);
            Assert.True(visitante.Reposicao);
            Assert.False(resposta.Alunos.Single(a => a.AlunoId == MatriculadoId).Reposicao);
        }

        [Fact]
        public async Task Recusa_aluno_inativo()
        {
            var aulaId = await NovaAulaAgendadaAsync();

            var excecao = await Assert.ThrowsAsync<ConflitoException>(() => _cenario.Servico.AdicionarParticipanteAsync(aulaId, InativoId));

            Assert.Equal("O aluno esta inativo.", excecao.Message);
        }

        [Fact]
        public async Task Recusa_aluno_inexistente()
        {
            var aulaId = await NovaAulaAgendadaAsync();

            await Assert.ThrowsAsync<NaoEncontradoException>(() => _cenario.Servico.AdicionarParticipanteAsync(aulaId, 999));
        }

        [Theory]
        [InlineData(MatriculadoId)]
        [InlineData(VisitanteId)]
        public async Task Recusa_aluno_que_ja_participa(int alunoId)
        {
            var aulaId = await NovaAulaAgendadaAsync();
            if (alunoId == VisitanteId)
            {
                await _cenario.Servico.AdicionarParticipanteAsync(aulaId, VisitanteId);
            }

            var excecao = await Assert.ThrowsAsync<ConflitoException>(() => _cenario.Servico.AdicionarParticipanteAsync(aulaId, alunoId));

            Assert.Equal("O aluno ja participa desta aula.", excecao.Message);
            Assert.Single(Aula(aulaId).AulaAlunos, aa => aa.AlunoId == alunoId);
        }

        [Theory]
        [InlineData("Realizada")]
        [InlineData("Cancelada")]
        public async Task Recusa_aula_que_nao_esta_agendada(string status)
        {
            var aulaId = await NovaAulaAgendadaAsync();
            Aula(aulaId).Status = status;

            await Assert.ThrowsAsync<ConflitoException>(() => _cenario.Servico.AdicionarParticipanteAsync(aulaId, VisitanteId));
            await Assert.ThrowsAsync<ConflitoException>(() => _cenario.Servico.RemoverParticipanteAsync(aulaId, VisitanteId));
        }

        [Fact]
        public async Task Recusa_aula_excluida()
        {
            var aulaId = await NovaAulaAgendadaAsync();
            Aula(aulaId).Ativo = false;

            await Assert.ThrowsAsync<NaoEncontradoException>(() => _cenario.Servico.AdicionarParticipanteAsync(aulaId, VisitanteId));
        }

        // FR-017: aula individual nao recebe alunos extras.
        [Fact]
        public async Task Recusa_aula_individual()
        {
            var aulaId = (await _cenario.Servico.CadastrarAsync(1, Request(turmaId: null, alunoId: MatriculadoId))).Id;

            var excecao = await Assert.ThrowsAsync<ConflitoException>(() => _cenario.Servico.AdicionarParticipanteAsync(aulaId, VisitanteId));

            Assert.Equal("Aula individual nao aceita alunos de reposicao.", excecao.Message);
        }

        // ---------- Remover ----------

        [Fact]
        public async Task Remove_aluno_de_reposicao()
        {
            var aulaId = await NovaAulaAgendadaAsync();
            await _cenario.Servico.AdicionarParticipanteAsync(aulaId, VisitanteId);

            var resposta = await _cenario.Servico.RemoverParticipanteAsync(aulaId, VisitanteId);

            Assert.DoesNotContain(resposta.Alunos, a => a.AlunoId == VisitanteId);
            Assert.Contains(resposta.Alunos, a => a.AlunoId == MatriculadoId);
        }

        // FR-016 (MUST NOT): o matriculado nunca sai da aula individualmente.
        [Fact]
        public async Task Recusa_remover_aluno_matriculado_e_ele_continua_na_aula()
        {
            var aulaId = await NovaAulaAgendadaAsync();

            var excecao = await Assert.ThrowsAsync<ConflitoException>(() => _cenario.Servico.RemoverParticipanteAsync(aulaId, MatriculadoId));

            Assert.Equal("So alunos de reposicao podem ser removidos da aula; este aluno e matriculado na turma.", excecao.Message);
            Assert.Contains(Aula(aulaId).AulaAlunos, aa => aa.AlunoId == MatriculadoId);
        }

        [Fact]
        public async Task Recusa_remover_aluno_que_nao_participa()
        {
            var aulaId = await NovaAulaAgendadaAsync();

            await Assert.ThrowsAsync<ConflitoException>(() => _cenario.Servico.RemoverParticipanteAsync(aulaId, VisitanteId));
        }

        // ---------- Edicao da aula (FR-018) ----------

        [Fact]
        public async Task Editar_a_aula_preserva_aluno_de_reposicao()
        {
            var aulaId = await NovaAulaAgendadaAsync();
            await _cenario.Servico.AdicionarParticipanteAsync(aulaId, VisitanteId);

            var resposta = await _cenario.Servico.AtualizarAsync(aulaId, Request() with { Descricao = "Revisao" });

            Assert.True(resposta.Alunos.Single(a => a.AlunoId == VisitanteId).Reposicao);
            Assert.Single(resposta.Alunos, a => a.AlunoId == MatriculadoId);
        }

        [Fact]
        public async Task Trocar_a_turma_mantem_reposicao_e_troca_os_matriculados()
        {
            const int OutroMatriculadoId = 4;
            var outro = CenarioCadastroAula.NovoAluno(OutroMatriculadoId, "Duda");
            _cenario.Aulas.AlunosConhecidos[OutroMatriculadoId] = outro;
            _cenario.Aulas.AlunosAtivosPorTurma[TurmaB] = new List<int> { OutroMatriculadoId };
            _cenario.TurmasRepo.Turmas.Add(new Turma { Id = TurmaB, Nome = "Turma B", Ativo = true });
            var aulaId = await NovaAulaAgendadaAsync();
            await _cenario.Servico.AdicionarParticipanteAsync(aulaId, VisitanteId);

            await _cenario.Servico.AtualizarAsync(aulaId, Request(turmaId: TurmaB));

            var participantes = Aula(aulaId).AulaAlunos;
            Assert.Equal(new[] { VisitanteId, OutroMatriculadoId }, participantes.Select(aa => aa.AlunoId).Order());
            Assert.True(participantes.Single(aa => aa.AlunoId == VisitanteId).Reposicao);
        }

        [Fact]
        public async Task Aluno_de_reposicao_que_virou_matriculado_nao_duplica()
        {
            var aulaId = await NovaAulaAgendadaAsync();
            await _cenario.Servico.AdicionarParticipanteAsync(aulaId, VisitanteId);
            _cenario.Aulas.AlunosAtivosPorTurma[CenarioCadastroAula.TurmaId].Add(VisitanteId);

            await _cenario.Servico.AtualizarAsync(aulaId, Request());

            var visitante = Assert.Single(Aula(aulaId).AulaAlunos, aa => aa.AlunoId == VisitanteId);
            Assert.False(visitante.Reposicao);
        }

        [Fact]
        public async Task Aula_que_vira_individual_perde_os_alunos_de_reposicao()
        {
            var aulaId = await NovaAulaAgendadaAsync();
            await _cenario.Servico.AdicionarParticipanteAsync(aulaId, VisitanteId);

            await _cenario.Servico.AtualizarAsync(aulaId, Request(turmaId: null, alunoId: MatriculadoId));

            Assert.Equal(new[] { MatriculadoId }, Aula(aulaId).AulaAlunos.Select(aa => aa.AlunoId));
        }

        // ---------- Cobranca (FR-019) ----------

        [Fact]
        public async Task Presenca_do_aluno_de_reposicao_consome_o_pacote_dele()
        {
            var pacote = new VinculoCobranca { Id = 7, AlunoId = VisitanteId, Modalidade = ModalidadeCobranca.Pacote, Valor = 500m, SaldoAulas = 3, Ativo = true };
            _cenario.Vinculos.Vinculos.Add(pacote);
            var aulaId = await NovaAulaAgendadaAsync();
            await _cenario.Servico.AdicionarParticipanteAsync(aulaId, VisitanteId);

            var resposta = await _cenario.Servico.RegistrarSessaoAsync(aulaId, new RegistrarSessaoRequest
            {
                Presencas = { [MatriculadoId] = true, [VisitanteId] = true }
            });

            Assert.True(resposta.Alunos.Single(a => a.AlunoId == VisitanteId).Presente);
            Assert.Equal(2, pacote.SaldoAulas);
            // So o matriculado (sem vinculo) gera conta a receber, pelo valor da aula.
            Assert.Equal(MatriculadoId, Assert.Single(_cenario.Pagamentos.Gerados).AlunoId);
        }
    }
}
