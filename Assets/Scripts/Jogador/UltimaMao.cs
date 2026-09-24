using System.Collections.Generic;
using UnityEngine;
using CartaBranca.Nucleo;
using CartaBranca.Inimigos;
using CartaBranca.Mundo;

namespace CartaBranca.Jogador
{
    /// <summary>Acao especial da Carta Branca: a ULTIMA MAO (royal flush).
    /// Vantagem clara: derruba na hora todo naipe num raio em volta dela, com invulnerabilidade curta.
    /// Punicao que forca a economia:
    ///  - custa fichas, e ficha no bolso vale pontos no placar final;
    ///  - o preco sobe a cada uso na mesma partida (a casa aumenta a aposta);
    ///  - quem cai pela Ultima Mao nao solta fichas nem conta para a sequencia.</summary>
    [RequireComponent(typeof(ControleCartaBranca))]
    public class UltimaMao : MonoBehaviour
    {
        [SerializeField] float raio = 7.5f;
        [SerializeField] int custoBase = 10;
        [SerializeField] int acrescimoPorUso = 5;
        [SerializeField] float invulnerabilidade = 0.8f;

        [Header("Efeitos (preenchidos pelo construtor)")]
        public GameObject anelPrefab;
        public Sprite cartaSprite;

        ControleCartaBranca _jogador;
        int _usos;

        public int Custo { get { return custoBase + acrescimoPorUso * _usos; } }
        public int Usos { get { return _usos; } }
        public float Raio { get { return raio; } }
        public bool PodeUsar
        {
            get
            {
                GerenciadorDeJogo jogo = GerenciadorDeJogo.Instancia;
                return jogo != null && !jogo.EmPausa && _jogador.Vivo && jogo.Fichas >= Custo;
            }
        }

        void Awake()
        {
            _jogador = GetComponent<ControleCartaBranca>();
        }

        void Start()
        {
            if (GerenciadorDeJogo.Instancia != null) GerenciadorDeJogo.Instancia.AoReiniciar += Zerar;
        }

        void OnDestroy()
        {
            if (GerenciadorDeJogo.Instancia != null) GerenciadorDeJogo.Instancia.AoReiniciar -= Zerar;
        }

        void Zerar()
        {
            _usos = 0;
        }

        void Update()
        {
            GerenciadorDeJogo jogo = GerenciadorDeJogo.Instancia;
            if (jogo == null || jogo.EmPausa || !_jogador.Vivo) return;
            if (Input.GetKeyDown(KeyCode.L) || Input.GetMouseButtonDown(1)) Usar();
        }

        public void Usar()
        {
            GerenciadorDeJogo jogo = GerenciadorDeJogo.Instancia;
            if (jogo == null) return;

            int custo = Custo;
            if (!jogo.GastarFichas(custo))
            {
                jogo.MostrarMensagem("SEM FICHAS PARA A ÚLTIMA MÃO  -  custa " + custo + ", você tem " + jogo.Fichas, 1.4f);
                Sonoplasta.Tocar(Som.Negado);
                return;
            }

            _usos++;
            _jogador.TornarInvulneravel(invulnerabilidade);

            // copia: derrubar um inimigo tira ele da lista original
            Vector2 centro = transform.position;
            List<Inimigo> alvos = new List<Inimigo>(jogo.Vivos);
            int derrubados = 0;
            foreach (Inimigo inimigo in alvos)
            {
                if (inimigo == null || !inimigo.Vivo) continue;
                if (Vector2.Distance(centro, inimigo.transform.position) > raio) continue;
                inimigo.DerrubarPelaMesa();
                derrubados++;
            }

            Espetaculo();
            jogo.MostrarMensagem("ÚLTIMA MÃO!  " + derrubados + (derrubados == 1 ? " naipe" : " naipes") +
                                 " na lona   (-" + custo + " fichas, próxima custa " + Custo + ")", 2f);
        }

        void Espetaculo()
        {
            if (anelPrefab != null)
            {
                GameObject anel = Instantiate(anelPrefab, transform.position, Quaternion.identity);
                anel.transform.localScale = Vector3.one * 1.2f;
            }

            // as cinco cartas do royal flush saem girando em leque
            if (cartaSprite != null)
            {
                for (int i = 0; i < 5; i++)
                {
                    float ang = (90f + (i - 2) * 38f) * Mathf.Deg2Rad;
                    Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang) * 0.6f);
                    GameObject c = new GameObject("CartaDoFlush");
                    c.transform.position = transform.position;
                    c.transform.localScale = Vector3.one * 1.6f;
                    SpriteRenderer sr = c.AddComponent<SpriteRenderer>();
                    sr.sprite = cartaSprite;
                    sr.sortingOrder = 62;
                    Efeito e = c.AddComponent<Efeito>();
                    e.duracao = 0.55f;
                    e.crescimento = 0.5f;
                    e.subida = 0f;
                    e.deriva = dir * 16f;
                    e.giro = (i % 2 == 0 ? 1f : -1f) * 900f;
                }
            }

            Sonoplasta.Tocar(Som.Especial);
            CameraSuave.Tremer(0.7f);
            CameraSuave.Aproximar(6.4f, 0.35f);
            if (GerenciadorDeJogo.Instancia != null) GerenciadorDeJogo.Instancia.CamaraLenta(0.35f, 0.3f);
        }
    }
}
