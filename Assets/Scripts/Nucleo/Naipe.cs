namespace CartaBranca.Nucleo
{
    /// <summary>Os quatro naipes do baralho. Cada inimigo carrega um naipe,
    /// e o naipe define pontuacao e cor do efeito de morte.</summary>
    public enum Naipe
    {
        Espadas = 0,
        Copas   = 1,
        Ouros   = 2,
        Paus    = 3
    }

    /// <summary>Metodos estaticos de extensao para o enum (classe estatica + metodos estaticos).</summary>
    public static class NaipeExtensoes
    {
        public static string Simbolo(this Naipe naipe)
        {
            switch (naipe)
            {
                case Naipe.Espadas: return "\u2660";
                case Naipe.Copas:   return "\u2665";
                case Naipe.Ouros:   return "\u2666";
                case Naipe.Paus:    return "\u2663";
                default:            return "?";
            }
        }

        public static bool Vermelho(this Naipe naipe)
        {
            return naipe == Naipe.Copas || naipe == Naipe.Ouros;
        }
    }
}
