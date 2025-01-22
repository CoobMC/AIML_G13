# AIML Group 13: Extended ML-Agents Soccer Environment

## Table of Contents

- [Overview](#overview)
- [Key Changes](#key-changes)
  - [Increased Number of Players](#increased-number-of-players)
  - [Modified Reward System](#modified-reward-system)
  - [Goal Scoring Adjustments](#goal-scoring-adjustments)
- [Setup and Installation](#setup-and-installation)
  - [Cloning the Repository](#cloning-the-repository)
  - [Environment Setup](#environment-setup)
- [Usage Instructions](#usage-instructions)
  - [Running the Environment](#running-the-environment)
  - [Training the Agents](#training-the-agents)
  - [Reproducing Experiments](#reproducing-experiments)
    - [Baseline Training](#baseline-training)
    - [Parameter Tuning](#parameter-tuning)
    - [Custom Features](#custom-features)
- [Key Findings](#key-findings)
- [Results and Metrics](#results-and-metrics)
  - [Learning Rate Experiments](#learning-rate-experiments)
  - [Number of Layers Experiments](#number-of-layers-experiments)
  - [Batch Size Experiments](#batch-size-experiments)
  - [Hidden Units Experiments](#hidden-units-experiments)
- [Performance Comparison](#performance-comparison)
  - [In-Editor vs Compiled Executable Training](#in-editor-vs-compiled-executable-training)
- [Contributing](#contributing)
- [References](#references)

---

## Overview

This project extends Unity ML-Agents' Soccer environment, introducing modifications to increase complexity and evaluate agent behaviors in dynamic and collaborative gameplay. Our main goals are to:

- Increase the number of players per team.
- Adjust reward mechanisms to encourage teamwork.
- Analyze agent learning outcomes with varying parameters.

The modified environment provides valuable insights into Deep Reinforcement Learning (DRL) in multi-agent scenarios.

## Key Changes

### Increased Number of Players

- **Original Setup:** 2 players per team.
- **Modified Setup:** 3 players per team, enhancing gameplay complexity and requiring better coordination.
- **Files Modified:**
  - `SoccerTwos.unity`
  - `SoccerEnvController.cs`

### Modified Reward System

- **Existential Penalty:** Increased by 1.3x for striker players.
- **Pass Rewards:**
  - 0.5 reward for successful passes exceeding a minimum distance.
  - 0.2 reward for a receiving player if intercepted by an opponent.
  - -0.5 penalty for the passer in case of interception.
- **Files Modified:**
  - `AgentSoccer.cs`
  - `SoccerBallController.cs`

### Goal Scoring Adjustments

- **Own Goal Penalty:** -0.5 penalty.
- **Goal Scorer Reward:** 1.0 for the last player touching the ball.
- **Files Modified:**
  - `SoccerBallController.cs`
  - `SoccerEnvController.cs`

---

## Setup and Installation

### Cloning the Repository

```bash
git clone https://github.com/CoobMC/AIML_G13.git
cd AIML_G13
```

### Environment Setup

1. Install [Unity Hub](https://unity.com/download) and Unity Editor version 2022.1.0 or newer.
2. Install Python (>=3.8).
3. Set up the Unity ML-Agents Toolkit:
   ```bash
   pip install mlagents
   ```
4. Open the project in Unity:
   - Navigate to `SoccerTwos.unity` under `ML-Agents/Examples/Soccer/Scenes`.

---

## Usage Instructions

### Running the Environment

1. Launch Unity Editor.
2. Open the Soccer scene: `SoccerThrees.unity`.
3. Press Play to observe the environment.

### Training the Agents

Use the following command to start training:

```bash
mlagents-learn config/trainer_config.yaml --run-id=<run_name>
```

Replace `<run_name>` with your desired experiment name.

### Reproducing Experiments

#### Baseline Training

- **Description:** Train agents using default ML-Agents configurations.
- **Metrics:** Cumulative rewards, training time, and task-specific performance indicators.
- **Steps:**
  1. Open `SoccerTwos.unity`.
  2. Use the default YAML configuration.

#### Parameter Tuning

- **Parameters Tested:**
  - Learning rates (0.0001, 0.0003, 0.001).
  - Neural network layers (1, 2, 3).
  - Batch sizes (1024, 2048, 4096).
  - Hidden units (128, 256, 512).
- **Steps:** Modify the `trainer_config.yaml` file for each parameter set.

#### Custom Features

- **Memory of Observations:** Add memory-based observation components in the Unity scene.
- **Sound-Based Sensors:** Implement auditory inputs for agents using Unity's AudioSource and AudioListener components.

---

## Key Findings

- Larger teams require improved collaboration and communication strategies.
- Modified reward mechanisms encourage teamwork but increase training complexity.
- Optimized parameters lead to faster convergence and better agent performance.

## Results and Metrics

### Learning Rate Experiments

| Learning Rate | Cumulative Reward | Episode Length | Final ELO |
| ------------- | ----------------- | -------------- | --------- |
| 0.0001        | 8.77              | 230            | 1439.9    |
| 0.0003        | 16.70             | 380            | 1561.9    |
| 0.001         | 18.64             | 496            | 1391.8    |

### Number of Layers Experiments

| Layers | Cumulative Reward | Episode Length | Final ELO |
| ------ | ----------------- | -------------- | --------- |
| 1      | 10.53             | 420            | 1511.2    |
| 2      | 16.70             | 380            | 1602      |
| 3      | 17.20             | 450            | 1694.3    |

### Batch Size Experiments

| Batch Size | Final ELO | Convergence Behavior | Resource Usage |
| ---------- | --------- | -------------------- | -------------- |
| 1024       | 1500      | Stabilized quickly   | Moderate       |
| 2048       | 1600      | Gradual upward trend | Efficient      |
| 4096       | 1695      | Required tuning      | High           |

### Hidden Units Experiments

| Hidden Units | Final ELO | Stability | Resource Usage |
| ------------ | --------- | --------- | -------------- |
| 128          | 1432.5    | Stable    | Low            |
| 256          | 1559      | Stable    | Moderate       |
| 512          | 1601      | Stable    | High           |

---

## Performance Comparison

### In-Editor vs Compiled Executable Training

| Environment         | CPU Usage | Memory Usage | Training Time |
| ------------------- | --------- | ------------ | ------------- |
| In-Editor           | 52%       | 18.4 GB      | 12.5 hours    |
| Compiled Executable | 43%       | 15.3 GB      | 8.3 hours     |

---

## Contributing

1. Fork the repository.
2. Create a new branch for your feature or fix.
3. Submit a pull request.

---

## References

1. Sutton, R. S., & Barto, A. G. (1998). _Reinforcement Learning: An Introduction._
2. Unity Technologies. [Unity ML-Agents Toolkit](https://unity.com/ml-agents).
3. Juliani, A., et al. (2020). Unity: A General Platform for Intelligent Agents.
