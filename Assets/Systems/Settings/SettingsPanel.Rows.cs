using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Conteúdo das abas do SettingsPanel: linha = nome (com dica pequena embaixo) à esquerda e controle à direita.
// Controles no kit pixel: barra (Px_BarFrame/Px_BarFill) com pegador desenhado, botões de escolha (o escolhido é o
// dourado) e botão de mudo com alto-falante. Cada controle se redesenha a partir do SettingsStore.Current.
public partial class SettingsPanel
{
    [Header("ROWS")]
    [SerializeField] private float rowHeight = 84f;
    [SerializeField] private float rowTop = 24f;
    [SerializeField] private float rowPad = 28f;
    [SerializeField] private float labelWidth = 440f;
    [SerializeField] private float labelSize = 30f;
    [SerializeField] private float hintSize = 20f;
    [SerializeField] private float controlWidth = 520f;
    [SerializeField] private float controlHeight = 64f;
    [SerializeField] private float controlFont = 26f;
    [SerializeField] private float sliderHeight = 30f;
    [SerializeField] private float muteSize = 72f;
    [SerializeField] private float percentWidth = 100f;
    [SerializeField] private float creditsSize = 24f;
    [SerializeField] private Color lineColor = new Color(.66f, .58f, .71f, .22f);

    private static readonly string[] OffOn = { "DESLIGADO", "LIGADO" };

    private Action refreshFullscreen;

    #region PAGES

    private void BuildSoundPage(RectTransform page)
    {
        VolumeRow(page, 0, "VOLUME GERAL", null, s => s.master, (s, v) => s.master = v, s => s.masterMuted, (s, m) => s.masterMuted = m);
        VolumeRow(page, 1, "MÚSICA", null, s => s.music, (s, v) => s.music = v, s => s.musicMuted, (s, m) => s.musicMuted = m);
        VolumeRow(page, 2, "EFEITOS", "desastres, equipes e cliques", s => s.sfx, (s, v) => s.sfx = v, s => s.sfxMuted, (s, m) => s.sfxMuted = m);
        VolumeRow(page, 3, "AMBIENTE", "cidade, chuva e fogo", s => s.ambience, (s, v) => s.ambience = v, s => s.ambienceMuted, (s, m) => s.ambienceMuted = m);
    }

    private void BuildScreenPage(RectTransform page)
    {
        //Tela cheia é do sistema (o navegador sai dela no Esc): lida da tela a cada quadro, não guardada
        refreshFullscreen = ChoiceRow(page, 0, "TELA CHEIA", "também sai com Esc no navegador", new[] { "JANELA", "TELA CHEIA" },
            () => Screen.fullScreen ? 1 : 0, i => Screen.fullScreen = i == 1);
        StoreChoice(page, 1, "TAMANHO DA INTERFACE", "botões e textos do HUD", new[] { "PEQUENO", "MÉDIO", "GRANDE" },
            s => (int)s.uiSize, (s, i) => s.uiSize = (UiSize)i);
        StoreChoice(page, 2, "PARTÍCULAS", "reduzido ajuda PC mais fraco", new[] { "REDUZIDO", "COMPLETO" },
            s => s.fullParticles ? 1 : 0, (s, i) => s.fullParticles = i == 1);
        StoreChoice(page, 3, "MOSTRAR FPS", "quadros por segundo no canto", OffOn, s => s.showFps ? 1 : 0, (s, i) => s.showFps = i == 1);
        StoreChoice(page, 4, "MODO DALTÔNICO", "cores seguras nos pinos de chamado", OffOn, s => s.colorblind ? 1 : 0, (s, i) => s.colorblind = i == 1);
    }

    private void BuildGamePage(RectTransform page)
    {
        StoreChoice(page, 0, "TREMOR DE TELA", "terremoto, raio e meteoro", OffOn, s => s.shake ? 1 : 0, (s, i) => s.shake = i == 1, PreviewShake);
        VolumeRow(page, 1, "INTENSIDADE DO TREMOR", null, s => s.shakeIntensity, (s, v) => { s.shakeIntensity = v; if (v > 0f) s.shake = true; }, null, null, PreviewShake);
        bool haptics = GameHaptics.Supported;
        StoreChoice(page, 2, "VIBRAÇÃO DO CONTROLE", haptics ? "controle ligado no PC" : "só no jogo para PC", OffOn,
            s => s.vibration ? 1 : 0, (s, i) => s.vibration = i == 1, null, haptics);
        StoreChoice(page, 3, "DICAS E TUTORIAIS", "balões de ajuda na partida", OffOn, s => s.tips ? 1 : 0, (s, i) => s.tips = i == 1, SkipRunningTutorial);
        ActionRow(page, 4, "REVER TUTORIAIS", "mostra tudo de novo", "MOSTRAR DE NOVO", ReviewTutorials);
    }

