using SPI.Application.Aulas.Dtos;
using SPI.Application.Aulas.Services;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using Xunit;

namespace SPI.Application.Tests.Aulas
{
    // specs/041 (US2): bloqueio de presenca quando o Pacote do contexto da
    // aula esta com SaldoAulas == 0 -- FR-005/FR-006/FR-007/FR-010/FR-012.
    public class AulaServiceRegistrarSessaoPacoteEsgotadoTests
    {
        private const int AlunoEsgotadoId = 1;
        private const int AlunoComSaldoId = 2;
        private const int TurmaId = 10;

        private static Aluno NovoAluno(int id, string nome) =>
            new() { Id = id, Nome = nome, Ativo = true, ValorAula = 80m };

        private static Materia NovaMateria() => new() { Id = 1, Nome = "Matematica", Ativo = true };

        private static Aula NovaAula(int id, IEnumerable<Aluno> alunos, int? turmaId = TurmaId) => new()
        {
            Id = id,
            MateriaId = 1,
            Materia = NovaMateria(),
            ProfessorId = 1,
            TurmaId = turmaId,
            Status = "Agendada",
            DataInicio = new DateOnly(2026, 9, 15).AddDays(id),
            HoraInicio = new TimeOnly(10, 0),
            HoraFim = new TimeOnly(11, 0),
            Ativo = true,
            AulaAlunos = alunos.Select(a => new AulaAluno { AulaId = id, AlunoId = a.Id, Aluno = a, Presente = null }).ToList()
        };

        private static AulaService CriarServico(
            FakeAulaRepositoryParaAula aulas, FakePagamentoRepositoryParaAula pagamentos, FakeVinculoCobrancaRepositoryParaAula vinculos) =>
            new(
                aulas,
                new FakeAlunoRepositoryVazio(),
                new FakeTurmaRepositoryVazia(),
                new FakeMateriaRepositoryVazia(),
                new FakeLembreteServiceVazio(),
                pagamentos,
                new FakeCategoriaReceitaRepositoryParaAula
                {
                    AulaEmTurma = new CategoriaReceita { Id = 1, Nome = "Aula em turma", Ativo = true },
                    AulaParticular = new CategoriaReceita { Id = 2, Nome = "Aula particular", Ativo = true },
                },
                vinculos);

        private static VinculoCobranca NovoVinculoPacote(int alunoId, int? turmaId, int? saldoAulas, bool ativo = true, ModalidadeCobranca modalidade = ModalidadeCobranca.Pacote) =>
            new() { AlunoId = alunoId, TurmaId = turmaId, Modalidade = modalidade, Valor = 500m, SaldoAulas = saldoAulas, Ativo = ativo };

        // ---------- Positivos ----------

        [Fact]
        public async Task Presenca_com_saldo_zero_e_barrada_com_motivo_e_aviso()
        {
            var alunoEsgotado = NovoAluno(AlunoEsgotadoId, "Bruno");
            var aula = NovaAula(1, new[] { alunoEsgotado });
            var vinculos = new FakeVinculoCobrancaRepositoryParaAula();
            vinculos.Vinculos.Add(NovoVinculoPacote(AlunoEsgotadoId, TurmaId, saldoAulas: 0));
            var aulas = new FakeAulaRepositoryParaAula { AulaSemeada = aula };
            var pagamentos = new FakePagamentoRepositoryParaAula();
            var servico = CriarServico(aulas, pagamentos, vinculos);

            var resposta = await servico.RegistrarSessaoAsync(aula.Id, new RegistrarSessaoRequest { Presencas = { [AlunoEsgotadoId] = true } });

            var alunoResp = Assert.Single(resposta.Alunos);
            Assert.False(alunoResp.Presente);
            Assert.Equal("PacoteEsgotado", alunoResp.MotivoNaoRegistro);
            Assert.Equal("Realizada", resposta.Status);
            var aviso = Assert.Single(resposta.Avisos);
            Assert.Equal(
                "Pacote esgotado: Bruno não teve a presença registrada. Renove o pacote (edite o Vínculo de Cobrança) para liberar novos registros.",
                aviso);
        }

