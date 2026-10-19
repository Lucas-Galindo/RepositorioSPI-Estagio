"use client";

import { Icon } from "@/components/shared/Icon";
import { useTheme } from "@/contexts/ThemeContext";
import { useToast } from "@/contexts/ToastContext";
import { coresBase, DESTAQUE_PADRAO, DESTAQUES, MODOS_DE_CORES } from "@/lib/paleta";
import { usePageHeader } from "@/lib/usePageHeader";

export default function ConfiguracoesPage() {
  usePageHeader("Configurações", "Aparência e acessibilidade do sistema");
  const { dark, setDark, colorMode, setColorMode, destaque, setDestaque } = useTheme();
  const { mostrarToast } = useToast();

  // specs/057: so ha cores da paleta oficial (lib/paleta.ts); a cor do texto
  // de cada opcao e derivada automaticamente para manter o contraste.
  const destaqueBloqueado = colorMode !== "normal";

  return (
    <>
      <div className="settings-card">
        <div className="settings-head">
          <div className="ic">
            <Icon name="sun" size={18} />
          </div>
          <div>
            <h3>Tema</h3>
            <p>Escolha entre o modo claro e escuro.</p>
          </div>
        </div>
        <div className="theme-preview">
          <button type="button" className={`theme-opt light${!dark ? " active" : ""}`} onClick={() => setDark(false)}>
            <div className="ic">
              <Icon name="sun" size={17} />
            </div>
            <div className="lb">Modo claro</div>
          </button>
          <button type="button" className={`theme-opt dark${dark ? " active" : ""}`} onClick={() => setDark(true)}>
            <div className="ic">
              <Icon name="moon" size={17} />
            </div>
            <div className="lb">Modo escuro</div>
          </button>
        </div>
      </div>

      <div className="settings-card">
        <div className="settings-head">
          <div className="ic">
            <Icon name="shield" size={18} />
          </div>
          <div>
            <h3>Modo de cores</h3>
            <p>Paletas seguras para diferentes tipos de daltonismo.</p>
          </div>
        </div>
        <div className="colormode-grid">
          {MODOS_DE_CORES.map((cm) => {
            const cores = coresBase({ dark, colorMode: cm.id, destaque });
            return (
              <button
                type="button"
                key={cm.id}
                className={`colormode-card${colorMode === cm.id ? " active" : ""}`}
                onClick={() => {
                  setColorMode(cm.id);
                  mostrarToast(`Modo de cores atualizado: ${cm.nome}.`);
                }}
              >
                <div className="colormode-swatches">
                  {[cores.accent, cores.accent2, cores.accent3, cores.danger].map((cor) => (
                    <span className="swatch" style={{ background: cor }} key={cor} />
                  ))}
                </div>
                <div className="cm-name">{cm.nome}</div>
                <div className="cm-desc">{cm.desc}</div>
              </button>
            );
          })}
        </div>
      </div>

      <div className="settings-card">
        <div className="settings-head">
          <div className="ic">
            <Icon name="palette" size={18} />
          </div>
          <div>
            <h3>Cor de destaque</h3>
            <p>
              {destaqueBloqueado
                ? "Desativada enquanto um modo de daltonismo estiver ativo, para não sobrepor a paleta de acessibilidade."
                : "Cor principal de botões, links e destaques. O texto se ajusta sozinho para continuar legível."}
            </p>
          </div>
        </div>
        <div className="colormode-grid">
          {DESTAQUES.map((d) => (
            <button
              type="button"
              key={d.id}
              disabled={destaqueBloqueado}
              className={`colormode-card${!destaqueBloqueado && destaque === d.id ? " active" : ""}`}
              onClick={() => {
                setDestaque(d.id);
                mostrarToast(`Cor de destaque atualizada: ${d.nome}.`);
              }}
            >
              <div className="colormode-swatches">
                <span className="swatch" style={{ background: d.cor }} />
              </div>
              <div className="cm-name">
                {d.nome}
                {d.id === DESTAQUE_PADRAO && " (padrão)"}
              </div>
            </button>
          ))}
        </div>
      </div>
    </>
  );
}
