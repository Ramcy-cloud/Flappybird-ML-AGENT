# 🐦 Flappy Bird — ML Agent

> Teaching an AI to play Flappy Bird using Reinforcement Learning and Unity ML-Agents.

---

## Overview

This project combines the classic **Flappy Bird** game with a **machine learning agent** trained via reinforcement learning. Instead of hand-crafted rules, the agent learns entirely through trial and error — starting from random behavior and gradually mastering the game.

The agent is trained using **PPO (Proximal Policy Optimization)** through the Unity ML-Agents framework, using only 4 numerical inputs to make decisions.

---

## Demo

| Mode | Description |
|------|-------------|
| 🤖 AI Mode | Trained model plays autonomously |
| 🎮 Human Mode | Classic manual gameplay |

---

## How It Works

### Observations (Input — 4 values)

The agent does not see pixels. It receives 4 normalized numbers:

| # | Observation | Normalization |
|---|-------------|---------------|
| 1 | Bird Y position | / Y_MAX (50) |
| 2 | Bird vertical velocity | / JUMP_AMOUNT (90) |
| 3 | Distance to next pipe | / 100 |
| 4 | Next pipe gap Y position | / Y_MAX (50) |

### Actions (Output — discrete)

| Value | Action |
|-------|--------|
| `1` | Jump (apply upward velocity) |
| `0` | Do nothing (gravity pulls down) |

### Reward System

| Event | Reward |
|-------|--------|
| Each frame survived | **+0.01** |
| Passing through a pipe | **+1.00** |
| Collision or out of bounds | **−1.00** |

---

## Project Structure

```
Flappybird-ML-AGENT/
├── Assets/
│   ├── scripts/
│   │   ├── Birdagent.cs        # ML-Agent logic (obs, actions, rewards)
│   │   ├── Level.cs            # Pipe generation + difficulty scaling
│   │   ├── GameHandler.cs      # Game initialization
│   │   ├── GameAssets.cs       # Asset manager (singleton)
│   │   ├── Score.cs            # Score tracking
│   │   ├── SoundManager.cs     # Audio system
│   │   └── ...                 # UI scripts
│   ├── ML-Agents/              # ML-Agents package
│   ├── Prefabs/                # Reusable game objects
│   ├── Scenes/                 # Unity scenes
│   ├── Animations/             # Bird & pipe animations
│   └── texture/                # Sprites
├── results/
│   ├── runFinal/
│   │   ├── FlappyBird.onnx     # Trained neural network model
│   │   ├── configuration.yaml  # Run configuration snapshot
│   │   └── run_logs/           # TensorBoard training logs
│   └── flappy_v1/              # Earlier training run
├── flappy.yaml                 # PPO training configuration
└── ProjectSettings/
```

---

## Training Configuration (`flappy.yaml`)

```yaml
behaviors:
  FlappyBird:
    trainer_type: ppo
    hyperparameters:
      batch_size: 64
      buffer_size: 12000
      learning_rate: 3.0e-4
      beta: 5.0e-3
      epsilon: 0.2
      lambd: 0.95
      num_epoch: 3
    network_settings:
      normalize: false
      hidden_units: 128
      num_layers: 2
    reward_signals:
      extrinsic:
        gamma: 0.99
        strength: 1.0
    max_steps: 5000000
    time_horizon: 64
    summary_freq: 1000
    checkpoint_interval: 50000
    keep_checkpoints: 5
```

---

## Difficulty Scaling

The game automatically increases difficulty as the agent progresses:

| Pipes Passed | Difficulty | Gap Size | Spawn Interval |
|-------------|------------|----------|----------------|
| 0 – 4 | Easy | 50 | 1.4s |
| 5 – 11 | Medium | 40 | 1.3s |
| 12 – 23 | Hard | 33 | 1.1s |
| 24+ | Impossible | 24 | 1.0s |

---

## Training Progress

```
Step 0         →  Random jumps, dies immediately
Step ~50K      →  Learns that falling = death
Step ~500K     →  Occasionally passes a pipe
Step ~2M       →  Consistently clears multiple pipes
Step 5M        →  Expert-level play, model saved as FlappyBird.onnx
```

---

## Neural Network Architecture

```
Input Layer     →  4 observations
Hidden Layer 1  →  128 units (ReLU)
Hidden Layer 2  →  128 units (ReLU)
Output Layer    →  2 actions (Jump / Do Nothing)
```

---

## Setup & Running

### Requirements

- Unity 2022.3+
- [ML-Agents Release 21+](https://github.com/Unity-Technologies/ml-agents)
- Python 3.9+ (for training only)

### Run the Trained Agent

1. Open the project in Unity
2. Select the **Bird** GameObject in the scene
3. In the Inspector, assign `results/runFinal/FlappyBird.onnx` to the **Model** field
4. Set **Behavior Type** to `Inference`
5. Press Play

### Train from Scratch

```bash
# Install ML-Agents Python package
pip install mlagents

# Start training
mlagents-learn flappy.yaml --run-id=my_run

# Press Play in Unity to begin
```

### Monitor Training

```bash
tensorboard --logdir results
```

---

## Key Takeaways

- **Minimal input is powerful** — 4 numbers are enough to master the game
- **Reward design matters** — the +0.01 survival bonus was critical for learning
- **PPO is stable** — no divergence, reliable convergence for discrete action spaces
- **Unity ML-Agents** makes game-based RL accessible and easy to iterate on

---

## Tech Stack

| Tool | Purpose |
|------|---------|
| Unity 2022+ | Game engine |
| C# | Game & agent scripting |
| Unity ML-Agents | RL framework |
| PPO | Training algorithm |
| ONNX | Model export format |
| TensorBoard | Training visualization |

---

## Author

**Ramcy-cloud** — March 2026
[github.com/Ramcy-cloud/Flappybird-ML-AGENT](https://github.com/Ramcy-cloud/Flappybird-ML-AGENT)
