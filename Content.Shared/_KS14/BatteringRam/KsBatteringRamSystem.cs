using System.Numerics;
using Content.Shared.Camera;
using Content.Shared.Clumsy;
using Content.Shared.Damage.Systems;
using Content.Shared.Doors.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._KS14.BatteringRam;

/// <inheritdoc cref="KsBatteringRamComponent"/>
public sealed partial class KsBatteringRamSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _netManager = default!;
    [Dependency] private EntityLookupSystem _entityLookupSystem = default!;
    [Dependency] private SharedAudioSystem _audioSystem = default!;
    [Dependency] private SharedCameraRecoilSystem _cameraRecoilSystem = default!;
    [Dependency] private SharedPopupSystem _popupSystem = default!;
    [Dependency] private SharedStaminaSystem _staminaSystem = default!;
    [Dependency] private SharedStunSystem _stunSystem = default!;
    [Dependency] private SharedTransformSystem _transformSystem = default!;
    [Dependency] private ThrowingSystem _throwingSystem = default!;
    [Dependency] private EntityQuery<ClumsyComponent> _clumsyQuery = default!;
    [Dependency] private EntityQuery<DoorComponent> _doorQuery = default!;
    [Dependency] private EntityQuery<MobStateComponent> _mobStateQuery = default!;

    private readonly HashSet<Entity<CameraRecoilComponent>> _bystanders = new();

    [SubscribeLocalEvent]
    private void OnMeleeHit(Entity<KsBatteringRamComponent> entity, ref MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        var userUid = args.User;
        _staminaSystem.TakeStaminaDamage(userUid, entity.Comp.UserStaminaCost, source: userUid, with: entity.Owner, visual: false);

        foreach (var hitUid in args.HitEntities)
        {
            if (_doorQuery.TryComp(hitUid, out var doorComponent) && doorComponent.State == DoorState.Open)
            {
                FallIntoDoor(entity, userUid, hitUid);
                return;
            }
        }

        foreach (var hitUid in args.HitEntities)
        {
            if (!_mobStateQuery.HasComp(hitUid))
            {
                ShakeBystanders(entity, hitUid);
                break;
            }
        }

        if (_clumsyQuery.HasComp(userUid)
            && SharedRandomExtensions.PredictedProb(_timing, entity.Comp.ClumsySlipChance, GetNetEntity(userUid)))
        {
            SlipBackwards(entity, userUid, args.HitEntities.Count > 0 ? args.HitEntities[0] : null);
        }
    }

    private void FallIntoDoor(Entity<KsBatteringRamComponent> entity, EntityUid userUid, EntityUid doorUid)
    {
        _stunSystem.TryKnockdown(userUid, entity.Comp.FallKnockdownTime, force: true);
        _throwingSystem.TryThrow(userUid, DirectionBetween(userUid, doorUid), baseThrowSpeed: entity.Comp.FallThrowSpeed, doSpin: false, playSound: false);
        _audioSystem.PlayPredicted(entity.Comp.FallSound, userUid, userUid);

        var userName = Identity.Entity(userUid, EntityManager);
        var doorName = Identity.Entity(doorUid, EntityManager);
        _popupSystem.PopupEntity(
            Loc.GetString("ks-battering-ram-fall-into-door-self", ("door", doorName)),
            Loc.GetString("ks-battering-ram-fall-into-door-others", ("user", userName), ("door", doorName)),
            userUid,
            userUid,
            type: PopupType.MediumCaution);
    }

    private void SlipBackwards(Entity<KsBatteringRamComponent> entity, EntityUid userUid, EntityUid? targetUid)
    {
        var direction = targetUid is { } target
            ? -DirectionBetween(userUid, target)
            : -_transformSystem.GetWorldRotation(userUid).ToWorldVec();

        _stunSystem.TryKnockdown(userUid, entity.Comp.FallKnockdownTime, force: true);
        _throwingSystem.TryThrow(userUid, direction, baseThrowSpeed: entity.Comp.FallThrowSpeed, playSound: false);
        _audioSystem.PlayPredicted(entity.Comp.SlipSound, userUid, userUid);

        _popupSystem.PopupEntity(
            Loc.GetString("ks-battering-ram-slip-self"),
            Loc.GetString("ks-battering-ram-slip-others", ("user", Identity.Entity(userUid, EntityManager))),
            userUid,
            userUid,
            type: PopupType.MediumCaution);
    }

    private void ShakeBystanders(Entity<KsBatteringRamComponent> entity, EntityUid rammedUid)
    {
        // KickCamera on the server networks the kick to each client; bystanders other than the wielder can't
        //      predict it anyway.
        if (!_netManager.IsServer)
            return;

        var rammedPosition = _transformSystem.GetMapCoordinates(rammedUid);
        _bystanders.Clear();
        _entityLookupSystem.GetEntitiesInRange(rammedPosition, entity.Comp.BystanderShakeRange, _bystanders);

        foreach (var bystander in _bystanders)
        {
            var away = _transformSystem.GetMapCoordinates(bystander).Position - rammedPosition.Position;
            var kick = away.LengthSquared() > 0.0001f ? Vector2.Normalize(away) : Vector2.UnitY;
            _cameraRecoilSystem.KickCamera(bystander, kick * entity.Comp.BystanderShakeStrength, bystander.Comp);
        }
    }

    private Vector2 DirectionBetween(EntityUid fromUid, EntityUid toUid)
    {
        var delta = _transformSystem.GetMapCoordinates(toUid).Position - _transformSystem.GetMapCoordinates(fromUid).Position;
        return delta.LengthSquared() > 0.0001f ? Vector2.Normalize(delta) : Vector2.UnitY;
    }
}
