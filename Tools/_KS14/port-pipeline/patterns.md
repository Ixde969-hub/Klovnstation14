# DM → SS14 patterns

Grows as features are ported. Each entry: the DM idiom, the SS14 equivalent, and a note.
Design decisions answered by the user go in the second section so they are never asked twice.
✅ = verified in a real port (named in the notes); unmarked rows are unverified guesses.

## Translation patterns

| DM | SS14 / Klovn | Notes |
|---|---|---|
| `/obj/item/foo` subtype with var overrides | YAML entity prototype with `parent:` | ✅ battering ram. IDs get `Ks` prefix when upstream could plausibly use the name |
| `force` (wielded) | `MeleeWeapon.damage` | ✅ battering ram |
| `demolition_mod` | Extra `Structural` damage | ✅ battering ram. Airlock = 500 dmg, `StrongMetallic` (Blunt ×0.7 −10 flat, Structural unmodified) — size Structural so hits-to-break match |
| `throwforce` | `DamageOtherOnHit` | ✅ battering ram |
| `/datum/component/two_handed` + `require_twohands` | `Wieldable` + `MeleeRequiresWield` | ✅ battering ram. **Do not add `MultiHandedItem` too** — it holds the second hand, wielding can't spawn its virtual item, and the item silently unwields |
| `slowdown` + `SLOWS_WHILE_IN_HAND` | `ClothingSpeedModifier` + `HeldSpeedModifier` (mirrors clothing) | ✅ battering ram. tg slowdown 1 ≈ 0.8 multiplier |
| `slot_flags = ITEM_SLOT_BACK` | `Clothing` with `slots: [back]`, state `equipped-BACKPACK` | ✅ battering ram |
| `ADD_TRAIT(holder, …)` on equip-to-hands / remove on drop | `HeldGrantComponent` (Klovn `_KS14/Held`) granting a component to the holder | ✅ battering ram (`Insulated` while held) |
| `TRAIT_AIRLOCK_SHOCKIMMUNE` | `Insulated` on the mob | ✅ battering ram — broader: blocks all shocks, not just airlocks |
| `/obj/structure/fireaxecabinet/<x>` | parent `FireAxeCabinet`, own sprite layers + `ItemSlots` whitelist by tag | ✅ battering ram cabinet |
| `hitsound`, `pickup_sound`, `drop_sound` | `MeleeWeapon.soundHit`, `EmitSoundOnPickup`, `EmitSoundOnDrop` | ✅ battering ram |
| `attack()` throwing the target one tile | `MeleeThrowOnHit { distance: 1 }` | ✅ battering ram |
| `afterattack()` side effects on the user | `[SubscribeLocalEvent] MeleeHitEvent` on the weapon, guard `if (!args.IsHit) return;` | ✅ battering ram. Examining a melee weapon also raises it with `IsHit = false` |
| `adjustStaminaLoss` | `SharedStaminaSystem.TakeStaminaDamage` | ✅ battering ram |
| `Knockdown(t)` | `SharedStunSystem.TryKnockdown(uid, t, force: true)` | ✅ battering ram |
| `safe_throw_at(get_step(...))` | `ThrowingSystem.TryThrow(uid, direction, baseThrowSpeed: …)` | ✅ battering ram |
| `HAS_TRAIT(user, TRAIT_CLUMSY) && prob(x)` | `ClumsyComponent` + `SharedRandomExtensions.PredictedProb(_timing, x, GetNetEntity(uid))` | ✅ battering ram — predicted-safe random |
| `shake_camera(mob, …)` | `SharedCameraRecoilSystem.KickCamera`, server only (it networks itself) | ✅ battering ram |
| `to_chat(user)` + `visible_message` | `PopupEntity(selfMsg, othersMsg, uid, recipient)` | ✅ battering ram |
| `do_after` wind-up before a melee swing | No equivalent in SS14 melee; slower `attackRate` | Design decision below |
| `attack_self(mob/user)` | `UseInHandEvent` subscription | |
| `do_after(user, delay, target)` | `DoAfterSystem.TryStartDoAfter` + a `DoAfterEvent` subclass | |
| `addtimer` / `spawn` / `sleep` | `Timer.Spawn`, accumulators in `Update`, or DoAfter | Never block |
| `qdel(src)` | `QueueDel(uid)` / `PredictedQueueDel` | |

## Assets

| DM | SS14 | Notes |
|---|---|---|
| `icon_state` in a `.dmi` | `tools/dmi2rsi.py SRC.dmi OUT.rsi --states old=new` | ✅ BYOND order is frame-major with dirs inside; RSI is dir-major. Delays ticks→seconds |
| item `icon_state` | `icon` | |
| in-hands `lefthand_file`/`righthand_file` | `inhand-left` / `inhand-right` (4 dirs) | |
| two_handed `icon_wielded` in-hands | `wielded-inhand-left` / `wielded-inhand-right` | |
| worn on back (`icons/mob/clothing/back.dmi`) | `equipped-BACKPACK` | Worn sprites live in core tg DMIs, not the item's own file — search all DMIs |
| `sound/...ogg` | `Resources/Audio/_KS14/...` + `attributions.yml` | |

Credit: find the original author with `git log -S <state> -- <dmi>` on the SurfShack checkout (slow on the blobless clone).

## Testing checklist (per port)

1. `dotnet build Content.Server -c Release` and `Content.Client -c Release` (warnings-as-errors).
2. `dotnet run --project Content.YAMLLinter -c Release`.
3. `py -3.10 Tools/_KS14/validate_rsis.py <each new .rsi>` (needs `jsonschema`).
4. A `_KS14` integration test per mechanic, run with `-c Debug`; wait out `MeleeWeapon.NextAttack` after wielding.
5. **Break each mechanic and watch its test fail**, then restore (Klovn CONTRIBUTING §6). Sabotage by deleting lines, not by adding duplicate components — a duplicate crashes prototype loading and proves nothing.
6. Wider suites: EntityTest, SandboxTest, StorageTest, contraband.

## Design decisions

| Question | Decision | Date |
|---|---|---|
| Port the doubloons metacurrency store / antag radio? | Skip | 2026-10-06 |
| Battering ram's 0.5 s wind-up before each swing (SS14 melee has no wind-up) | `attackRate: 0.75` instead — **unconfirmed, ask user** | 2026-10-06 |
| Battering ram shock immunity: airlocks only vs all shocks | `Insulated` (all shocks) while held — fits "insulating rubber" — **unconfirmed** | 2026-10-06 |
| Battering ram: SS13 zero-gravity recoil, suicide verb, wound bonus | Dropped (no direct equivalent) — **unconfirmed** | 2026-10-06 |
| Battering ram: needs two free hands to pick up (SS13) | Can pick up one-handed; needs both to wield and to swing | 2026-10-06 |
| Battering ram: who can get it | Security contraband; cabinet access Security/Command; not added to any map, lathe or uplink yet — **ask user** | 2026-10-06 |
