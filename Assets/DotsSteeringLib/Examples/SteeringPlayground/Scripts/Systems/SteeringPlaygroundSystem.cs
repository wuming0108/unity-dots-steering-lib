﻿using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Rendering;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
using Himeki.DOTS.UnitySteeringLib;

[AlwaysSynchronizeSystem]
public class SteeringPlaygroundSystem : JobComponentSystem
{

    private EntityArchetype playerArchetype;
    private EntityArchetype agentArchetype;
    private EntityArchetype obstacleArchetype;

    private Entity playerEntity;

    protected override void OnCreate()
    {
        playerArchetype = EntityManager.CreateArchetype(
            typeof(PlayerControl),
            typeof(Velocity),
            typeof(Translation),
            typeof(RenderMesh),
            typeof(LocalToWorld),
            typeof(Scale),
            typeof(Rotation),
            typeof(RenderBounds),
            typeof(ChunkWorldRenderBounds)
            );

        agentArchetype = EntityManager.CreateArchetype(
            typeof(SteeringAgentParameters),
            typeof(TargetEntity),
            typeof(Velocity),
            typeof(Translation),
            typeof(RenderMesh),
            typeof(LocalToWorld),
            typeof(Scale),
            typeof(Rotation),
            typeof(RenderBounds),
            typeof(ChunkWorldRenderBounds)
        );

        obstacleArchetype = EntityManager.CreateArchetype(
            typeof(Obstacle),
            typeof(Translation),
            typeof(RenderMesh),
            typeof(LocalToWorld),
            typeof(Scale),
            typeof(Rotation),
            typeof(RenderBounds),
            typeof(ChunkWorldRenderBounds)
        );

        playerEntity = CreatePlayer();
        CreateAgents(5000);
        CreateObstacles(20);
    }

    public Entity CreatePlayer()
    {
        var playerMat = Resources.Load("PlayerMat", typeof(Material)) as Material;
        var entityMesh = Resources.Load("Cube", typeof(Mesh)) as Mesh;

        Entity playerEntity = EntityManager.CreateEntity(playerArchetype);

        EntityManager.SetComponentData(playerEntity, new Scale { Value = 1f });
        EntityManager.SetSharedComponentData(playerEntity, new RenderMesh
        {
            mesh = entityMesh,
            material = playerMat,
            subMesh = 0,
            layer = 0,
            castShadows = ShadowCastingMode.On,
            receiveShadows = true
        });

        return playerEntity;
    }

    public void CreateAgents(int amount)
    {
        if (amount > 0)
        {
            float randomSpreadRadius = 90f;

            var agentsMat = Resources.Load("AgentsMat", typeof(Material)) as Material;
            var entityMesh = Resources.Load("Cube", typeof(Mesh)) as Mesh;

            NativeArray<Entity> entities = new NativeArray<Entity>(amount, Allocator.Temp);
            EntityManager.CreateEntity(agentArchetype, entities);

            for (int i = 0; i < amount; i++)
            {
                var e = entities[i];
                EntityManager.SetComponentData(e, new Scale { Value = 1f });
                EntityManager.SetComponentData(e, new Translation
                {
                    Value = new float3(UnityEngine.Random.Range(-randomSpreadRadius, randomSpreadRadius),
                                                                            0f,
                                                                            UnityEngine.Random.Range(-randomSpreadRadius, randomSpreadRadius))
                });
                EntityManager.SetComponentData(e, new TargetEntity { entity = playerEntity });
                EntityManager.SetComponentData(e, new SteeringAgentParameters
                {
                    mass = 1f,
                    radius = 1f,
                    maxForce = 25f,
                    maxSpeed = 20f,
                    behaviour = SteeringBehaviourId.Evade,
                    avoidObstacles = true
                });
                EntityManager.SetSharedComponentData(e, new RenderMesh
                {
                    mesh = entityMesh,
                    material = agentsMat,
                    subMesh = 0,
                    layer = 0,
                    castShadows = ShadowCastingMode.On,
                    receiveShadows = true
                });
            }

            entities.Dispose();
        }
    }

    public void CreateObstacles(int amount)
    {
        if (amount > 0)
        {
            float randomSpreadRadius = 60f;

            var obstaclesMat = Resources.Load("ObstaclesMat", typeof(Material)) as Material;
            var entityMesh = Resources.Load("Cube", typeof(Mesh)) as Mesh;

            NativeArray<Entity> entities = new NativeArray<Entity>(amount, Allocator.Temp);
            EntityManager.CreateEntity(obstacleArchetype, entities);

            for (int i = 0; i < amount; i++)
            {
                var e = entities[i];
                EntityManager.SetComponentData(e, new Scale { Value = 2f });
                EntityManager.SetComponentData(e, new Obstacle { radius = 20f });
                EntityManager.SetComponentData(e, new Translation
                {
                    Value = new float3(UnityEngine.Random.Range(-randomSpreadRadius, randomSpreadRadius),
                                                                            0f,
                                                                            UnityEngine.Random.Range(-randomSpreadRadius, randomSpreadRadius))
                });
                EntityManager.SetSharedComponentData(e, new RenderMesh
                {
                    mesh = entityMesh,
                    material = obstaclesMat,
                    subMesh = 0,
                    layer = 0,
                    castShadows = ShadowCastingMode.On,
                    receiveShadows = true
                });
            }

            entities.Dispose();
        }
    }



    protected override void OnDestroy()
    {
    }

    protected override unsafe JobHandle OnUpdate(JobHandle handle)
    {
        float deltaTime = Time.DeltaTime;
        float inputH = Input.GetAxis("Horizontal");
        float inputV = Input.GetAxis("Vertical");
        float3 inputVector = new float3(inputH, 0f, inputV);

        if (math.length(inputVector) > 0.01f)
        {
            Entities.
                WithAll<PlayerControl>().
                ForEach((Entity e, ref Translation translation, ref Rotation rotation, ref LocalToWorld localToWorld) =>
            {
                float3 movementDirection = math.normalize(inputVector);

                float3 newPos = translation.Value + movementDirection * 35f * deltaTime;
                translation = new Translation { Value = newPos };

                quaternion newRotation = quaternion.LookRotation(movementDirection, localToWorld.Up);

                rotation = new Rotation { Value = newRotation };
            }).Run(); //Not worth running on worker thread
        }

        return default;
        
    }

        

}