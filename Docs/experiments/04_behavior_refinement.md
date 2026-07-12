# Experiment 04 — Reward & Behavior Refinement

**Date:** 2026-07-12

---

# Goal

Reduce the frequency of draw outcomes before balance optimization and encourage agents to engage in combat more actively and accurately.

This experiment focuses on improving combat tempo, attack precision, and episode decisiveness, providing a more reliable foundation for subsequent automated balance optimization.

---

# Motivation

Previous experiments successfully established distinct combat behaviors through asymmetric reward shaping and richer environmental observations.

However, telemetry analysis revealed several remaining limitations.

- A considerable number of matches still ended in draws due to prolonged disengagement.
- Agents occasionally preferred passive repositioning over active combat.
- The Warrior continued to perform unnecessary Basic Attacks despite previous reward refinements.

Before applying automated balance optimization, these issues must be addressed to ensure that combat outcomes accurately reflect policy quality rather than episode termination.

---

# System & Reward Refinement

## 1. Draw Reduction System

To minimize draw outcomes and encourage decisive engagements, several environment-level mechanisms were introduced.

---

### Time-Decay Penalty

A dynamic time-decay penalty is applied throughout the episode.

The longer an episode continues, the larger the accumulated penalty becomes.

This discourages excessive repositioning and encourages agents to actively seek combat.

---

### Magnetic Field System (Sudden Death)

After **40 seconds**, a magnetic field system is activated.

Every **0.5 seconds**, both agents receive continuous damage equal to **5% of their maximum HP**.

This mechanism guarantees that prolonged matches eventually reach a decisive conclusion.

| Parameter | Value |
|-----------|------:|
| Activation Time | 40 sec |
| Damage Interval | 0.5 sec |
| Damage | 5% of Max HP |

---

### Victory Determination

If both agents are defeated by the magnetic field during the same damage tick,

the winner is determined by comparing the remaining HP ratio immediately before damage was applied.

| Condition | Result |
|-----------|--------|
| Higher Remaining HP Ratio | Victory |
| Lower Remaining HP Ratio | Defeat |

This criterion rewards the agent that maintained a stronger combat advantage throughout the episode.

---

### Draw Penalty

If both agents possess exactly the same remaining HP ratio,

both are recorded as defeated.

Rather than allowing repeated draw outcomes,

this policy encourages both agents to avoid passive gameplay and pursue decisive victories.

---

## 2. Warrior Reward Refinement

### Whiff Penalty

To further improve attack precision,

an additional penalty is introduced when the Warrior's Basic Attack fails to hit the opponent.

| Condition | Reward |
|-----------|--------:|
| Basic Attack Miss | **-0.01** |

Instead of rewarding attack frequency,

the policy is encouraged to maximize successful attack opportunities.

This refinement discourages unnecessary attack spamming and promotes more deliberate melee engagements.

---

# Expected Outcome

Compared with Experiment 03, this refinement aims to

- Reduce draw frequency through environment-level combat enforcement.
- Encourage earlier and more decisive engagements.
- Improve Warrior Basic Attack precision.
- Suppress inefficient attack spamming.
- Produce higher-quality telemetry suitable for automated balance optimization.

---

## Pending Evaluation

The following sections will be added after training is completed.

- Training Result
- TensorBoard Analysis
- Telemetry Analysis
- Discussion
- Conclusion

The objective is to evaluate whether the refined reward structure and environment mechanics successfully reduce draw frequency while preserving stable PPO optimization and meaningful combat behavior.