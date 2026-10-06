#nullable enable
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared._KS14.BatteringRam;
using Content.Shared.CombatMode;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Electrocution;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._KS14.Weapons;

/// <summary>
///     The battering ram ported from SurfShack13: what its prototype wires up (two-handed swings, insulation while
///         held) and what <see cref="KsBatteringRamSystem"/> adds (stamina per swing, falling into open doors).
/// </summary>
public sealed class KsBatteringRamTest : InteractionTest
{
    protected override string PlayerPrototype => "MobHuman"; // two hands, so the ram can be picked up and wielded

    private const string BatteringRam = "KsBatteringRam";

    [Test]
    public async Task HoldingGrantsInsulation()
    {
        var ramNetEntity = await Spawn(BatteringRam);
        await Pickup(ramNetEntity);
        Assert.That(SEntMan.HasComponent<InsulatedComponent>(SPlayer), "holding the ram should insulate the holder");

        await Drop();
        Assert.That(SEntMan.HasComponent<InsulatedComponent>(SPlayer), Is.False, "dropping the ram should take the insulation away");
    }

    [Test]
    public async Task CannotSwingUnwielded()
    {
        var ramUid = await PickUpRam(wield: false);
        await SpawnTarget("Girder");

        await Server.WaitAssertion(() =>
        {
            var meleeSystem = SEntMan.System<SharedMeleeWeaponSystem>();
            var attacked = meleeSystem.AttemptLightAttack(SPlayer, ramUid, SEntMan.GetComponent<MeleeWeaponComponent>(ramUid), STarget!.Value);
            Assert.That(attacked, Is.False, "the ram must be wielded to swing");
        });
    }

    [Test]
    public async Task SwingCostsStamina()
    {
        var ramUid = await PickUpRam(wield: true);
        await SpawnTarget("Girder");

        var staminaBefore = SEntMan.GetComponent<StaminaComponent>(SPlayer).StaminaDamage;
        var expectedCost = SEntMan.GetComponent<KsBatteringRamComponent>(ramUid).UserStaminaCost;

        await Server.WaitAssertion(() =>
        {
            var meleeSystem = SEntMan.System<SharedMeleeWeaponSystem>();
            var weapon = SEntMan.GetComponent<MeleeWeaponComponent>(ramUid);
            Assert.That(meleeSystem.AttemptLightAttack(SPlayer, ramUid, weapon, STarget!.Value));

            var staminaAfter = SEntMan.GetComponent<StaminaComponent>(SPlayer).StaminaDamage;
            Assert.That(staminaAfter - staminaBefore, Is.EqualTo(expectedCost).Within(0.01f));
        });
    }

    [Test]
    public async Task RammingOpenDoorKnocksWielderDown()
    {
        var ramUid = await PickUpRam(wield: true);
        await SpawnTarget("Airlock");

        await Server.WaitPost(() => SEntMan.System<SharedDoorSystem>().StartOpening(STarget!.Value));
        await RunSeconds(2f);
        Assert.That(SEntMan.GetComponent<DoorComponent>(STarget!.Value).State, Is.EqualTo(DoorState.Open));
        Assert.That(SEntMan.HasComponent<KnockedDownComponent>(SPlayer), Is.False);

        await Server.WaitAssertion(() =>
        {
            var meleeSystem = SEntMan.System<SharedMeleeWeaponSystem>();
            Assert.That(meleeSystem.AttemptLightAttack(SPlayer, ramUid, SEntMan.GetComponent<MeleeWeaponComponent>(ramUid), STarget!.Value));
            Assert.That(SEntMan.HasComponent<KnockedDownComponent>(SPlayer), "ramming an open door should knock the wielder down");
        });
    }

    [Test]
    public async Task RammingClosedDoorDoesNotKnockWielderDown()
    {
        var ramUid = await PickUpRam(wield: true);
        await SpawnTarget("Airlock");

        await Server.WaitAssertion(() =>
        {
            var meleeSystem = SEntMan.System<SharedMeleeWeaponSystem>();
            Assert.That(meleeSystem.AttemptLightAttack(SPlayer, ramUid, SEntMan.GetComponent<MeleeWeaponComponent>(ramUid), STarget!.Value));
            Assert.That(SEntMan.HasComponent<KnockedDownComponent>(SPlayer), Is.False, "only open doors trip the wielder");
        });
    }

    private async Task<EntityUid> PickUpRam(bool wield)
    {
        var ramNetEntity = await Spawn(BatteringRam);
        await Pickup(ramNetEntity);
        var ramUid = SEntMan.GetEntity(ramNetEntity);

        await Server.WaitPost(() =>
        {
            SEntMan.System<SharedCombatModeSystem>().SetInCombatMode(SPlayer, true);
            if (wield)
                Assert.That(SEntMan.System<SharedWieldableSystem>().TryWield(ramUid, SPlayer), "two free hands should be enough to wield the ram");
        });
        // Wielding puts the melee weapon on cooldown; wait it out so the first swing in each test is a real one.
        await RunSeconds(2f);

        return ramUid;
    }
}
