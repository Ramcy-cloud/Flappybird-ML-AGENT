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

    private static Bird instance;
    public static Bird GetInstance() { return instance; }

    public event EventHandler OnDied;
    public event EventHandler OnStartedPlaying;

    private Rigidbody2D birdRigidbody2D;
    private BehaviorParameters behaviorParameters;
    private bool isDead = false;
    private bool hasStarted = false;

    private bool IsTraining =>
        behaviorParameters.BehaviorType == BehaviorType.Default;

    private void Awake()
    {
        instance = this;
        birdRigidbody2D = GetComponent<Rigidbody2D>();
        behaviorParameters = GetComponent<BehaviorParameters>();
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

        if (Level.GetInstance() != null)
            Level.GetInstance().ResetLevel();

        if (IsTraining || behaviorParameters.BehaviorType == BehaviorType.InferenceOnly)
        {
            // Training or Inference: start immediately
            birdRigidbody2D.bodyType = RigidbodyType2D.Dynamic;
            hasStarted = true;
            if (OnStartedPlaying != null)
                OnStartedPlaying(this, EventArgs.Empty);
        }
        else
        {
            // Manual/Heuristic mode: wait for first click/space
            birdRigidbody2D.bodyType = RigidbodyType2D.Static;
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.position.y / Y_MAX);
        sensor.AddObservation(birdRigidbody2D.linearVelocity.y / JUMP_AMOUNT);

        GameObject nextPipe = FindNextPipe();
        if (nextPipe != null)
        {
            sensor.AddObservation((nextPipe.transform.position.x - transform.position.x) / 100f);
            sensor.AddObservation(nextPipe.transform.position.y / Y_MAX);
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
                if (OnStartedPlaying != null)
                    OnStartedPlaying(this, EventArgs.Empty);
            }
            Jump();
        }

        if (hasStarted)
        {
            transform.eulerAngles = new Vector3(0, 0, birdRigidbody2D.linearVelocity.y * .15f);

            // Survival reward
            AddReward(0.01f);

            // Gap alignment reward — guide bird toward pipe gap center
            GameObject nextPipe = FindNextPipe();
            if (nextPipe != null)
            {
                float distToGap = Mathf.Abs(transform.position.y - nextPipe.transform.position.y);
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

        if (OnDied != null)
            OnDied(this, EventArgs.Empty);

        AddReward(-2f);

        if (IsTraining)
        {
            // Mode entraînement : restart immédiat
            EndEpisode();
        }
        // Mode manuel : Game Over s'affiche, pas de restart automatique
        // Le bouton Retry dans GameOverWindow recharge la scène
    }

    private GameObject FindNextPipe()
    {
        GameObject[] pipes = GameObject.FindGameObjectsWithTag("Pipe");
        GameObject closest = null;
        float minDist = float.MaxValue;

        foreach (var pipe in pipes)
        {
            float dist = pipe.transform.position.x - transform.position.x;
            if (dist > -1f && dist < minDist)
            {
                minDist = dist;
                closest = pipe;
            }
        }
        return closest;
    }
}