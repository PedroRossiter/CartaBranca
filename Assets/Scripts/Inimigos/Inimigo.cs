using UnityEngine;
using CartaBranca.Nucleo;
using CartaBranca.Jogador;
using CartaBranca.Mundo;

namespace CartaBranca.Inimigos
{
    /// <summary>Base abstrata dos naipes hostis. Cuida do que todo inimigo tem em comum:
    /// achar o alvo, machucar a Carta Branca no toque, morrer, soltar fichas e avisar o gerador.
    /// O movimento fica por conta de cada subclasse em Comportamento().</summary>
    public abstract class Inimigo : Entidade
    {
        [Header("Identidade")]
        [SerializeField] protected Naipe naipe = Naipe.Espadas;
        [SerializeField] protected int pontos = 10;

        [Header("Movimento")]
        [SerializeField] protected float velocidadeBase = 3f;

        [Header("Recompensa")]
        [SerializeField] protected int fichas = 1;
        [SerializeField] protected bool machucaAoTocar = true;

        [Header("Prefabs (preenchidos pelo construtor)")]
        public GameObject explosaoPrefab;
        public GameObject poeiraPrefab;
        public GameObject fichaPrefab;
        public GameObject fichaOuroPrefab;

        /// <summary>Disparado quando este inimigo sai de cena; o gerador escuta para agendar o respawn.</summary>
        public event System.Action<Inimigo> AoSair;

        public Naipe Naipe { get { return naipe; } }
        public int Pontos { get { return pontos; } }
        public int Fichas { get { return fichas; } }
        public float Velocidade { get; protected set; }

        bool _pelaMesa;

        protected static ControleCartaBranca AlvoGlobal;
        protected ControleCartaBranca Alvo
        {
            get
            {
                if (AlvoGlobal == null)
                    AlvoGlobal = Object.FindAnyObjectByType<ControleCartaBranca>();
                return AlvoGlobal;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            Velocidade = velocidadeBase;
        }

        protected virtual void Start()
        {
            if (GerenciadorDeJogo.Instancia != null)
                GerenciadorDeJogo.Instancia.Registrar(this);
        }

        /// <summary>Aplica a dificuldade da onda atual. Subclasses podem estender.</summary>
        public virtual void Configurar(float escala, int vida)
        {
            Velocidade = velocidadeBase * Mathf.Clamp(escala, 1f, 2.4f);
            DefinirVidaMaxima(vida);
        }

        protected override void Update()
        {
            base.Update();
            GerenciadorDeJogo jogo = GerenciadorDeJogo.Instancia;
            if (jogo != null && jogo.EmPausa) return;
            if (!Vivo) return;
            Comportamento();
        }

        /// <summary>Cada naipe se move do seu jeito (ligacao dinamica em tempo de execucao).</summary>
        protected abstract void Comportamento();

        protected virtual void OnTriggerEnter2D(Collider2D outro) { Encostou(outro); }
        protected virtual void OnTriggerStay2D(Collider2D outro)  { Encostou(outro); }

        void Encostou(Collider2D outro)
        {
            if (!Vivo || !machucaAoTocar || !outro.CompareTag("Player")) return;
            ControleCartaBranca jogador = outro.GetComponentInParent<ControleCartaBranca>();
            if (jogador == null || !jogador.Vivo) return;
            jogador.Danificar(1, transform.position);
        }

        /// <summary>Derrubado pela Ultima Mao: morre na hora, conta pontos,
        /// mas nao solta fichas nem alimenta a sequencia ("mao queimada").</summary>
        public void DerrubarPelaMesa()
        {
            if (!Vivo) return;
            _pelaMesa = true;
            Vida = 0;
            Morrer();
        }

        /// <summary>Morte: pontos, efeito, fichas e aviso ao gerador para o respawn.</summary>
        protected override void Morrer()
        {
            if (GerenciadorDeJogo.Instancia != null)
            {
                GerenciadorDeJogo.Instancia.ContarAbate(this, _pelaMesa);
                GerenciadorDeJogo.Instancia.Remover(this);
            }

            Sonoplasta.Tocar(Som.Abate);

            if (explosaoPrefab != null)
            {
                GameObject fx = Instantiate(explosaoPrefab, transform.position, Quaternion.identity);
                Efeito e = fx.GetComponent<Efeito>();
                if (e != null)
                    e.Tingir(naipe.Vermelho() ? new Color(0.87f, 0.27f, 0.36f) : new Color(0.65f, 0.71f, 0.82f));
            }

            if (!_pelaMesa) SoltarFichas();
            Sair();
        }

        void SoltarFichas()
        {
            int resto = fichas;
            while (resto >= 5 && fichaOuroPrefab != null)
            {
                LancarFicha(fichaOuroPrefab);
                resto -= 5;
            }
            for (int i = 0; i < resto; i++) LancarFicha(fichaPrefab);
        }

        void LancarFicha(GameObject prefab)
        {
            if (prefab == null) return;
            GameObject go = Instantiate(prefab, transform.position, Quaternion.identity);
            Ficha f = go.GetComponent<Ficha>();
            if (f != null) f.Lancar(new Vector2(Random.Range(-3.5f, 3.5f), Random.Range(6f, 9f)));
        }

        /// <summary>Sai de cena sem contar como abate (usado no reinicio da partida e na fuga do Ouros).</summary>
        public void Retirar()
        {
            if (GerenciadorDeJogo.Instancia != null) GerenciadorDeJogo.Instancia.Remover(this);
            Sair();
        }

        void Sair()
        {
            if (AoSair != null) AoSair(this);
            AoSair = null;
            Destroy(gameObject);
        }

        protected override void AoLevarDano(Vector2 origem)
        {
            TornarInvulneravel(0.05f);
            Sonoplasta.Tocar(Som.Acerto, 0.8f);
        }
    }
}
