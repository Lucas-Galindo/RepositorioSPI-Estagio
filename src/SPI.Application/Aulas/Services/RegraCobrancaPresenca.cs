using SPI.Domain.Entities;
using SPI.Domain.Enums;

namespace SPI.Application.Aulas.Services
{
    public enum TipoCobrancaPresenca
    {
        // Aluno com mensalidade: a presenca nao gera cobranca.
        Mensalidade,
        // Consome o pacote do grupo da aula (ou, se o saldo nunca foi informado, so registra o uso).
        Pacote,
        // Gera conta a receber pelo valor da Avulsa do contexto da aula.
        Avulsa,
        // Pacote esgotado e sem Avulsa para cair: a presenca e barrada (specs/041).
        Bloqueada,
        // Nenhum vinculo aplicavel: cobra o "Valor da aula" do aluno.
        SemVinculo,
    }

    // Resultado da decisao para uma presenca. Vinculo e o vinculo efetivamente
    // aplicado (gravado em AulaAluno.VinculoCobrancaId); PacoteEsgotado indica que a
    // presenca caiu na Avulsa porque o pacote do aluno acabou (gera um aviso).
    public record DecisaoCobranca(TipoCobrancaPresenca Tipo, VinculoCobranca? Vinculo, bool PacoteEsgotado = false);

    // specs/056: decide, para UMA presenca confirmada, qual vinculo de cobranca vale.
    // Funcao pura (nao acessa banco) e o unico lugar com essa ordem de prioridade --
    // AulaService so busca os vinculos ativos do aluno e executa o efeito decidido.
    public static class RegraCobrancaPresenca
    {
        public static DecisaoCobranca Decidir(IReadOnlyCollection<VinculoCobranca> ativosDoAluno, int? turmaIdDaAula)
        {
            // Com conflito herdado da regra antiga pode haver mais de um vinculo do
            // mesmo tipo; nesse caso vale sempre o mais antigo (menor Id).
            var porAntiguidade = ativosDoAluno.Where(v => v.Ativo).OrderBy(v => v.Id).ToList();

            // 1) Mensalidade cobre tudo, e prevalece ate sobre um pacote herdado,
            //    para nunca cobrar o aluno duas vezes.
            var mensalidade = porAntiguidade.FirstOrDefault(v => v.Modalidade == ModalidadeCobranca.Mensalidade);
            if (mensalidade is not null)
            {
                return new DecisaoCobranca(TipoCobrancaPresenca.Mensalidade, mensalidade);
            }

            // 2) Pacote so vale no SEU grupo (specs/063): o pacote de uma turma, so em aula
            //    dessa turma; o pacote de aulas individuais (sem turma), so em aula
            //    individual. O que decide e a turma da aula, nao a matricula do aluno.
            //    Fora do grupo o pacote nao interfere (nem consome, nem barra, nem avisa):
            //    a presenca cai na Avulsa do contexto ou no valor da aula.
            //    Dentro do grupo: primeiro um pacote com saldo; depois um pacote cujo saldo
            //    nunca foi informado (comportamento antigo preservado: nao cobra nem
            //    bloqueia). Ha no maximo um pacote ativo por grupo, entao nao ha escolha
            //    entre pacotes (dado herdado com duplicado desempata pelo menor Id).
            var pacotes = porAntiguidade
                .Where(v => v.Modalidade == ModalidadeCobranca.Pacote && v.TurmaId == turmaIdDaAula)
                .ToList();
            var pacoteUsavel = pacotes.FirstOrDefault(p => p.SaldoAulas > 0) ?? pacotes.FirstOrDefault(p => p.SaldoAulas is null);
            if (pacoteUsavel is not null)
            {
                return new DecisaoCobranca(TipoCobrancaPresenca.Pacote, pacoteUsavel);
            }

            // 3) Avulsa vale so no proprio contexto: a mesma turma da aula, ou
            //    atendimento individual (turma nula dos dois lados).
            var avulsa = porAntiguidade.FirstOrDefault(v => v.Modalidade == ModalidadeCobranca.Avulsa && v.TurmaId == turmaIdDaAula);
            if (avulsa is not null)
            {
                return new DecisaoCobranca(TipoCobrancaPresenca.Avulsa, avulsa, PacoteEsgotado: pacotes.Count > 0);
            }

            // 4) Tem pacote, mas esgotado, e nada para cair: barra a presenca.
            if (pacotes.Count > 0)
            {
                return new DecisaoCobranca(TipoCobrancaPresenca.Bloqueada, pacotes[0]);
            }

            // 5) Nenhum vinculo aplicavel.
            return new DecisaoCobranca(TipoCobrancaPresenca.SemVinculo, null);
        }
    }
}
