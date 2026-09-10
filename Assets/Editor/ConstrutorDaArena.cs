using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using CartaBranca.Nucleo;
using CartaBranca.Jogador;
using CartaBranca.Inimigos;
using CartaBranca.Mundo;
using CartaBranca.UI;

namespace CartaBranca.EditorTools
{
    /// <summary>Monta o jogo inteiro a partir dos PNGs e dos scripts:
    /// importacao das texturas, clipes de animacao, controladores, prefabs e a cena da arena.
    /// Menu: Carta Branca > Construir arena.</summary>
    public static class ConstrutorDaArena
    {
        public const string ART   = "Assets/Art";
        public const string ANIM  = "Assets/Animacoes";
        public const string PREF  = "Assets/Prefabs";
        public const string CENAS = "Assets/Cenas";
        public const string CENA  = CENAS + "/Arena.unity";
        const int PPU = 16;

        static readonly string[] REPETIR = { "ceu", "horizonte_longe", "horizonte_perto", "tile_chao", "tile_plataforma" };

        static readonly Color INK    = new Color32(11, 13, 20, 255);
        static readonly Color PAPEL  = new Color32(233, 231, 220, 255);
        static readonly Color CARMIM = new Color32(222, 70, 92, 255);
        static readonly Color OURO   = new Color32(198, 162, 88, 255);
        static readonly Color CINZA  = new Color32(150, 166, 196, 255);

        // ---------- geometria da arena ----------
        const float TOPO_CHAO = -6f;
        const float MEIA_LARGURA = 24f;

        [MenuItem("Carta Branca/Construir arena", false, 0)]
        public static void Construir()
        {
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
            AssetDatabase.Refresh();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Pasta(ANIM); Pasta(PREF); Pasta(CENAS);
            ImportarTexturas();

            AnimatorController ctrlJogador = ControladorJogador();
            AnimatorController ctrlEspadas = ControladorSimples("Espadas", "esp_anda_", 4, 9f);
            AnimatorController ctrlCopas   = ControladorSimples("Copas", "cop_voa_", 4, 12f);
            AnimatorController ctrlCarta   = ControladorSimples("Carta", "carta_", 4, 20f);

            GameObject fxEstouro = PrefabEfeito("FxEstouro", "fx_estouro", 0.34f, 2.6f, 1.4f, 1f, PAPEL);
            GameObject fxPoeira  = PrefabEfeito("FxPoeira", "fx_poeira", 0.40f, 1.9f, 0.6f, 0.7f, new Color(1f, 1f, 1f, 0.75f));
            GameObject carta     = PrefabCarta(ctrlCarta);
            GameObject espadas   = PrefabEspadas(ctrlEspadas, fxEstouro, fxPoeira);
            GameObject copas     = PrefabCopas(ctrlCopas, fxEstouro, fxPoeira);

            MontarCena(ctrlJogador, carta, fxEstouro, fxPoeira, espadas, copas);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<b>Carta Branca</b>: arena construida. Abra " + CENA + " e aperte Play.");
        }

        // ------------------------------------------------------------------ util
        static void Pasta(string caminho)
        {
            if (!AssetDatabase.IsValidFolder(caminho))
                AssetDatabase.CreateFolder(Path.GetDirectoryName(caminho).Replace('\\', '/'), Path.GetFileName(caminho));
        }

        static Sprite S(string nome)
        {
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(ART + "/" + nome + ".png");
            if (s == null) Debug.LogWarning("sprite ausente: " + nome);
            return s;
        }

