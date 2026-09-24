using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CartaBranca.Nucleo;
using CartaBranca.Mundo;

namespace CartaBranca.UI
{
    /// <summary>Menu de entrada: Jogar, Como jogar e Sair (Sair some no WebGL,
    /// onde fechar a aba e o "sair"). Mostra o recorde salvo.</summary>
    public class MenuPrincipal : MonoBehaviour
    {
        public string cenaDoJogo = "Arena";

        public GameObject painelPrincipal;
        public GameObject painelComoJogar;
        public Button jogar;
        public Button comoJogar;
        public Button sair;
        public Button voltar;
        public Text recorde;

        void Start()
        {
            Time.timeScale = 1f;
            Mostrar(false, false);

            jogar.onClick.AddListener(Jogar);
            comoJogar.onClick.AddListener(() => Mostrar(true, true));
            voltar.onClick.AddListener(() => Mostrar(false, true));
            sair.onClick.AddListener(Sair);

            if (Application.platform == RuntimePlatform.WebGLPlayer) sair.gameObject.SetActive(false);

            if (recorde != null)
            {
                recorde.text = Recordes.MelhorPontuacao > 0
                    ? "RECORDE  " + Recordes.MelhorPontuacao + "   ·   ONDA " + Recordes.MelhorOnda +
                      "   ·   " + Hud.Formatar(Recordes.MelhorTempo) + " DE MESA"
                    : "NENHUMA MÃO JOGADA AINDA";
            }
        }

        void Update()
        {
            if (painelComoJogar.activeSelf && Input.GetKeyDown(KeyCode.Escape)) Mostrar(false, true);
        }

        void Mostrar(bool ajuda, bool comSom)
        {
            if (comSom) Sonoplasta.Tocar(Som.Clique);
            painelComoJogar.SetActive(ajuda);
            painelPrincipal.SetActive(!ajuda);
            Selecionar(ajuda ? voltar : (comSom ? comoJogar : jogar));
        }

        void Jogar()
        {
            Sonoplasta.Tocar(Som.Clique);
            SceneManager.LoadScene(cenaDoJogo);
        }

        void Sair()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        static void Selecionar(Button botao)
        {
            if (botao != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(botao.gameObject);
        }
    }
}