    private void BuildCreditsPage(RectTransform page)
    {
        string gold = "<color=#" + ColorUtility.ToHtmlStringRGB(accentColor) + ">";
        string text =
            gold + "DEFESA CIVIL</color>\n" +
            "jogo de game jam, tema Citizen Tech\n\n" +
            gold + "CRIAÇÃO</color>\n" +
            "Pietro Piccoli, com apoio de IA (Claude e PixelLab)\n\n" +
            gold + "FERRAMENTAS</color>\n" +
            "Unity 6, DOTween (Demigiant), Feel e Nice Vibrations (More Mountains),\n" +
            "Text Animator (Febucci), Odin Inspector (Sirenix)\n\n" +
            gold + "SOM E FONTE</color>\n" +
            "Gamemaster Audio Pro Sound Collection, Sonniss GDC 2026 Bundle,\n" +
            "música de síntese própria, fonte Modern DOS (Jayvee Enaguas, CC0)";

        TMP_Text credits = FeelUI.CreateText("Credits", page, text, creditsSize, labelColor);
        credits.lineSpacing = 8f;
        credits.textWrappingMode = TextWrappingModes.Normal;
        FeelUI.Stretch(credits.rectTransform, 24f);
    }

    #endregion

    #region ROWS

    private RectTransform Row(RectTransform page, int index, string label, string hint)
    {
        RectTransform row = FeelUI.CreateRect("Row " + label, page);
        row.anchorMin = new Vector2(0f, 1f);
        row.anchorMax = Vector2.one;
        row.pivot = new Vector2(.5f, 1f);
        row.sizeDelta = new Vector2(0f, rowHeight);
        row.anchoredPosition = new Vector2(0f, -rowTop - index * rowHeight);

        Vector2 left = new Vector2(0f, .5f);
        TMP_Text lbl = FeelUI.CreateText("Lbl", row, label, labelSize, labelColor, TextAlignmentOptions.Left);
        FeelUI.Place(lbl.rectTransform, left, left, new Vector2(rowPad, hint != null ? 12f : 0f), new Vector2(labelWidth, labelSize * 1.3f));

        if (hint != null)
        {
            TMP_Text hintLbl = FeelUI.CreateText("Hint", row, hint, hintSize, hintColor, TextAlignmentOptions.Left);
            FeelUI.Place(hintLbl.rectTransform, left, left, new Vector2(rowPad, -20f), new Vector2(labelWidth, hintSize * 1.3f));
        }

        //Linha fina entre as linhas
        Image line = FeelUI.CreateImage("Line", row, lineColor);
        line.rectTransform.anchorMin = Vector2.zero;
        line.rectTransform.anchorMax = new Vector2(1f, 0f);
        line.rectTransform.pivot = new Vector2(.5f, 0f);
        line.rectTransform.offsetMin = new Vector2(rowPad, 0f);
        line.rectTransform.offsetMax = new Vector2(-rowPad, FeelUI.PixelSize);
        return row;
    }

    // Área do controle, colada à direita da linha
    private RectTransform ControlArea(RectTransform row)
    {
        RectTransform area = FeelUI.CreateRect("Control", row);
        Vector2 right = new Vector2(1f, .5f);
        FeelUI.Place(area, right, right, new Vector2(-rowPad, 0f), new Vector2(controlWidth, controlHeight));
        return area;
    }

