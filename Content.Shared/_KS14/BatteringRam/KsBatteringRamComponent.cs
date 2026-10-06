using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._KS14.BatteringRam;

/// <summary>
///     The side effects of swinging a battering ram: every swing costs the wielder stamina, ramming an open door
///         makes them fall through it, clumsy wielders sometimes fall over backwards, and slamming into something
///         that isn't a mob rattles the screens of whoever is standing nearby.
/// </summary>
/// <remarks>
///     Ported from SurfShack13's <c>/obj/item/batteringram</c>. The damage, wielding, knockback and shock insulation
///         are plain components on the prototype; this only covers what has no generic component.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(KsBatteringRamSystem))]
public sealed partial class KsBatteringRamComponent : Component
{
    /// <summary>
    ///     Stamina damage dealt to the wielder on every swing, hit or miss.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float UserStaminaCost = 9f;

    /// <summary>
    ///     How long the wielder is knocked down after ramming an open door or slipping.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan FallKnockdownTime = TimeSpan.FromSeconds(3);

    /// <summary>
    ///     Speed the wielder is thrown at when they fall into a door or slip backwards.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float FallThrowSpeed = 5f;

    /// <summary>
    ///     Chance per swing that a clumsy wielder slips and falls backwards.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ClumsySlipChance = 0.3f;

    /// <summary>
    ///     Mobs within this many tiles of a non-mob that gets rammed have their camera shaken.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BystanderShakeRange = 2f;

    /// <summary>
    ///     Strength of the camera kick bystanders receive.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BystanderShakeStrength = 0.5f;

    [DataField]
    public SoundSpecifier FallSound = new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg");

    [DataField]
    public SoundSpecifier SlipSound = new SoundPathSpecifier("/Audio/Effects/slip.ogg");
}
