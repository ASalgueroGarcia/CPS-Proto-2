# Enemy Components

Each enemy prefab owns its tuning. Select the prefab root to edit its components.

- **Enemy** manages patrol, detection and the attack state machine. Its inline **EnemySettings** are grouped into Core Stats, Movement, Detection, Patrol and Attack Timing. It applies health and navigation speed at spawn.
- **EnemyEditor** organizes these settings in the Inspector and shows calculated patrol speed. Each prefab and instance can be tuned independently.
- **CardChargeAttack**, **MeleeAttack**, **RangedAttack** and **DashAttack** implement attack-specific behavior and expose their own tuning.
- **EnemyVisualFeedback** coordinates attack colours and temporary hit highlighting.
- **EnemyUIAutoSetup** creates the shared health bar; **WorldSpaceHealthBar** follows the model and updates from Health events.

Use Enemy for common stats, the attack component for attack mechanics, and presentation components for visuals. See the enemy prefab folder README for Card Soldier field locations.

EnemyData remains only to keep existing data assets readable; the current prefabs no longer reference it. Legacy enemy controllers remain for compatibility. The editor migration tool converts legacy Heavy/Ranged prefabs with local defaults and preserves settings on already converted prefabs.
