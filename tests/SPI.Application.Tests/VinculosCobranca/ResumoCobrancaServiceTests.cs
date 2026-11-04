using SPI.Application.Aulas.Services;
using SPI.Application.VinculosCobranca.Services;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using Xunit;

namespace SPI.Application.Tests.VinculosCobranca
{
    // specs/068: o selo de cobranca e calculado no servidor, em lote, com a MESMA regra que cobra a presenca
    // (RegraCobrancaPresenca.Decidir) e o MESMO progresso de pacote da spec 058.
    public class ResumoCobrancaServiceTests
    {
        private const int Turma1 = 10;
        private const int Turma2 = 20;

        private readonly FakeVinculoCobrancaRepositoryParaTeste _vinculos = new();
        private readonly FakeAulaRepositoryParaVinculo _aulas = new();
        private readonly ResumoCobrancaService _servico;

        public ResumoCobrancaServiceTests()
        {
            _vinculos.TurmasConhecidas.Add(new Turma { Id = Turma1, Nome = "Turma Um" });
            _vinculos.TurmasConhecidas.Add(new Turma { Id = Turma2, Nome = "Turma Dois" });
            _servico = new ResumoCobrancaService(_vinculos, _aulas);
        }

        private static Aluno NovoAluno(int id, decimal valorAula = 90m) => new() { Id = id, Nome = $"Aluno {id}", Ra = $"RA{id}", ValorAula = valorAula };

        private VinculoCobranca Semear(int id, int alunoId, ModalidadeCobranca modalidade, int? turmaId = null, decimal valor = 100m,
            int? saldo = null, int? total = null, bool ativo = true) =>
            _vinculos.Semear(new VinculoCobranca
            {
                Id = id, AlunoId = alunoId, Modalidade = modalidade, TurmaId = turmaId, Valor = valor,
                SaldoAulas = saldo, TotalAulasPacote = total, Ativo = ativo,
            });

        private void Presencas(int vinculoId, int quantidade)
        {
            for (var i = 0; i < quantidade; i++)
            {
                _aulas.Presencas.Add((vinculoId, 2026, 10));
            }
        }

        // ---------- lista ----------

        [Fact]
        public async Task Lista_o_principal_segue_Mensalidade_Pacote_Avulsa()
        {
            // Todos os vinculos no mesmo aluno so para provar a ordem (na pratica a mensalidade e exclusiva).
            Semear(1, 1, ModalidadeCobranca.Avulsa, Turma1, 80m);
            Semear(2, 1, ModalidadeCobranca.Pacote, Turma1, 300m, saldo: 8, total: 10);
            Semear(3, 1, ModalidadeCobranca.Mensalidade, null, 250m);

            var selo = (await _servico.ResumirParaListaAsync(new[] { NovoAluno(1) }))[1];

            Assert.Equal("Mensalidade", selo.Categoria);
            Assert.Equal(250m, selo.Valor);
            Assert.Equal(2, selo.OutrosVinculos);
            Assert.Equal(new[] { "Mensalidade", "Pacote", "Avulsa" }, selo.Detalhes.Select(d => d.Categoria));
        }

        [Fact]
        public async Task Lista_pacote_antes_da_avulsa_e_traz_a_turma_o_valor_e_os_outros()
        {
            Semear(1, 1, ModalidadeCobranca.Avulsa, Turma2, 80m);
            Semear(2, 1, ModalidadeCobranca.Pacote, Turma1, 300m, saldo: 8, total: 10);
            Presencas(2, 2);

            var selo = (await _servico.ResumirParaListaAsync(new[] { NovoAluno(1) }))[1];

            Assert.Equal("Pacote", selo.Categoria);
            Assert.Equal(300m, selo.Valor);
            Assert.Equal("Turma Um", selo.TurmaNome);
            Assert.Equal(8, selo.Progresso!.AulasRestantes);
            Assert.Equal(1, selo.OutrosVinculos);
            Assert.Contains(selo.Detalhes, d => d.Categoria == "Avulsa" && d.TurmaNome == "Turma Dois" && d.Valor == 80m);
        }

        [Fact]
        public async Task Lista_em_empate_da_mesma_categoria_vale_o_mais_recente()
        {
            Semear(1, 1, ModalidadeCobranca.Avulsa, Turma1, 70m);
            Semear(7, 1, ModalidadeCobranca.Avulsa, Turma2, 95m);

            var selo = (await _servico.ResumirParaListaAsync(new[] { NovoAluno(1) }))[1];

            Assert.Equal(95m, selo.Valor);
            Assert.Equal("Turma Dois", selo.TurmaNome);
            Assert.Equal(1, selo.OutrosVinculos);
        }

