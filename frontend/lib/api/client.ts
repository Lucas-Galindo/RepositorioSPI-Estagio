const API_URL = process.env.NEXT_PUBLIC_API_URL;

if (!API_URL) {
  throw new Error(
    "NEXT_PUBLIC_API_URL nao configurada. Defina-a em frontend/.env.local (ver .env.local.example)."
  );
}

export class ApiError extends Error {
  readonly status: number;
  readonly details?: string[];

  constructor(status: number, message: string, details?: string[]) {
    super(message);
    this.status = status;
    this.details = details;
  }
}

/**
 * Extrai uma mensagem legivel do corpo de erro da API, que pode vir em
 * formatos diferentes conforme o controller: lista de strings (validacao
 * do FluentValidation), texto puro (BadRequest/Unauthorized/Conflict com
 * string), ou ProblemDetails ({ title, detail }) do Problem().
 */
async function extrairErro(response: Response): Promise<ApiError> {
  const texto = await response.text();

  if (!texto) {
    return new ApiError(response.status, "Ocorreu um erro inesperado. Tente novamente.");
  }

  try {
    const corpo = JSON.parse(texto);

    if (Array.isArray(corpo)) {
      return new ApiError(response.status, corpo[0] ?? "Dados invalidos.", corpo);
    }

    if (typeof corpo === "string") {
      return new ApiError(response.status, corpo);
    }

    const mensagem = corpo.detail ?? corpo.title ?? corpo.mensagem ?? "Ocorreu um erro inesperado.";
    return new ApiError(response.status, mensagem);
  } catch {
    return new ApiError(response.status, texto);
  }
}

export async function apiPost<TResponse>(
  path: string,
  body: unknown,
  accessToken?: string
): Promise<TResponse> {
  const response = await fetch(`${API_URL}${path}`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}),
    },
    body: JSON.stringify(body),
  });

  if (!response.ok) {
    throw await extrairErro(response);
  }

  const texto = await response.text();
  return (texto ? JSON.parse(texto) : undefined) as TResponse;
}

/** Query params opcionais: valores undefined/null/"" sao omitidos da URL. */
export type QueryParams = Record<string, string | number | boolean | undefined | null>;

function montarQueryString(params?: QueryParams): string {
  if (!params) {
    return "";
  }

  const busca = new URLSearchParams();
  for (const [chave, valor] of Object.entries(params)) {
    if (valor !== undefined && valor !== null && valor !== "") {
      busca.set(chave, String(valor));
    }
  }

  const texto = busca.toString();
  return texto ? `?${texto}` : "";
}

export async function apiGet<TResponse>(
  path: string,
  accessToken: string,
  params?: QueryParams
): Promise<TResponse> {
  const response = await fetch(`${API_URL}${path}${montarQueryString(params)}`, {
    headers: { Authorization: `Bearer ${accessToken}` },
  });

  if (!response.ok) {
    throw await extrairErro(response);
  }

  const texto = await response.text();
  return (texto ? JSON.parse(texto) : undefined) as TResponse;
}

export async function apiPut<TResponse>(
  path: string,
  body: unknown,
  accessToken: string
): Promise<TResponse> {
  const response = await fetch(`${API_URL}${path}`, {
    method: "PUT",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${accessToken}`,
    },
    body: JSON.stringify(body),
  });

  if (!response.ok) {
    throw await extrairErro(response);
  }

  const texto = await response.text();
  return (texto ? JSON.parse(texto) : undefined) as TResponse;
}

/**
 * Envia um arquivo como multipart/form-data (campo "arquivo"). Nao define
 * Content-Type manualmente -- o navegador define o boundary automaticamente.
 */
export async function apiPostFile<TResponse>(
  path: string,
  arquivo: File,
  accessToken: string
): Promise<TResponse> {
  const formData = new FormData();
  formData.append("arquivo", arquivo);

  const response = await fetch(`${API_URL}${path}`, {
    method: "POST",
    headers: { Authorization: `Bearer ${accessToken}` },
    body: formData,
  });

  if (!response.ok) {
    throw await extrairErro(response);
  }

  const texto = await response.text();
  return (texto ? JSON.parse(texto) : undefined) as TResponse;
}

/** Extrai o nome do arquivo do cabecalho Content-Disposition (attachment/inline; filename="..."). */
function extrairNomeArquivo(response: Response): string {
  const cabecalho = response.headers.get("Content-Disposition") ?? "";
  const match = /filename="?([^";]+)"?/i.exec(cabecalho);
  return match ? match[1] : "arquivo";
}

/** Baixa um anexo binario (Blob), com o nome original extraido do cabecalho da resposta. */
export async function apiGetBlob(path: string, accessToken: string): Promise<{ blob: Blob; nomeArquivo: string }> {
  const response = await fetch(`${API_URL}${path}`, {
    headers: { Authorization: `Bearer ${accessToken}` },
  });

  if (!response.ok) {
    throw await extrairErro(response);
  }

  return { blob: await response.blob(), nomeArquivo: extrairNomeArquivo(response) };
}

export async function apiDelete(path: string, accessToken: string): Promise<void> {
  const response = await fetch(`${API_URL}${path}`, {
    method: "DELETE",
    headers: { Authorization: `Bearer ${accessToken}` },
  });

  if (!response.ok) {
    throw await extrairErro(response);
  }
}
