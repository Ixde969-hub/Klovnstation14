# DM → SS14 patterns

Grows as features are ported. Each entry: the DM idiom, the SS14 equivalent, and a note.
Design decisions answered by the user go in the second section so they are never asked twice.

## Translation patterns

| DM | SS14 | Notes |
|---|---|---|
| `/obj/item/foo` subtype with var overrides | YAML entity prototype with `parent:` | Var overrides become component fields |
| `force`, `throwforce`, `w_class` | `MeleeWeapon.damage`, `DamageOtherOnHit`, `Item.size` | |
| `/datum/component/two_handed` | `Wieldable` + `IncreaseDamageOnWield` | |
| `attack_self(mob/user)` | `UseInHandEvent` subscription in an EntitySystem | |
| `RegisterSignal(src, COMSIG_ITEM_EQUIPPED, ...)` | `SubscribeLocalEvent<FooComponent, GotEquippedHandEvent>` | |
| `do_after(user, delay, target)` | `DoAfterSystem.TryStartDoAfter` + a `DoAfterEvent` subclass | |
| `to_chat` / `balloon_alert` | `PopupSystem.PopupEntity` / `PopupClient` | |
| `visible_message` | `PopupSystem` with `Filter.Pvs` | |
| `playsound` | `SharedAudioSystem.PlayPvs` | |
| `addtimer` / `spawn` / `sleep` | `Timer.Spawn`, component accumulators in `Update`, or DoAfter | Never block |
| `ADD_TRAIT` / `HAS_TRAIT` | Marker components or status effects | |
| `qdel(src)` | `QueueDel(uid)` | |
| `prob(x)` | `IRobustRandom.Prob(x / 100f)` | Must be predicted-safe on client |

*(Unverified against the chosen SS14 base yet — confirm names once the fork is cloned.)*

## Design decisions

| Question | Decision | Date |
|---|---|---|
