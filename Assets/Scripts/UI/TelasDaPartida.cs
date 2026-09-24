using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using CartaBranca.Nucleo;
using CartaBranca.Mundo;

namespace CartaBranca.UI
{
    /// <summary>Telas por cima da arena: PAUSA (Esc / P) e FIM DE JOGO ("mao morta"),
    /// com o resumo da partida. Escuta os eventos do GerenciadorDeJogo.</summary>
    public class TelasDaPartida : MonoBehaviour
    {
        [Header("Pausa")]
        public GameObject painelPausa;
        public Button continuar;
        public Button reiniciarNaPausa;
        public Button menuNaPausa;

        [Header("Fim de jogo")]
        public GameObject painelFim;
        public Text tituloFim;
        public Text resumoFim;
        public Button jogarDeNovo;
        public Button menuNoFim;

        GerenciadorDeJogo _jogo;

        void Start()
        {
            _jogo = GerenciadorDeJogo.Instancia;
            Esconder();

            if (_jogo != null)
            {
                _jogo.AoPausar += AoPausar;
                _jogo.AoFimDeJogo += MostrarFim;
                _jogo.AoReiniciar += Esconder;
            }

            Ligar(continuar, () => _jogo.Pausar(false));
            Ligar(reiniciarNaPausa, () => _jogo.Reiniciar());
            Ligar(menuNaPausa, () => _jogo.IrParaMenu());
            Ligar(jogarDeNovo, () => _jogo.Reiniciar());
            Ligar(menuNoFim, () => _jogo.IrParaMenu());
        }

        void OnDestroy()
        {
            if (_jogo == null) return;
            _jogo.AoPausar -= AoPausar;
            _jogo.AoFimDeJogo -= MostrarFim;
            _jogo.AoReiniciar -= Esconder;
        }

        void Ligar(Button botao, UnityAction acao)
        {
            if (botao == null) return;
            botao.onClick.AddListener(() =>
            {
                Sonoplasta.Tocar(Som.Clique);
                if (_jogo != null) acao();
            });
        }

        void Update()
        {
            if (_jogo == null) return;
            bool pausa = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P);

            switch (_jogo.Estado)
            {
                case EstadoDaPartida.Jogando:
                    if (pausa) _jogo.AlternarPausa();
                    else if (_jogo.Pausado)
                    {
                        if (Input.GetKeyDown(KeyCode.R)) _jogo.Reiniciar();
                        else if (Input.GetKeyDown(KeyCode.M)) _jogo.IrParaMenu();
                    }
                    break;

                case EstadoDaPartida.FimDeJogo:
                    if (Input.GetKeyDown(KeyCode.R)) _jogo.Reiniciar();
                    else if (Input.GetKeyDown(KeyCode.M)) _jogo.IrParaMenu();
                    break;
            }
        }

        void Esconder()
        {
            if (painelPausa != null) painelPausa.SetActive(false);
            if (painelFim != null) painelFim.SetActive(false);
        }

        void AoPausar(bool pausado)
        {
            if (painelPausa != null) painelPausa.SetActive(pausado);
            if (pausado) Selecionar(continuar);
        }

        void MostrarFim()
        {
            if (painelFim == null) return;
            painelFim.SetActive(true);

            if (tituloFim != null)
                tituloFim.text = _jogo.NovoRecorde ? "MÃO MORTA  ·  NOVO RECORDE!" : "MÃO MORTA";

            if (resumoFim != null)
            {
                resumoFim.text =
                    "PONTOS NA MESA   " + _jogo.Pontos + "\n" +
                    "FICHAS NO BOLSO   " + _jogo.Fichas + " × " + _jogo.PontosPorFicha + " = " + (_jogo.Fichas * _jogo.PontosPorFicha) + "\n" +
                    "<size=52><color=#DE465C>TOTAL   " + _jogo.PontuacaoFinal + "</color></size>\n\n" +
                    "ONDA " + _jogo.Onda + "   ·   TEMPO " + Hud.Formatar(_jogo.TempoDeVida) + "   ·   ABATES " + _jogo.Abates + "\n" +
                    "MELHOR SEQUÊNCIA " + _jogo.MelhorSequencia + "   ·   FICHAS GASTAS " + _jogo.FichasGastas +
                    "   ·   LEVADAS PELA CASA " + _jogo.FichasPerdidas + "\n\n" +
                    "<color=#C6A258>RECORDE  " + Recordes.MelhorPontuacao + "</color>";
            }

            Selecionar(jogarDeNovo);
        }

        static void Selecionar(Button botao)
        {
            if (botao != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(botao.gameObject);
        }
    }
}