        [Fact]
        public async Task Lista_vinculo_inativo_e_ignorado()
        {
            Semear(1, 1, ModalidadeCobranca.Mensalidade, null, 250m, ativo: false);
            Semear(2, 1, ModalidadeCobranca.Avulsa, Turma1, 80m);

            var selo = (await _servico.ResumirParaListaAsync(new[] { NovoAluno(1) }))[1];

            Assert.Equal("Avulsa", selo.Categoria);
            Assert.Equal(0, selo.OutrosVinculos);
            Assert.Single(selo.Detalhes);
        }

        [Fact]
        public async Task Lista_aluno_sem_vinculo_mostra_o_valor_da_aula()
        {
            var selo = (await _servico.ResumirParaListaAsync(new[] { NovoAluno(1, valorAula: 65m) }))[1];

            Assert.Equal("SemVinculo", selo.Categoria);
            Assert.Equal(65m, selo.Valor);
            Assert.Null(selo.Progresso);
            Assert.Empty(selo.Detalhes);
        }

        [Fact]
        public async Task Lista_pacote_esgotado_total_estimado_e_sem_total()
        {
            Semear(1, 1, ModalidadeCobranca.Pacote, Turma1, 300m, saldo: 0, total: 10);
            Presencas(1, 10);
            Semear(2, 2, ModalidadeCobranca.Pacote, Turma1, 300m, saldo: 1, total: 10);
            Presencas(2, 9);
            Semear(3, 3, ModalidadeCobranca.Pacote, Turma1, 300m, saldo: null, total: null);

            var selos = await _servico.ResumirParaListaAsync(new[] { NovoAluno(1), NovoAluno(2), NovoAluno(3) });

            Assert.Equal("Esgotado", selos[1].Progresso!.Estado);
            Assert.Equal(0, selos[1].Progresso!.AulasRestantes);
            Assert.Equal("Atencao", selos[2].Progresso!.Estado);
            Assert.Equal(1, selos[2].Progresso!.AulasRestantes);
            Assert.Equal("SemTotal", selos[3].Progresso!.Estado);
            Assert.Null(selos[3].Progresso!.AulasRestantes);
        }

        // ---------- grupo: consistencia com a regra que cobra a presenca ----------

        [Fact]
        public async Task Grupo_o_selo_coincide_com_a_decisao_da_presenca_numa_matriz_de_vinculos()
        {
            // Cada aluno tem um conjunto diferente de vinculos ativos.
            Semear(1, 1, ModalidadeCobranca.Mensalidade, null, 250m);                                    // mensalidade
            Semear(2, 2, ModalidadeCobranca.Pacote, Turma1, 300m, saldo: 5, total: 10);                  // pacote da turma 1 com saldo
            Semear(3, 3, ModalidadeCobranca.Pacote, Turma2, 300m, saldo: 5, total: 10);                  // pacote de OUTRA turma
            Semear(4, 4, ModalidadeCobranca.Avulsa, Turma1, 80m);                                        // avulsa do contexto
            Semear(5, 5, ModalidadeCobranca.Pacote, Turma1, 300m, saldo: 0, total: 10);                  // esgotado + avulsa
            Semear(6, 5, ModalidadeCobranca.Avulsa, Turma1, 80m);
            Semear(7, 6, ModalidadeCobranca.Pacote, Turma1, 300m, saldo: 0, total: 10);                  // esgotado sem avulsa
            Semear(8, 8, ModalidadeCobranca.Pacote, null, 200m, saldo: 3, total: 5);                     // pacote de aulas individuais
            var alunos = Enumerable.Range(1, 8).Select(i => NovoAluno(i)).ToList();                      // aluno 7: sem vinculo

            foreach (var turmaId in new int?[] { Turma1, null })
            {
                var selos = await _servico.ResumirParaGrupoAsync(alunos, turmaId);

                foreach (var aluno in alunos)
                {
                    var ativos = _vinculos.Vinculos.Where(v => v.AlunoId == aluno.Id && v.Ativo).ToList();
                    var decisao = RegraCobrancaPresenca.Decidir(ativos, turmaId, DateOnly.FromDateTime(DateTime.Now));
                    var selo = selos[aluno.Id];

                    var categoriaEsperada = decisao.Tipo switch
                    {
                        TipoCobrancaPresenca.PlanoPeriodico => "Mensalidade",
                        TipoCobrancaPresenca.Pacote or TipoCobrancaPresenca.Bloqueada => "Pacote",
                        TipoCobrancaPresenca.Avulsa => "Avulsa",
                        _ => "SemVinculo",
                    };

                    Assert.True(categoriaEsperada == selo.Categoria, $"aluno {aluno.Id}, turma {turmaId}: esperava {categoriaEsperada} e veio {selo.Categoria}");
                    Assert.Equal(decisao.Tipo == TipoCobrancaPresenca.Avulsa && decisao.PacoteEsgotado, selo.AvisoPacoteEsgotado);
                    Assert.Equal(decisao.Tipo == TipoCobrancaPresenca.Bloqueada ? "Esgotado" : (decisao.Tipo == TipoCobrancaPresenca.Pacote ? selo.Progresso!.Estado : null), selo.Progresso?.Estado);
                }
            }
        }

