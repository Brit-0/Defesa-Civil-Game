using UnityEngine;

//Receitas padrão do FeelParticles (os campos do Inspector começam com elas). Tamanho, velocidade e gravidade são
//frações do "scale" de quem emite: a altura da casa no mundo, a altura da tela no HUD.
public static class FxPresets
{
    public static FxPreset Dust() => new FxPreset
    {
        shape = FxShape.Circle, count = new Vector2Int(9, 12), size = new Vector2(.08f, .15f), speed = new Vector2(.5f, 1.1f),
        lifetime = new Vector2(.6f, 1f), spread = 200f, spawnRadius = .12f, gravity = -.2f, drag = 3.5f, endSize = 1.8f,
        fadeOutFrom = .25f,
        colors = new[] { new Color(.8f, .74f, .64f, .9f), new Color(.7f, .64f, .55f, .85f), new Color(.88f, .84f, .76f, .8f) }
    };

    public static FxPreset Debris() => new FxPreset
    {
        count = new Vector2Int(5, 8), size = new Vector2(.03f, .06f), speed = new Vector2(.9f, 1.5f), lifetime = new Vector2(.55f, .8f),
        spread = 110f, gravity = 5f, drag = .4f, endSize = .8f, spin = 540f, fadeOutFrom = .7f,
        colors = new[] { new Color(.45f, .36f, .3f), new Color(.6f, .5f, .42f), new Color(.35f, .33f, .36f) }
    };

    public static FxPreset Sparks() => new FxPreset
    {
        count = new Vector2Int(10, 14), size = new Vector2(.025f, .04f), speed = new Vector2(1.3f, 2.2f), lifetime = new Vector2(.35f, .6f),
        gravity = 1.6f, drag = 4f, endSize = .3f, alignToVelocity = true, stretch = 3f, fadeOutFrom = .5f,
        colors = new[] { new Color(1f, .95f, .6f), Color.white, new Color(1f, .82f, .35f) }
    };

    public static FxPreset Smoke() => new FxPreset
    {
        shape = FxShape.Circle, count = new Vector2Int(6, 9), size = new Vector2(.12f, .2f), speed = new Vector2(.25f, .5f),
        lifetime = new Vector2(1.2f, 1.8f), spread = 70f, spawnRadius = .15f, gravity = -.3f, drag = 1f, endSize = 2.2f,
        sway = .05f, swayFrequency = 2f, fadeIn = .1f, fadeOutFrom = .3f,
        colors = new[] { new Color(.2f, .2f, .23f, .75f), new Color(.3f, .29f, .32f, .7f) }
    };

    public static FxPreset Embers() => new FxPreset
    {
        count = new Vector2Int(1, 1), size = new Vector2(.015f, .03f), speed = new Vector2(.5f, .9f), lifetime = new Vector2(.7f, 1.1f),
        spread = 50f, gravity = -.6f, drag = .5f, endSize = .4f, sway = .06f, swayFrequency = 6f, fadeOutFrom = .5f,
        colors = new[] { new Color(1f, .85f, .3f), new Color(1f, .6f, .15f) }, useEndColor = true, endColor = new Color(.9f, .2f, .1f)
    };

    public static FxPreset Steam() => new FxPreset
    {
        shape = FxShape.Circle, count = new Vector2Int(8, 12), size = new Vector2(.1f, .16f), speed = new Vector2(.4f, .8f),
        lifetime = new Vector2(.8f, 1.2f), spread = 100f, spawnRadius = .15f, gravity = -.5f, drag = 2f, endSize = 2f, fadeOutFrom = .2f,
        colors = new[] { new Color(1f, 1f, 1f, .8f), new Color(.85f, .9f, .95f, .75f) }
    };

    //Confete e cinza são do HUD: scale = altura da tela de referência (1080)
    public static FxPreset Confetti() => new FxPreset
    {
        shape = FxShape.Strip, count = new Vector2Int(60, 80), size = new Vector2(.012f, .02f), speed = new Vector2(.9f, 1.6f),
        lifetime = new Vector2(2.5f, 4f), spread = 40f, gravity = .9f, drag = 1.5f, endSize = 1f, spin = 360f,
        sway = .02f, swayFrequency = 4f, fadeOutFrom = .8f, unscaledTime = true,
        colors = new[]
        {
            new Color(.95f, .3f, .25f), new Color(1f, .82f, .25f), new Color(.45f, .9f, .5f),
            new Color(.35f, .65f, 1f), new Color(.95f, .5f, .8f), Color.white
        }
    };

