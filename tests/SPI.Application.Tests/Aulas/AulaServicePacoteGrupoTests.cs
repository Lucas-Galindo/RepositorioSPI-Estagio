using SPI.Application.Aulas.Dtos;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using Xunit;

namespace SPI.Application.Tests.Aulas
{
    // specs/063 (US1, FR-007 a FR-013, FR-023): o pacote so vale no seu grupo e toda
    // presenca que manda cobrar gera conta a receber -- inclusive a do "outro aluno"
    // (reposicao) e a da aula criada ja realizada. A ordem de decisao em si e coberta
    // por RegraCobrancaPresencaTests; aqui o fluxo completo do AulaService.
    public class AulaServicePacoteGrupoTests
    {
        private const int MatriculadoId = 1;
        private const int VisitanteId = 2;
        private const int TurmaDaAula = CenarioCadastroAula.TurmaId;
        private const int OutraTurma = 20;

        private readonly CenarioCadastroAula _cenario;

        public AulaServicePacoteGrupoTests()
        {
            _cenario = new CenarioCadastroAula(CenarioCadastroAula.NovoAluno(MatriculadoId, "Ana"));

            // O visitante existe e esta ativo, mas nao e matriculado na turma da aula.
            var visitante = CenarioCadastroAula.NovoAluno(VisitanteId, "Bruno");
            _cenario.AlunosRepo.Alunos[VisitanteId] = visitante;
            _cenario.Aulas.AlunosConhecidos[VisitanteId] = visitante;
        }

        private VinculoCobranca Vinculo(int alunoId, ModalidadeCobranca modalidade, int? turmaId = null, int? saldo = null)
        {
            var vinculo = new VinculoCobranca
            {
                Id = _cenario.Vinculos.Vinculos.Count + 1,
                AlunoId = alunoId,
                Modalidade = modalidade,
                TurmaId = turmaId,
                SaldoAulas = saldo,
                Valor = 500m,
                Ativo = true
            };
            _cenario.Vinculos.Vinculos.Add(vinculo);
            return vinculo;
        }

        private static AulaRequest Request(int dias = 7, Dictionary<int, bool>? presencas = null) => new()
        {
            MateriaId = 1,
            TurmaId = TurmaDaAula,
            DataInicio = DateOnly.FromDateTime(DateTime.Now).AddDays(dias),
            HoraInicio = new TimeOnly(10, 0),
            HoraFim = new TimeOnly(11, 0),
            Presencas = presencas,
        };

        // Aula agendada com o matriculado e o visitante (reposicao), ja com a sessao registrada.
        private async Task<AulaResponse> RegistrarComVisitanteAsync(bool visitantePresente = true)
        {
            var aulaId = (await _cenario.Servico.CadastrarAsync(1, Request())).Id;
            await _cenario.Servico.AdicionarParticipanteAsync(aulaId, VisitanteId);
            return await _cenario.Servico.RegistrarSessaoAsync(aulaId, new RegistrarSessaoRequest
            {
                Presencas = { [MatriculadoId] = true, [VisitanteId] = visitantePresente }
            });
        }

        // ---- "Outros alunos" (reposicao): quem decide e a turma da aula ----

        // FR-008/FR-023: pacote de OUTRA turma nao e consumido; o visitante paga avulso e a conta nasce.
        [Fact]
        public async Task Outro_aluno_com_pacote_de_outra_turma_nao_consome_e_gera_conta()
        {
            var pacote = Vinculo(VisitanteId, ModalidadeCobranca.Pacote, OutraTurma, saldo: 5);

            var resposta = await RegistrarComVisitanteAsync();

            Assert.True(resposta.Alunos.Single(a => a.AlunoId == VisitanteId).Presente);
            Assert.Equal(5, pacote.SaldoAulas);
            Assert.Contains(_cenario.Pagamentos.Gerados, p => p.AlunoId == VisitanteId && p.ValorFinal == 80m);
        }

        // FR-008/FR-023: pacote de aulas individuais nao vale em aula de turma.
        [Fact]
        public async Task Outro_aluno_com_pacote_de_aulas_individuais_nao_consome_e_gera_conta()
        {
            var pacote = Vinculo(VisitanteId, ModalidadeCobranca.Pacote, turmaId: null, saldo: 5);

            await RegistrarComVisitanteAsync();

            Assert.Equal(5, pacote.SaldoAulas);
            Assert.Contains(_cenario.Pagamentos.Gerados, p => p.AlunoId == VisitanteId && p.ValorFinal == 80m);
        }

        // Caso do aluno que saiu da turma (nao e mais matriculado) mas tem o pacote dela ativo:
        // a aula e do grupo do pacote, entao consome, sem conta.
        [Fact]
        public async Task Aluno_que_saiu_da_turma_participa_como_outro_aluno_e_consome_o_pacote_dela()
        {
            var pacote = Vinculo(VisitanteId, ModalidadeCobranca.Pacote, TurmaDaAula, saldo: 5);

            await RegistrarComVisitanteAsync();

            Assert.Equal(4, pacote.SaldoAulas);
            Assert.DoesNotContain(_cenario.Pagamentos.Gerados, p => p.AlunoId == VisitanteId);
        }

        // Mensalidade acessa tudo sem custo adicional, inclusive como "outro aluno".
        [Fact]
        public async Task Outro_aluno_com_mensalidade_nao_gera_conta()
        {
            Vinculo(VisitanteId, ModalidadeCobranca.Mensalidade);

            await RegistrarComVisitanteAsync();

            Assert.DoesNotContain(_cenario.Pagamentos.Gerados, p => p.AlunoId == VisitanteId);
        }