        [Fact]
        public async Task Aluno_com_saldo_positivo_processado_normalmente_mesmo_com_outro_bloqueado()
        {
            var alunoEsgotado = NovoAluno(AlunoEsgotadoId, "Bruno");
            var alunoComSaldo = NovoAluno(AlunoComSaldoId, "Ana");
            var aula = NovaAula(1, new[] { alunoEsgotado, alunoComSaldo });
            var vinculos = new FakeVinculoCobrancaRepositoryParaAula();
            vinculos.Vinculos.Add(NovoVinculoPacote(AlunoEsgotadoId, TurmaId, saldoAulas: 0));
            var vinculoComSaldo = NovoVinculoPacote(AlunoComSaldoId, TurmaId, saldoAulas: 5);
            vinculos.Vinculos.Add(vinculoComSaldo);
            var aulas = new FakeAulaRepositoryParaAula { AulaSemeada = aula };
            var pagamentos = new FakePagamentoRepositoryParaAula();
            var servico = CriarServico(aulas, pagamentos, vinculos);

            var resposta = await servico.RegistrarSessaoAsync(aula.Id, new RegistrarSessaoRequest { Presencas = { [AlunoEsgotadoId] = true, [AlunoComSaldoId] = true } });

            Assert.Equal("Realizada", resposta.Status);
            var respostaAna = resposta.Alunos.Single(a => a.AlunoId == AlunoComSaldoId);
            Assert.True(respostaAna.Presente);
            Assert.Null(respostaAna.MotivoNaoRegistro);
            Assert.Equal(1, alunoComSaldo.Frequencia);
            Assert.Equal(4, vinculoComSaldo.SaldoAulas);
        }

        [Fact]
        public async Task Todos_os_presentes_esgotados_ainda_fica_realizada()
        {
            var alunoA = NovoAluno(AlunoEsgotadoId, "Bruno");
            var alunoB = NovoAluno(AlunoComSaldoId, "Carla");
            var aula = NovaAula(1, new[] { alunoA, alunoB });
            var vinculos = new FakeVinculoCobrancaRepositoryParaAula();
            vinculos.Vinculos.Add(NovoVinculoPacote(AlunoEsgotadoId, TurmaId, saldoAulas: 0));
            vinculos.Vinculos.Add(NovoVinculoPacote(AlunoComSaldoId, TurmaId, saldoAulas: 0));
            var aulas = new FakeAulaRepositoryParaAula { AulaSemeada = aula };
            var pagamentos = new FakePagamentoRepositoryParaAula();
            var servico = CriarServico(aulas, pagamentos, vinculos);

            var resposta = await servico.RegistrarSessaoAsync(aula.Id, new RegistrarSessaoRequest { Presencas = { [AlunoEsgotadoId] = true, [AlunoComSaldoId] = true } });

            Assert.Equal("Realizada", resposta.Status);
            Assert.Equal(2, resposta.Avisos.Count);
            Assert.All(resposta.Alunos, a => Assert.False(a.Presente));
        }

        [Fact]
        public async Task Nunca_lanca_excecao_por_pacote_esgotado()
        {
            var alunoEsgotado = NovoAluno(AlunoEsgotadoId, "Bruno");
            var aula = NovaAula(1, new[] { alunoEsgotado });
            var vinculos = new FakeVinculoCobrancaRepositoryParaAula();
            vinculos.Vinculos.Add(NovoVinculoPacote(AlunoEsgotadoId, TurmaId, saldoAulas: 0));
            var aulas = new FakeAulaRepositoryParaAula { AulaSemeada = aula };
            var pagamentos = new FakePagamentoRepositoryParaAula();
            var servico = CriarServico(aulas, pagamentos, vinculos);

            var excecao = await Record.ExceptionAsync(() =>
                servico.RegistrarSessaoAsync(aula.Id, new RegistrarSessaoRequest { Presencas = { [AlunoEsgotadoId] = true } }));

            Assert.Null(excecao);
        }

        // ---------- Negativos (FR-007/FR-010) ----------

        [Fact]
        public async Task Ausencia_de_aluno_esgotado_e_aceita_como_falta_comum_sem_motivo()
        {
            var alunoEsgotado = NovoAluno(AlunoEsgotadoId, "Bruno");
            var aula = NovaAula(1, new[] { alunoEsgotado });
            var vinculos = new FakeVinculoCobrancaRepositoryParaAula();
            vinculos.Vinculos.Add(NovoVinculoPacote(AlunoEsgotadoId, TurmaId, saldoAulas: 0));
            var aulas = new FakeAulaRepositoryParaAula { AulaSemeada = aula };
            var pagamentos = new FakePagamentoRepositoryParaAula();
            var servico = CriarServico(aulas, pagamentos, vinculos);

            var resposta = await servico.RegistrarSessaoAsync(aula.Id, new RegistrarSessaoRequest { Presencas = { [AlunoEsgotadoId] = false } });

            var alunoResp = Assert.Single(resposta.Alunos);
            Assert.False(alunoResp.Presente);
            Assert.Null(alunoResp.MotivoNaoRegistro);
            Assert.Empty(resposta.Avisos);
        }

