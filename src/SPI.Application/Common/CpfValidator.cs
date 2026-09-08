using System.Text.RegularExpressions;

namespace SPI.Application.Common
{
    // Estoria 1 (ERS): "O sistema valida que o CPF ... possui formato e
    // digito verificador validos." Algoritmo padrao de digito verificador do CPF.
    public static class CpfValidator
    {
        private static readonly Regex ApenasDigitos = new(@"\D", RegexOptions.Compiled);

        public static bool EhValido(string? cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf))
            {
                return false;
            }

            var digitos = ApenasDigitos.Replace(cpf, string.Empty);
            if (digitos.Length != 11 || digitos.Distinct().Count() == 1)
            {
                return false;
            }

            var numeros = digitos.Select(c => c - '0').ToArray();

            var primeiroDigito = CalcularDigito(numeros, 9);
            var segundoDigito = CalcularDigito(numeros, 10);

            return numeros[9] == primeiroDigito && numeros[10] == segundoDigito;
        }

        private static int CalcularDigito(int[] numeros, int quantidade)
        {
            var soma = 0;
            var multiplicador = quantidade + 1;

            for (var i = 0; i < quantidade; i++)
            {
                soma += numeros[i] * multiplicador;
                multiplicador--;
            }

            var resto = soma % 11;
            return resto < 2 ? 0 : 11 - resto;
        }
    }
}
