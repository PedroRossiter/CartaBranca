# Carta Branca — Última Mão

Jogo de sobrevivência 2D feito em Unity 6 (6000.5.10f1) para o **Desafio Individual Unity 2 —
Jogos Digitais**. Estética *rubber-hose* dos anos 30 com paleta noir fria: você é a **Carta Branca**,
uma figura encapuzada de rosto em branco e olho carmim, encurralada numa arena por naipes hostis
que não param de chegar.

Toda a arte é **pixel art gerada proceduralmente** (script Python em `Ferramentas/gerar_arte.py`) —
nenhum asset de terceiros.

---

## Como rodar

1. Abra a pasta do projeto pelo **Unity Hub** com a versão **6000.5.10f1**.
2. Na primeira abertura o projeto se monta sozinho: texturas, animações, prefabs e a cena
   `Assets/Cenas/Arena.unity`.
3. Se quiser reconstruir tudo do zero: menu **Carta Branca ▸ Construir arena**.
4. Abra `Assets/Cenas/Arena.unity` e aperte **Play**.

## Controles

| Ação | Tecla |
|---|---|
| Correr | `A` / `D` ou setas |
| Pular | `Espaço`, `W` ou `↑` |
| Atirar carta | `J` ou botão esquerdo do mouse |
| Esquivar (dash com invulnerabilidade) | `K` ou `Shift` esquerdo |

O pulo é de altura variável (soltar a tecla corta a subida) e tem *coyote time* e *jump buffer*.

---

## Como o jogo cumpre cada regra do desafio

| Regra do enunciado | Onde está |
|---|---|
| Personagem principal com **pelo menos 3 ações** | `ControleCartaBranca` — correr, pular, atirar carta e esquivar (4 ações) |
| **Pelo menos 1 tipo de inimigo**, aparecendo várias vezes | `InimigoEspadas` (chão) e `InimigoCopas` (voador), gerados em ondas por `GeradorDeInimigos` |
| Inimigo com **≥1 animação**, personagem com **≥2** | Inimigos: 1 ciclo cada (`Espadas_Loop`, `Copas_Loop`). Carta Branca: 5 clipes — `CB_Parado`, `CB_Corre`, `CB_Pula`, `CB_Cai`, `CB_Saca` |
| Inimigo ataca o personagem | Colisão direta (`Inimigo.Encostou`); Copas ainda faz mergulhos periódicos |
| Ao ser atingido, o personagem **morre e volta ao estado inicial** | `ControleCartaBranca.Morrer()` → `GerenciadorDeJogo.MorteDoJogador()` → zera onda, pontos, tempo, limpa a arena e chama `Renascer()` |
| Personagem **ataca o inimigo** e ele some da tela | `Carta` (projétil) → `IDanificavel.Danificar` → `Inimigo.Morrer()` com efeito de estouro |
| **Respawn do inimigo** ao ser atingido | `GeradorDeInimigos.AoSairInimigo` agenda a próxima entrada; a espera encurta a cada onda |
| Inimigos **aparecem aos poucos, com dificuldade crescente** | `GeradorDeInimigos`: simultâneos, intervalo, chance de voador e vida escalam com a onda; `GerenciadorDeJogo.Escala` aumenta velocidade e pontuação |

### A curva de dificuldade, em números

A onda sobe a cada 18 segundos de sobrevivência (`GerenciadorDeJogo.segundosPorOnda`).

| O que muda | Fórmula |
|---|---|
| Inimigos simultâneos | `min(2 + onda, 12)` |
| Intervalo entre entradas | `max(0,55s ; 2,6s − 0,16 × (onda−1))` |
| Chance de Copas (voador) | `0` até a onda 2, depois `min(55% ; 12% × (onda−1))` |
| Velocidade do inimigo | `base × (1 + 0,11 × (onda−1))`, limitada a 2,4× |
| Vida do inimigo | `1`, virando `2` na onda 5 e subindo de 5 em 5 ondas |
| Espera do respawn | `max(0,25s ; 1,2s − 0,06 × (onda−1))` |
| Valor dos pontos | pontos do naipe × escala da onda |

---

## Arquitetura