        [Fact]
        public async Task Saldo_nulo_nunca_informado_nao_bloqueia()
        {
            var aluno = NovoAluno(AlunoEsgotadoId, "Bruno");
            var aula = NovaAula(1, new[] { aluno });
            var vinculos = new FakeVinculoCobrancaRepositoryParaAula();
            vinculos.Vinculos.Add(NovoVinculoPacote(AlunoEsgotadoId, TurmaId, saldoAulas: null));
            var aulas = new FakeAulaRepositoryParaAula { AulaSemeada = aula };
            var pagamentos = new FakePagamentoRepositoryParaAula();
            var servico = CriarServico(aulas, pagamentos, vinculos);

            var resposta = await servico.RegistrarSessaoAsync(aula.Id, new RegistrarSessaoRequest { Presencas = { [AlunoEsgotadoId] = true } });

            var alunoResp = Assert.Single(resposta.Alunos);
            Assert.True(alunoResp.Presente);
            Assert.Null(alunoResp.MotivoNaoRegistro);
            Assert.Empty(resposta.Avisos);
        }

        [Fact]
        public async Task Vinculo_esgotado_em_outro_contexto_nao_bloqueia()
        {
            var aluno = NovoAluno(AlunoEsgotadoId, "Bruno");
            var aula = NovaAula(1, new[] { aluno }, turmaId: TurmaId);
            var vinculos = new FakeVinculoCobrancaRepositoryParaAula();
            // Vinculo esgotado em OUTRA turma (99), nao na TurmaId da aula.
            vinculos.Vinculos.Add(NovoVinculoPacote(AlunoEsgotadoId, 99, saldoAulas: 0));
            var aulas = new FakeAulaRepositoryParaAula { AulaSemeada = aula };
            var pagamentos = new FakePagamentoRepositoryParaAula();
            var servico = CriarServico(aulas, pagamentos, vinculos);

            var resposta = await servico.RegistrarSessaoAsync(aula.Id, new RegistrarSessaoRequest { Presencas = { [AlunoEsgotadoId] = true } });

            var alunoResp = Assert.Single(resposta.Alunos);
            Assert.True(alunoResp.Presente);
            Assert.Null(alunoResp.MotivoNaoRegistro);
        }

        [Fact]
        public async Task Vinculo_pacote_inativo_nao_bloqueia()
        {
            var aluno = NovoAluno(AlunoEsgotadoId, "Bruno");
            var aula = NovaAula(1, new[] { aluno });
            var vinculos = new FakeVinculoCobrancaRepositoryParaAula();
            vinculos.Vinculos.Add(NovoVinculoPacote(AlunoEsgotadoId, TurmaId, saldoAulas: 0, ativo: false));
            var aulas = new FakeAulaRepositoryParaAula { AulaSemeada = aula };
            var pagamentos = new FakePagamentoRepositoryParaAula();
            var servico = CriarServico(aulas, pagamentos, vinculos);

            var resposta = await servico.RegistrarSessaoAsync(aula.Id, new RegistrarSessaoRequest { Presencas = { [AlunoEsgotadoId] = true } });

            // FakeVinculoCobrancaRepositoryParaAula.ObterAtivoPorAlunoEContextoAsync ja
            // filtra v.Ativo, entao um vinculo inativo nunca e' resolvido -- aluno vira
            // "sem vinculo no contexto", cobrado por ValorAula (comportamento pre-existente).
            var alunoResp = Assert.Single(resposta.Alunos);
            Assert.True(alunoResp.Presente);
            Assert.Null(alunoResp.MotivoNaoRegistro);
        }

