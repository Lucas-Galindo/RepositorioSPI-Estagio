# Implementation Plan: Melhoria do Código sem Mudar o Comportamento

**Branch**: `065-melhoria-de-codigo` | **Date**: 2026-10-31 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/065-melhoria-de-codigo/spec.md`

## Summary

Refatoração de **comportamento preservado**. A ordem é pensada para provar a igualdade antes de mexer:

1. **Rede de segurança primeiro**: instantâneo das respostas da API e capturas de tela de antes, mais a suíte de testes (464) e o build como linha de base.
2. **Código morto** (risco menor): CSS sem uso, exports sem uso, comentário de teste obsoleto, `PackageReference` desnecessário.
3. **Tratamento de erro centralizado** (maior ganho): um filtro de exceção na API decide 404, 409 ou 500 **pelos `ProducesResponseType` que cada ação já declara**; as ações perdem os blocos `try/catch` repetidos. As respostas ficam idênticas porque 84 das 87 ações já declaram exatamente o que capturam e as 3 restantes devolvem `NotFound()` direto, sem exceção.
4. **Lógica repetida** nos serviços (decisão de turma do vínculo; parte comum de aluno).
5. **Comentários** que explicam regras (diff só de comentário).
6. **Telas copiadas** (opcional, só se as capturas forem idênticas).

Nenhum arquivo do banco muda. A zona da spec 064 fica intocada (FR-010).

## Technical Context

**Language/Version**: C# / .NET 10; TypeScript / React 19 / Next.js 16

**Primary Dependencies**: nenhuma nova. Uma remoção: `Microsoft.Extensions.Logging.Abstractions` do `SPI.Application.csproj`, se o build provar que é desnecessária.

**Storage**: MySQL, **sem mudança**

**Testing**: suíte xUnit existente (sem alterar expectativas); testes novos só do filtro de erros (o projeto de testes passa a referenciar `SPI.Api`); instantâneo da API e capturas de tela com scripts fora do repositório

**Project Type**: web application

**Constraints**: respostas HTTP idênticas; mensagens e telas idênticas; nenhuma regra de negócio alterada; sem commit até o usuário pedir

**Scale/Scope**: 17 controllers (87 ações), ~12 métodos a comentar, 18 classes CSS, 3 exports

## Constitution Check

- **I. Exclusão lógica**: sem impacto (nada apagado do banco).
- **II. Regra de negócio única no backend**: **reforçado**: o mapeamento de erros de negócio para HTTP passa a viver em um lugar só; as decisões repetidas nos serviços passam a existir uma vez.
- **III. Nada configurável sem efeito**: sem impacto; nenhuma opção nova.
- **IV. Segredos e perfil**: sem impacto; o filtro não registra nem expõe mais do que o código de hoje (mesmo corpo da resposta).
- **V. Documentação retroativa**: nota nas specs que mencionem "try/catch nos controllers" só se existir; nenhuma regra de negócio muda.

Sem violações. Pós-design: sem mudança.

## Project Structure

### Documentação (esta feature)

```text
specs/065-melhoria-de-codigo/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── tasks.md
├── analysis.md
├── checklists/requirements.md
└── contracts/erros-da-api.md
```

### Código-fonte

```text
src/SPI.Api/
├── Filters/ErrosDeNegocioFilter.cs              # NOVO (IExceptionFilter)
├── Program.cs                                   # registra o filtro em AddControllers
└── Controllers/*Controller.cs                   # 17 arquivos: sem try/catch repetido; + [ProducesResponseType(409)] onde faltava

src/SPI.Application/
├── SPI.Application.csproj                       # -PackageReference desnecessário (se o build provar)
├── VinculosCobranca/Services/VinculoCobrancaService.cs   # decisão de turma em um método
├── Alunos/ (Dtos, Validators)                   # parte comum (se der sem mudar mensagens)
└── ...Services (PagamentoService, AlunoService, FinanceiroService, RelatorioService, LembretePagamentoService)  # só comentários
src/SPI.Infrastructure/
└── Repositories + BackgroundServices            # só comentários

tests/SPI.Application.Tests/
├── SPI.Application.Tests.csproj                 # + ProjectReference para SPI.Api
├── Api/ErrosDeNegocioFilterTests.cs             # NOVO
└── Aulas/AulaServiceRegistrarSessaoPacoteRenovacaoTests.cs   # comentário corrigido

frontend/
├── styles/dashboard.css, styles/login.css       # -18 classes sem uso
├── lib/jwt.ts, lib/paleta.ts                    # exports sem uso
└── app/(app)/financeiro/** e components/        # (P3, opcional) trechos compartilhados
```

**Structure Decision**: aplicação web existente. Um arquivo novo no backend (o filtro), um de teste e nenhum no frontend, exceto se a US5 extrair componentes provados idênticos.

## Complexity Tracking

| Ponto | Por quê | Alternativa mais simples descartada |
|---|---|---|
| O filtro lê os `ProducesResponseType` de cada ação | É o que reproduz **exatamente** o comportamento atual: hoje só as ações que capturam "não encontrado" ou "conflito" respondem 404 ou 409; as demais viram 500 | Mapear sempre 404 e 409: seria melhor, mas **muda** respostas de ações que hoje devolvem 500 (fora do pedido) |
| Projeto de testes passa a referenciar `SPI.Api` | Único jeito de testar o filtro sem subir a API | Testar só pelo instantâneo: sem teste automatizado do filtro |
