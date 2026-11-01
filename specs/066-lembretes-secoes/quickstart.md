# Quickstart: validar a divisão da aba Lembretes (spec 066)

## Pré-requisitos

- API local (porta 5099) com `Smtp__SmtpHost=smtp.invalido.localhost` (nenhum e-mail sai) e frontend de produção na porta 3000 (CORS só aceita `localhost:3000`).
- Login de teste da professora pela variável de ambiente `SPI_TESTE_SENHA` (nunca em arquivo).
- Nenhum dado é criado nem alterado: a validação só **lê** a tela.

## Passos

| # | Passo | Esperado |
|---|---|---|
| 1 | Abrir `/lembretes` (modo claro) | Título "Lembretes de aula" + frase, depois indicadores, filtros, contagem, "Novo lembrete" e tabela |
| 2 | Rolar até o fim | Linha divisória e o título "Lembretes de pagamento" + frase, acima do bloco "Pagamento pendente" |
| 3 | Usar um filtro e limpar | Só a tabela e os indicadores de aula mudam; a parte de pagamento não muda (como antes) |
| 4 | Clicar numa linha / no lixo / em "Novo lembrete" | Mesmas ações de antes (editar, confirmar exclusão, ir para o cadastro) |
| 5 | Trocar para o modo escuro | Títulos e frases legíveis; divisória visível |
| 6 | Repetir com a lista de aula vazia (filtro sem resultado) | Título da parte de aula continua; só a tabela mostra o estado vazio |
| 7 | Capturas das 34 telas, antes e depois | Apenas `/lembretes` (claro e escuro) difere |
| 8 | `npm run verificar-contraste` | 0 pares abaixo de 4.5:1 |
