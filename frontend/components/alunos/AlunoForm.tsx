"use client";

import { useEffect, useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import { Icon } from "@/components/shared/Icon";
import { useAuth } from "@/contexts/AuthContext";
import { useToast } from "@/contexts/ToastContext";
import { ApiError } from "@/lib/api/client";
import { listarTurmas, type Turma } from "@/lib/api/turmas";
import { buscarEnderecoPorCep } from "@/lib/viacep";
import {
  cadastrarAluno,
  atualizarAluno,
  type Aluno,
  type CadastrarAlunoRequest,
  type AtualizarAlunoRequest,
} from "@/lib/api/alunos";

interface AlunoFormProps {
  aluno?: Aluno;
}

export function AlunoForm({ aluno }: AlunoFormProps) {
  const { sessao } = useAuth();
  const { mostrarToast } = useToast();
  const router = useRouter();
  const editando = Boolean(aluno);

  const [turmas, setTurmas] = useState<Turma[]>([]);
  const [nome, setNome] = useState(aluno?.nome ?? "");
  const [cpf, setCpf] = useState(aluno?.cpf ?? "");
  const [telefoneAluno, setTelefoneAluno] = useState(aluno?.telefoneAluno ?? "");
  const [email, setEmail] = useState(aluno?.email ?? "");
  const [senha, setSenha] = useState("");
  // Nao ha campo persistido para "e menor de idade" (so existe como flag de
  // request) -- ao editar, inferimos o estado a partir da presenca de dados de
  // responsavel ja salvos, senao a secao de endereco do responsavel sumiria
  // toda vez que o formulario fosse reaberto para um aluno menor ja cadastrado.
  const [ehMenorDeIdade, setEhMenorDeIdade] = useState(
    () =>
      Boolean(aluno?.telefoneResponsavel) ||
      Boolean(aluno?.emailResponsavel) ||
      Boolean(aluno?.responsavelCep) ||
      Boolean(aluno?.responsavelRua) ||
      Boolean(aluno?.responsavelNumero) ||
      Boolean(aluno?.responsavelComplemento) ||
      Boolean(aluno?.responsavelBairro) ||
      Boolean(aluno?.responsavelCidade) ||
      Boolean(aluno?.responsavelEstado)
  );
  const [telefoneResponsavel, setTelefoneResponsavel] = useState(aluno?.telefoneResponsavel ?? "");
  const [emailResponsavel, setEmailResponsavel] = useState(aluno?.emailResponsavel ?? "");
  const [valorAula, setValorAula] = useState(aluno?.valorAula.toString() ?? "");
  const [turmaId, setTurmaId] = useState("");

  const [cep, setCep] = useState(aluno?.cep ?? "");
  const [rua, setRua] = useState(aluno?.rua ?? "");
  const [numero, setNumero] = useState(aluno?.numero ?? "");
  const [complemento, setComplemento] = useState(aluno?.complemento ?? "");
  const [bairro, setBairro] = useState(aluno?.bairro ?? "");
  const [cidade, setCidade] = useState(aluno?.cidade ?? "");
  const [estado, setEstado] = useState(aluno?.estado ?? "");

  const [responsavelMesmoEndereco, setResponsavelMesmoEndereco] = useState(aluno?.responsavelMesmoEndereco ?? true);
  const [responsavelCep, setResponsavelCep] = useState(aluno?.responsavelCep ?? "");
  const [responsavelRua, setResponsavelRua] = useState(aluno?.responsavelRua ?? "");
  const [responsavelNumero, setResponsavelNumero] = useState(aluno?.responsavelNumero ?? "");
  const [responsavelComplemento, setResponsavelComplemento] = useState(aluno?.responsavelComplemento ?? "");
  const [responsavelBairro, setResponsavelBairro] = useState(aluno?.responsavelBairro ?? "");
  const [responsavelCidade, setResponsavelCidade] = useState(aluno?.responsavelCidade ?? "");
  const [responsavelEstado, setResponsavelEstado] = useState(aluno?.responsavelEstado ?? "");

  const [erros, setErros] = useState<string[]>([]);
  const [salvando, setSalvando] = useState(false);

  useEffect(() => {
    if (!sessao) return;
    listarTurmas(sessao.accessToken, { ativo: true }).then(setTurmas).catch(() => setTurmas([]));
  }, [sessao]);

  /** Dispara a busca automatica quando o CEP atinge 8 digitos; falha silenciosamente (FR-012). */
  const autoPreencherPorCep = async (
    cepDigitado: string,
    setRuaAlvo: (v: string) => void,
    setBairroAlvo: (v: string) => void,
    setCidadeAlvo: (v: string) => void,
    setEstadoAlvo: (v: string) => void
  ) => {
    const digitos = cepDigitado.replace(/\D/g, "");
    if (digitos.length !== 8) return;

    const endereco = await buscarEnderecoPorCep(digitos);
    if (!endereco) return;

    setRuaAlvo(endereco.logradouro);
    setBairroAlvo(endereco.bairro);
    setCidadeAlvo(endereco.localidade);
    setEstadoAlvo(endereco.uf);
  };

  const handleCepChange = (value: string) => {
    setCep(value);
    void autoPreencherPorCep(value, setRua, setBairro, setCidade, setEstado);
  };

  const handleResponsavelCepChange = (value: string) => {
    setResponsavelCep(value);
    void autoPreencherPorCep(value, setResponsavelRua, setResponsavelBairro, setResponsavelCidade, setResponsavelEstado);
  };

  const handleToggleMesmoEndereco = (checked: boolean) => {
    setResponsavelMesmoEndereco(checked);
    if (checked) {
      // Os campos serao sobrescritos pelo espelhamento do endereco do aluno de
      // qualquer forma -- limpa para nao manter um endereco "fantasma" no estado
      // do formulario caso a usuaria desmarque de novo.
      setResponsavelCep("");
      setResponsavelRua("");
      setResponsavelNumero("");
      setResponsavelComplemento("");
      setResponsavelBairro("");
      setResponsavelCidade("");
      setResponsavelEstado("");
    }
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!sessao) return;

    setErros([]);
    setSalvando(true);

    try {
      if (editando && aluno) {
        const request: AtualizarAlunoRequest = {
          nome,
          cpf: cpf || null,
          telefoneAluno: telefoneAluno || null,
          telefoneResponsavel: telefoneResponsavel || null,
          email: email || null,
          emailResponsavel: emailResponsavel || null,
          valorAula: Number(valorAula) || 0,
          ehMenorDeIdade,
          cep: cep || null,
          rua: rua || null,
          numero: numero || null,
          complemento: complemento || null,
          bairro: bairro || null,
          cidade: cidade || null,
          estado: estado || null,
          responsavelMesmoEndereco,
          responsavelCep: responsavelMesmoEndereco ? null : responsavelCep || null,
          responsavelRua: responsavelMesmoEndereco ? null : responsavelRua || null,
          responsavelNumero: responsavelMesmoEndereco ? null : responsavelNumero || null,
          responsavelComplemento: responsavelMesmoEndereco ? null : responsavelComplemento || null,
          responsavelBairro: responsavelMesmoEndereco ? null : responsavelBairro || null,
          responsavelCidade: responsavelMesmoEndereco ? null : responsavelCidade || null,
          responsavelEstado: responsavelMesmoEndereco ? null : responsavelEstado || null,
        };
        const atualizado = await atualizarAluno(aluno.id, request, sessao.accessToken);
        mostrarToast("Aluno atualizado com sucesso.");
        router.push(`/alunos/${atualizado.id}`);
      } else {
        const request: CadastrarAlunoRequest = {
          nome,
          cpf: cpf || null,
          telefoneAluno: telefoneAluno || null,
          telefoneResponsavel: telefoneResponsavel || null,
          email: email || null,
          senha: senha || null,
          emailResponsavel: emailResponsavel || null,
          valorAula: Number(valorAula) || 0,
          turmaId: turmaId ? Number(turmaId) : null,
          ehMenorDeIdade,
          cep: cep || null,
          rua: rua || null,
          numero: numero || null,
          complemento: complemento || null,
          bairro: bairro || null,
          cidade: cidade || null,
          estado: estado || null,
          responsavelMesmoEndereco,
          responsavelCep: responsavelMesmoEndereco ? null : responsavelCep || null,
          responsavelRua: responsavelMesmoEndereco ? null : responsavelRua || null,
          responsavelNumero: responsavelMesmoEndereco ? null : responsavelNumero || null,
          responsavelComplemento: responsavelMesmoEndereco ? null : responsavelComplemento || null,
          responsavelBairro: responsavelMesmoEndereco ? null : responsavelBairro || null,
          responsavelCidade: responsavelMesmoEndereco ? null : responsavelCidade || null,
          responsavelEstado: responsavelMesmoEndereco ? null : responsavelEstado || null,
        };
        const criado = await cadastrarAluno(request, sessao.accessToken);
        mostrarToast(`Aluno cadastrado com sucesso. RA: ${criado.ra}`);
        router.push(`/alunos/${criado.id}`);
      }
    } catch (excecao) {
      setErros(excecao instanceof ApiError ? excecao.details ?? [excecao.message] : ["Não foi possível salvar o aluno."]);
    } finally {
      setSalvando(false);
    }
  };

  return (
    <div className="form-wrap">
      <form onSubmit={handleSubmit}>
        {erros.length > 0 && (
          <div className="err-banner show" style={{ marginBottom: 20 }}>
            <Icon name="warn" size={15} />
            <span>{erros[0]}</span>
          </div>
        )}

        <div className="form-section">
          <div className="fs-title">
            <span className="fs-num">1</span> Dados do aluno
          </div>
          <div className="field-grid">
            <div className="field">
              <label>
                Nome completo<span className="req">*</span>
              </label>
              <input required value={nome} onChange={(e) => setNome(e.target.value)} />
            </div>
            <div className="field">
              <label>CPF (opcional)</label>
              <input value={cpf} onChange={(e) => setCpf(e.target.value)} placeholder="000.000.000-00" />
            </div>
            <div className="field">
              <label>
                Valor da aula<span className="req">*</span>
              </label>
              <input required type="number" step="0.01" min="0" value={valorAula} onChange={(e) => setValorAula(e.target.value)} placeholder="80.00" />
            </div>
          </div>
        </div>

        <div className="form-section">
          <div className="fs-title">
            <span className="fs-num">2</span> Contato e acesso próprio
          </div>
          <div className="field-grid two">
            <div className="field">
              <label>Telefone do aluno</label>
              <input value={telefoneAluno} onChange={(e) => setTelefoneAluno(e.target.value)} />
            </div>
            <div className="field">
              <label>E-mail do aluno (login próprio)</label>
              <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} />
            </div>
          </div>
          {!editando && (
            <div className="field-grid two" style={{ marginTop: 15 }}>
              <div className="field">
                <label>Senha do aluno</label>
                <input type="password" value={senha} onChange={(e) => setSenha(e.target.value)} placeholder="Mínimo 8 caracteres, alfanumérica" />
                <span className="hint">Só é usada se um e-mail de login for informado.</span>
              </div>
              <div className="field">
                <label>Turma (opcional)</label>
                <select value={turmaId} onChange={(e) => setTurmaId(e.target.value)}>
                  <option value="">Atendimento individual</option>
                  {turmas.map((t) => (
                    <option key={t.id} value={t.id}>
                      {t.nome}
                    </option>
                  ))}
                </select>
              </div>
            </div>
          )}
        </div>

        <div className="form-section">
          <div className="fs-title">
            <span className="fs-num">3</span> Endereço
          </div>
          <div className="field-grid">
            <div className="field">
              <label>
                CEP<span className="req">*</span>
              </label>
              <input required value={cep} onChange={(e) => handleCepChange(e.target.value)} placeholder="00000000" maxLength={8} />
            </div>
            <div className="field">
              <label>
                Rua<span className="req">*</span>
              </label>
              <input required value={rua} onChange={(e) => setRua(e.target.value)} />
            </div>
            <div className="field">
              <label>
                Número<span className="req">*</span>
              </label>
              <input required value={numero} onChange={(e) => setNumero(e.target.value)} />
            </div>
          </div>
          <div className="field-grid" style={{ marginTop: 15 }}>
            <div className="field">
              <label>Bairro (opcional)</label>
              <input value={bairro} onChange={(e) => setBairro(e.target.value)} />
            </div>
            <div className="field">
              <label>Cidade (opcional)</label>
              <input value={cidade} onChange={(e) => setCidade(e.target.value)} />
            </div>
            <div className="field">
              <label>Estado (opcional)</label>
              <input value={estado} onChange={(e) => setEstado(e.target.value)} placeholder="UF" maxLength={2} />
            </div>
          </div>
          <div className="field-grid two" style={{ marginTop: 15 }}>
            <div className="field">
              <label>Complemento (opcional)</label>
              <input value={complemento} onChange={(e) => setComplemento(e.target.value)} />
            </div>
          </div>
        </div>

        <div className="form-section">
          <div className="fs-title">
            <span className="fs-num">4</span> Responsável
          </div>
          <div className="chk-pill" style={{ marginBottom: 15, width: "fit-content" }}>
            <input
              type="checkbox"
              id="menor"
              checked={ehMenorDeIdade}
              onChange={(e) => setEhMenorDeIdade(e.target.checked)}
            />
            <label htmlFor="menor">Aluno é menor de idade</label>
          </div>
          <div className="field-grid two">
            <div className="field">
              <label>
                Telefone do responsável{ehMenorDeIdade && <span className="req">*</span>}
              </label>
              <input
                required={ehMenorDeIdade}
                value={telefoneResponsavel}
                onChange={(e) => setTelefoneResponsavel(e.target.value)}
              />
            </div>
            <div className="field">
              <label>
                E-mail do responsável{ehMenorDeIdade && <span className="req">*</span>}
              </label>
              <input
                required={ehMenorDeIdade}
                type="email"
                value={emailResponsavel}
                onChange={(e) => setEmailResponsavel(e.target.value)}
              />
            </div>
          </div>

          {ehMenorDeIdade && (
            <div style={{ marginTop: 20 }}>
              <div className="chk-pill" style={{ marginBottom: 15, width: "fit-content" }}>
                <input
                  type="checkbox"
                  id="mesmoEndereco"
                  checked={responsavelMesmoEndereco}
                  onChange={(e) => handleToggleMesmoEndereco(e.target.checked)}
                />
                <label htmlFor="mesmoEndereco">Mesmo endereço do aluno</label>
              </div>

              {!responsavelMesmoEndereco && (
                <>
                  <div className="field-grid">
                    <div className="field">
                      <label>
                        CEP do responsável<span className="req">*</span>
                      </label>
                      <input
                        required
                        value={responsavelCep}
                        onChange={(e) => handleResponsavelCepChange(e.target.value)}
                        placeholder="00000000"
                        maxLength={8}
                      />
                    </div>
                    <div className="field">
                      <label>
                        Rua do responsável<span className="req">*</span>
                      </label>
                      <input required value={responsavelRua} onChange={(e) => setResponsavelRua(e.target.value)} />
                    </div>
                    <div className="field">
                      <label>
                        Número do responsável<span className="req">*</span>
                      </label>
                      <input required value={responsavelNumero} onChange={(e) => setResponsavelNumero(e.target.value)} />
                    </div>
                  </div>
                  <div className="field-grid" style={{ marginTop: 15 }}>
                    <div className="field">
                      <label>Bairro do responsável (opcional)</label>
                      <input value={responsavelBairro} onChange={(e) => setResponsavelBairro(e.target.value)} />
                    </div>
                    <div className="field">
                      <label>Cidade do responsável (opcional)</label>
                      <input value={responsavelCidade} onChange={(e) => setResponsavelCidade(e.target.value)} />
                    </div>
                    <div className="field">
                      <label>Estado do responsável (opcional)</label>
                      <input
                        value={responsavelEstado}
                        onChange={(e) => setResponsavelEstado(e.target.value)}
                        placeholder="UF"
                        maxLength={2}
                      />
                    </div>
                  </div>
                  <div className="field-grid two" style={{ marginTop: 15 }}>
                    <div className="field">
                      <label>Complemento do responsável (opcional)</label>
                      <input value={responsavelComplemento} onChange={(e) => setResponsavelComplemento(e.target.value)} />
                    </div>
                  </div>
                </>
              )}
            </div>
          )}
        </div>

        <div className="form-actions">
          <button
            type="button"
            className="btn btn-ghost"
            onClick={() => router.push(editando && aluno ? `/alunos/${aluno.id}` : "/alunos")}
          >
            Cancelar
          </button>
          <button type="submit" className="btn btn-primary" disabled={salvando}>
            {salvando ? "Salvando..." : editando ? "Salvar alterações" : "Cadastrar aluno"}
          </button>
        </div>
      </form>
    </div>
  );
}