```
Assets/
  Art/              38 PNGs de pixel art (gerados)
  Animacoes/        clipes .anim e AnimatorControllers (gerados pelo construtor)
  Prefabs/          Carta, NaipeEspadas, NaipeCopas, FxEstouro, FxPoeira
  Cenas/Arena.unity
  Editor/
    ConstrutorDaArena.cs    monta importação, animações, prefabs e cena
  Scripts/
    Nucleo/     Naipe.cs  IDanificavel.cs  Entidade.cs  GerenciadorDeJogo.cs  Recordes.cs
    Jogador/    ControleCartaBranca.cs  Carta.cs
    Inimigos/   Inimigo.cs  InimigoEspadas.cs  InimigoCopas.cs
    Mundo/      GeradorDeInimigos.cs  CameraSuave.cs  Paralaxe.cs  Efeito.cs
    UI/         Hud.cs
Ferramentas/
  gerar_arte.py     gerador de sprites (Python + Pillow)
```

A hierarquia de tipos é a espinha do projeto:

```
IDanificavel (interface)
      ▲
   Entidade (abstract : MonoBehaviour)
      ├── ControleCartaBranca
      └── Inimigo (abstract)
            ├── InimigoEspadas
            └── InimigoCopas
```

`Carta` (o projétil) conversa apenas com `IDanificavel` — não conhece jogador nem inimigo.
É o que permite acrescentar novos naipes sem tocar em nada do código de ataque.

## Onde cada tópico do estudo dirigido de C# aparece

| Tópico | Arquivo / trecho |
|---|---|
| Classes, atributos, construtores, métodos | todo o projeto; `Recordes` é uma classe estática pura |
| Tipos primitivos × referência, casts | `Inimigo.Morrer()` faz cast de cor por naipe; `(int)Naipe.Espadas` no construtor |
| Métodos e atributos estáticos | `GerenciadorDeJogo.Instancia`, `Recordes`, `NaipeExtensoes` |
| Namespaces | `CartaBranca.Nucleo`, `.Jogador`, `.Inimigos`, `.Mundo`, `.UI`, `.EditorTools` |
| Convenções de codificação | PascalCase em tipos/métodos/propriedades, camelCase em campos, `_campo` em privados |
| Information hiding (get/set) | `Entidade.Vida { get; protected set; }`, `ControleCartaBranca.RecargaEsquiva` |
| Encapsulamento | campos `[SerializeField]` privados expostos só por propriedades |
| Sobrecarga (overloading) | `Entidade.Danificar(int)` e `Danificar(int, Vector2)` |
| Sobrescrita (overriding) | `Morrer()` em jogador e inimigo; `Configurar()` em `InimigoCopas` |
| Polimorfismo e ligação dinâmica | `Comportamento()` chamado pela base sem saber qual naipe está rodando |
| Classes abstratas | `Entidade`, `Inimigo` |
| Interfaces | `IDanificavel`, usada por `Carta` |
| Herança e acesso à classe mãe | `base.Awake()`, `base.Danificar(...)`, `base.Configurar(...)` |
| Estruturas de controle | `switch` em `NaipeExtensoes.Simbolo`, `for`/`foreach`/`while` no construtor e no gerador |
| Enums | `Naipe` + métodos de extensão |
| Coleções genéricas | `List<Inimigo>`, `IReadOnlyList<Inimigo>`, arrays de buffer para física |
| Exceções (try/catch/finally) | `Recordes.Salvar` e as propriedades de leitura |
| Eventos / delegates | `Action<Inimigo> AoSair`, `AoReiniciar`, `AoTrocarOnda` |

---

## Roteiro sugerido para o vídeo (até 10 min)

1. **O que é** — tema, referência ao *Deadman's Hand*, por que uma arena de sobrevivência.
2. **Jogando** — 4 ações, mostrar morte por colisão e o reinício ao estado inicial.
3. **Escalada** — sobreviver até a onda 3-4 para o voador entrar e o ritmo apertar.
4. **Código** — abrir a hierarquia `Entidade → Inimigo → naipes` e mostrar `IDanificavel` desacoplando o projétil.
5. **Construtor da arena** — `Carta Branca ▸ Construir arena` reconstruindo o jogo do zero.
6. **Dificuldades e como resolvi** — contatos de corpos cinemáticos em trigger
   (`useFullKinematicContacts`), detecção de chão ignorando triggers via `ContactFilter2D`,
   e a ordem de inscrição nos eventos (o gerenciador só inicia a partida no primeiro `Update`,
   depois que todo mundo já rodou o `Start`).

## Próximo desafio

O enunciado avisa que este jogo vira base do próximo. A arquitetura já prevê isso:
`Naipe` tem os quatro naipes, mas só Espadas e Copas foram usados. Ouros e Paus entram
como novas subclasses de `Inimigo` (ou como armas do jogador, no eixo de gameplay por naipe)
sem alterar nada do que já existe.
