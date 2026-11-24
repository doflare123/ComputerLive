using System;
using System.Collections.Generic;
using Godot;

namespace ComputerLive;

internal sealed record Choice(string Text, Action Run, bool Enabled = true);

internal sealed class ClubInterface
{
	private static readonly Color Paper = new("#efe0bc");
	private static readonly Color Muted = new("#c9c7ac");
	private static readonly Color Amber = new("#f2c274");
	private readonly Control _root = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
	private readonly Label _cash;
	private readonly Label _day;
	private readonly Label _objective;
	private readonly Label _prompt;
	private readonly Label _cornerPrompt;
	private readonly Label _result;
	private readonly Label _cross;
	private Control? _modal;

	public bool IsOpen => _modal != null;

	public ClubInterface(Node parent)
	{
		var canvas = new CanvasLayer();
		parent.AddChild(canvas);
		canvas.AddChild(_root);
		_root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

		var top = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		top.AddThemeConstantOverride("separation", 14);
		_root.AddChild(top);
		top.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft);
		top.Position = new Vector2(26, 24);
		_day = Badge(top, 16, Paper);
		_cash = Badge(top, 18, Amber);

		var bottom = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		bottom.AddThemeConstantOverride("separation", 4);
		_root.AddChild(bottom);
		bottom.SetAnchorAndOffset(Side.Left, 0, 26);
		bottom.SetAnchorAndOffset(Side.Right, 1, -26);
		bottom.SetAnchorAndOffset(Side.Top, 1, -104);
		bottom.SetAnchorAndOffset(Side.Bottom, 1, -19);
		bottom.Alignment = BoxContainer.AlignmentMode.End;
		_result = MakeLabel("", 16, Amber);
		bottom.AddChild(_result);
		_objective = Badge(bottom, 17, Paper);
		((Control)_objective.GetParent()).SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;

		var center = new CenterContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		_root.AddChild(center);
		center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_cross = MakeLabel("·", 29, Paper);
		center.AddChild(_cross);

		_prompt = MakeLabel("", 19, Paper);
		_prompt.HorizontalAlignment = HorizontalAlignment.Center;
		_root.AddChild(_prompt);
		_prompt.SetAnchorAndOffset(Side.Right, 0.88f, 0);
		_prompt.SetAnchorAndOffset(Side.Left, 0.12f, 0);
		_prompt.SetAnchorAndOffset(Side.Bottom, 0.5f, 58);
		_prompt.SetAnchorAndOffset(Side.Top, 0.5f, 29);

