using System.Linq;
using System.Numerics;
using Content.Client.UserInterface.Controls;
using Content.Shared._KS14.Cultivation.Body;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._KS14.Cultivation.UI;

/// <summary>
///     A body cultivator's tempered body at a glance: the stage, the tempering banked, and every part with its level,
///         how close it is to the next, and what it has unlocked. Powers that change how you play stand out in gold;
///         hover any power for what it does.
/// </summary>
public sealed partial class BodyPowersWindow : FancyWindow
{
    private static readonly Color Gold = Color.FromHex("#e8c060");
    private static readonly Color Muted = Color.FromHex("#8a8a8a");
    private static readonly Color Locked = Color.FromHex("#5c5c5c");

    [Dependency] private IPrototypeManager _prototypeManager = default!;

    private readonly RichTextLabel _header;
    private readonly BoxContainer _parts;
    private string _lastState = string.Empty;

    public BodyPowersWindow()
    {
        IoCManager.InjectDependencies(this);
        Title = Loc.GetString("body-powers-window-title");
        MinSize = new Vector2(540, 420);
        SetSize = new Vector2(540, 600);

        var root = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, Margin = new Thickness(10, 6), SeparationOverride = 6 };
        _header = new RichTextLabel();
        root.AddChild(_header);
        root.AddChild(new RichTextLabel { Text = Loc.GetString("body-powers-window-hint"), Modulate = Muted });

