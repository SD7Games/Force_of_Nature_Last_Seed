# Last Seed: Survival

2D mobile auto-shooter built in Unity 6 and C#, focused on modular gameplay systems, mobile performance, data-driven balancing, and maintainable feature implementation.

Status: in development, Android-focused, prepared around a public mobile release workflow.

## Overview

Last Seed: Survival is my current main Unity project. The goal is to build a mobile auto-shooter where combat, rewards, enemy behavior, UI flow, and balancing can grow without turning the project into tightly coupled scene logic.

The project is structured around practical production concerns:

- gameplay systems should be easy to extend;
- reward and weapon values should be tuned through data;
- frequently spawned objects should be pooled;
- UI/presentation should not own core gameplay rules;
- mobile runtime behavior should stay stable under object-heavy combat.

## My Role

Solo Unity C# development across gameplay programming, Unity scene/prefab setup, UI flow, reward balancing, performance work, debugging, and Android build preparation.

## Key Systems I Built

### Modular Weapon System

The weapon layer is built around runtime state, weapon configs, projectile configs, and modifier-driven shooting behavior.

Implemented work includes:

- projectile weapon system;
- Acacia Thorn secondary weapon system;
- runtime weapon progression state;
- shot pattern builder;
- damage, fire rate, critical chance, critical power, penetration, salvo, projectile speed, and parallel projectile modifiers;
- safety clamp for extreme projectile counts;
- weapon rebuild flow when rewards modify runtime stats.

Engineering value:

- weapon behavior can be extended without rewriting the whole shooter;
- balancing data is separated from runtime state;
- projectile spawning is routed through pools instead of direct repeated instantiation.

### Reward Choice System

The reward system uses a runtime context, reward database, roll service, apply service, reward effects, rarity slots, and category rules.

Implemented work includes:

- 3-choice reward roll flow;
- rarity-based reward pools;
- reward category uniqueness rules;
- guaranteed rarity support;
- new weapon unlock gating based on worm progress;
- DPS-aware reward bias to help weaker weapon groups catch up;
- reward apply service that modifies runtime weapon state;
- UI-facing reward choice data;
- reroll, ad-reroll, take-all, and revive-related reward flow support.

Engineering value:

- reward logic is testable and readable outside UI code;
- reward data can be tuned without hardcoding values in gameplay components;
- future reward effects can be added through dedicated effect classes.

### Segmented Worm Enemy

The main enemy is a segmented worm built from sections and segments, moving along a rail path with combat sections, rollback behavior, HP scaling, and presentation controllers.

Implemented work includes:

- worm spawner, factory, controller, section builder, and pattern builder;
- worm segment pool;
- rail path movement;
- worm section HP generation;
- damage receivers for worm segments;
- combat progress tracking;
- rollback behavior when sections are destroyed;
- cocoon rules and visual states;
- HP/progress/damage popup presenters.

Engineering value:

- enemy logic is split between movement, combat, balance, and presentation;
- pooled segments reduce object churn;
- section-based combat supports more interesting enemy behavior than a single health value.

### Object Pooling And Runtime Performance

The project uses custom pools for frequently reused gameplay objects.

Implemented work includes:

- projectile pool with prewarm;
- pool registry;
- worm segment pool;
- active projectile release flow;
- screen bounds service for projectile lifecycle;
- mobile target frame-rate bootstrap based on refresh rate, memory, and CPU tier.

Engineering value:

- reduced runtime Instantiate/Destroy pressure;
- fewer GC spikes during combat-heavy scenes;
- more stable runtime behavior on mobile devices.

### UI And Popup Flow

Gameplay UI and popup presentation are separated from core reward/combat rules.

Implemented work includes:

- reward popup view and choice binding;
- popup root and popup events;
- win and revival popup views;
- reward popup animation helpers;
- interaction gates for safe UI state transitions;
- reward text formatting and visual catalog data.

Engineering value:

- gameplay logic can request UI changes without directly owning UI object state;
- popup behavior is easier to iterate without changing combat code.

### Unity Editor Balancing Tool

The project includes a custom Editor tool for balance simulations.

Implemented work includes:

- Worm Balance Lab editor window;
- deterministic simulation seed;
- configurable simulation count, level number, worm sections, path timing, hit efficiency, reward strategy, rerolls, ad assists, and revive behavior;
- simulation using real reward database, reward effects, weapon configs, HP resolver, and DPS estimation;
- summary output for balance iteration.

Engineering value:

- balancing can be tested faster than by replaying manually every time;
- real project data is used in simulations;
- combat tuning becomes more measurable and less guess-based.

## Architecture

The project is organized around clear responsibility boundaries:

- Bootstrap: scene startup, DOTween initialization, performance bootstrap, scene loading.
- Systems: input, pooling, screen bounds, rewarded ads abstraction.
- Gameplay: player, combat, weapons, projectiles, rewards, worm enemy, revive flow.
- Presentation: popups, reward UI, worm visuals, damage feedback.
- Editor: rail path tools and balance simulation tools.

Architecture principles used:

- Single Responsibility Principle;
- composition through scene/bootstrap references;
- ScriptableObject-driven configuration;
- event-driven UI/gameplay communication where it reduces coupling;
- pooled objects for frequent runtime entities;
- cached references and setup-time dependency resolution.

## Tech Stack

- Unity 6
- C#
- UGUI
- ScriptableObjects
- Input System
- Physics2D
- DOTween
- URP
- Custom object pooling
- Unity Editor tooling
- Android/mobile-oriented runtime constraints

## What This Project Demonstrates

- Building a gameplay system beyond a simple tutorial prototype.
- Splitting gameplay, data, UI, presentation, systems, and editor tooling into maintainable areas.
- Designing reward and weapon systems that can be extended through new data/effect classes.
- Thinking about mobile performance before the project becomes object-heavy.
- Turning a prototype into a project that can be balanced, debugged, and prepared for release.

## Repository Notes

The repository is public as a portfolio/code review sample. Some visual assets, final balancing, videos, and release materials may change while the project is still in development.

## Author

Oleksandr Tokarev  
Unity C# Developer  
Portfolio: https://tokarevdev.github.io/