    public static FxPreset Ash() => new FxPreset
    {
        count = new Vector2Int(1, 1), size = new Vector2(.005f, .01f), speed = new Vector2(.03f, .08f), lifetime = new Vector2(4f, 6f),
        spread = 60f, direction = 270f, gravity = .02f, drag = 0f, endSize = .6f, spin = 90f, sway = .02f, swayFrequency = 1.5f,
        fadeIn = .2f, fadeOutFrom = .7f, unscaledTime = true,
        colors = new[] { new Color(.55f, .53f, .56f, .8f), new Color(.35f, .33f, .36f, .8f) }
    };

    public static FxPreset Splash() => new FxPreset
    {
        count = new Vector2Int(3, 4), size = new Vector2(.004f, .007f), speed = new Vector2(.05f, .1f), lifetime = new Vector2(.25f, .35f),
        spread = 120f, gravity = .4f, drag = 1f, endSize = .5f, fadeOutFrom = .3f,
        colors = new[] { new Color(.75f, .85f, 1f, .7f) }
    };

    //Moedas que pulam da casa no pagamento: giram de lado (achatam e voltam) e caem
    public static FxPreset Coin() => new FxPreset
    {
        shape = FxShape.Coin, count = new Vector2Int(6, 9), size = new Vector2(.08f, .1f), speed = new Vector2(1f, 1.6f),
        lifetime = new Vector2(.7f, .95f), spread = 80f, spawnRadius = .05f, gravity = 4.5f, drag = .6f, endSize = .9f,
        fadeOutFrom = .75f, colors = new[] { PixelFx.Gold, new Color(1f, .88f, .45f) }
    };

    //Brilho de 4 pontas que encolhe parado: sucesso, compra, marco
    public static FxPreset Glint() => new FxPreset
    {
        shape = FxShape.Glint, count = new Vector2Int(4, 6), size = new Vector2(.1f, .16f), speed = new Vector2(.15f, .45f),
        lifetime = new Vector2(.45f, .7f), spread = 360f, spawnRadius = .25f, gravity = -.2f, drag = 2f, endSize = .15f,
        fadeIn = .15f, fadeOutFrom = .55f, colors = new[] { PixelFx.Gold, PixelFx.Cream }
    };

    //Solda do conserto: fagulhas curtas e claras que caem
    public static FxPreset Weld() => new FxPreset
    {
        count = new Vector2Int(5, 8), size = new Vector2(.015f, .025f), speed = new Vector2(.9f, 1.7f), lifetime = new Vector2(.22f, .4f),
        spread = 150f, gravity = 3.5f, drag = 2.5f, endSize = .4f, alignToVelocity = true, stretch = 3f, fadeOutFrom = .45f,
        colors = new[] { PixelFx.Cream, PixelFx.Gold, new Color(.8f, .9f, 1f) }
    };

    //Pegada do trabalhador: parada no chão, some devagar (scale = altura do trabalhador)
    public static FxPreset Footstep() => new FxPreset
    {
        count = new Vector2Int(1, 1), size = new Vector2(.12f, .12f), speed = Vector2.zero, lifetime = new Vector2(1.1f, 1.4f),
        spread = 0f, drag = 0f, endSize = 1f, fadeOutFrom = .2f, colors = new[] { new Color(.08f, .07f, .1f, .35f) }
    };

    //Estouro de cruzinhas sem cor própria (quem emite tinge): sucesso verde, falha vermelha, pronto
    public static FxPreset Burst() => new FxPreset
    {
        shape = FxShape.Plus, count = new Vector2Int(8, 12), size = new Vector2(.05f, .07f), speed = new Vector2(.6f, 1.1f),
        lifetime = new Vector2(.5f, .75f), spread = 360f, spawnRadius = .1f, gravity = -.3f, drag = 3f, endSize = .35f,
        fadeOutFrom = .5f, colors = new[] { PixelFx.Cream }
    };
}