        _parts = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, SeparationOverride = 8 };
        var scroll = new ScrollContainer { VerticalExpand = true, HScrollEnabled = false };
        scroll.AddChild(_parts);
        root.AddChild(scroll);
        ContentsContainer.AddChild(root);
    }

    public void Update(BodyCultivatorComponent body)
    {
        // Only rebuild when something shown has changed, so hovering a power keeps its tooltip.
        var state = $"{body.Stage}|{(int)body.Tempering}|" + string.Join(",", SharedBodyCultivationSystem.AllGroups.Select(g =>
            $"{SharedBodyCultivationSystem.GetLevel(body, g)}:{body.Progress.GetValueOrDefault(g):0.#}"));
        if (state == _lastState)
            return;

        _lastState = state;
        var cap = SharedBodyCultivationSystem.TemperingCapFor(body.Stage);
        _header.SetMessage(FormattedMessage.FromMarkupOrThrow(Loc.GetString("body-powers-window-header",
            ("stage", body.Stage), ("stageName", Loc.GetString($"body-stage-{body.Stage}")),
            ("tempering", (int)body.Tempering), ("cap", (int)cap))));

        var powers = _prototypeManager.EnumeratePrototypes<BodyTemperingPowerPrototype>().ToList();
        var partCap = SharedBodyCultivationSystem.PartCap(body);
        _parts.RemoveAllChildren();
        foreach (var group in SharedBodyCultivationSystem.AllGroups)
            _parts.AddChild(PartRow(body, group, partCap, powers.Where(p => p.Group == group).OrderBy(p => p.Level).ToList()));

        _parts.AddChild(ArtsByStage(body));
    }

    /// <summary>
    ///     The arts each stage opens: those reached in white, those still ahead greyed with the stage that opens them.
    /// </summary>
    private Control ArtsByStage(BodyCultivatorComponent body)
    {
        var section = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, SeparationOverride = 2 };
        var title = new RichTextLabel();
        title.SetMessage(FormattedMessage.FromMarkupOrThrow($"[bold]{Loc.GetString("body-powers-window-arts")}[/bold]"));
        section.AddChild(title);

        // Fixed-size pills, three to a row, so long names never run into each other.
        BoxContainer? row = null;
        var count = 0;
        foreach (var (action, stage) in Content.Shared._KS14.Cultivation.CultivationTechniqueLists.BodyStageArts)
        {
            if (!_prototypeManager.TryIndex(action, out var prototype))
                continue;

            if (count++ % 3 == 0)
            {
                row = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal, SeparationOverride = 6, Margin = new Thickness(6, 0, 0, 0) };
                section.AddChild(row);
            }

            var reached = stage <= body.Stage;
            var pill = Pill(
                reached ? prototype.Name : Loc.GetString("body-powers-window-art-locked", ("art", prototype.Name), ("stage", stage)),
                prototype.Description,
                reached ? Color.White : Locked,
                reached ? Gold.WithAlpha(0.45f) : LockedBorder,
                reached ? ReachedBackground : LockedBackground);
            row!.AddChild(pill);
        }

        return section;
    }

    private Control PartRow(BodyCultivatorComponent body, BodyGroup group, int partCap, List<BodyTemperingPowerPrototype> powers)
    {
        var level = SharedBodyCultivationSystem.GetLevel(body, group);
        var row = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, SeparationOverride = 2 };

        // Name, level and progress towards the next level.
        var top = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal, SeparationOverride = 8 };
        var name = new RichTextLabel { MinWidth = 90 };
        name.SetMessage(FormattedMessage.FromMarkupOrThrow($"[bold]{Capitalize(Loc.GetString($"body-group-{group}"))}[/bold]"));
        top.AddChild(name);
        top.AddChild(new Label { Text = Loc.GetString("body-powers-window-level", ("level", level), ("max", BodyCultivatorComponent.MaxStage)), MinWidth = 70, FontColorOverride = Muted });

        if (level < BodyCultivatorComponent.MaxStage)
        {
            var cost = SharedBodyCultivationSystem.PartCost(level + 1);
            var progress = body.Progress.GetValueOrDefault(group);
            top.AddChild(new ProgressBar
            {
                MinValue = 0, MaxValue = cost, Value = progress,
                HorizontalExpand = true, MinHeight = 8, VerticalAlignment = VAlignment.Center,
                ToolTip = level >= partCap
                    ? Loc.GetString("body-powers-window-capped")
                    : Loc.GetString("body-powers-window-progress", ("progress", (int)progress), ("cost", cost)),
            });
        }
        else
        {
            top.AddChild(new Label { Text = Loc.GetString("body-powers-window-perfected"), FontColorOverride = Gold });
        }

        row.AddChild(top);

        // Unlocked powers as fixed-size pills, three to a row (gold when major), then the next one greyed out. Fixed
        //     sizes, because rich text in a grid measures its height wrong and the rows end up drawn over each other.
        var chips = powers.Where(p => p.Level <= level).Select(p => Chip(p, unlocked: true)).ToList();
        if (powers.FirstOrDefault(p => p.Level > level) is { } next)
            chips.Add(Chip(next, unlocked: false));

        BoxContainer? chipRow = null;
        for (var i = 0; i < chips.Count; i++)
        {
            if (i % 3 == 0)
            {
                chipRow = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal, SeparationOverride = 6, Margin = new Thickness(6, 0, 0, 0) };
                row.AddChild(chipRow);
            }

            chipRow!.AddChild(chips[i]);
        }

        return row;
    }

    private static readonly Color ReachedBackground = Color.FromHex("#2a2d33");
    private static readonly Color LockedBackground = Color.FromHex("#1c1d21");
    private static readonly Color PlainBorder = Color.FromHex("#3a3d44");
    private static readonly Color LockedBorder = Color.FromHex("#303236");

    /// <summary>
    ///     A name in a box of fixed size; anything too long is cut off, and the tooltip says what it does.
    /// </summary>
    private static PanelContainer Pill(string text, string tooltip, Color textColor, Color border, Color background)
    {
        var pill = new PanelContainer
        {
            SetSize = new Vector2(160, 24),
            MouseFilter = MouseFilterMode.Stop,
            ToolTip = tooltip,
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = background,
                BorderColor = border,
                BorderThickness = new Thickness(1),
                ContentMarginLeftOverride = 6,
                ContentMarginRightOverride = 6,
            },
        };
        pill.AddChild(new Label
        {
            Text = text,
            ClipText = true,
            HorizontalExpand = true,
            VAlign = Label.VAlignMode.Center,
            FontColorOverride = textColor,
        });
        return pill;
    }

    private Control Chip(BodyTemperingPowerPrototype power, bool unlocked)
    {
        var name = Loc.GetString(power.Name);
        var description = Loc.GetString(power.Description);
        if (!unlocked)
            return Pill(Loc.GetString("body-powers-window-next", ("power", name), ("level", power.Level)), description, Locked, LockedBorder, LockedBackground);

        return power.Major
            ? Pill(name, description, Gold, Gold.WithAlpha(0.6f), ReachedBackground)
            : Pill(name, description, Color.White, PlainBorder, ReachedBackground);
    }

    private static string Capitalize(string text)
    {
        // Upper-case the first letter without joining a char to a string, which the client sandbox forbids.
        return text.Length == 0 ? text : text[..1].ToUpperInvariant() + text[1..];
    }
}