    // Barra de 0 a 100% com o número ao lado e, se tiver, o botão de mudo. Arrastar não grava (grava ao soltar).
    private void VolumeRow(RectTransform page, int index, string label, string hint, Func<GameSettings, float> get, Action<GameSettings, float> set,
        Func<GameSettings, bool> getMute, Action<GameSettings, bool> setMute, Action onRelease = null)
    {
        RectTransform area = ControlArea(Row(page, index, label, hint));
        Vector2 right = new Vector2(1f, .5f), left = new Vector2(0f, .5f);
        bool hasMute = getMute != null;

        Image muteIcon = null;
        if (hasMute)
        {
            Button mute = FeelUI.CreateRoundButton("Mute", area, "M", new Vector2(muteSize, muteSize), FeelUI.PanelColor, 26f, SettingsIcons.Speaker);
            FeelUI.Place((RectTransform)mute.transform, right, right, Vector2.zero, new Vector2(muteSize, muteSize));
            Transform icon = mute.transform.Find("Icon");
            muteIcon = icon != null ? icon.GetComponent<Image>() : null;
            mute.onClick.AddListener(() => SettingsStore.Change(s => setMute(s, !getMute(s))));
        }

        float numberRight = hasMute ? muteSize + 14f : 0f;
        TMP_Text percent = FeelUI.CreateText("Percent", area, "100%", controlFont, labelColor, TextAlignmentOptions.Right);
        FeelUI.Place(percent.rectTransform, right, right, new Vector2(-numberRight, 0f), new Vector2(percentWidth, controlFont * 1.3f));

        float sliderWidth = controlWidth - numberRight - percentWidth - 20f;
        Slider slider = CreateSlider(area, new Vector2(sliderWidth, sliderHeight));
        FeelUI.Place((RectTransform)slider.transform, left, left, Vector2.zero, new Vector2(sliderWidth, sliderHeight));
        Image fill = slider.fillRect.GetComponent<Image>();

        //Mexer na barra tira o mudo dela
        slider.onValueChanged.AddListener(v => SettingsStore.Change(s =>
        {
            set(s, v);
            if (hasMute && v > 0f && getMute(s)) setMute(s, false);
        }, false));

        EventTrigger trigger = slider.gameObject.AddComponent<EventTrigger>();
        EventTrigger.Entry up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        up.callback.AddListener(_ =>
        {
            SettingsStore.Save();
            //Prévia: o clique sai no volume de efeitos novo (a interface segue os efeitos)
            if (AudioManager.HasSound("ui_click")) AudioManager.Play("ui_click");
            onRelease?.Invoke();
        });
        trigger.triggers.Add(up);

        refreshers.Add(() =>
        {
            GameSettings s = SettingsStore.Current;
            bool muted = hasMute && getMute(s);
            float value = get(s);
            slider.SetValueWithoutNotify(value);
            percent.text = muted ? "MUDO" : GameSettings.Percent(value);
            percent.color = muted ? hintColor : labelColor;
            fill.color = muted ? hintColor : accentColor;
            if (muteIcon != null) muteIcon.sprite = muted ? SettingsIcons.Muted : SettingsIcons.Speaker;
        });
    }

    private Slider CreateSlider(Transform parent, Vector2 size)
    {
        FeelUITheme px = FeelUI.Pixel;
        float pixel = FeelUI.PixelSize;
        RectTransform root = FeelUI.CreateRect("Slider", parent);
        root.sizeDelta = size;

        Image back = px != null ? FeelUI.CreateSliced("Back", root, px.barFrame, Color.white) : FeelUI.CreatePanel("Back", root, FeelUI.PanelColor);
        back.raycastTarget = true;
        FeelUI.Stretch(back.rectTransform);

        //Recheio dentro da moldura de 3 pixels do Px_BarFrame
        RectTransform fillArea = FeelUI.CreateRect("FillArea", root);
        FeelUI.Stretch(fillArea, 3f * pixel);
        Image fill = px != null ? FeelUI.CreateSliced("Fill", fillArea, px.barFill, accentColor) : FeelUI.CreatePanel("Fill", fillArea, accentColor);
        fill.rectTransform.sizeDelta = Vector2.zero;

        Sprite knobSprite = SettingsIcons.Knob;
        Vector2 knobSize = knobSprite.rect.size * pixel;
        RectTransform handleArea = FeelUI.CreateRect("HandleArea", root);
        FeelUI.Stretch(handleArea);
        handleArea.offsetMin = new Vector2(knobSize.x * .5f, 0f);
        handleArea.offsetMax = new Vector2(-knobSize.x * .5f, 0f);
        Image knob = FeelUI.CreateImage("Knob", handleArea, Color.white, knobSprite);
        knob.raycastTarget = true;

        Slider slider = root.gameObject.AddComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.handleRect = knob.rectTransform;
        slider.targetGraphic = knob;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };
        ColorBlock colors = slider.colors;
        colors.highlightedColor = new Color(1f, .93f, .75f);
        colors.pressedColor = new Color(1f, .82f, .3f);
        colors.selectedColor = Color.white;
        colors.fadeDuration = .05f;
        slider.colors = colors;

