using UnityEngine;
using UnityEngine.UI;
using CartaBranca.Nucleo;
using CartaBranca.Jogador;

namespace CartaBranca.UI
{
    /// <summary>Placar da mesa: onda, pontos, tempo, recorde, aviso central
    /// e a barrinha de recarga da esquiva.</summary>
    public class Hud : MonoBehaviour
    {
        public Text onda;
        public Text pontos;
        public Text tempo;
        public Text recorde;
        public Text aviso;
        public Text ajuda;
        public Image barraEsquiva;

        ControleCartaBranca _jogador;

        void Start()
        {
            _jogador = Object.FindFirstObjectByType<ControleCartaBranca>();
            if (ajuda != null)
                ajuda.text = "A/D mover   ESPACO pular   J atirar carta   K esquivar";
        }

        void Update()
        {
            GerenciadorDeJogo jogo = GerenciadorDeJogo.Instancia;
            if (jogo == null) return;

            if (onda != null)    onda.text = "ONDA " + jogo.Onda;
            if (pontos != null)  pontos.text = jogo.Pontos + " PTS   " + jogo.Abates + " ABATES";
            if (tempo != null)   tempo.text = Formatar(jogo.TempoDeVida);
            if (recorde != null) recorde.text = "RECORDE " + Recordes.MelhorPontuacao;
            if (aviso != null)   aviso.text = jogo.Mensagem;

            if (barraEsquiva != null && _jogador != null)
            {
                barraEsquiva.fillAmount = _jogador.RecargaEsquiva;
                barraEsquiva.color = _jogador.RecargaEsquiva >= 1f
                    ? new Color(0.87f, 0.27f, 0.36f)
                    : new Color(0.45f, 0.52f, 0.66f);
            }
        }

        static string Formatar(float segundos)
        {
            int m = Mathf.FloorToInt(segundos / 60f);
            int s = Mathf.FloorToInt(segundos % 60f);
            return string.Format("{0:00}:{1:00}", m, s);
        }
    }
}
