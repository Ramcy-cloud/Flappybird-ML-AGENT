using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeMonkey;
using CodeMonkey.Utils;

public class Level : MonoBehaviour
{

    private const float CAMERA_ORTHO_SIZE = 50f;
    private const float PIPE_WIDTH = 7.8f;
    private const float PIPE_HEAD_HEIGHT = 3.75f;
    private const float PIPE_MOVE_SPEED = 30f;
    private const float PIPE_DESTROY_X_POSITION = -100f;
    private const float PIPE_SPAWN_X_POSITION = +100f;
    private const float GROUND_DESTROY_X_POSITION = -200f;
    private const float CLOUD_DESTROY_X_POSITION = -160f;
    private const float CLOUD_SPAWN_X_POSITION = +160f;
    private const float CLOUD_SPAWN_Y_POSITION = +30f;
    private const float BIRD_X_POSITION = 0f;

    private Bird bird;

    private List<Transform> groundList;
    private List<Transform> cloudList;
    private float cloudSpawnTimer;
    private List<Pipe> pipeList;
    private int pipesPassedCount;
    private int pipesSpawned;
    private float pipeSpawnTimer;
    private float pipeSpawnTimerMax;
    private float gapSize;
    private State state;

    public enum Difficulty
    {
        Easy, Medium, Hard, Impossible,
    }

    private enum State
    {
        WaitingToStart,
        Playing,
        BirdDead,
    }

    private void Awake()
    {
        SpawnInitialGround();
        SpawnInitialClouds();
        pipeList = new List<Pipe>();
        pipeSpawnTimerMax = 1f;
        SetDifficulty(Difficulty.Easy);
        state = State.WaitingToStart;
    }

    private void Start()
    {
        bird = GetComponentInChildren<Bird>();
        if (bird == null) bird = FindFirstObjectByType<Bird>();
        if (bird != null)
        {
            bird.OnDied += Bird_OnDied;
            bird.OnStartedPlaying += Bird_OnStartedPlaying;
        }
    }

    private void Bird_OnStartedPlaying(object sender, System.EventArgs e)
    {
        state = State.Playing;
    }

    private void Bird_OnDied(object sender, System.EventArgs e)
    {
        state = State.BirdDead;
    }

    private void Update()
    {
        if (state == State.Playing)
        {
            HandlePipeMovement();
            HandlePipeSpawning();
            HandleGround();
            HandleClouds();
        }
        // Si WaitingToStart ou BirdDead → rien ne bouge !
    }

    // ─── RESET POUR ML-AGENT ───────────────────────────────
    public void ResetLevel()
    {
        foreach (Pipe pipe in pipeList)
        {
            pipe.DestroySelf();
        }
        pipeList.Clear();

        pipesPassedCount = 0;
        pipesSpawned = 0;
        pipeSpawnTimer = 0f;

        SetDifficulty(Difficulty.Easy);

        // ← NE PAS mettre state = Playing ici !
        // C'est OnStartedPlaying qui le fera
        // comme ça en mode manuel le décor attend le clic
        state = State.WaitingToStart;
    }
    // ───────────────────────────────────────────────────────

    private void SpawnInitialClouds()
    {
        cloudList = new List<Transform>();
        Transform cloudTransform = Instantiate(GetCloudPrefabTransform(), new Vector3(0, CLOUD_SPAWN_Y_POSITION, 0), Quaternion.identity);
        cloudList.Add(cloudTransform);
    }

    private Transform GetCloudPrefabTransform()
    {
        switch (Random.Range(0, 3))
        {
            default:
            case 0: return GameAssets.GetInstance().pfCloud_1;
            case 1: return GameAssets.GetInstance().pfCloud_2;
            case 2: return GameAssets.GetInstance().pfCloud_3;
        }
    }

