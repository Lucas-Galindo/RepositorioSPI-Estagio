using FluentValidation;
using SPI.Application.Common;
using SPI.Application.Professores.Dtos;

namespace SPI.Application.Professores.Validators
{
    public class AlterarSenhaProfessorRequestValidator : AbstractValidator<AlterarSenhaProfessorRequest>
    {

        //RuleFor seleciona propriedade para indicar qual campo
        //ou prorpiedade da classe sera validado

        //Classe para pedido de alteração de senha do professor
        public AlterarSenhaProfessorRequestValidator()
        {
            //Campo senha atual nao é enviado como vazio ou nulo
            RuleFor(x => x.SenhaAtual)
                .NotEmpty().WithMessage("O campo SenhaAtual e obrigatorio.");

            //Obriga a colocar uma regra personalizada
            RuleFor(x => x.NovaSenha).SenhaForte();
        }
    }
}
