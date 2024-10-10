# Modified ML-Agents Soccer Environment

## Project Overview

This project is a modified version of the Unity ML-Agents' Soccer environment. The goal of this modification was to:

- Increase the number of players on each team from 2 to 3.
- Adjust the reward system to account for changes in gameplay dynamics.

These changes modify an existing machine learning environment and observe the effects on agent behavior and learning outcomes.

## Key Changes

### 1. Increased Number of Players

The original soccer environment (`SoccerTwos`) had 2 players per team. This was increased to **3 players per team** to create a more complex game scenario. The scene and related scripts were updated to support 3 players per team.

### 2. Modified Reward System

- Striker players now receive a slightly harsher existential penalty to account for more stikers, multiplied by 1.3x (`AgentSoccer.cs`).
- A new **pass reward** mechanism was added:
  - Players receive a reward for making a successful pass to a teammate if the pass distance is greater than a specified minimum (`SoccerBallController.cs`).
  - Rewards are assigned as follows:
    - **0.5 reward** for a successful pass to a teammate.
    - **0.2 reward** for the receiving player if the pass was intercepted by the opponent.
    - **-0.5 penalty** for the previous player if the pass was intercepted by the opponent.

### 3. Goal Scoring Adjustments

- Additional checks were added to penalize players for scoring an **own goal** with a **-0.5 penalty**.
- A reward of **1.0** is given to the last player who touched the ball before a valid goal is scored.

## Code Modifications

The main files that were changed include:

- **SoccerTwos.unity**: Scene file updated to accommodate 3 players.
- **AgentSoccer.cs**: Added 1.3x multiplier to existential penalty for strikers.
- **SoccerBallController.cs**: Added logic for pass distance checks and rewarding/punishing based on successful passes or interceptions.
- **SoccerEnvController.cs**: Logic for tracking the last player who touched the ball and implementing the modified reward system for goals and passes.

## Setup and Usage

1. Clone the repository:
   ```git clone https://github.com/CoobMC/AIML_G13```
2. Checkout `3players` branch.
3. Open the project in Unity.
4. Navigate to the `Soccer` scene under `ML-Agents/Examples/Soccer/Scenes/SoccerThrees.unity`.
5. Train the model using the modified environment. Follow the usual ML-Agents training setup instructions in the original repository.

## Expected Outcomes

- Teams with 3 players will introduce new dynamics in agent coordination and strategy.
- The modified reward system encourages teamwork by rewarding successful passes and penalizing interception and own goals.
- This change should result in more sophisticated agent behaviors in response to the more complex environment.

## Conclusion

This project demonstrates the effects of minor adjustments in the number of players and reward mechanisms in a reinforcement learning environment. By increasing the team size and adding pass-based rewards, the agents will need to learn new strategies for collaboration and competition.
