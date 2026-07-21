# EP08 — Expanded Search Space Optimization (8-Dimensional Balance Search)

**Date:** 2026-07-22

---

## Objective

EP07 demonstrated that telemetry-driven multi-objective optimization could successfully balance multiple gameplay metrics simultaneously.

However, optimization was still limited to only four balance parameters:

- Warrior HP
- Mage HP
- Warrior Damage Multiplier
- Mage Damage Multiplier

This experiment investigates whether expanding the optimization search space enables the optimizer to discover fundamentally different combat behaviors.

---

# Motivation

Previous experiments optimized only combat statistics.

Although telemetry was incorporated into the objective function, agents consistently converged toward a kite-oriented combat style.

This raised an important research question:

> **Can expanding the balance parameter space change the emergent combat meta?**

To answer this question, movement speed and skill cooldown multipliers were introduced as additional optimization variables.

---

# Step 1 — Search Space Expansion

The optimization variables were expanded from four dimensions to eight dimensions.

## Previous Search Space (EP07)

| Parameter |
|-----------|
| Warrior HP |
| Mage HP |
| Warrior Damage Multiplier |
| Mage Damage Multiplier |

---

## Expanded Search Space (EP08)

| Parameter |
|-----------|
| Warrior HP |
| Mage HP |
| Warrior Damage Multiplier |
| Mage Damage Multiplier |
| Warrior Movement Speed |
| Mage Movement Speed |
| Warrior Skill Cooldown Multiplier |
| Mage Skill Cooldown Multiplier |

Movement speed and cooldown multipliers were optimized within ±20% of their default values.

| Parameter | Search Range |
|-----------|-------------:|
| Movement Speed | 0.80 – 1.20 |
| Cooldown Multiplier | 0.80 – 1.20 |

---

# Step 2 — Unity Environment Update

The Unity environment was modified so that Optuna could directly control gameplay pacing variables.

During every optimization trial, Unity received eight balance parameters through the ML-Agents Environment Parameters API.

Updated runtime parameters included:

- HP
- Damage Multiplier
- Movement Speed
- Skill Cooldown Multiplier

Unlike EP07, movement speed and cooldown values were no longer randomized.

Instead, they became explicit optimization variables explored by Optuna.

---

# Step 3 — Multi-Objective Optimization

The telemetry-driven objective function introduced in EP07 remained unchanged.

Optimization targets:

- Win Rate Gap
- Survival Time
- Timeout Rate
- Hit Rate
- Average Combat Distance
- DPS

The only difference was that Optuna now searched an eight-dimensional balance space instead of four dimensions.

This significantly increased the flexibility of the optimization process while preserving the same gameplay objectives.

---

# Optimization Result

After more than 110 optimization trials, the following configuration achieved the lowest integrated loss.

| Parameter | Value |
|-----------|------:|
| Warrior HP | **300** |
| Warrior Damage Multiplier | **1.30** |
| Warrior Movement Speed | **0.85×** |
| Warrior Cooldown Multiplier | **1.15×** |
| Mage HP | **150** |
| Mage Damage Multiplier | **1.50** |
| Mage Movement Speed | **1.20×** |
| Mage Cooldown Multiplier | **1.15×** |

---

# Discussion

## Result Analysis

Among all optimization trials, **Trial 26** achieved the lowest integrated loss.

| Metric | Result |
|---------|--------:|
| Integrated Loss | **0.1668** |
| Win Rate Gap | **0.00 (50:50)** |
| Average Survival Time | **24.0 s** |
| Average Combat Distance | **3.9** |
| Warrior DPS | **9.8** |

The optimizer successfully achieved a perfectly balanced win rate while maintaining relatively stable combat duration.

---

## Search Space Analysis

Compared to EP07, the optimizer could now adjust not only combat statistics but also gameplay pacing variables.

This greatly expanded the optimization search space and allowed exploration of a wider variety of balance configurations.

Despite this increased flexibility, the optimizer converged toward a solution with performance very similar to previous experiments.

---

## Emergent Combat Meta

Although movement speed and cooldown multipliers became optimization variables, the learned combat behavior changed very little.

The agents continued to:

- Maintain relatively large combat distances
- Avoid prolonged close-range engagements
- Trade burst damage after repositioning
- Prioritize survival over aggressive pressure

The average combat distance remained **3.9**, noticeably higher than the designer target of **3.2**.

This indicates that expanding the parameter space alone was insufficient to alter the dominant combat strategy.

---

## Asymmetric Class Evolution

Interestingly, to achieve a perfect 50:50 win rate, the optimizer drove the two agents into extreme, asymmetric archetypes.

The Warrior evolved into a "Slow Bruiser," sacrificing mobility (0.85× Speed) for maximum survivability (300 HP).

