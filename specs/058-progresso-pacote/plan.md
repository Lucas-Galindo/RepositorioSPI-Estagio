# Implementation Plan: Progresso de Pacote de Aulas

**Branch**: `058-progresso-pacote` | **Date**: 2026-10-19 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/058-progresso-pacote/spec.md`

## Summary

A função pura `ProgressoPacote.Calcular(vinculo, aulasDadas)` passa a ser o único cálculo de total, total estimado, dadas, restantes e estado (Princípio II). As aulas dadas vêm da contagem de presenças por vínculo (`aula_aluno.vinculo_cobranca_id`), feita **em lote**: uma consulta para todos os pacotes da tela.

O cálculo é usado por:
- **`VinculoCobrancaService`**: listagem e detalhe de vínculos, validação do total na edição, e saldo inicial igual ao total no cadastro;
- **novo endpoint**: aulas dadas de um pacote;
- **`DashboardService`**: a seção "Pacotes de aulas", com indicadores e lista de renovação, que substitui o "Pacotes em atenção" baseado em saldo.

No frontend:
- **formulário de pacote**: ganha "Total de aulas";
- **ficha do aluno**: mostra barra de progresso, estado e a lista de aulas;
- **Home**: os indicadores e a lista de renovação passam a vir do novo cálculo, com filtro de aluno e período.

Sem tabela nem coluna nova (FR-011).

## Technical Context

**Language/Version**: C# / .NET 10; TypeScript / React 19 / Next.js 16

**Primary Dependencies**: nenhuma nova

**Storage**: MySQL, sem migração

**Testing**: xUnit com fakes (cálculo, vínculo, dashboard); quickstart contra a API local

**Project Type**: web application

**Constraints**: a mesma função de cálculo na ficha e na Home; contagem de presenças em lote, sem uma consulta por pacote

**Scale/Scope**: dezenas de pacotes por professora

## Constitution Check

- **I. Exclusão lógica**: pacotes inativos continuam visíveis (excluídos/concluídos); nada é apagado.
- **II. Regra no backend**: total, restantes e estado vêm do backend (`ProgressoPacote`); o frontend só desenha a barra com os números recebidos.
- **III. Nada configurável sem efeito**: o campo "Total de aulas" passa a ser editável e usado de fato. Antes ele existia só no backend, sem tela.
- **IV**: endpoints sob o perfil Professor, como os demais de vínculo e dashboard.
- **V. Documentação retroativa**:
  - a spec 052 recebe nota: "aulas dadas" do pacote passou a ser contagem de presenças;
  - a spec 041 recebe nota: o painel da Home passou a usar o progresso, e o bloqueio continua no saldo.

**Resultado**: PASS.

## Project Structure

```text
src/SPI.Application/VinculosCobranca/Services/ProgressoPacote.cs        # NOVO (cálculo único)
src/SPI.Application/VinculosCobranca/Dtos/ProgressoPacoteResponse.cs    # NOVO
src/SPI.Application/VinculosCobranca/Dtos/AulaDoPacoteResponse.cs       # NOVO
src/SPI.Application/VinculosCobranca/Dtos/VinculoCobrancaResponse.cs    # AulasDadas -> Progresso
src/SPI.Application/VinculosCobranca/Services/(I)VinculoCobrancaService.cs  # progresso em lote, saldo inicial, validação, aulas do pacote
src/SPI.Application/Dashboard/Services/DashboardService.cs + Dtos       # seção Pacotes (indicadores + renovação)
src/SPI.Domain/Repositories/IAulaRepository.cs                          # contagem em lote + presenças do vínculo
src/SPI.Domain/Repositories/IVinculoCobrancaRepository.cs               # ListarPacotesAsync(alunoId?)
src/SPI.Infrastructure/Repositories/{AulaRepository,VinculoCobrancaRepository}.cs
src/SPI.Api/Controllers/{VinculosCobrancaController,DashboardController}.cs
tests/SPI.Application.Tests/...                                         # ProgressoPacote, vínculo, dashboard
frontend/lib/api/{vinculosCobranca,dashboard}.ts
frontend/components/alunos/{VinculoCobrancaFormModal,VinculosCobrancaSection}.tsx
frontend/components/dashboard/PacotesEmAtencaoPanel.tsx -> PacotesPanel (indicadores + renovação)
frontend/app/(app)/dashboard/page.tsx
frontend/styles/dashboard.css                                           # barra de progresso (tokens)
specs/041-pacote-esgotado/spec.md, specs/052-auditoria-vinculo-cobranca/spec.md  # notas
```

## Complexity Tracking

Sem violações.