        // Aluno avulso (sem pacote) como "outro aluno": cobrado avulso.
        [Fact]
        public async Task Outro_aluno_sem_vinculo_gera_conta_pelo_valor_da_aula()
        {
            await RegistrarComVisitanteAsync();

            Assert.Contains(_cenario.Pagamentos.Gerados, p => p.AlunoId == VisitanteId && p.ValorFinal == 80m);
        }

        // FR-009: falta nunca consome nem cobra.
        [Fact]
        public async Task Falta_do_outro_aluno_nao_consome_nem_gera_conta()
        {
            var pacote = Vinculo(VisitanteId, ModalidadeCobranca.Pacote, TurmaDaAula, saldo: 5);

            var resposta = await RegistrarComVisitanteAsync(visitantePresente: false);

            Assert.False(resposta.Alunos.Single(a => a.AlunoId == VisitanteId).Presente);
            Assert.Equal(5, pacote.SaldoAulas);
            Assert.DoesNotContain(_cenario.Pagamentos.Gerados, p => p.AlunoId == VisitanteId);
        }

        // ---- FR-008: aluno matriculado, aula da propria turma e aula de outra turma ----

        // Pacote esgotado de outra turma nao barra a presenca do matriculado.
        [Fact]
        public async Task Pacote_esgotado_de_outra_turma_nao_barra_o_matriculado()
        {
            Vinculo(MatriculadoId, ModalidadeCobranca.Pacote, OutraTurma, saldo: 0);

            var aulaId = (await _cenario.Servico.CadastrarAsync(1, Request())).Id;
            var resposta = await _cenario.Servico.RegistrarSessaoAsync(aulaId, new RegistrarSessaoRequest { Presencas = { [MatriculadoId] = true } });

            var ana = Assert.Single(resposta.Alunos);
            Assert.True(ana.Presente);
            Assert.Null(ana.MotivoNaoRegistro);
            Assert.Empty(resposta.Avisos);
            Assert.Equal(80m, Assert.Single(_cenario.Pagamentos.Gerados).ValorFinal);
        }

        // Pacote da propria turma esgotado e sem avulsa: barra (regra da spec 041 mantida).
        [Fact]
        public async Task Pacote_da_propria_turma_esgotado_barra_a_presenca()
        {
            Vinculo(MatriculadoId, ModalidadeCobranca.Pacote, TurmaDaAula, saldo: 0);

            var aulaId = (await _cenario.Servico.CadastrarAsync(1, Request())).Id;
            var resposta = await _cenario.Servico.RegistrarSessaoAsync(aulaId, new RegistrarSessaoRequest { Presencas = { [MatriculadoId] = true } });

            var ana = Assert.Single(resposta.Alunos);
            Assert.False(ana.Presente);
            Assert.Equal("PacoteEsgotado", ana.MotivoNaoRegistro);
            Assert.Empty(_cenario.Pagamentos.Gerados);
        }

        // ---- FR-023: aula criada ja realizada (retroativa) segue a mesma regra ----

        [Fact]
        public async Task Aula_retroativa_gera_conta_para_pacote_de_outra_turma_e_consome_o_da_propria()
        {
            var pacoteOutra = Vinculo(MatriculadoId, ModalidadeCobranca.Pacote, OutraTurma, saldo: 5);

            var resposta = await _cenario.Servico.CadastrarAsync(1, Request(dias: -1, presencas: new Dictionary<int, bool> { [MatriculadoId] = true }));

            Assert.Equal("Realizada", resposta.Status);
            Assert.Equal(5, pacoteOutra.SaldoAulas);
            Assert.Equal(80m, Assert.Single(_cenario.Pagamentos.Gerados).ValorFinal);
        }

        [Fact]
        public async Task Aula_retroativa_consome_o_pacote_da_propria_turma_sem_conta()
        {
            var pacote = Vinculo(MatriculadoId, ModalidadeCobranca.Pacote, TurmaDaAula, saldo: 5);

            await _cenario.Servico.CadastrarAsync(1, Request(dias: -1, presencas: new Dictionary<int, bool> { [MatriculadoId] = true }));

            Assert.Equal(4, pacote.SaldoAulas);
            Assert.Empty(_cenario.Pagamentos.Gerados);
        }

        // ---- FR-013: reposicao continua funcionando ----

        [Theory]
        [InlineData(ModalidadeCobranca.Pacote)]
        [InlineData(ModalidadeCobranca.Mensalidade)]
        public async Task Reposicao_continua_adicionando_e_removendo_aluno_de_pacote_e_de_mensalidade(ModalidadeCobranca modalidade)
        {
            Vinculo(VisitanteId, modalidade, turmaId: null, saldo: modalidade == ModalidadeCobranca.Pacote ? 5 : null);
            var aulaId = (await _cenario.Servico.CadastrarAsync(1, Request())).Id;

            var adicionada = await _cenario.Servico.AdicionarParticipanteAsync(aulaId, VisitanteId);
            Assert.True(adicionada.Alunos.Single(a => a.AlunoId == VisitanteId).Reposicao);

            var removida = await _cenario.Servico.RemoverParticipanteAsync(aulaId, VisitanteId);
            Assert.DoesNotContain(removida.Alunos, a => a.AlunoId == VisitanteId);
        }
    }
}
