using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using CodeMonkey;

public class Bird : Agent
{
    private const float JUMP_AMOUNT = 90f;
    private const float Y_MIN = -47f;
    private const float Y_MAX = 50f;

    public event EventHandler OnDied;
    public event EventHandler OnStartedPlaying;

    private Rigidbody2D birdRigidbody2D;
    private BehaviorParameters behaviorParameters;
    private Level level;
    private bool isDead = false;
    private bool hasStarted = false;

    private bool IsTraining =>
        behaviorParameters.BehaviorType == BehaviorType.Default;

    private void Awake()
    {
        birdRigidbody2D = GetComponent<Rigidbody2D>();
        behaviorParameters = GetComponent<BehaviorParameters>();
        // Multi-agent: each bird finds its own Level in the parent hierarchy
        level = GetComponentInParent<Level>();
        if (level == null) level = FindFirstObjectByType<Level>();
    }

    public override void Initialize() { }

    public override void OnEpisodeBegin()
    {
        isDead = false;
        hasStarted = false;

        transform.position = new Vector3(0f, 0f, 0f);
        transform.rotation = Quaternion.identity;
        birdRigidbody2D.linearVelocity = Vector2.zero;
        birdRigidbody2D.angularVelocity = 0f;

        if (level != null)
            level.ResetLevel();

        if (IsTraining || behaviorParameters.BehaviorType == BehaviorType.InferenceOnly)
        {
            birdRigidbody2D.bodyType = RigidbodyType2D.Dynamic;
            hasStarted = true;
            OnStartedPlaying?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            birdRigidbody2D.bodyType = RigidbodyType2D.Static;
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.position.y / Y_MAX);
        sensor.AddObservation(birdRigidbody2D.linearVelocity.y / JUMP_AMOUNT);

        // Use Level's pipe list — safe for multi-agent (no cross-environment contamination)
        Vector2? nextPipe = level?.GetNextPipeInfo(transform.position.x);
        if (nextPipe.HasValue)
        {
            sensor.AddObservation((nextPipe.Value.x - transform.position.x) / 100f);
            sensor.AddObservation(nextPipe.Value.y / Y_MAX);
        }
        else
        {
            sensor.AddObservation(1f);
            sensor.AddObservation(0f);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (isDead) return;

        if (actions.DiscreteActions[0] == 1)
        {
            if (!hasStarted && !IsTraining)
            {
                hasStarted = true;
                birdRigidbody2D.bodyType = RigidbodyType2D.Dynamic;
                OnStartedPlaying?.Invoke(this, EventArgs.Empty);
            }
            Jump();
        }

        if (hasStarted)
        {
            transform.eulerAngles = new Vector3(0, 0, birdRigidbody2D.linearVelocity.y * .15f);

            // Survival reward
            AddReward(0.01f);

            // Gap alignment reward
            Vector2? nextPipe = level?.GetNextPipeInfo(transform.position.x);
            if (nextPipe.HasValue)
            {
                float distToGap = Mathf.Abs(transform.position.y - nextPipe.Value.y);
                float normalizedDist = distToGap / (Y_MAX - Y_MIN);
                AddReward(0.005f * (1f - normalizedDist));
            }

            if (transform.position.y < Y_MIN || transform.position.y > Y_MAX)
                Die();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discrete = actionsOut.DiscreteActions;
        discrete[0] = (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)) ? 1 : 0;
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.CompareTag("PipePass"))
        {
            AddReward(1f);
            return;
        }
        Die();
    }

    private void Jump()
    {
        birdRigidbody2D.linearVelocity = Vector2.up * JUMP_AMOUNT;
        SoundManager.PlaySound(SoundManager.Sound.BirdJump);
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        birdRigidbody2D.bodyType = RigidbodyType2D.Static;
        SoundManager.PlaySound(SoundManager.Sound.Lose);

        OnDied?.Invoke(this, EventArgs.Empty);
        AddReward(-2f);

        if (IsTraining)
            EndEpisode();
    }
}
