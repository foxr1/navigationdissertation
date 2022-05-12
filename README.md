# Learning Navigation on Procedurally Generated Cities: A Reinforcement Learning Perspective

A dissertation project for third year Computer Science at Newcastle University, by Oliver Fox.

## Introduction
This project aims to create an efficient AI to navigate a procedurally generated city, while accounting for dynamic updates like avoiding obstacles at runtime. Both supervised and unsupervised methods have been implemented, with a comparison scene show casing how they both perform on this environment.

### Scenes
This project contains three cities, 5x5, 7x7, 13x13 which the agent is trained in various ways, using different model configurations as well as different observations, etc. Roadblocks can be generated in all cities.

Eventually, only a 13x13 city was used, with 40 roadblocks changing at 5 second intervals.

## Learning Algorithms
For the supervised learning, reinforcement learning was used using ML-Agents described below, and for the unsupervised method, an implentation of A* with replanning was created.

## Training the AI
The training was carried out using [Unity's ML-Agent's Tooklit](https://github.com/Unity-Technologies/ml-agents), an open-source project that uses a Python API to train agents using reinforcement learning.

### Setting up the virtual environment
Due to GitHub file storage limitations, it is unwise to commit the virtual environment used for the reinforcement learning in the project, therefore to setup for this project, follow [Unity's installation guide](https://github.com/Unity-Technologies/ml-agents/blob/main/docs/Installation.md).
