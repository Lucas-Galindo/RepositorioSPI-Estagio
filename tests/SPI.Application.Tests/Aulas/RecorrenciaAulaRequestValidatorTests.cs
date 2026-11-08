using FluentValidation.TestHelper;
using SPI.Application.Aulas.Dtos;
using SPI.Application.Aulas.Validators;
using Xunit;

namespace SPI.Application.Tests.Aulas
{
    // specs/077: formato do pedido de serie.
    public class RecorrenciaAulaRequestValidatorTests
    {
        private readonly RecorrenciaAulaRequestValidator _validator = new();

        private static HorarioSerieRequest Horario(int dia, int inicio = 14, int fim = 15) =>
            new() { DiaSemana = dia, HoraInicio = new TimeOnly(inicio, 0), HoraFim = new TimeOnly(fim, 0) };

        private static RecorrenciaAulaRequest Valido() => new()
        {
            MateriaId = 1, TurmaId = 10, DataInicial = new DateOnly(2026, 11, 9),
            Horarios = { Horario(2) }, QuantidadeAulas = 10,
        };

        [Fact]
        public void Pedido_valido_passa()
        {
            _validator.TestValidate(Valido()).ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Sem_horarios_e_recusado()
        {
            var r = Valido(); r.Horarios.Clear();

            _validator.TestValidate(r).ShouldHaveValidationErrorFor(x => x.Horarios);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(7)]
        public void Dia_da_semana_fora_de_0_a_6_e_recusado(int dia)
        {
            var r = Valido(); r.Horarios[0] = Horario(dia);

            _validator.TestValidate(r).ShouldHaveValidationErrorFor("Horarios[0].DiaSemana");
        }

        [Fact]
        public void Fim_nao_posterior_ao_inicio_e_recusado()
        {
            var r = Valido(); r.Horarios[0] = Horario(2, 15, 15);

            _validator.TestValidate(r).ShouldHaveValidationErrorFor("Horarios[0].HoraFim");
        }

        [Fact]
        public void Horarios_sobrepostos_no_mesmo_dia_sao_recusados()
        {
            var r = Valido(); r.Horarios.Add(Horario(2, 14, 16));

            _validator.TestValidate(r).ShouldHaveValidationErrorFor(x => x.Horarios);
        }

        [Fact]
        public void Horarios_encostados_no_mesmo_dia_sao_aceitos()
        {
            var r = Valido(); r.Horarios.Add(Horario(2, 15, 16));

            _validator.TestValidate(r).ShouldNotHaveValidationErrorFor(x => x.Horarios);
        }

        [Fact]
        public void Mesmo_horario_em_dias_diferentes_e_aceito()
        {
            var r = Valido(); r.Horarios.Add(Horario(4));

            _validator.TestValidate(r).ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Quantidade_e_data_final_juntas_sao_recusadas()
        {
            var r = Valido(); r.DataFinal = new DateOnly(2026, 12, 31);

            _validator.TestValidate(r).ShouldHaveValidationErrorFor(x => x);
        }

        [Fact]
        public void Sem_quantidade_nem_data_final_e_recusado()
        {
            var r = Valido(); r.QuantidadeAulas = null;

            _validator.TestValidate(r).ShouldHaveValidationErrorFor(x => x);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(61)]
        public void Quantidade_fora_de_1_a_60_e_recusada(int quantidade)
        {
            var r = Valido(); r.QuantidadeAulas = quantidade;

            _validator.TestValidate(r).ShouldHaveValidationErrorFor("QuantidadeAulas.Value");
        }

        [Theory]
        [InlineData(1)]
        [InlineData(60)]
        public void Quantidade_nos_limites_e_aceita(int quantidade)
        {
            var r = Valido(); r.QuantidadeAulas = quantidade;

            _validator.TestValidate(r).ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Data_final_antes_da_inicial_e_recusada()
        {
            var r = Valido(); r.QuantidadeAulas = null; r.DataFinal = new DateOnly(2026, 11, 1);

            _validator.TestValidate(r).ShouldHaveValidationErrorFor("DataFinal.Value");
        }

        [Fact]
        public void Turma_e_aluno_juntos_sao_recusados()
        {
            var r = Valido(); r.AlunoId = 3;

            _validator.TestValidate(r).ShouldHaveValidationErrorFor(x => x.AlunoId);
        }

        [Fact]
        public void Sem_turma_o_aluno_e_obrigatorio()
        {
            var r = Valido(); r.TurmaId = null;

            _validator.TestValidate(r).ShouldHaveValidationErrorFor(x => x.AlunoId);
        }

        [Fact]
        public void Materia_e_obrigatoria()
        {
            var r = Valido(); r.MateriaId = 0;

            _validator.TestValidate(r).ShouldHaveValidationErrorFor(x => x.MateriaId);
        }
    }
}
