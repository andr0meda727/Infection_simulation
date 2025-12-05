# Infection Simulation

A 2D infection spread simulation built with WPF and C# (.NET 8), demonstrating disease transmission dynamics in a bounded population with realistic movement patterns and infection mechanics.

## Overview

This project simulates the spread of a viral infection in a two-dimensional area where individuals move randomly, interact, and potentially transmit disease to one another. The simulation models asymptomatic and symptomatic carriers, immunity acquisition, and population dynamics with individuals entering and leaving the simulation space.

## Features

### Simulation Mechanics

- **Movement System**: Individuals move in random directions with speeds up to 2.5 m/s, with occasional random direction changes
- **Boundary Behavior**: Upon reaching boundaries, individuals either:
  - Turn back into the area (50% probability)
  - Leave the simulation (50% probability)
- **Population Dynamics**: New individuals spawn at random boundary points to maintain population continuity
- **Initial Infection**: 10% chance for new individuals to enter already infected

### Infection Rules

The simulation implements realistic infection transmission:

- **Transmission Requirements**:
  - Distance ≤ 2 meters between individuals
  - Sustained proximity for ≥ 3 seconds
  
- **Transmission Probability**:
  - From asymptomatic carriers: 50%
  - From symptomatic carriers: 100%

- **Disease Progression**:
  - Infection duration: 20-30 seconds (randomized per individual)
  - After recovery: permanent immunity

### Individual States

Each individual can be in one of four states:

1. **Healthy** (Green) - Susceptible to infection
2. **Infected (Asymptomatic)** (Yellow) - Contagious, 50% transmission rate
3. **Infected (Symptomatic)** (Red) - Contagious, 100% transmission rate
4. **Immune** (Blue) - Cannot be infected

### Optional Immunity

- Toggle to start 30% of the initial population with natural immunity
- Immune individuals never contract the disease

## Design Patterns

The project implements several software design patterns:

### 1. **State Pattern**
- **Location**: `States/` directory
- **Implementation**: `IIndividualState` interface with concrete states:
  - `HealthyState`
  - `InfectedAsymptomaticState`
  - `InfectedSymptomaticState`
  - `ImmuneState`
- **Purpose**: Encapsulates state-specific behavior for individuals, allowing clean transitions between health states without complex conditional logic

### 2. **Memento Pattern**
- **Location**: `Models/IndividualMemento.cs`, `Models/SimulationMemento.cs`
- **Implementation**: 
  - `Individual.SaveState()` creates memento
  - `Individual.RestoreState()` restores from memento
  - `SimulationEngine.CreateMemento()` / `RestoreMemento()`
- **Purpose**: Enables save/load functionality without exposing internal state structure, maintaining encapsulation

## Technical Details

### Simulation Parameters

- **Canvas Size**: 800×600 pixels (80×60 meters at 10 pixels/meter scale)
- **Time Step**: 25 updates per second (40ms fixed timestep)
- **Initial Population**: 50 individuals
- **Maximum Population**: 100 individuals
- **Spawn Rate**: 5% chance per update cycle
- **Speed Range**: 0.5 - 2.5 m/s (5-25 pixels/second)
- **Infection Distance**: 20 pixels (2 meters)
- **Contact Time Required**: 3 seconds

### Performance Optimizations

- **Parallel Processing**: Individual updates run in parallel using `Parallel.ForEach`

## User Interface

### Control Panel

- **Start** - Begin simulation
- **Pause** - Pause simulation
- **Reset** - Reset to initial state
- **Save State** - Export simulation to JSON
- **Load State** - Import saved simulation
- **Immunity Checkbox** - Toggle initial immunity (requires reset)

### Statistics Display

Real-time statistics showing:
- Simulation time
- Total population
- Healthy individuals
- Infected individuals
- Immune individuals

### Visual Legend

Color-coded representation of individual states for easy visual tracking.

## Project Structure

```
Infection_Simulation/
├── Models/
│   ├── Individual.cs              # Individual agent with movement and state
│   ├── IndividualMemento.cs       # Memento for individual state
│   └── SimulationMemento.cs       # Memento for entire simulation
├── Simulation/
│   ├── SimConfig.cs               # Simulation constants
│   ├── SimulationEngine.cs        # Core simulation logic
│   └── SimulationSerializer.cs    # Save/load functionality
├── States/
│   ├── IIndividualState.cs        # State pattern interface
│   ├── HealthyState.cs            # Healthy state implementation
│   ├── InfectedAsymptomaticState.cs
│   ├── InfectedSymptomaticState.cs
│   └── ImmuneState.cs
├── Vectors/
│   └── Vector2D.cs                # 2D vector math utilities
├── MainWindow.xaml                # UI definition
└── MainWindow.xaml.cs             # UI logic and rendering
```