    private void HandleClouds()
    {
        cloudSpawnTimer -= Time.deltaTime;
        if (cloudSpawnTimer < 0)
        {
            float cloudSpawnTimerMax = 6f;
            cloudSpawnTimer = cloudSpawnTimerMax;
            Transform cloudTransform = Instantiate(GetCloudPrefabTransform(), new Vector3(CLOUD_SPAWN_X_POSITION, CLOUD_SPAWN_Y_POSITION, 0), Quaternion.identity);
            cloudList.Add(cloudTransform);
        }

        for (int i = 0; i < cloudList.Count; i++)
        {
            Transform cloudTransform = cloudList[i];
            cloudTransform.position += new Vector3(-1, 0, 0) * PIPE_MOVE_SPEED * Time.deltaTime * .7f;
            if (cloudTransform.position.x < CLOUD_DESTROY_X_POSITION)
            {
                Destroy(cloudTransform.gameObject);
                cloudList.RemoveAt(i);
                i--;
            }
        }
    }

    private void SpawnInitialGround()
    {
        groundList = new List<Transform>();
        float groundY = -47.5f;
        float groundWidth = 192f;
        groundList.Add(Instantiate(GameAssets.GetInstance().pfGround, new Vector3(0, groundY, 0), Quaternion.identity));
        groundList.Add(Instantiate(GameAssets.GetInstance().pfGround, new Vector3(groundWidth, groundY, 0), Quaternion.identity));
        groundList.Add(Instantiate(GameAssets.GetInstance().pfGround, new Vector3(groundWidth * 2f, groundY, 0), Quaternion.identity));
    }

    private void HandleGround()
    {
        foreach (Transform groundTransform in groundList)
        {
            groundTransform.position += new Vector3(-1, 0, 0) * PIPE_MOVE_SPEED * Time.deltaTime;
            if (groundTransform.position.x < GROUND_DESTROY_X_POSITION)
            {
                float rightMostXPosition = -100f;
                for (int i = 0; i < groundList.Count; i++)
                {
                    if (groundList[i].position.x > rightMostXPosition)
                        rightMostXPosition = groundList[i].position.x;
                }
                float groundWidth = 192f;
                groundTransform.position = new Vector3(rightMostXPosition + groundWidth, groundTransform.position.y, groundTransform.position.z);
            }
        }
    }

    private void HandlePipeSpawning()
    {
        pipeSpawnTimer -= Time.deltaTime;
        if (pipeSpawnTimer < 0)
        {
            pipeSpawnTimer += pipeSpawnTimerMax;
            float heightEdgeLimit = 10f;
            float minHeight = gapSize * .5f + heightEdgeLimit;
            float totalHeight = CAMERA_ORTHO_SIZE * 2f;
            float maxHeight = totalHeight - gapSize * .5f - heightEdgeLimit;
            float height = Random.Range(minHeight, maxHeight);
            CreateGapPipes(height, gapSize, transform.position.x + PIPE_SPAWN_X_POSITION);
        }
    }

    private void HandlePipeMovement()
    {
        for (int i = 0; i < pipeList.Count; i++)
        {
            Pipe pipe = pipeList[i];
            bool isToTheRightOfBird = pipe.GetXPosition() > transform.position.x + BIRD_X_POSITION;
            pipe.Move();
            if (isToTheRightOfBird && pipe.GetXPosition() <= BIRD_X_POSITION && pipe.IsBottom())
            {
                pipesPassedCount++;
                SoundManager.PlaySound(SoundManager.Sound.Score);
            }
            if (pipe.GetXPosition() < transform.position.x + PIPE_DESTROY_X_POSITION)
            {
                pipe.DestroySelf();
                pipeList.Remove(pipe);
                i--;
            }
        }
    }

    private void SetDifficulty(Difficulty difficulty)
    {
        switch (difficulty)
        {
            case Difficulty.Easy: gapSize = 50f; pipeSpawnTimerMax = 1.4f; break;
            case Difficulty.Medium: gapSize = 40f; pipeSpawnTimerMax = 1.3f; break;
            case Difficulty.Hard: gapSize = 33f; pipeSpawnTimerMax = 1.1f; break;
            case Difficulty.Impossible: gapSize = 24f; pipeSpawnTimerMax = 1.0f; break;
        }
    }

    private Difficulty GetDifficulty()
    {
        if (pipesSpawned >= 24) return Difficulty.Impossible;
        if (pipesSpawned >= 12) return Difficulty.Hard;
        if (pipesSpawned >= 5) return Difficulty.Medium;
        return Difficulty.Easy;
    }

