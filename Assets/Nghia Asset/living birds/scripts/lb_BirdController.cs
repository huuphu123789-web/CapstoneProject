using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class lb_BirdController : MonoBehaviour
{
    [Header("Bird Amount")]
    public int idealNumberOfBirds = 20;
    public int maximumNumberOfBirds = 30;

    [Header("Camera")]
    public Camera currentCamera;

    [Header("Spawn Distance")]
    public float unspawnDistance = 10f;

    [Header("Quality")]
    public bool highQuality = true;

    [Header("Collision")]
    public bool collideWithObjects = true;
    public LayerMask groundLayer;

    [Header("Bird Scale")]
    public float birdScale = 1f;

    [Header("Bird Species")]

    public bool robin = true;
    public bool blueJay = true;
    public bool cardinal = true;
    public bool chickadee = true;
    public bool sparrow = true;
    public bool goldFinch = true;
    public bool crow = true;

    private readonly List<GameObject> myBirds = new List<GameObject>();
    private readonly List<string> myBirdTypes = new List<string>();

    private readonly List<GameObject> groundTargets = new List<GameObject>();
    private readonly List<GameObject> perchTargets = new List<GameObject>();

    private int activeBirdCount = 0;
    private int activeBirdIndex = 0;

    private GameObject currentGroundTarget;
    private GameObject currentPerchTarget;

    private bool initialized = false;

    [Header("Feather Emitters")]

    public ParticleSystem featherEmitter1;
    public ParticleSystem featherEmitter2;
    public ParticleSystem featherEmitter3;

    private void Awake()
    {
        if (currentCamera == null)
        {
            currentCamera = Camera.main;
        }
    }


    private void Start()
    {
        InitializeController();
    }


    private void OnEnable()
    {
        CancelInvoke();

        InvokeRepeating(nameof(UpdateBirds), 1f, 1f);

        StartCoroutine(UpdateTargetsRoutine());
    }


    private void OnDisable()
    {
        CancelInvoke();
        StopAllCoroutines();
    }

    private void InitializeController()
    {
        if (initialized)
            return;

        if (currentCamera == null)
        {
            currentCamera = Camera.main;
        }

        FindTargets();

        initialized = true;
    }

    private void FindTargets()
    {
        groundTargets.Clear();
        perchTargets.Clear();

        GameObject[] grounds = GameObject.FindGameObjectsWithTag("lb_groundTarget");

        foreach (GameObject target in grounds)
        {
            if (target != null)
            {
                groundTargets.Add(target);
            }
        }


        GameObject[] perches = GameObject.FindGameObjectsWithTag("lb_perchTarget");

        foreach (GameObject target in perches)
        {
            if (target != null)
            {
                perchTargets.Add(target);
            }
        }


        if (groundTargets.Count > 0)
        {
            currentGroundTarget = groundTargets[0];
        }

        if (perchTargets.Count > 0)
        {
            currentPerchTarget = perchTargets[0];
        }
    }

    public void ChangeCamera(Camera newCamera)
    {
        if (newCamera == null)
        {
            Debug.LogWarning("lb_BirdController: New camera is null.");
            return;
        }

        currentCamera = newCamera;
    }

    public void Pause()
    {
        AllPause();
    }


    public void AllPause()
    {
        for (int i = 0; i < myBirds.Count; i++)
        {
            GameObject bird = myBirds[i];

            if (bird == null)
                continue;

            bird.SendMessage(
                "PauseBird",
                SendMessageOptions.DontRequireReceiver
            );
        }
    }

    public void AllUnPause()
    {
        for (int i = 0; i < myBirds.Count; i++)
        {
            GameObject bird = myBirds[i];

            if (bird == null)
                continue;

            bird.SendMessage(
                "UnPauseBird",
                SendMessageOptions.DontRequireReceiver
            );
        }
    }

    public void AllFlee()
    {
        for (int i = 0; i < myBirds.Count; i++)
        {
            GameObject bird = myBirds[i];

            if (bird == null)
                continue;

            bird.SendMessage(
                "Flee",
                SendMessageOptions.DontRequireReceiver
            );
        }
    }

    public void SpawnAmount(int amount)
    {
        if (amount <= 0)
            return;

        int amountToSpawn = Mathf.Min(
            amount,
            maximumNumberOfBirds - activeBirdCount
        );

        if (amountToSpawn <= 0)
            return;

        for (int i = 0; i < amountToSpawn; i++)
        {
            SpawnBird();
        }
    }

    private void UpdateBirds()
    {
        if (!initialized)
            InitializeController();

        if (currentCamera == null)
            currentCamera = Camera.main;

        if (currentCamera == null)
            return;

        if (activeBirdCount < idealNumberOfBirds)
        {
            int amount = idealNumberOfBirds - activeBirdCount;

            SpawnAmount(amount);
        }

        if (activeBirdCount > maximumNumberOfBirds)
        {
            activeBirdCount = maximumNumberOfBirds;
        }

        for (int i = 0; i < myBirds.Count; i++)
        {
            GameObject bird = myBirds[i];

            if (bird == null)
                continue;

            BirdOffCamera(bird);
        }
    }

    private IEnumerator UpdateTargetsRoutine()
    {
        while (true)
        {
            UpdateTargets();

            yield return new WaitForSeconds(1f);
        }
    }


    private void UpdateTargets()
    {
        if (groundTargets.Count == 0 && perchTargets.Count == 0)
        {
            FindTargets();
            return;
        }


        if (groundTargets.Count > 0)
        {
            currentGroundTarget =
                groundTargets[
                    Random.Range(0, groundTargets.Count)
                ];
        }


        if (perchTargets.Count > 0)
        {
            currentPerchTarget =
                perchTargets[
                    Random.Range(0, perchTargets.Count)
                ];
        }
    }

    private void SpawnBird()
    {
        if (activeBirdCount >= maximumNumberOfBirds)
            return;

        if (currentCamera == null)
        {
            currentCamera = Camera.main;
        }

        if (currentCamera == null)
        {
            Debug.LogWarning(
                "lb_BirdController: Không tìm thấy Camera."
            );

            return;
        }


        GameObject prefab = GetRandomBirdPrefab();

        if (prefab == null)
        {
            Debug.LogWarning(
                "lb_BirdController: Không tìm thấy Bird Prefab trong Resources."
            );

            return;
        }


        Vector3 spawnPosition = FindPositionOffCamera();


        GameObject newBird = Instantiate(
            prefab,
            spawnPosition,
            Quaternion.identity
        );

        newBird.transform.localScale =
            newBird.transform.localScale * birdScale;

        myBirds.Add(newBird);

        myBirdTypes.Add(prefab.name);

        activeBirdCount++;

        newBird.SendMessage(
            "SetController",
            this,
            SendMessageOptions.DontRequireReceiver
        );

        newBird.SendMessage(
            "BirdFindTarget",
            SendMessageOptions.DontRequireReceiver
        );
    }

    private GameObject GetRandomBirdPrefab()
    {
        List<string> availableBirds =
            new List<string>();


        if (robin)
            availableBirds.Add(
                highQuality ? "lb_robinHQ" : "lb_robin"
            );


        if (blueJay)
            availableBirds.Add(
                highQuality ? "lb_blueJayHQ" : "lb_blueJay"
            );


        if (cardinal)
            availableBirds.Add(
                highQuality ? "lb_cardinalHQ" : "lb_cardinal"
            );


        if (chickadee)
            availableBirds.Add(
                highQuality ? "lb_chickadeeHQ" : "lb_chickadee"
            );


        if (sparrow)
            availableBirds.Add(
                highQuality ? "lb_sparrowHQ" : "lb_sparrow"
            );


        if (goldFinch)
            availableBirds.Add(
                highQuality ? "lb_goldFinchHQ" : "lb_goldFinch"
            );


        if (crow)
            availableBirds.Add(
                highQuality ? "lb_crowHQ" : "lb_crow"
            );


        if (availableBirds.Count == 0)
            return null;


        string prefabName =
            availableBirds[
                Random.Range(0, availableBirds.Count)
            ];


        GameObject prefab =
            Resources.Load<GameObject>(prefabName);


        return prefab;
    }

    private Vector3 FindPositionOffCamera()
    {
        Vector3 position = Vector3.zero;


        for (int attempt = 0; attempt < 50; attempt++)
        {
            Vector2 randomScreenPoint =
                new Vector2(
                    Random.Range(-0.2f, 1.2f),
                    Random.Range(-0.2f, 1.2f)
                );


            float distance =
                Random.Range(
                    unspawnDistance,
                    unspawnDistance * 2f
                );


            Ray ray =
                currentCamera.ViewportPointToRay(
                    new Vector3(
                        randomScreenPoint.x,
                        randomScreenPoint.y,
                        0f
                    )
                );


            position =
                ray.origin +
                ray.direction * distance;

            if (Physics.Raycast(
                    position + Vector3.up * 20f,
                    Vector3.down,
                    out RaycastHit hit,
                    50f,
                    groundLayer))
            {
                position = hit.point;

                position += Vector3.up * 0.1f;

                return position;
            }
        }

        position =
            currentCamera.transform.position +
            currentCamera.transform.forward *
            (unspawnDistance * 2f);


        return position;
    }

    private void BirdOffCamera(GameObject bird)
    {
        if (bird == null || currentCamera == null)
            return;


        Vector3 viewportPosition =
            currentCamera.WorldToViewportPoint(
                bird.transform.position
            );


        bool behindCamera =
            viewportPosition.z < 0f;


        bool outsideCamera =
            viewportPosition.x < -0.5f ||
            viewportPosition.x > 1.5f ||
            viewportPosition.y < -0.5f ||
            viewportPosition.y > 1.5f;


        if (behindCamera || outsideCamera)
        {
            float distance =
                Vector3.Distance(
                    bird.transform.position,
                    currentCamera.transform.position
                );


            if (distance >
                unspawnDistance * 4f)
            {
                Unspawn(bird);
            }
        }
    }

    private void Unspawn(GameObject bird)
    {
        if (bird == null)
            return;


        int index =
            myBirds.IndexOf(bird);


        if (index >= 0)
        {
            myBirds.RemoveAt(index);

            if (index < myBirdTypes.Count)
            {
                myBirdTypes.RemoveAt(index);
            }
        }


        activeBirdCount =
            Mathf.Max(0, activeBirdCount - 1);


        Destroy(bird);
    }

    public bool AreThereActiveTargets()
    {
        return
            groundTargets.Count > 0 ||
            perchTargets.Count > 0;
    }

    public Vector3 FindPointInGroundTarget()
    {
        if (groundTargets.Count == 0)
        {
            FindTargets();
        }


        if (groundTargets.Count == 0)
        {
            return transform.position;
        }


        GameObject target =
            groundTargets[
                Random.Range(
                    0,
                    groundTargets.Count
                )
            ];


        if (target == null)
        {
            return transform.position;
        }


        Collider targetCollider =
            target.GetComponent<Collider>();


        if (targetCollider != null)
        {
            Bounds bounds =
                targetCollider.bounds;


            Vector3 point =
                new Vector3(
                    Random.Range(
                        bounds.min.x,
                        bounds.max.x
                    ),
                    bounds.center.y,
                    Random.Range(
                        bounds.min.z,
                        bounds.max.z
                    )
                );


            return point;
        }


        return target.transform.position;
    }

    public void BirdFindTarget(GameObject bird)
    {
        if (bird == null)
            return;


        Vector3 targetPosition;

        if (groundTargets.Count > 0)
        {
            targetPosition =
                FindPointInGroundTarget();
        }
        else if (perchTargets.Count > 0)
        {
            GameObject target =
                perchTargets[
                    Random.Range(
                        0,
                        perchTargets.Count
                    )
                ];


            targetPosition =
                target.transform.position;
        }
        else
        {
            return;
        }


        bird.SendMessage(
            "FlyToTarget",
            targetPosition,
            SendMessageOptions.DontRequireReceiver
        );
    }

    public void BirdFindTarget()
    {
        if (myBirds.Count == 0)
            return;


        if (activeBirdIndex >= myBirds.Count)
        {
            activeBirdIndex = 0;
        }


        GameObject bird =
            myBirds[activeBirdIndex];


        activeBirdIndex++;


        if (bird != null)
        {
            BirdFindTarget(bird);
        }
    }

    public void FeatherEmit(Vector3 position)
    {
        PlayFeatherEmitter(
            featherEmitter1,
            position
        );

        PlayFeatherEmitter(
            featherEmitter2,
            position
        );

        PlayFeatherEmitter(
            featherEmitter3,
            position
        );
    }

    private void PlayFeatherEmitter(
        ParticleSystem emitter,
        Vector3 position)
    {
        if (emitter == null)
            return;


        emitter.transform.position =
            position;


        emitter.gameObject.SetActive(true);

        emitter.Stop(true);

        emitter.Play(true);
    }

    public void DeactivateFeathers()
    {
        DeactivateEmitter(featherEmitter1);
        DeactivateEmitter(featherEmitter2);
        DeactivateEmitter(featherEmitter3);
    }


    private void DeactivateEmitter(
        ParticleSystem emitter)
    {
        if (emitter == null)
            return;


        emitter.Stop(true);

        emitter.gameObject.SetActive(false);
    }

    public void SetBirdScale(float scale)
    {
        birdScale = scale;


        for (int i = 0; i < myBirds.Count; i++)
        {
            if (myBirds[i] == null)
                continue;


            myBirds[i].transform.localScale =
                Vector3.one * birdScale;
        }
    }

    public int GetActiveBirdCount()
    {
        return activeBirdCount;
    }

    public List<GameObject> GetBirds()
    {
        return myBirds;
    }

    private void CleanupBirdList()
    {
        for (int i = myBirds.Count - 1; i >= 0; i--)
        {
            if (myBirds[i] == null)
            {
                myBirds.RemoveAt(i);

                if (i < myBirdTypes.Count)
                {
                    myBirdTypes.RemoveAt(i);
                }
            }
        }


        activeBirdCount =
            myBirds.Count;
    }

    private void OnDrawGizmosSelected()
    {
        if (currentCamera == null)
            return;


        Gizmos.DrawWireSphere(
            currentCamera.transform.position,
            unspawnDistance
        );
    }
}