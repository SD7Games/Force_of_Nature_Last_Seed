# Last Seed Survivor

Unity 6 mobile 2D auto-shooter project focused on modular combat, ScriptableObject-driven rewards, segmented enemy behavior, object pooling, balance simulations, and Android release preparation.

Portfolio hub: https://tokarevdev.github.io

Gameplay video: https://youtube.com/shorts/HiQBlYjienI?feature=share

Architecture notes: ./ARCHITECTURE.md

Status: closed test passed. Android release preparation continues with modular combat, pooled runtime systems, and balance tooling ready for review.

## Quick Review

This repository is public as a portfolio/code review sample. Unity vendor packages and imported assets are present, but the portfolio-relevant code lives under `Assets/_Project/App/`.

Start here:

- Main project code: `Assets/_Project/App/`
- Bootstrap and scene startup: `Assets/_Project/App/Bootstrap/`
- Combat, weapons, projectiles, and rewards: `Assets/_Project/App/Gameplay/Combat/`
- Segmented enemy systems and balance tooling: `Assets/_Project/App/Gameplay/Enemy/Worm/`
- Presentation/UI gameplay flow: `Assets/_Project/App/Presentation/`

## Overview

Last Seed Survivor is my current main Unity systems project. The goal is to build mobile survival gameplay where combat, rewards, enemy behavior, UI flow, and balance data can grow without becoming tightly coupled scene logic.

The project is structured around practical production concerns:

- gameplay systems should be easy to extend;
- reward and weapon values should be tuned through data;
- frequently spawned objects should be pooled;
- UI/presentation should not own core gameplay rules;
- mobile runtime behavior should stay stable under object-heavy combat.

## Impact

- Challenge: mobile survival gameplay needed scalable combat, rewards, enemy pressure, and tuning instead of one-off prototype logic.
- Action: separated data, runtime systems, UI binding, bootstrap, pooling, segmented enemy logic, and balance simulation tools.
- Result: closed test passed; Android release preparation continues with modular combat, pooled runtime systems, and balance tooling ready for review.

## My Role

Solo Unity C# development across gameplay programming, Unity scene/prefab setup, UI flow, reward balancing, performance work, debugging, Android build preparation, and code review readiness.

## Key Systems

### Modular Weapon System

- Projectile weapon system, Acacia Thorn secondary weapon, runtime weapon progression state, shot pattern builder, and weapon rebuild flow when rewards modify runtime stats.
- Runtime modifiers cover damage, fire rate, critical chance, critical power, penetration, salvo count, projectile speed, and parallel projectiles.
- Weapon behavior can be extended without rewriting the whole shooter, while balancing data stays separated from runtime state.

### Reward Choice System

- Three-choice reward roll flow, rarity-based pools, category uniqueness rules, guaranteed rarity support, weapon unlock gating, rerolls, ad-reroll, take-all, and revive-related reward flow support.
- DPS-aware reward bias helps weaker weapon groups catch up during progression.
- Reward roll/apply services are separated from UI binding, so future reward effects can be added through dedicated effect classes.

### Segmented Worm Enemy

- Worm spawner, factory, controller, section builder, pattern builder, rail-path movement, section HP generation, damage receivers, combat progress tracking, rollback behavior, cocoon rules, and visual states.
- Worm segments are pooled to reduce runtime object churn.
- Enemy logic is split between movement, combat, balance, and presentation instead of using one large enemy script.

### Object Pooling And Runtime Performance

- Projectile pool with prewarm, pool registry, worm segment pool, active projectile release flow, screen bounds service, and mobile target frame-rate bootstrap.
- Frequent gameplay objects are reused instead of repeatedly instantiated/destroyed during combat-heavy scenes.
- Cached references and setup-time dependency resolution reduce hidden runtime searches.

### UI And Popup Flow

- Reward popup view and choice binding, popup root/events, win and revival popups, reward animation helpers, interaction gates, reward text formatting, and visual catalog data.
- Gameplay logic can request UI changes without directly owning UI object state.
- Popup behavior can be iterated without changing combat and reward rules.

### Unity Editor Balance Lab

- Custom Worm Balance Lab editor window with deterministic simulation seed, configurable simulation count, level number, worm sections, path timing, hit efficiency, reward strategy, rerolls, ad assists, and revive behavior.
- Simulations use real reward database, reward effects, weapon configs, HP resolver, and DPS estimation.
- Balance iteration becomes measurable instead of relying only on manual replay.

## Architecture

The project is organized around clear responsibility boundaries:

- Bootstrap: scene startup, DOTween initialization, performance bootstrap, scene loading.
- Systems: input, pooling, screen bounds, rewarded ads abstraction.
- Gameplay: player, combat, weapons, projectiles, rewards, worm enemy, revive flow.
- Presentation: popups, reward UI, worm visuals, damage feedback.
- Editor: rail path tools and balance simulation tools.

Architecture principles used:

- Single Responsibility Principle.
- Composition through scene/bootstrap references.
- ScriptableObject-driven configuration.
- Event-driven UI/gameplay communication where it reduces coupling.
- Pooled objects for frequent runtime entities.
- Cached references and setup-time dependency resolution.

## Code Review Map

- Main project code: `Assets/_Project/App/`
- Bootstrap and scene startup: `Assets/_Project/App/Bootstrap/`
- Gameplay combat, weapons, projectiles, and rewards: `Assets/_Project/App/Gameplay/Combat/`
- Segmented enemy systems and balance tooling: `Assets/_Project/App/Gameplay/Enemy/Worm/`
- Presentation/UI-related gameplay flow: `Assets/_Project/App/Presentation/`

Unity vendor packages and imported assets are present in the repository, but the portfolio-relevant code lives under `Assets/_Project/App/`.

## Tech Stack

Unity 6, C#, UGUI, ScriptableObjects, Input System, Physics2D, DOTween, URP, custom object pooling, Unity Editor tooling, Android/mobile-oriented runtime constraints.

## What This Project Demonstrates

- Building a gameplay system beyond a simple tutorial prototype.
- Splitting gameplay, data, UI, presentation, systems, and editor tooling into maintainable areas.
- Designing reward and weapon systems that can be extended through new data/effect classes.
- Thinking about mobile performance before the project becomes object-heavy.
- Turning a prototype into a project that can be balanced, debugged, and prepared for release.

## Repository Notes

The repository is public as a portfolio/code review sample. Some visual assets, final balancing, gameplay media, and release materials may change while the project is still in development.

## Author

Oleksandr Tokarev  
Unity C# Developer / C# Gameplay Programmer  
Portfolio: https://tokarevdev.github.io/
