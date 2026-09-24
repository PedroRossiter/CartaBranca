using System.Collections.Generic;
using UnityEngine;

namespace CartaBranca.Mundo
{
    /// <summary>Grade de navegacao da arena e o pathfinding A* dos naipes que "pensam".
    /// Na primeira consulta, varre a arena em celulas de 0,5 u e marca como bloqueada
    /// toda celula onde o corpo de um inimigo encostaria em algo solido (a Tilemap do chao
    /// e das plataformas, as paredes). Depois disso, cada pedido de rota e so busca em grafo.</summary>
    public class MapaDeNavegacao : MonoBehaviour
    {
        public static MapaDeNavegacao Instancia { get; private set; }

        [SerializeField] Vector2 origem = new Vector2(-24f, -6f);        // canto inferior esquerdo
        [SerializeField] Vector2Int tamanho = new Vector2Int(96, 34);    // 48 x 17 unidades
        [SerializeField] float celula = 0.5f;
        [SerializeField] Vector2 folga = new Vector2(0.9f, 0.9f);        // corpo do inimigo
        [SerializeField] int limiteDeNos = 4000;

        static readonly Vector2Int[] VIZINHOS =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
        };
        static readonly Vector2Int INVALIDA = new Vector2Int(-1, -1);
        const float DIAGONAL = 1.4142f;

        bool[,] _livre;
        bool _pronto;

        // estruturas do A*, reaproveitadas entre consultas (sem lixo por quadro)
        readonly FilaDePrioridade<Vector2Int> _aberta = new FilaDePrioridade<Vector2Int>();
        readonly Dictionary<Vector2Int, Vector2Int> _veioDe = new Dictionary<Vector2Int, Vector2Int>();
        readonly Dictionary<Vector2Int, float> _custo = new Dictionary<Vector2Int, float>();
        readonly HashSet<Vector2Int> _fechada = new HashSet<Vector2Int>();

        public int CelulasLivres { get; private set; }

        void Awake()
        {
            Instancia = this;
        }

        void OnDestroy()
        {
            if (Instancia == this) Instancia = null;
        }

        // ------------------------------------------------------------ grade
        void Preparar()
        {
            if (_pronto) return;
            _pronto = true;
            Physics2D.SyncTransforms();

            _livre = new bool[tamanho.x, tamanho.y];
            ContactFilter2D filtro = new ContactFilter2D();
            filtro.useTriggers = false;
            filtro.useLayerMask = false;
            Collider2D[] buffer = new Collider2D[4];

            CelulasLivres = 0;
            for (int x = 0; x < tamanho.x; x++)
            {
                for (int y = 0; y < tamanho.y; y++)
                {
                    int n = Physics2D.OverlapBox(Centro(new Vector2Int(x, y)), folga, 0f, filtro, buffer);
                    bool livre = true;
                    for (int i = 0; i < n; i++)
                    {
                        // so cenario conta: o jogador (corpo dinamico) nao e parede
                        Rigidbody2D rb = buffer[i].attachedRigidbody;
                        if (rb == null || rb.bodyType == RigidbodyType2D.Static) { livre = false; break; }
                    }
                    _livre[x, y] = livre;
                    if (livre) CelulasLivres++;
                }
            }
        }

        public Vector2Int Celula(Vector2 p)
        {
            return new Vector2Int(Mathf.FloorToInt((p.x - origem.x) / celula),
                                  Mathf.FloorToInt((p.y - origem.y) / celula));
        }

        public Vector2 Centro(Vector2Int c)
        {
            return new Vector2(origem.x + (c.x + 0.5f) * celula, origem.y + (c.y + 0.5f) * celula);
        }

        public bool Livre(Vector2Int c)
        {
            return c.x >= 0 && c.y >= 0 && c.x < tamanho.x && c.y < tamanho.y && _livre[c.x, c.y];
        }

        public bool Livre(Vector2 mundo)
        {
            Preparar();
            return Livre(Celula(mundo));
        }

        /// <summary>Ha uma reta desimpedida entre os dois pontos? Usado para "puxar o barbante"
        /// da rota (cortar caminho quando da) e para o bote do Paus.</summary>
        public bool LinhaLivre(Vector2 a, Vector2 b)
        {
            Preparar();
            float dist = Vector2.Distance(a, b);
            int passos = Mathf.Max(1, Mathf.CeilToInt(dist / (celula * 0.5f)));
            for (int i = 1; i <= passos; i++)
            {
                if (!Livre(Celula(Vector2.Lerp(a, b, i / (float)passos)))) return false;
            }
            return true;
        }