        //O pegador estica na altura da área (âncoras 0 a 1): o sizeDelta é o que passa da barra
        knob.rectTransform.sizeDelta = new Vector2(knobSize.x, knobSize.y - size.y);
        return slider;
    }

    private void StoreChoice(RectTransform page, int index, string label, string hint, string[] options, Func<GameSettings, int> get,
        Action<GameSettings, int> set, Action after = null, bool interactable = true)
    {
        ChoiceRow(page, index, label, hint, options, () => get(SettingsStore.Current), i =>
        {
            SettingsStore.Change(s => set(s, i));
            after?.Invoke();
        }, interactable);
    }

    // Botões lado a lado; o escolhido fica dourado. Devolve o redesenho (a tela cheia redesenha a cada quadro).
    private Action ChoiceRow(RectTransform page, int index, string label, string hint, string[] options, Func<int> get, Action<int> choose, bool interactable = true)
    {
        RectTransform area = ControlArea(Row(page, index, label, hint));
        const float gap = 14f;
        float width = (controlWidth - gap * (options.Length - 1)) / options.Length;
        Button[] buttons = new Button[options.Length];
        int shown = -1;

        for (int i = 0; i < options.Length; i++)
        {
            int option = i;
            Button button = FeelUI.CreateButton("Option " + options[i], area, options[i], new Vector2(width, controlHeight), FeelUI.ButtonColor, controlFont);
            Vector2 left = new Vector2(0f, .5f);
            FeelUI.Place((RectTransform)button.transform, left, left, new Vector2(i * (width + gap), 0f), new Vector2(width, controlHeight));
            button.interactable = interactable;
            button.onClick.AddListener(() =>
            {
                if (get() == option) return;
                choose(option);
                Punch(button.transform, .12f);
            });
            buttons[i] = button;
        }

        Action refresh = () =>
        {
            int selected = get();
            if (selected == shown) return;

            shown = selected;
            for (int i = 0; i < buttons.Length; i++)
            {
                SetSelected(buttons[i], i == selected && interactable);
                if (!interactable) buttons[i].GetComponentInChildren<TMP_Text>(true).color = hintColor;
            }
        };
        refreshers.Add(refresh);
        return refresh;
    }

    private void ActionRow(RectTransform page, int index, string label, string hint, string buttonText, Action action)
    {
        RectTransform area = ControlArea(Row(page, index, label, hint));
        Button button = FeelUI.CreateButton("Action", area, buttonText, new Vector2(controlWidth, controlHeight), FeelUI.ButtonColor, controlFont);
        FeelUI.Stretch((RectTransform)button.transform);
        button.onClick.AddListener(() =>
        {
            action();
            Punch(button.transform, .12f);
        });
    }

    private void RefreshLive()
    {
        refreshFullscreen?.Invoke();
    }

    #endregion

    #region ACTIONS

    // O painel treme do tamanho do tremor escolhido (o jogo está pausado; a câmera não treme na pausa)
    private void PreviewShake()
    {
        float amount = SettingsStore.Current.ShakeMultiplier;
        if (amount <= 0f || panel == null) return;

        panel.DOComplete();
        panel.DOShakeAnchorPos(.35f, 16f * amount, 24, 90f, false, true).SetUpdate(true).SetLink(panel.gameObject);
    }

    private void ReviewTutorials()
    {
        SettingsStore.ReviewTutorials();
        ShowFooter("TUTORIAIS VÃO VOLTAR", accentColor, toastTime);
    }

    // Dicas desligadas com um tutorial no meio: ele acaba como no botão PULAR (solta os bloqueios e a câmera lenta)
    private void SkipRunningTutorial()
    {
        if (SettingsStore.Current.tips) return;

        foreach (TutorialView view in FindObjectsByType<TutorialView>(FindObjectsInactive.Include)) view.Skip();
    }

    #endregion
}