        static void Campo(Object alvo, string propriedade, System.Action<SerializedProperty> ajuste)
        {
            SerializedObject so = new SerializedObject(alvo);
            SerializedProperty p = so.FindProperty(propriedade);
            if (p == null) { Debug.LogWarning("propriedade ausente: " + propriedade); return; }
            ajuste(p);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void ImportarTexturas()
        {
            string abs = Path.Combine(Application.dataPath, "Art");
            if (!Directory.Exists(abs)) { Debug.LogError("pasta Art nao encontrada"); return; }

            foreach (string f in Directory.GetFiles(abs, "*.png", SearchOption.TopDirectoryOnly))
            {
                string caminho = ART + "/" + Path.GetFileName(f);
                TextureImporter ti = AssetImporter.GetAtPath(caminho) as TextureImporter;
                if (ti == null) continue;

                string nome = Path.GetFileNameWithoutExtension(caminho);
                bool repete = System.Array.IndexOf(REPETIR, nome) >= 0;

                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = PPU;
                ti.filterMode = FilterMode.Point;
                ti.mipmapEnabled = false;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.alphaIsTransparency = true;
                ti.wrapMode = repete ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;

                TextureImporterSettings set = new TextureImporterSettings();
                ti.ReadTextureSettings(set);
                set.spriteMeshType = SpriteMeshType.FullRect;
                set.spriteAlignment = (int)SpriteAlignment.Center;
                ti.SetTextureSettings(set);
                ti.SaveAndReimport();
            }
        }

        // ------------------------------------------------------------------ animacao
        static AnimationClip Clipe(string nome, string prefixo, int quadros, float fps, bool loop)
        {
            AnimationClip clip = new AnimationClip();
            clip.frameRate = fps;

            EditorCurveBinding b = new EditorCurveBinding();
            b.type = typeof(SpriteRenderer);
            b.path = "";
            b.propertyName = "m_Sprite";

            ObjectReferenceKeyframe[] chaves = new ObjectReferenceKeyframe[quadros];
            for (int i = 0; i < quadros; i++)
            {
                chaves[i] = new ObjectReferenceKeyframe();
                chaves[i].time = i / fps;
                chaves[i].value = S(prefixo + i);
            }
            AnimationUtility.SetObjectReferenceCurve(clip, b, chaves);

            AnimationClipSettings st = AnimationUtility.GetAnimationClipSettings(clip);
            st.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, st);

            string p = ANIM + "/" + nome + ".anim";
            AssetDatabase.DeleteAsset(p);
            AssetDatabase.CreateAsset(clip, p);
            return clip;
        }