        [Fact]
        public async Task Grupo_pacote_de_outra_turma_nao_vale_e_cai_na_avulsa_ou_sem_vinculo()
        {
            Semear(1, 1, ModalidadeCobranca.Pacote, Turma2, 300m, saldo: 5, total: 10);
            Semear(2, 2, ModalidadeCobranca.Pacote, Turma2, 300m, saldo: 5, total: 10);
            Semear(3, 2, ModalidadeCobranca.Avulsa, Turma1, 80m);

            var selos = await _servico.ResumirParaGrupoAsync(new[] { NovoAluno(1, 65m), NovoAluno(2) }, Turma1);

            Assert.Equal("SemVinculo", selos[1].Categoria);
            Assert.Equal(65m, selos[1].Valor);
            Assert.Equal("Avulsa", selos[2].Categoria);
            Assert.False(selos[2].AvisoPacoteEsgotado);
        }

        [Fact]
        public async Task Grupo_esgotado_com_avulsa_vira_avulsa_com_aviso_e_sem_avulsa_vira_pacote_esgotado()
        {
            Semear(1, 1, ModalidadeCobranca.Pacote, Turma1, 300m, saldo: 0, total: 10);
            Semear(2, 1, ModalidadeCobranca.Avulsa, Turma1, 80m);
            Semear(3, 2, ModalidadeCobranca.Pacote, Turma1, 300m, saldo: 0, total: 10);

            var selos = await _servico.ResumirParaGrupoAsync(new[] { NovoAluno(1), NovoAluno(2) }, Turma1);

            Assert.Equal("Avulsa", selos[1].Categoria);
            Assert.True(selos[1].AvisoPacoteEsgotado);
            Assert.Equal("Pacote", selos[2].Categoria);
            Assert.Equal("Esgotado", selos[2].Progresso!.Estado);
            Assert.Equal(0, selos[2].Progresso!.AulasRestantes);
        }

        [Fact]
        public async Task Grupo_nao_traz_turma_nem_outros_vinculos_nem_detalhes()
        {
            Semear(1, 1, ModalidadeCobranca.Pacote, Turma1, 300m, saldo: 5, total: 10);
            Semear(2, 1, ModalidadeCobranca.Avulsa, Turma1, 80m);

            var selo = (await _servico.ResumirParaGrupoAsync(new[] { NovoAluno(1) }, Turma1))[1];

            Assert.Null(selo.TurmaNome);
            Assert.Equal(0, selo.OutrosVinculos);
            Assert.Empty(selo.Detalhes);
        }

        // ---------- sem consulta por aluno (FR-006) ----------

        [Fact]
        public async Task Uma_lista_de_30_alunos_faz_uma_consulta_de_vinculos_e_no_maximo_uma_de_presencas()
        {
            var alunos = new List<Aluno>();
            for (var i = 1; i <= 30; i++)
            {
                alunos.Add(NovoAluno(i));
                Semear(i, i, i % 2 == 0 ? ModalidadeCobranca.Pacote : ModalidadeCobranca.Avulsa, Turma1, 100m, saldo: i % 2 == 0 ? 5 : null, total: i % 2 == 0 ? 10 : null);
            }

            var lista = await _servico.ResumirParaListaAsync(alunos);
            var grupo = await _servico.ResumirParaGrupoAsync(alunos, Turma1);

            Assert.Equal(30, lista.Count);
            Assert.Equal(30, grupo.Count);
            Assert.Equal(2, _vinculos.ChamadasListarAtivosDosAlunos); // uma por chamada (lista e grupo), nunca por aluno
            Assert.Equal(2, _aulas.ChamadasContarPresencas);
        }

        [Fact]
        public async Task Sem_pacote_nao_consulta_presencas_e_lista_vazia_nao_consulta_nada()
        {
            Semear(1, 1, ModalidadeCobranca.Avulsa, Turma1, 80m);

            await _servico.ResumirParaListaAsync(new[] { NovoAluno(1) });
            Assert.Equal(1, _vinculos.ChamadasListarAtivosDosAlunos);
            Assert.Equal(0, _aulas.ChamadasContarPresencas);

            var vazio = await _servico.ResumirParaListaAsync(Array.Empty<Aluno>());
            Assert.Empty(vazio);
            Assert.Equal(1, _vinculos.ChamadasListarAtivosDosAlunos);
        }
    }
}
