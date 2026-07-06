# CHANGELOG

All notable changes to the project are documented here.

---

## v0.2.0 — Asymmetric Reward Update *(In Progress)*

This update focuses on improving combat behavior through reward shaping rather than simply increasing skill statistics.

---

### Reward Shaping

#### Asymmetric Reward Functions

Introduced class-specific reward functions to encourage distinct combat identities.

**Warrior**

- Receives a continuous reward for maintaining close-range pressure (`distance <= 2.5`).
- Receives a small penalty for staying too far from the opponent (`distance >= 5.0`).

Expected behavior:

- Close the distance aggressively.
- Maintain melee pressure.
- Reduce passive movement.

---

**Mage**

- Receives a continuous reward for maintaining long-range spacing (`distance >= 4.0`).
- Receives a penalty when allowing enemies to get too close (`distance <= 2.0`).

Expected behavior:

- Kite opponents.
- Preserve safe casting distance.
- Utilize ranged advantages.

---

#### Combo Reward

Added a combo reward mechanic for the Warrior.

When consecutive attacks successfully deal damage within a **1.5-second combo window**, an additional reward is granted.

```
Combo Window : 1.5 seconds
Bonus Reward : +0.2
```

Purpose:

- Encourage sustained offensive pressure.
- Reward successful follow-up attacks.
- Reduce "hit once then disengage" behavior.

---

#### Increased Damage Reward

The reward for successfully dealing damage has been significantly increased.

```
Previous:
0.01 × Damage

Updated:
0.05 × Damage
```

This places greater emphasis on effective combat rather than passive survival.

---

### PPO Training Improvements

#### Increased Batch Size

```
1024 → 2048
```

Larger batches are expected to reduce gradient noise generated during self-play and provide more stable policy updates.

---

#### Gradient Clipping

Added gradient clipping during optimization.

```python
torch.nn.utils.clip_grad_norm_(
    model.parameters(),
    max_norm=0.5
)
```

Purpose:

- Prevent exploding gradients.
- Reduce overshooting during policy updates.
- Improve overall PPO training stability.

---

## Expected Outcome

Compared with Experiment 01, this update aims to:

- Reduce excessive disengagement.
- Encourage class-specific combat strategies.
- Increase kill pressure.
- Lower timeout frequency.
- Produce more distinguishable Warrior and Mage playstyles.