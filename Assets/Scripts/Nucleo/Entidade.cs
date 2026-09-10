using UnityEngine;

namespace CartaBranca.Nucleo
{
    /// <summary>Classe abstrata base de tudo que tem vida na arena.
    /// Concentra vida, invulnerabilidade temporaria e o piscar de dano.
    /// Subclasses obrigatoriamente definem o que acontece ao morrer.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public abstract class Entidade : MonoBehaviour, IDanificavel
    {
        [Header("Vida")]
        [SerializeField] protected int vidaMaxima = 1;
        [SerializeField] protected float tempoInvulneravel = 0f;

        // Information hiding: o campo so muda por dentro, mas qualquer um pode ler.
        public int Vida { get; protected set; }
        public bool Vivo { get { return Vida > 0; } }
        public int VidaMaxima { get { return vidaMaxima; } }

        protected SpriteRenderer Corpo { get; private set; }

        float _invulneravelAte;
        Color _corOriginal;

        protected virtual void Awake()
        {
            Corpo = GetComponent<SpriteRenderer>();
            _corOriginal = Corpo.color;
            Vida = vidaMaxima;
        }

        protected virtual void OnEnable()
        {
            Vida = vidaMaxima;
            _invulneravelAte = 0f;
            if (Corpo != null) Corpo.color = _corOriginal;
        }

        /// <summary>Sobrecarga (overloading): versao curta que assume a propria posicao como origem.</summary>
        public void Danificar(int dano)
        {
            Danificar(dano, transform.position);
        }

        /// <summary>Metodo virtual: as subclasses estendem chamando base.Danificar(...).</summary>
        public virtual void Danificar(int dano, Vector2 origem)
        {
            if (!Vivo || Invulneravel) return;

            Vida -= Mathf.Max(1, dano);
            if (tempoInvulneravel > 0f) _invulneravelAte = Time.time + tempoInvulneravel;

            if (Vida <= 0) Morrer();
            else AoLevarDano(origem);
        }

        public bool Invulneravel { get { return Time.time < _invulneravelAte; } }

        public void TornarInvulneravel(float segundos)
        {
            _invulneravelAte = Mathf.Max(_invulneravelAte, Time.time + segundos);
        }

        /// <summary>Gancho opcional para reagir ao dano sem morrer.</summary>
        protected virtual void AoLevarDano(Vector2 origem) { }

        /// <summary>Metodo abstrato: cada entidade morre do seu jeito (ligacao dinamica).</summary>
        protected abstract void Morrer();

        protected virtual void Update()
        {
            if (Corpo == null) return;
            // pisca enquanto invulneravel
            if (Invulneravel)
            {
                float a = Mathf.PingPong(Time.time * 12f, 1f) * 0.6f + 0.4f;
                Corpo.color = new Color(_corOriginal.r, _corOriginal.g, _corOriginal.b, a);
            }
            else if (Corpo.color != _corOriginal)
            {
                Corpo.color = _corOriginal;
            }
        }

        public void RestaurarVida()
        {
            Vida = vidaMaxima;
        }

        public void DefinirVidaMaxima(int novo)
        {
            vidaMaxima = Mathf.Max(1, novo);
            Vida = vidaMaxima;
        }
    }
}
