# Turn-Based Combat Game

## Overview

This is a **menu-based, turn-based combat game** designed for **two players**.
Each player selects **three Bots** from a total of **eight**, with every Bot having unique stats and moves.

The game should played **on a single computer**, with players alternating turns.

Anything in this README that is in *italics* has **not** been implemented in the game and is subject to change.

[https://sammmyd.itch.io/turn-based-combat-game](Play on itch.io)

---

## Game Information

## Stats

Each Bot has the following stats:

- Health
- Armor Health
- Strength
- Speed
- Energy Capacity

All stats are **multiples of 5**.

### Bot Types

*Weaknesses and resistances to damage types has not been implemented*
| Type     | *Resistant To*        | *Weak To*              |
|----------|-----------------------|---------------------------|
| Cyborg   | Electric              | Pierce, Blunt             |
| Android  | Explosive, Fire       | Laser, Blunt, Electric    |
| Mech     | Pierce, Laser, Blunt  | Explosive, Electric, Fire |
| Flying   | Blunt, Fire           | Pierce, Electric          |

---


### Power Rating

```
Power = Health + Armor Health + Strength + Speed + Energy Capacity
```

Represents the Bot’s overall stat strength and is used as a tie-breaker in turn order calculations.

---

## Damage Types

*Most damage effects are not implemented and are all subject to change*
| Type       | Effect |
|------------|--------|
| Pierce     | *Damages health directly* |
| Laser      | *Increases target energy (10–30% of Energy Capacity)* |
| Explosive  | *25% of damage dealt as AOE* |
| Blunt      | Deals 25% more damage to armor |
| Electric   | *Disables extra turns* |
| Fire       | *Applies burn* |

---

## Strength & Damage

### Damage Formula

```
Damage = (Strength / 100) × Attack Power
```

- **Strength**: Strength stat of attacker
- **Attack Power (AP)**: Power of the move used

### Damage Descriptors

| Descriptor  | AP Range |
|-------------|----------|
| Light       | ≤ 20     |
| Moderate    | 25–40    |
| Heavy       | 45–50    |
| Very Heavy  | 55+      |

---

## Speed & Turn System

This game uses a **dynamic turn system** rather than a fixed turn order.

### Turn Calculation

1. Each Bot has a **Count**, starting at `0`
2. All Counts increase by the bot’s **Speed**
3. If any Bot reaches a Count **1000 or more**:
   - The Bot with the highest Count takes a turn
   - Tie → highest **Power**
   - Tie again → random selection
4. Only the acting bot’s Count is reduced by **1000**
5. Repeat the process

Faster Bots can **lap slower Bots**, gaining additional turns.

---

## Moves

Each Bot has **4 moves**, each with:

- Attack Power (AP)
- Energy Cost
- Cooldown

---

## Energy System

- Each Bot has a maximum **Energy Capacity**
- Moves can be used as long as current energy is **below capacity**
- Energy decreases by **10%** at the end of each turn

### Cooldown Move

- Reduces energy by **40%**
- Can be used when energy is over capacity

### Overcapacity Rules

- If energy exceeds max capacity:
  - Bots cannot use energy-cost moves
  - Only **Cooldown** or **0-energy moves** are allowed

---

## Status Effects

### Negative Status Effects

#### Control Effects

| Effect | Duration | Description |
|------|----------|-------------|
| Shutdown | 1 turn | Target skips their turn |
| Corrupt | 1 turn | Target uses a random move against their own team |

---

#### Torture Effects (Damage Over Time)

| Effect | Duration | Description |
|------|----------|-------------|
| Melt | 3 turns | Deals 15% damage and adds +10% energy each turn |
| Leak | 4 turns | Deals 10% health damage and reduces cooling by 50% |
| Acid | 3 turns | Deals 15% damage to armor and 10% to health |
| Shock | 4 turns | Deals 10% damage and reduces accuracy by 20% |

---

#### Miscellaneous Negative Effects

| Effect | Duration | Description |
|------|----------|-------------|
| Optic Jam | 2 turns | Reduces accuracy by 50% |
| Static | 3 turns | Reduces accuracy and damage by 25% |
| Overclock | 3 turns | Increases energy by 15% |
| Vulnerable | 3 turns | Increases damage taken by 25% |

---

### Positive Status Effects

#### Immunities

| Effect | Duration | Description |
|------|----------|-------------|
| Control Immunity | 2 turns | Immunity to all control status effects |
| Torture Immunity | 2 turns | Immunity to all torture status effects |

#### Life & Healing Effects

| Effect | Duration | Description |
|------|----------|-------------|
| Mend Armor | 3 turns | Restores 20% armor each turn |
| Double Healing | 2 turns | Doubles all healing received |
| Regeneration | 3 turns | Restores 20% total health each turn |

---

#### Damage Effects

| Effect | Duration | Description |
|------|----------|-------------|
| Damage Boost | 3 turns | Increases damage dealt by 50% |
| Double Damage | 2 turns | Increases damage dealt by 100% |
| Evasion | 1 turn | Ignores all damage and negative effects |
| Reinforce | 2 turns | Reduces damage taken by 50% |
| Kinetic Armor | 2 turns | Blocks damage from one attack |

---

#### Miscellaneous Positive Effects

| Effect | Duration | Description |
|------|----------|-------------|
| Coolant | 3 turns | Reduces energy by 20% each turn |
| Target Lock | 2 turns | Increases accuracy by 50% |
| Target Override | 2 turns | Directs all single-target attacks to self |

---

### Other Effects

#### Other Negative Effects

| Effect | Description |
|------|-------------|
| Remove Positive Effects | Removes positive status effects |

---

#### Other Positive Effects

| Effect | Description |
|------|-------------|
| Remove Negative Effects | Removes negative status effects |
| Heal | Restores health |
| Revive | Revives a defeated Bot |
| Extra Turn | Grants an immediate extra turn |
