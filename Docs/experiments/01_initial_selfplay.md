# Experiment 01 — Initial Self-Play Training

**Date:** 2026-07-06

---

## Goal

Evaluate the initial self-play environment and identify reward-shaping issues.

---

# Environment Setup

| Setting | Value |
|---------|-------|
| Episode Length | 60 seconds |
| Warrior HP | 200 |
| Mage HP | 200 |
| Warrior Move Speed | 3 |
| Mage Move Speed | 3 |
| Training Mode | Self-Play |
| Observation Space | 30 dimensions |
| Action Space | Multi-Discrete (9 Movement + 5 Skills) |

---

# Observation Space

At every FixedUpdate step, each agent observes a **30-dimensional state vector** before selecting an action.

s_t = [s_self, s_enemy, s_hazard] ∈ R^30

where the observation consists of the agent's own state, the opponent's state, and nearby environmental hazards.

---

## Self State (13)

| Feature | Description |
|---------|-------------|
| Class ID | Warrior (0) / Mage (1) |
| Position | Local X/Y position (normalized by 5) |
| Velocity | Normalized movement vector |
| HP Ratio | Current HP / Max HP |
| Skill Ready | Cooldown availability of four skills |
| Status | Stun / Invincible / CC Immunity |

The self-state vector is defined as

s_self = [
    class_id,
    pos_x,
    pos_y,
    vel_x,
    vel_y,
    hp_ratio,
    skill_ready_1,
    skill_ready_2,
    skill_ready_3,
    skill_ready_4,
    stun,
    invincible,
    cc_immunity
]

---

## Enemy State (10)

| Feature | Description |
|---------|-------------|
| Relative Position | Relative X/Y position (normalized by 10) |
| Velocity | Enemy movement direction |
| HP Ratio | Enemy HP ratio |
| Relative Distance | Normalized distance |
| Status | Stun / Invincible / CC Immunity / Charging |

The opponent observation is represented as

s_enemy = [
    rel_pos_x,
    rel_pos_y,
    enemy_vel_x,
    enemy_vel_y,
    enemy_hp_ratio,
    relative_distance,
    enemy_stun,
    enemy_invincible,
    enemy_cc_immunity,
    enemy_charging
]

---

## Hazard Radar (7)

| Feature | Description |
|---------|-------------|
| Closest Hazard | Relative position of the nearest projectile or AoE |
| Reserved Space | Five-dimensional padding reserved for future extensions |

The environmental observation is defined as

s_hazard = [
    nearest_hazard_x,
    nearest_hazard_y,
    reserved_1,
    reserved_2,
    reserved_3,
    reserved_4,
    reserved_5
]

All observations are normalized before being passed to the neural network. Relative coordinates are used instead of absolute world coordinates to improve translation invariance and facilitate policy generalization across the arena.

---

## Observation Normalization

All continuous observations are normalized before being fed into the policy network.

Position and distance values are scaled to approximately

\[
[-1,1]
\]

to reduce gradient variance and stabilize PPO optimization.

Furthermore, relative coordinates are used instead of absolute world coordinates, introducing translation invariance into the learned policy.

As a result, the agent learns spatial relationships with respect to its opponent rather than memorizing specific arena locations, improving policy generalization across different environments.

## Relative Coordinates

Instead of absolute world coordinates,

\[
(x,y)
\]

the agent observes

\[
(x_{enemy}-x_{self},
y_{enemy}-y_{self})
\]

This introduces translation invariance into the policy.

Consequently, the learned behavior becomes independent of arena location.

---

# Action Space
a_t = (a_t^{move}, a_t^{skill})

a_t^{move}\in\{0,\cdots,8\}
a_t^{skill}\in\{0,\cdots,4\}

The final action is sampled independently from two categorical distributions produced by the actor network.

---

# Runtime Optimization

### Rendering Optimization
Disabled VSync

Reduced Game View resolution

### Memory Optimization
Instead of allocating and destroying projectile instances every frame,

all temporary gameplay objects are recycled through a custom object pool.

This minimizes garbage collection overhead and stabilizes simulation throughput during long training sessions.

### Telemetry Pipeline

Raw gameplay events are asynchronously logged to CSV during training.

Statistical metrics are generated in a post-processing stage to avoid interfering with PPO optimization.

All expensive metrics (DPS, Hit Rate, Skill Usage, etc.) are computed offline using analyze_telemetry.py.

This keeps the training loop lightweight and minimizes CPU bottlenecks.

---

# Training Result #1 (1000 Episodes)

## Overall Result

| Metric | Value |
|--------|------:|
| Mage Win Rate | 30% |
| Warrior Win Rate | 26% |
| Timeout / Draw Rate | **44%** |

---

## Key Findings

### 1. Excessive Timeouts

- **44%** of all matches ended in a draw.
- Both agents survived too long relative to their damage output.
- Kill pressure was insufficient.

