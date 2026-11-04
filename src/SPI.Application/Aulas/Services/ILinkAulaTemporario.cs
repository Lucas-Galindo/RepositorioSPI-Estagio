namespace SPI.Application.Aulas.Services
{
    // specs/067: link de reuniao de uma aula, guardado SOMENTE em memoria do servidor (nunca em banco
    // nem em arquivo) e valido ate o fim da aula. Se a API reiniciar, os links ainda nao usados se
    // perdem (risco aceito pelo usuario; a tela avisa). O link pode ter senha de reuniao: nenhuma
    // implementacao pode registra-lo em log nem em mensagem de excecao.
    public interface ILinkAulaTemporario
    {
        // Nulo se nao existe ou se a aula ja terminou.
        string? Obter(int aulaId);

        // Guarda (ou troca) o link da aula. fimDaAula e a hora de parede de Sao Paulo (data + hora de
        // fim da aula). Se o fim ja passou, nada e guardado e qualquer link anterior e descartado.
        void Definir(int aulaId, string link, DateTime fimDaAula);

        void Remover(int aulaId);
    }
}
