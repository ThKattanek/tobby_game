﻿using game.Entities;
using game.Entities.Abilitites;
using game.Entities.Enemies;
using game.Helpers;
using game.Managers;
using game.Scenes;
using SFML.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace game.Abilities
{
    public class OrbitalAbility : Ability
    {
        private Player player;
        private float circleSpeed;
        private float circleRadius;
        private int entityCount; // Number of entities to spawn

        private bool IsCurrentlyActive = false;
        private List<OrbitalEntity> orbitals;

        public OrbitalAbility(Player player, float cooldown, float circleSpeed, float circleRadius, int entityCount)
            : base("OrbitalAbility", 0, cooldown) // Assuming the ability itself does no direct damage
        {
            this.player = player;
            this.circleSpeed = circleSpeed;
            this.circleRadius = circleRadius;
            this.entityCount = entityCount;

            orbitals = new List<OrbitalEntity>();

        }

        public override void Activate()
        {
            if(!IsCurrentlyActive)
            {
                UniversalLog.LogInfo("activating OrbitaAbility");
                UniversalLog.LogInfo("OrbitalAbilityEntityCount: " + orbitals.Count.ToString());
                entityCount = 10;

                float angleIncrement = 360f / entityCount; // Divide the circle into equal parts based on entity count
                for (int i = 0; i < entityCount; i++)
                {
                    float angle = angleIncrement * i * (MathF.PI / 180);
                    Vector2f spawnPosition = new Vector2f(
                        player.Position.X + MathF.Cos(angle) * circleRadius,
                        player.Position.Y + MathF.Sin(angle) * circleRadius
                    );

                    var orbitalEntity = EntityManager.Instance.CreateAbilityEntity(spawnPosition, typeof(OrbitalEntity)) as OrbitalEntity;
                    orbitalEntity.SetPosition(spawnPosition);
                    orbitalEntity.SetStats(circleSpeed, circleRadius);
                    orbitalEntity.IsActive = true;

                    //OrbitalEntity orbitalEntity = new OrbitalEntity(player, spawnPosition, circleSpeed, circleRadius);
                    //orbitalEntity.SetScale(Random.Shared.NextFloat(1f, 3f));
                    //EntityManager.Instance.AddEntity(orbitalEntity);

                    orbitals.Add(orbitalEntity);
                }


                IsCurrentlyActive = true;
                abilityClock.Restart();
                UniversalLog.LogInfo("OrbitalAbilityActivated-OrbitalAbilityEntityCount: " + orbitals.Count.ToString());
            }

            
        }

        public override void Update()
        {
            if (IsCurrentlyActive)
            {
                //UniversalLog.LogInfo("OrbitalAbilityEntityCount: " + orbitals.Count.ToString());
                // Get a list of inactive orbital entities
                var inactiveOrbitals = EntityManager.Instance.abilityEntities
                    .OfType<OrbitalEntity>()
                    .Where(x => !x.IsActive) // Use !x.IsActive to find inactive orbitals
                    .ToList();

                //UniversalLog.LogInfo("inactiveOrbitals found: " + inactiveOrbitals.Count.ToString());

                // Remove any orbital entities that are in the inactiveOrbitals list
                orbitals.RemoveAll(orbital => inactiveOrbitals.Contains(orbital));

                //UniversalLog.LogInfo("OrbitalAbilityEntityCount-afterRemovingInactive: " + orbitals.Count.ToString());

                // If there are no more orbitals, set the ability to inactive
                if (orbitals.Count == 0)
                {
                    //UniversalLog.LogInfo("OrbitaAbility done.");
                    IsCurrentlyActive = false;
                }
            }
        }
    }
}