---

### 2. Warrior Damage Leakage

- Basic Attack was cast **23.01** times on average.
- Hit Rate was only **8%**.
- Suggested that melee range and attack angle were too restrictive.

---

### 3. Mage's Meteor Became the Decisive Skill

- Meteor achieved a **49%** hit rate despite its **1.5-second charge time**.
- The skill appeared to have the largest impact on match outcomes.

---

# Initial Skill Adjustment (Warrior)

After the first training run, the Warrior's basic attack was buffed to improve melee effectiveness.

| Parameter | Before | After |
|-----------|--------|-------|
| Attack Radius | 1.5 | 2.0 |
| Attack Angle | 90° | 120° |
| Damage | 10 | 20 |

---

# Training Result #2 (1000 Episodes)

## Warrior

### Overall Statistics

| Metric | Value |
|--------|------:|
| Win Rate | 27% |
| Total Damage | 125.95 |
| Damage Taken | 171.27 |
| DPS | 4.03 |
| Survival Time | 31.23 sec |
| Average Distance | 5.26 |

### Skill Telemetry

| Skill | Cast Count | Hit Rate | Avg Skill Damage |
|------|-----------:|----------:|-----------------:|
| Basic Attack | 35.39 | 7% | 43.06 |
| Dash | 11.79 | 15% | 15.42 |
| Charge Attack | 7.61 | 9% | 20.25 |
| CC Pull | 4.76 | **28%** | 74.52 |

#### Additional Metrics

| Metric | Value |
|--------|------:|
| Charge Success Rate | 93% |
| CC Success Rate | 9% |
| CC Pull Success Rate | 82% |

---

## Mage

### Overall Statistics

| Metric | Value |
|--------|------:|
| Win Rate | 32% |
| Total Damage | 116.60 |
| Damage Taken | 85.75 |
| DPS | 5.48 |
| Survival Time | 21.26 sec |
| Average Distance | 5.06 |

### Skill Telemetry

| Skill | Cast Count | Hit Rate | Avg Skill Damage |
|------|-----------:|----------:|-----------------:|
| Basic Attack | 22.45 | 38% | 44.47 |
| Teleport | 7.31 | - | - |
| Charge Attack | 7.35 | 8% | 17.51 |
| Meteor | 2.30 | **49%** | 114.63 |

#### Additional Metrics

| Metric | Value |
|--------|------:|
| Charge Success Rate | 92% |
| Meteor Success Rate | 60% |

---

# Analysis

## Combat Pattern Analysis

### 1. Warrior Still Failed to Convert Pressure into Damage

- Basic Attack usage increased from **23.01 → 35.39** casts.
- Hit Rate remained extremely low at **7%**.
- Increasing attack range, angle, and damage alone did not significantly improve combat performance.

---

### 2. Mage Maintained Higher Combat Efficiency

Compared to the Warrior, the Mage demonstrated:

- Lower damage taken (**85.75**)
- Higher DPS (**5.48**)
- Higher Basic Attack hit rate (**38%**)

Overall, the Mage maintained superior combat efficiency throughout training.

---

### 3. Meteor Remained the Highest-Impact Skill

Although Meteor was cast only **2.3 times** per episode,

- Hit Rate: **49%**
- Average Damage: **114.63**

It consistently produced the highest impact on combat outcomes.

---

### 4. Timeout Issue Persisted

Both agents continued spending a large portion of each episode repositioning instead of securing eliminations.

This resulted in many matches reaching the 60-second limit.

---

# Conclusion

The first self-play experiment revealed that **simple stat buffs alone were insufficient to fundamentally change agent behavior.**

Although the Warrior received significant improvements to attack range, attack angle, and damage, the agent continued exhibiting inefficient melee engagement patterns and failed to substantially improve hit conversion.

These findings suggest that **reward design has a much greater influence on learned behavior than raw skill statistics.**

Therefore, subsequent experiments focus on modifying optimization objectives rather than modifying gameplay parameters.

---

# Next Experiment

The next iteration will introduce **class-specific asymmetric reward functions**.

Instead of sharing an identical reward structure:

- **Warrior** will receive stronger incentives for maintaining melee pressure, closing distance, and successfully landing close-range attacks.

- **Mage** will be rewarded for maintaining optimal spacing, surviving longer, and maximizing the effectiveness of high-value ranged abilities.

The objective is to shift optimization from **stat balancing** toward **behavior shaping through reward engineering**, enabling each class to learn strategies better aligned with its intended gameplay role.

---

# Lessons Learned

Experiment 01 provided several insights that influenced the next design iteration.

- Increasing raw skill statistics alone did not significantly improve combat behavior.

- Reward shaping had a much greater impact on learned strategies than parameter tuning.

- Rich telemetry data proved essential for identifying behavioral bottlenecks.

- Class-specific behaviors require class-specific optimization objectives rather than symmetric reward functions.