        Vector2Int MaisProximaLivre(Vector2Int c)
        {
            if (Livre(c)) return c;
            for (int r = 1; r <= 5; r++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    for (int dy = -r; dy <= r; dy++)
                    {
                        if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue;   // so o anel
                        Vector2Int v = new Vector2Int(c.x + dx, c.y + dy);
                        if (Livre(v)) return v;
                    }
                }
            }
            return INVALIDA;
        }

        // ------------------------------------------------------------ A*
        static float Heuristica(Vector2Int a, Vector2Int b)
        {
            int dx = Mathf.Abs(a.x - b.x);
            int dy = Mathf.Abs(a.y - b.y);
            return (dx + dy) + (DIAGONAL - 2f) * Mathf.Min(dx, dy);   // distancia octil
        }

        /// <summary>Calcula a rota de "de" ate "para" e escreve os pontos em "saida".
        /// Devolve false se nao houver caminho.</summary>
        public bool Caminho(Vector2 de, Vector2 para, List<Vector2> saida)
        {
            Preparar();
            saida.Clear();

            Vector2Int inicio = MaisProximaLivre(Celula(de));
            Vector2Int fim = MaisProximaLivre(Celula(para));
            if (inicio == INVALIDA || fim == INVALIDA) return false;

            _aberta.Limpar();
            _veioDe.Clear();
            _custo.Clear();
            _fechada.Clear();

            _aberta.Enfileirar(inicio, 0f);
            _custo[inicio] = 0f;

            bool achou = false;
            int orcamento = limiteDeNos;
            while (_aberta.Count > 0 && orcamento-- > 0)
            {
                Vector2Int atual = _aberta.Desenfileirar();
                if (atual == fim) { achou = true; break; }
                if (!_fechada.Add(atual)) continue;   // ja expandido por um caminho melhor

                foreach (Vector2Int d in VIZINHOS)
                {
                    Vector2Int v = atual + d;
                    if (!Livre(v) || _fechada.Contains(v)) continue;

                    bool diagonal = d.x != 0 && d.y != 0;
                    // diagonal nao corta quina de plataforma
                    if (diagonal && (!Livre(new Vector2Int(atual.x + d.x, atual.y)) ||
                                     !Livre(new Vector2Int(atual.x, atual.y + d.y)))) continue;

                    float novo = _custo[atual] + (diagonal ? DIAGONAL : 1f);
                    float anterior;
                    if (_custo.TryGetValue(v, out anterior) && novo >= anterior) continue;

                    _custo[v] = novo;
                    _veioDe[v] = atual;
                    _aberta.Enfileirar(v, novo + Heuristica(v, fim));
                }
            }

            if (!achou) return false;

            Vector2Int passo = fim;
            saida.Add(Centro(passo));
            while (passo != inicio)
            {
                passo = _veioDe[passo];
                saida.Add(Centro(passo));
            }
            saida.Reverse();
            return true;
        }

        /// <summary>Escolhe um esconderijo: longe da ameaca, sem ser longe demais de quem foge.
        /// Usado pelo Naipe de Ouros.</summary>
        public Vector2 PontoDeFuga(Vector2 de, Vector2 ameaca)
        {
            Preparar();
            Vector2 melhor = de;
            float melhorNota = float.MinValue;
            for (int i = 0; i < 32; i++)
            {
                Vector2Int c = new Vector2Int(Random.Range(0, tamanho.x), Random.Range(0, tamanho.y));
                if (!Livre(c)) continue;
                Vector2 p = Centro(c);
                float nota = Vector2.Distance(p, ameaca)
                           - 0.45f * Vector2.Distance(p, de)
                           - (p.y < -4.5f ? 3f : 0f);          // rente ao chao e facil de pegar
                if (nota > melhorNota)
                {
                    melhorNota = nota;
                    melhor = p;
                }
            }
            return melhor;
        }

        void OnDrawGizmosSelected()
        {
            if (_livre == null) return;
            Gizmos.color = new Color(1f, 0.2f, 0.3f, 0.35f);
            for (int x = 0; x < tamanho.x; x++)
                for (int y = 0; y < tamanho.y; y++)
                    if (!_livre[x, y]) Gizmos.DrawCube(Centro(new Vector2Int(x, y)), Vector3.one * celula * 0.9f);
        }
    }
}