    private void CreateGapPipes(float gapY, float gapSize, float xPosition)
    {
        // gapY is measured from the bottom of the camera (so center in world = gapY - CAMERA_ORTHO_SIZE)
        float gapCenterWorld = gapY - CAMERA_ORTHO_SIZE;
        CreatePipe(gapY - gapSize * .5f, xPosition, true, gapCenterWorld);
        CreatePipe(CAMERA_ORTHO_SIZE * 2f - gapY - gapSize * .5f, xPosition, false, gapCenterWorld);
        pipesSpawned++;
        SetDifficulty(GetDifficulty());
    }

    // Returns the X position and gap center Y of the next pipe ahead of birdX
    public Vector2? GetNextPipeInfo(float birdX)
    {
        Pipe closestPipe = null;
        for (int i = 0; i < pipeList.Count; i++)
        {
            Pipe p = pipeList[i];
            if (!p.IsBottom()) continue;
            if (p.GetXPosition() > birdX)
            {
                if (closestPipe == null || p.GetXPosition() < closestPipe.GetXPosition())
                    closestPipe = p;
            }
        }
        if (closestPipe == null) return null;
        return new Vector2(closestPipe.GetXPosition(), closestPipe.GetGapCenterY());
    }

    private void CreatePipe(float height, float xPosition, bool createBottom, float gapCenterY = 0f)
    {
        Transform pipeHead = Instantiate(GameAssets.GetInstance().pfPipeHead);
        float pipeHeadYPosition;
        if (createBottom)
        {
            pipeHeadYPosition = -CAMERA_ORTHO_SIZE + height - PIPE_HEAD_HEIGHT * .5f;
        }
        else
        {
            pipeHeadYPosition = +CAMERA_ORTHO_SIZE - height + PIPE_HEAD_HEIGHT * .5f;
        }
        pipeHead.position = new Vector3(xPosition, pipeHeadYPosition);

        Transform pipeBody = Instantiate(GameAssets.GetInstance().pfPipeBody);
        float pipeBodyYPosition;
        if (createBottom)
        {
            pipeBodyYPosition = -CAMERA_ORTHO_SIZE;
        }
        else
        {
            pipeBodyYPosition = +CAMERA_ORTHO_SIZE;
            pipeBody.localScale = new Vector3(1, -1, 1);
        }
        pipeBody.position = new Vector3(xPosition, pipeBodyYPosition);

        SpriteRenderer pipeBodySpriteRenderer = pipeBody.GetComponent<SpriteRenderer>();
        pipeBodySpriteRenderer.size = new Vector2(PIPE_WIDTH, height);

        BoxCollider2D pipeBodyBoxCollider = pipeBody.GetComponent<BoxCollider2D>();
        pipeBodyBoxCollider.size = new Vector2(PIPE_WIDTH, height);
        pipeBodyBoxCollider.offset = new Vector2(0f, height * .5f);

        Pipe pipe = new Pipe(pipeHead, pipeBody, createBottom, gapCenterY);
        pipeList.Add(pipe);
    }

    public int GetPipesSpawned() { return pipesSpawned; }
    public int GetPipesPassedCount() { return pipesPassedCount; }

    private class Pipe
    {
        private Transform pipeHeadTransform;
        private Transform pipeBodyTransform;
        private bool isBottom;
        private float gapCenterY;

        public Pipe(Transform pipeHeadTransform, Transform pipeBodyTransform, bool isBottom, float gapCenterY = 0f)
        {
            this.pipeHeadTransform = pipeHeadTransform;
            this.pipeBodyTransform = pipeBodyTransform;
            this.isBottom = isBottom;
            this.gapCenterY = gapCenterY;
        }

        public void Move()
        {
            pipeHeadTransform.position += new Vector3(-1, 0, 0) * PIPE_MOVE_SPEED * Time.deltaTime;
            pipeBodyTransform.position += new Vector3(-1, 0, 0) * PIPE_MOVE_SPEED * Time.deltaTime;
        }

        public float GetXPosition() { return pipeHeadTransform.position.x; }
        public bool IsBottom() { return isBottom; }
        public float GetGapCenterY() { return gapCenterY; }

        public void DestroySelf()
        {
            Destroy(pipeHeadTransform.gameObject);
            Destroy(pipeBodyTransform.gameObject);
        }
    }
}