        static AnimatorController ControladorSimples(string nome, string prefixo, int quadros, float fps)
        {
            AnimationClip c = Clipe(nome + "_Loop", prefixo, quadros, fps, true);
            string p = ANIM + "/" + nome + ".controller";
            AssetDatabase.DeleteAsset(p);
            AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(p);
            AnimatorState st = ctrl.layers[0].stateMachine.AddState("Loop");
            st.motion = c;
            ctrl.layers[0].stateMachine.defaultState = st;
            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        static AnimatorController ControladorJogador()
        {
            AnimationClip parado = Clipe("CB_Parado", "cb_parado_", 4, 8f, true);
            AnimationClip corre  = Clipe("CB_Corre",  "cb_corre_",  6, 14f, true);
            AnimationClip pula   = Clipe("CB_Pula",   "cb_pula_",   1, 8f, false);
            AnimationClip cai    = Clipe("CB_Cai",    "cb_cai_",    1, 8f, false);
            AnimationClip saca   = Clipe("CB_Saca",   "cb_saca_",   3, 18f, false);

            string p = ANIM + "/CartaBranca.controller";
            AssetDatabase.DeleteAsset(p);
            AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(p);
            ctrl.AddParameter("Velocidade", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("VelY", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("NoChao", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("Atirar", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
            AnimatorState sParado = sm.AddState("Parado", new Vector3(260f, 0f, 0f));   sParado.motion = parado;
            AnimatorState sCorre  = sm.AddState("Corre",  new Vector3(260f, 80f, 0f));  sCorre.motion  = corre;
            AnimatorState sPula   = sm.AddState("Pula",   new Vector3(500f, 0f, 0f));   sPula.motion   = pula;
            AnimatorState sCai    = sm.AddState("Cai",    new Vector3(500f, 80f, 0f));  sCai.motion    = cai;
            AnimatorState sSaca   = sm.AddState("Saca",   new Vector3(260f, 170f, 0f)); sSaca.motion   = saca;
            sm.defaultState = sParado;

            Liga(sParado, sCorre, "Velocidade", AnimatorConditionMode.Greater, 0.15f);
            Liga(sCorre, sParado, "Velocidade", AnimatorConditionMode.Less, 0.15f);

            AnimatorStateTransition qualquerPula = sm.AddAnyStateTransition(sPula);
            qualquerPula.hasExitTime = false; qualquerPula.duration = 0f;
            qualquerPula.canTransitionToSelf = false;
            qualquerPula.AddCondition(AnimatorConditionMode.IfNot, 0f, "NoChao");
            qualquerPula.AddCondition(AnimatorConditionMode.Greater, 0.5f, "VelY");

            AnimatorStateTransition qualquerCai = sm.AddAnyStateTransition(sCai);
            qualquerCai.hasExitTime = false; qualquerCai.duration = 0f;
            qualquerCai.canTransitionToSelf = false;
            qualquerCai.AddCondition(AnimatorConditionMode.IfNot, 0f, "NoChao");
            qualquerCai.AddCondition(AnimatorConditionMode.Less, 0.5f, "VelY");

            AnimatorStateTransition qualquerSaca = sm.AddAnyStateTransition(sSaca);
            qualquerSaca.hasExitTime = false; qualquerSaca.duration = 0f;
            qualquerSaca.canTransitionToSelf = false;
            qualquerSaca.AddCondition(AnimatorConditionMode.If, 0f, "Atirar");

            Volta(sPula, sParado, "NoChao", true);
            Volta(sCai, sParado, "NoChao", true);
            Volta(sSaca, sParado, null, false);

            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        static void Liga(AnimatorState de, AnimatorState para, string parametro, AnimatorConditionMode modo, float valor)
        {
            AnimatorStateTransition t = de.AddTransition(para);
            t.hasExitTime = false;
            t.duration = 0.02f;
            t.AddCondition(modo, valor, parametro);
        }

        static void Volta(AnimatorState de, AnimatorState para, string parametro, bool exigeVerdadeiro)
        {
            AnimatorStateTransition t = de.AddTransition(para);
            if (parametro == null)
            {
                t.hasExitTime = true;
                t.exitTime = 0.9f;
                t.duration = 0.05f;
            }
            else
            {
                t.hasExitTime = false;
                t.duration = 0.05f;
                t.AddCondition(exigeVerdadeiro ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parametro);
            }
        }

        // ------------------------------------------------------------------ prefabs
        static GameObject Salvar(GameObject go, string nome)
        {
            string p = PREF + "/" + nome + ".prefab";
            GameObject pre = PrefabUtility.SaveAsPrefabAsset(go, p);
            Object.DestroyImmediate(go);
            return pre;
        }

        static GameObject PrefabEfeito(string nome, string sprite, float dur, float cresc, float sobe, float escala, Color cor)
        {
            GameObject go = new GameObject(nome);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = S(sprite);
            sr.sortingOrder = 60;
            sr.color = cor;
            go.transform.localScale = Vector3.one * escala;

            Efeito e = go.AddComponent<Efeito>();
            e.duracao = dur; e.crescimento = cresc; e.subida = sobe;
            return Salvar(go, nome);
        }

        static GameObject PrefabCarta(AnimatorController ctrl)
        {
            GameObject go = new GameObject("Carta");
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = S("carta_0");
            sr.sortingOrder = 45;

            Animator an = go.AddComponent<Animator>();
            an.runtimeAnimatorController = ctrl;
            an.applyRootMotion = false;

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.useFullKinematicContacts = true;

            CircleCollider2D cc = go.AddComponent<CircleCollider2D>();
            cc.isTrigger = true;
            cc.radius = 0.24f;

            go.AddComponent<Carta>();
            return Salvar(go, "Carta");
        }

        static void CorpoInimigo(GameObject go, string sprite, AnimatorController ctrl, float raio, Vector2 offset)
        {
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = S(sprite);
            sr.sortingOrder = 30;

            Animator an = go.AddComponent<Animator>();
            an.runtimeAnimatorController = ctrl;
            an.applyRootMotion = false;

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.useFullKinematicContacts = true;

            CircleCollider2D cc = go.AddComponent<CircleCollider2D>();
            cc.isTrigger = true;
            cc.radius = raio;
            cc.offset = offset;
        }

        static GameObject PrefabEspadas(AnimatorController ctrl, GameObject estouro, GameObject poeira)
        {
            GameObject go = new GameObject("NaipeEspadas");
            CorpoInimigo(go, "esp_anda_0", ctrl, 0.5f, new Vector2(0f, -0.1f));

            InimigoEspadas i = go.AddComponent<InimigoEspadas>();
            i.explosaoPrefab = estouro;
            i.poeiraPrefab = poeira;
            Campo(i, "naipe", p => p.enumValueIndex = (int)Naipe.Espadas);
            Campo(i, "pontos", p => p.intValue = 10);
            Campo(i, "velocidadeBase", p => p.floatValue = 3.1f);
            Campo(i, "alturaPes", p => p.floatValue = 0.72f);
            Campo(i, "vidaMaxima", p => p.intValue = 1);
            return Salvar(go, "NaipeEspadas");
        }

        static GameObject PrefabCopas(AnimatorController ctrl, GameObject estouro, GameObject poeira)
        {
            GameObject go = new GameObject("NaipeCopas");
            CorpoInimigo(go, "cop_voa_0", ctrl, 0.46f, Vector2.zero);

            InimigoCopas i = go.AddComponent<InimigoCopas>();
            i.explosaoPrefab = estouro;
            i.poeiraPrefab = poeira;
            Campo(i, "naipe", p => p.enumValueIndex = (int)Naipe.Copas);
            Campo(i, "pontos", p => p.intValue = 18);
            Campo(i, "velocidadeBase", p => p.floatValue = 4.3f);
            Campo(i, "vidaMaxima", p => p.intValue = 1);
            return Salvar(go, "NaipeCopas");
        }

        // ------------------------------------------------------------------ cena
        static GameObject Tiled(string nome, string sprite, Vector3 pos, Vector2 tam, int ordem, Transform pai, Color tinta)
        {
            GameObject go = new GameObject(nome);
            if (pai != null) go.transform.SetParent(pai);
            go.transform.position = pos;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = S(sprite);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = tam;
            sr.sortingOrder = ordem;
            sr.color = tinta;
            return go;
        }

        static GameObject Plataforma(string nome, Vector3 pos, float largura, Transform pai)
        {
            GameObject go = Tiled(nome, "tile_plataforma", pos, new Vector2(largura, 0.5f), 12, pai, Color.white);
            BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
            bc.size = new Vector2(largura, 0.5f);
            return go;
        }

        static Transform Ponto(string nome, Vector3 pos, Transform pai)
        {
            GameObject go = new GameObject(nome);
            go.transform.SetParent(pai);
            go.transform.position = pos;
            return go.transform;
        }

        static Text Texto(Transform canvas, string nome, Vector2 ancora, Vector2 offset, Vector2 tam,
                          int corpo, TextAnchor alinha, Color cor)
        {
            GameObject go = new GameObject(nome, typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = ancora; rt.anchorMax = ancora; rt.pivot = ancora;
            rt.anchoredPosition = offset; rt.sizeDelta = tam;

            Text t = go.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = corpo;
            t.alignment = alinha;
            t.color = cor;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.text = "";

            Outline o = go.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.95f);
            o.effectDistance = new Vector2(2f, -2f);
            return t;
        }

        static void LimparCenaAtual()
        {
            UnityEngine.SceneManagement.Scene cena = EditorSceneManager.GetActiveScene();
            GameObject[] raizes = cena.GetRootGameObjects();
            for (int i = raizes.Length - 1; i >= 0; i--) Object.DestroyImmediate(raizes[i]);
        }

        static void MontarCena(AnimatorController ctrlJogador, GameObject cartaPrefab,
                               GameObject fxEstouro, GameObject fxPoeira,
                               GameObject espadasPrefab, GameObject copasPrefab)
        {
            LimparCenaAtual();

            // ---------- camera ----------
            GameObject camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            Camera cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 8f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = INK;
            cam.transform.position = new Vector3(0f, -3f, -10f);
            camGO.AddComponent<AudioListener>();
            CameraSuave rig = camGO.AddComponent<CameraSuave>();
            rig.limiteEsquerda = -MEIA_LARGURA + 14.5f;
            rig.limiteDireita  =  MEIA_LARGURA - 14.5f;
            rig.limiteBaixo = -3.4f;
            rig.limiteCima = 4f;

            // ---------- ceu preso na camera ----------
            GameObject ceu = new GameObject("Ceu");
            ceu.transform.SetParent(camGO.transform);
            ceu.transform.localPosition = new Vector3(0f, 0f, 30f);
            ceu.transform.localScale = new Vector3(80f, 5f, 1f);
            SpriteRenderer ceuSR = ceu.AddComponent<SpriteRenderer>();
            ceuSR.sprite = S("ceu");
            ceuSR.sortingOrder = -300;

            // ---------- fundo ----------
            GameObject fundo = new GameObject("Fundo");

            GameObject lua = new GameObject("Lua");
            lua.transform.SetParent(fundo.transform);
            lua.transform.position = new Vector3(6f, 6.5f, 20f);
            lua.transform.localScale = Vector3.one * 1.6f;
            SpriteRenderer luaSR = lua.AddComponent<SpriteRenderer>();
            luaSR.sprite = S("lua");
            luaSR.sortingOrder = -260;
            Paralaxe pl = lua.AddComponent<Paralaxe>(); pl.fator = 0.08f; pl.alturaFixa = 6.5f;

            GameObject longe = Tiled("HorizonteLonge", "horizonte_longe", new Vector3(0f, -3.4f, 18f),
                                     new Vector2(160f, 3f), -220, fundo.transform, new Color(1f, 1f, 1f, 0.85f));
            Paralaxe p1 = longe.AddComponent<Paralaxe>(); p1.fator = 0.22f; p1.alturaFixa = -3.4f;

            GameObject perto = Tiled("HorizontePerto", "horizonte_perto", new Vector3(0f, -4.6f, 16f),
                                     new Vector2(160f, 3f), -210, fundo.transform, Color.white);
            Paralaxe p2 = perto.AddComponent<Paralaxe>(); p2.fator = 0.42f; p2.alturaFixa = -4.6f;

            // ---------- arena ----------
            GameObject arena = new GameObject("Arena");

            GameObject chao = Tiled("Chao", "tile_chao", new Vector3(0f, TOPO_CHAO - 3f, 0f),
                                    new Vector2(MEIA_LARGURA * 2f + 4f, 6f), 10, arena.transform, Color.white);
            BoxCollider2D chaoBC = chao.AddComponent<BoxCollider2D>();
            chaoBC.size = new Vector2(MEIA_LARGURA * 2f + 4f, 6f);

            for (int lado = -1; lado <= 1; lado += 2)
            {
                GameObject parede = new GameObject(lado < 0 ? "ParedeEsquerda" : "ParedeDireita");
                parede.transform.SetParent(arena.transform);
                parede.transform.position = new Vector3(lado * (MEIA_LARGURA + 0.5f), 4f, 0f);
                BoxCollider2D bc = parede.AddComponent<BoxCollider2D>();
                bc.size = new Vector2(1f, 40f);
            }

            float[,] plats = {
                { -18f,  -2.4f, 5f }, { -11f,  0.6f, 6f }, { -4.5f, -1.8f, 5f },
                {  3f,    1.2f, 6f }, {  10.5f, -1.2f, 5f }, { 17f,  1.8f, 6f },
                { -14f,   4.2f, 4f }, {  0f,    4.6f, 5f }, { 13.5f, 5.0f, 4f }
            };
            for (int i = 0; i < plats.GetLength(0); i++)
                Plataforma("Plataforma" + (i + 1), new Vector3(plats[i, 0], plats[i, 1], 0f), plats[i, 2], arena.transform);

            // ---------- gerenciador ----------
            GameObject jogoGO = new GameObject("GerenciadorDeJogo");
            jogoGO.AddComponent<GerenciadorDeJogo>();

            // ---------- Carta Branca ----------
            GameObject jog = new GameObject("CartaBranca");
            jog.tag = "Player";
            jog.transform.position = new Vector3(0f, TOPO_CHAO + 1.2f, 0f);

            SpriteRenderer jsr = jog.AddComponent<SpriteRenderer>();
            jsr.sprite = S("cb_parado_0");
            jsr.sortingOrder = 40;

            Animator jan = jog.AddComponent<Animator>();
            jan.runtimeAnimatorController = ctrlJogador;
            jan.applyRootMotion = false;

            Rigidbody2D jrb = jog.AddComponent<Rigidbody2D>();
            jrb.gravityScale = 3.6f;
            jrb.freezeRotation = true;
            jrb.interpolation = RigidbodyInterpolation2D.Interpolate;
            jrb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            CapsuleCollider2D jcc = jog.AddComponent<CapsuleCollider2D>();
            jcc.direction = CapsuleDirection2D.Vertical;
            jcc.size = new Vector2(0.9f, 1.75f);
            jcc.offset = new Vector2(0f, 0.02f);

            GameObject mao = new GameObject("Mao");
            mao.transform.SetParent(jog.transform);
            mao.transform.localPosition = new Vector3(0.7f, 0.35f, 0f);

            ControleCartaBranca ctrl = jog.AddComponent<ControleCartaBranca>();
            ctrl.mao = mao.transform;
            ctrl.cartaPrefab = cartaPrefab;
            ctrl.impactoPrefab = fxEstouro;
            ctrl.explosaoPrefab = fxEstouro;
            ctrl.poeiraPrefab = fxPoeira;
            ctrl.animador = jan;
            Campo(ctrl, "vidaMaxima", p => p.intValue = 1);

            rig.alvo = jog.transform;

            // ---------- gerador ----------
            GameObject gerGO = new GameObject("GeradorDeInimigos");
            GeradorDeInimigos ger = gerGO.AddComponent<GeradorDeInimigos>();
            ger.espadasPrefab = espadasPrefab;
            ger.copasPrefab = copasPrefab;

            Transform pastaChao = Ponto("PontosChao", Vector3.zero, gerGO.transform);
            Transform pastaAr   = Ponto("PontosAr", Vector3.zero, gerGO.transform);

            float[] xChao = { -22f, -16f, -9f, 9f, 16f, 22f };
            foreach (float x in xChao)
                ger.pontosChao.Add(Ponto("Chao" + x, new Vector3(x, TOPO_CHAO + 1.5f, 0f), pastaChao));

            float[] xAr = { -19f, -8f, 8f, 19f };
            foreach (float x in xAr)
                ger.pontosAr.Add(Ponto("Ar" + x, new Vector3(x, 8.5f, 0f), pastaAr));

            // ---------- HUD ----------
            GameObject canvasGO = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler cs = canvasGO.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920f, 1080f);
            cs.matchWidthOrHeight = 0.5f;
            Transform ct = canvasGO.transform;

            Hud hud = canvasGO.AddComponent<Hud>();
            hud.onda    = Texto(ct, "Onda",    new Vector2(0f, 1f), new Vector2(48f, -44f),  new Vector2(500f, 60f), 46, TextAnchor.UpperLeft, CARMIM);
            hud.pontos  = Texto(ct, "Pontos",  new Vector2(0f, 1f), new Vector2(48f, -104f), new Vector2(700f, 50f), 34, TextAnchor.UpperLeft, PAPEL);
            hud.tempo   = Texto(ct, "Tempo",   new Vector2(1f, 1f), new Vector2(-48f, -44f), new Vector2(400f, 60f), 46, TextAnchor.UpperRight, PAPEL);
            hud.recorde = Texto(ct, "Recorde", new Vector2(1f, 1f), new Vector2(-48f, -104f), new Vector2(500f, 50f), 30, TextAnchor.UpperRight, OURO);
            hud.aviso   = Texto(ct, "Aviso",   new Vector2(0.5f, 0.5f), new Vector2(0f, 180f), new Vector2(1400f, 70f), 48, TextAnchor.MiddleCenter, PAPEL);
            hud.ajuda   = Texto(ct, "Ajuda",   new Vector2(0.5f, 0f), new Vector2(0f, 40f),  new Vector2(1600f, 50f), 28, TextAnchor.LowerCenter, CINZA);

            GameObject barraFundo = new GameObject("EsquivaFundo", typeof(RectTransform));
            barraFundo.transform.SetParent(ct, false);
            RectTransform bfrt = barraFundo.GetComponent<RectTransform>();
            bfrt.anchorMin = new Vector2(0f, 0f); bfrt.anchorMax = new Vector2(0f, 0f); bfrt.pivot = new Vector2(0f, 0f);
            bfrt.anchoredPosition = new Vector2(48f, 48f); bfrt.sizeDelta = new Vector2(260f, 18f);
            Image bfi = barraFundo.AddComponent<Image>();
            bfi.sprite = S("pixel");
            bfi.color = new Color(0f, 0f, 0f, 0.55f);

            GameObject barra = new GameObject("EsquivaBarra", typeof(RectTransform));
            barra.transform.SetParent(barraFundo.transform, false);
            RectTransform brt = barra.GetComponent<RectTransform>();
            brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
            brt.offsetMin = new Vector2(2f, 2f); brt.offsetMax = new Vector2(-2f, -2f);
            Image bi = barra.AddComponent<Image>();
            bi.sprite = S("pixel");
            bi.type = Image.Type.Filled;
            bi.fillMethod = Image.FillMethod.Horizontal;
            bi.fillOrigin = (int)Image.OriginHorizontal.Left;
            bi.fillAmount = 1f;
            hud.barraEsquiva = bi;

            // ---------- salvar ----------
            UnityEngine.SceneManagement.Scene cena = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena, CENA);

            SceneAsset asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(CENA);
            if (asset != null)
            {
                EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
                {
                    new EditorBuildSettingsScene(CENA, true)
                };
            }
        }
    }

    /// <summary>Na primeira vez que o projeto abre, monta a arena sozinho.</summary>
    [InitializeOnLoad]
    public static class ConstrucaoAutomatica
    {
        static ConstrucaoAutomatica()
        {
            EditorApplication.delayCall += Verificar;
        }

        static void Verificar()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += Verificar;
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ConstrutorDaArena.CENA) != null) return;

            ConstrutorDaArena.Construir();
            EditorSceneManager.OpenScene(ConstrutorDaArena.CENA);
        }
    }
}
