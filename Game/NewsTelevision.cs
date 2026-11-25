using Godot;

namespace ComputerLive;

internal sealed class NewsTelevision
{
    private readonly Texture2D _anchor = GD.Load<Texture2D>("res://Art/News/anchor.png");
    private readonly Texture2D _market = GD.Load<Texture2D>("res://Art/News/market.png");
    private readonly SubViewport _feed;
    private readonly TextureRect _picture;
    private readonly Label _caption;
    private readonly MeshInstance3D _screen;
    private readonly Material _glass = WorldGeometry.Material("#1d2427");
    private readonly ShaderMaterial _signal;
    private double _seconds;
    private int _segment = -1;
    private CampaignDay _day;

    public bool On { get; private set; }

    public NewsTelevision(Node3D room, Vector3 position)
    {
        _feed = new SubViewport
        {
            Name = "NewsFeed", Size = new Vector2I(384, 288), Disable3D = true, RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled,
        };
        room.AddChild(_feed);
        _picture = new TextureRect
        {
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            Texture = _anchor, Size = new Vector2(384, 288),
        };
        _feed.AddChild(_picture);
        _feed.AddChild(new ColorRect { Position = new Vector2(11, 218), Size = new Vector2(362, 57), Color = new Color("#172b49e9") });
        _caption = AddCaption("", new Vector2(21, 225), 18);
        _caption.Size = new Vector2(340, 43);
        _caption.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        AddCaption("ПЕТЕРБУРГ   /   НОВОСТИ", new Vector2(15, 11), 13);

        _signal = new ShaderMaterial { Shader = GD.Load<Shader>("res://Game/television.gdshader") };
        _signal.SetShaderParameter("signal_image", _feed.GetTexture());
        _screen = new MeshInstance3D
        {
            Name = "TelevisionScreen", Position = position, Rotation = new Vector3(0, Mathf.Pi, 0),
            Mesh = new QuadMesh { Size = new Vector2(0.515f, 0.386f) }, MaterialOverride = _glass,
        };
        room.AddChild(_screen);
    }

    public void Power(bool on, CampaignDay day)
    {
        On = on;
        _seconds = 0;
        _segment = -1;
        _screen.MaterialOverride = on ? _signal : _glass;
        _feed.RenderTargetUpdateMode = on ? SubViewport.UpdateMode.WhenVisible : SubViewport.UpdateMode.Disabled;
        if (on)
            Advance(0, day);
    }

    public void Advance(double delta, CampaignDay day)
    {
        if (!On)
            return;

        _seconds = (_seconds + delta) % 15;
        int segment = (int)(_seconds / 5);
        if (segment == _segment && day == _day)
            return;

        _segment = segment;
        _day = day;
        _picture.Texture = segment == 1 ? _market : _anchor;
        _caption.Text = day.NewsCaption(segment);
    }

    private Label AddCaption(string text, Vector2 position, int fontSize)
    {
        var label = new Label { Position = position, Text = text };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_outline_color", new Color("#0d182a"));
        label.AddThemeConstantOverride("outline_size", 4);
        _feed.AddChild(label);
        return label;
    }
}