		_cornerPrompt = MakeLabel("", 17, Paper);
		_cornerPrompt.HorizontalAlignment = HorizontalAlignment.Right;
		_root.AddChild(_cornerPrompt);
		_cornerPrompt.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomRight);
		_cornerPrompt.SetOffset(Side.Left, -420);
		_cornerPrompt.SetOffset(Side.Right, -26);
		_cornerPrompt.SetOffset(Side.Top, -45);
		_cornerPrompt.SetOffset(Side.Bottom, -18);
	}

	public void Update(ClubLedger club, string location, string objective, string result)
	{
		_day.Text = $"ПЕТЕРБУРГ / 1993     ДЕНЬ {Math.Min(club.Day, ClubLedger.MaxDay)} ИЗ {ClubLedger.MaxDay}     {location.ToUpperInvariant()}";
		_cash.Text = ClubLedger.Money(club.Cash);
		_objective.Text = objective;
		_result.Text = result;
		_result.Visible = result.Length != 0;
	}

	public void Target(string? title, bool corner = false)
	{
		(corner ? _cornerPrompt : _prompt).Text = title == null ? "" : $"[ E ]  {title}";
		_cross.Modulate = title == null ? Paper : Amber;
		_prompt.Visible = _modal == null && !corner && title != null;
		_cornerPrompt.Visible = _modal == null && corner && title != null;
		_cross.Visible = _modal == null;
	}

	public void Show(string title, string description, IReadOnlyList<Choice> choices, Action close)
	{
		Close();
		var shade = new ColorRect { Color = new Color(0.046f, 0.066f, 0.056f, 0.77f) };
		_modal = shade;
		_root.AddChild(shade);
		shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_cross.Hide();
		_prompt.Hide();
		_cornerPrompt.Hide();

		var center = new CenterContainer();
		shade.AddChild(center);
		center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		var panel = new PanelContainer { CustomMinimumSize = new Vector2(670, 0) };
		panel.AddThemeStyleboxOverride("panel", Surface(new Color("#202721"), new Color("#766549"), 26, 19));
		center.AddChild(panel);

		var column = new VBoxContainer();
		column.AddThemeConstantOverride("separation", 10);
		panel.AddChild(column);
		column.AddChild(MakeLabel(title, 29, Amber));
		var body = MakeLabel(description, 18, Paper);
		body.CustomMinimumSize = new Vector2(610, 0);
		body.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		column.AddChild(body);
		var gap = new Control { CustomMinimumSize = new Vector2(0, 3), MouseFilter = Control.MouseFilterEnum.Ignore };
		column.AddChild(gap);

		foreach (var choice in choices)
			column.AddChild(ActionButton(choice));

		var back = ActionButton(new Choice("Esc   Закрыть", close));
		back.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
		column.AddChild(back);
	}

	public void Close()
	{
		if (_modal == null)
			return;

		_root.RemoveChild(_modal);
		_modal.QueueFree();
		_modal = null;
	}

	private static Label Badge(Container parent, int fontSize, Color color)
	{
		var panel = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		panel.AddThemeStyleboxOverride("panel", Surface(new Color(0.080f, 0.098f, 0.080f, 0.92f), new Color("#62583f"), 13, 7));
		parent.AddChild(panel);
		var label = MakeLabel("", fontSize, color);
		panel.AddChild(label);
		return label;
	}

	private static Label MakeLabel(string text, int fontSize, Color color)
	{
		var label = new Label { Text = text, MouseFilter = Control.MouseFilterEnum.Ignore };
		label.AddThemeFontSizeOverride("font_size", fontSize);
		label.AddThemeColorOverride("font_color", color);
		label.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.75f));
		label.AddThemeConstantOverride("shadow_offset_y", 1);
		return label;
	}

	private static Button ActionButton(Choice choice)
	{
		var button = new Button
		{
			Text = choice.Text, Disabled = !choice.Enabled, Alignment = HorizontalAlignment.Left,
			CustomMinimumSize = new Vector2(0, 38),
		};
		button.AddThemeFontSizeOverride("font_size", 17);
		button.AddThemeColorOverride("font_color", Paper);
		button.AddThemeColorOverride("font_hover_color", Colors.White);
		button.AddThemeColorOverride("font_disabled_color", Muted.Darkened(0.32f));
		button.AddThemeStyleboxOverride("normal", Surface(new Color("#2c342b"), new Color("#59604b"), 12, 4));
		button.AddThemeStyleboxOverride("hover", Surface(new Color("#3e4734"), Amber, 12, 4));
		button.AddThemeStyleboxOverride("pressed", Surface(new Color("#4b4b32"), Amber, 12, 4));
		button.AddThemeStyleboxOverride("disabled", Surface(new Color("#222921"), new Color("#363d32"), 12, 4));
		var focus = Surface(Colors.Transparent, Amber, 12, 4);
		focus.DrawCenter = false;
		button.AddThemeStyleboxOverride("focus", focus);
		button.Pressed += choice.Run;
		return button;
	}

	private static StyleBoxFlat Surface(Color fill, Color border, int horizontal, int vertical) => new()
	{
		BgColor = fill, BorderColor = border,
		BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
		ContentMarginLeft = horizontal, ContentMarginRight = horizontal,
		ContentMarginTop = vertical, ContentMarginBottom = vertical,
	};
}