        [Theory]
        [InlineData(ModalidadeCobranca.Avulsa)]
        [InlineData(ModalidadeCobranca.Mensalidade)]
        public async Task Outras_modalidades_nao_bloqueiam_mesmo_com_saldo_zero_residual(ModalidadeCobranca modalidade)
        {
            var aluno = NovoAluno(AlunoEsgotadoId, "Bruno");
            var aula = NovaAula(1, new[] { aluno });
            var vinculos = new FakeVinculoCobrancaRepositoryParaAula();
            vinculos.Vinculos.Add(NovoVinculoPacote(AlunoEsgotadoId, TurmaId, saldoAulas: 0, modalidade: modalidade));
            var aulas = new FakeAulaRepositoryParaAula { AulaSemeada = aula };
            var pagamentos = new FakePagamentoRepositoryParaAula();
            var servico = CriarServico(aulas, pagamentos, vinculos);

            var resposta = await servico.RegistrarSessaoAsync(aula.Id, new RegistrarSessaoRequest { Presencas = { [AlunoEsgotadoId] = true } });

            var alunoResp = Assert.Single(resposta.Alunos);
            Assert.True(alunoResp.Presente);
            Assert.Null(alunoResp.MotivoNaoRegistro);
        }

        [Fact]
        public async Task Aluno_sem_vinculo_no_contexto_nao_bloqueia_e_e_cobrado_por_valor_aula()
        {
            var aluno = NovoAluno(AlunoEsgotadoId, "Bruno");
            var aula = NovaAula(1, new[] { aluno });
            var vinculos = new FakeVinculoCobrancaRepositoryParaAula();
            var aulas = new FakeAulaRepositoryParaAula { AulaSemeada = aula };
            var pagamentos = new FakePagamentoRepositoryParaAula();
            var servico = CriarServico(aulas, pagamentos, vinculos);

            var resposta = await servico.RegistrarSessaoAsync(aula.Id, new RegistrarSessaoRequest { Presencas = { [AlunoEsgotadoId] = true } });

            var alunoResp = Assert.Single(resposta.Alunos);
            Assert.True(alunoResp.Presente);
            Assert.Null(alunoResp.MotivoNaoRegistro);
            Assert.Single(pagamentos.Gerados);
        }

        [Fact]
        public async Task Aluno_barrado_nao_gera_pagamento_nem_altera_saldo_automaticamente()
        {
            var alunoEsgotado = NovoAluno(AlunoEsgotadoId, "Bruno");
            var aula = NovaAula(1, new[] { alunoEsgotado });
            var vinculos = new FakeVinculoCobrancaRepositoryParaAula();
            var vinculo = NovoVinculoPacote(AlunoEsgotadoId, TurmaId, saldoAulas: 0);
            vinculos.Vinculos.Add(vinculo);
            var aulas = new FakeAulaRepositoryParaAula { AulaSemeada = aula };
            var pagamentos = new FakePagamentoRepositoryParaAula();
            var servico = CriarServico(aulas, pagamentos, vinculos);

            await servico.RegistrarSessaoAsync(aula.Id, new RegistrarSessaoRequest { Presencas = { [AlunoEsgotadoId] = true } });

            Assert.Empty(pagamentos.Gerados);
            Assert.Equal(0, vinculo.SaldoAulas);
        }

        [Fact]
        public async Task Motivo_persistido_aparece_ao_consultar_a_aula_depois()
        {
            var alunoEsgotado = NovoAluno(AlunoEsgotadoId, "Bruno");
            var alunoComSaldo = NovoAluno(AlunoComSaldoId, "Ana");
            var aula = NovaAula(1, new[] { alunoEsgotado, alunoComSaldo });
            var vinculos = new FakeVinculoCobrancaRepositoryParaAula();
            vinculos.Vinculos.Add(NovoVinculoPacote(AlunoEsgotadoId, TurmaId, saldoAulas: 0));
            vinculos.Vinculos.Add(NovoVinculoPacote(AlunoComSaldoId, TurmaId, saldoAulas: 5));
            var aulas = new FakeAulaRepositoryParaAula { AulaSemeada = aula };
            var pagamentos = new FakePagamentoRepositoryParaAula();
            var servico = CriarServico(aulas, pagamentos, vinculos);

            await servico.RegistrarSessaoAsync(aula.Id, new RegistrarSessaoRequest { Presencas = { [AlunoEsgotadoId] = true, [AlunoComSaldoId] = true } });

            // "Consultar depois" = nova chamada usando o mesmo AulaSemeada (ja mutado
            // pelo registro), simulando um ObterPorIdAsync subsequente.
            var consulta = await servico.ObterPorIdAsync(aula.Id);

            var respostaBruno = consulta.Alunos.Single(a => a.AlunoId == AlunoEsgotadoId);
            Assert.Equal("PacoteEsgotado", respostaBruno.MotivoNaoRegistro);
            var respostaAna = consulta.Alunos.Single(a => a.AlunoId == AlunoComSaldoId);
            Assert.Null(respostaAna.MotivoNaoRegistro);
        }
    }
}
