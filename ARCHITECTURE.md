# Architecture Overview

This document describes the core architecture used in the project.

The main goal of the architecture is to keep gameplay systems modular,
extensible and suitable for mobile performance constraints.

The project follows several principles:

• Single Responsibility Principle  
• Modular gameplay systems  
• Separation between gameplay logic and presentation  
• Object pooling to avoid runtime allocations  

---

# Gameplay Layer

Gameplay systems represent the core game logic.

They do not depend on UI or presentation.

Main systems:

Player
Enemy
Combat
Weapons
Projectiles

---

# Player System

The player can only move horizontally and automatically fires projectiles.

Components:

PlayerController  
Handles high-level player behaviour.

PlayerMover  
Processes input and moves the player horizontally.

PlayerShooter  
Handles automatic weapon firing.

---

# Worm Enemy System

The main enemy is a segmented worm.

Structure:

Head  
Body segments  
Cocoon segments  
Tail  

Segments are spawned using WormSpawner and managed by WormController.

Movement occurs along a predefined RailPath.

Each segment follows the head using a fixed distance offset.

---

# Worm Rollback Mechanic

When a segment is destroyed:

1. The segment becomes inactive
2. WormController searches for the nearest alive segment
3. The worm head rolls back along the rail
4. Movement resumes from the new front segment

This creates the illusion that the worm is shrinking dynamically.

---

# Projectile System

The projectile system is modular and weapon-agnostic.

Projectiles are spawned using object pools to avoid runtime allocations.

Components:

Projectile  
Handles movement and collision.

ProjectileBounce  
Optional behaviour modifier.

ProjectileMovement  
Controls projectile trajectory.

---

# Weapon System

Weapons are defined using ScriptableObject configurations.

Weapon behaviour is composed using modifiers.

Modifiers include:

SpreadModifier  
ParallelModifier  

The modifier pipeline allows extending weapon behaviour without modifying
existing systems.

---

# Pooling System

The project uses custom object pools.

Main components:

PoolRegistry  
Stores references to all pools.

ProjectilePool  
Handles projectile reuse.

WormSegmentPool  
Handles worm segment reuse.

Pooling prevents runtime allocations and improves performance on mobile devices.

---

# Input System

The project uses Unity New Input System.

InputReader provides a wrapper layer allowing gameplay systems to stay
decoupled from Unity input implementation.