The Mage evolved into a "Glass Cannon," trading HP (150 HP) for extreme mobility (1.20× Speed) and high damage (1.50× Damage).
The AI discovered that creating two diametrically opposed class identities was the mathematical optimal solution to balance the win rate while maintaining the 24-second combat duration.

---

## Interpretation

The optimization result suggests that the learned combat meta is governed more strongly by the environment itself than by the balance parameters.

Even with eight independent optimization variables, Optuna repeatedly converged toward a kite-oriented equilibrium.

Rather than discovering a fundamentally different combat style, the optimizer adjusted parameter values while preserving nearly identical tactical behavior.

---

## Optimization Trade-off Analysis

The persistence of the kite-oriented combat meta can be partially explained by the weighting of the multi-objective loss function.

Within the integrated objective, the Distance Penalty contributed only **10%** of the total loss, whereas the Win Rate Gap and Survival Penalty accounted for **35%** and **20%**, respectively.

As a result, optimization favored solutions that maintained balanced win rates and desirable combat durations, even when average combat distance deviated from the designer-defined target.

The best trial achieved an average combat distance of **3.9**, compared to the target value of **3.2**, while still minimizing the overall integrated loss.

This outcome illustrates a common challenge in multi-objective optimization: improvements in higher-weighted objectives can outweigh deviations in lower-weighted objectives.

Rather than indicating an implementation error, this behavior reflects the trade-offs encoded in the objective function itself.

From a game-design perspective, the experiment suggests that the weighting of telemetry metrics has a direct influence on the combat strategies that emerge during optimization.

---

## Reward Hacking Perspective

The observed behavior can be interpreted as a form of reward hacking.

Because the optimization objective placed substantially greater emphasis on win-rate equality and survival time than on combat distance, the optimizer consistently converged toward solutions that minimized the weighted objective while allowing larger combat distances than originally intended.

Although this behavior satisfied the mathematical optimization objective, it did not fully align with the desired gameplay characteristics specified by the designer.

This observation highlights that reward hacking is not necessarily caused by flaws in the learning algorithm itself, but can also emerge when the objective function does not completely capture the designer's intentions.

---

## Implications

EP08 demonstrates an important limitation of parameter optimization.

Increasing the number of tunable variables does not necessarily produce more diverse gameplay.

Instead, reinforcement learning agents continue exploiting the combat dynamics that provide the lowest optimization loss under the existing environment rules.

This suggests that changing balance parameters alone is insufficient for altering emergent gameplay.

Meaningful changes to combat behavior may require modifications to the environment, reward structure, or combat mechanics themselves.

---

# Outcome

EP08 extends the balance optimization framework from a four-dimensional search space to an eight-dimensional search space.

Key improvements include:

- Expanded optimization variables
- Direct optimization of movement speed
- Direct optimization of skill cooldown multipliers
- Eight-dimensional Optuna balance search
- Evaluation under the same telemetry-driven objective function

---

# Conclusion

EP08 demonstrates that increasing the dimensionality of the optimization problem does not necessarily change emergent gameplay behavior.

Although Optuna successfully explored a significantly larger search space while maintaining balanced win rates and competitive integrated loss values, the learned agents consistently converged toward the same kite-oriented combat strategy observed in previous experiments.

These findings suggest that the dominant combat meta is determined primarily by the underlying environment dynamics rather than the balance parameters alone.

Future work will therefore focus on modifying the environment and reward formulation to encourage fundamentally different combat behaviors instead of relying solely on parameter optimization.

---

# Emergence of Classical RPG Archetypes

One of the most interesting observations in EP08 is the structure of the balance parameters discovered by the optimizer.

Starting from no prior knowledge of RPG class design, the optimization process consistently converged toward two highly asymmetric character identities.

| Warrior | Mage |
|----------|------|
| High HP | Low HP |
| Lower Movement Speed | Higher Movement Speed |
| Moderate Damage | High Damage |

Rather than producing two statistically identical agents, the optimizer naturally differentiated the classes into distinct combat roles.

The Warrior evolved into a durable front-line fighter capable of surviving prolonged engagements, while the Mage became a highly mobile, high-damage combatant with significantly lower survivability.

Interestingly, these characteristics closely resemble the traditional **Bruiser** and **Glass Cannon** archetypes commonly found in RPGs.

This convergence was not explicitly encoded by the designer.

Instead, it emerged solely from optimizing the weighted gameplay objective under the combat mechanics defined in the environment.

Although this experiment does **not** prove that these archetypes are universally optimal, it provides empirical evidence that classical RPG class structures can naturally emerge from reinforcement learning-based balance optimization.

More importantly, the experiment suggests that many long-established game design conventions may not be purely products of designer intuition.

Instead, they may represent stable strategic solutions that naturally arise under particular combat mechanics and optimization objectives.

This observation highlights an additional application of reinforcement learning beyond automated balance tuning.

Besides searching for balanced parameter sets, reinforcement learning can also serve as a computational tool for exploring and validating game design hypotheses through large-scale simulation.
