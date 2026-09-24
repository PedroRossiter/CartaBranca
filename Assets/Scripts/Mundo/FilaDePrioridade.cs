using System.Collections.Generic;

namespace CartaBranca.Mundo
{
    /// <summary>Fila de prioridade generica (heap binario de minimo).
    /// E o coracao do A*: sempre devolve o no com menor custo estimado em O(log n).
    /// Existe porque o .NET Standard 2.1 da Unity nao traz PriorityQueue.</summary>
    public class FilaDePrioridade<T>
    {
        readonly List<KeyValuePair<float, T>> _itens = new List<KeyValuePair<float, T>>();

        public int Count { get { return _itens.Count; } }

        public void Limpar()
        {
            _itens.Clear();
        }

        public void Enfileirar(T item, float prioridade)
        {
            _itens.Add(new KeyValuePair<float, T>(prioridade, item));

            // sobe o item ate o pai ter prioridade menor ou igual
            int i = _itens.Count - 1;
            while (i > 0)
            {
                int pai = (i - 1) / 2;
                if (_itens[pai].Key <= _itens[i].Key) break;
                Trocar(i, pai);
                i = pai;
            }
        }

        public T Desenfileirar()
        {
            T topo = _itens[0].Value;
            int ultimo = _itens.Count - 1;
            _itens[0] = _itens[ultimo];
            _itens.RemoveAt(ultimo);

            // desce o item ate os filhos serem maiores
            int i = 0;
            bool trocou;
            do
            {
                trocou = false;
                int esq = i * 2 + 1;
                int dir = esq + 1;
                int menor = i;
                if (esq < _itens.Count && _itens[esq].Key < _itens[menor].Key) menor = esq;
                if (dir < _itens.Count && _itens[dir].Key < _itens[menor].Key) menor = dir;
                if (menor != i)
                {
                    Trocar(i, menor);
                    i = menor;
                    trocou = true;
                }
            } while (trocou);

            return topo;
        }

        void Trocar(int a, int b)
        {
            KeyValuePair<float, T> t = _itens[a];
            _itens[a] = _itens[b];
            _itens[b] = t;
        }
    }
}
