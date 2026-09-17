export interface EnderecoViaCep {
  logradouro: string;
  bairro: string;
  localidade: string;
  uf: string;
}

/**
 * Busca endereco por CEP via ViaCEP (API publica de terceiros, chamada direto do
 * navegador -- nao passa pelo backend do SPI). Retorna null em qualquer falha
 * (rede, resposta nao-ok, ou CEP inexistente) -- nunca lanca excecao para quem
 * chama, para nunca bloquear o cadastro.
 */
export async function buscarEnderecoPorCep(cep: string): Promise<EnderecoViaCep | null> {
  try {
    const response = await fetch(`https://viacep.com.br/ws/${cep}/json/`);
    if (!response.ok) return null;

    const dados = await response.json();
    if (dados.erro) return null;

    return {
      logradouro: dados.logradouro ?? "",
      bairro: dados.bairro ?? "",
      localidade: dados.localidade ?? "",
      uf: dados.uf ?? "",
    };
  } catch {
    return null;
  }
}
