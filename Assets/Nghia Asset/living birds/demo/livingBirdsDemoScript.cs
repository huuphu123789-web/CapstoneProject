using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class livingBirdsDemoScript : MonoBehaviour
{
    [Header("Bird Controller")]
    public lb_BirdController birdControl;

    [Header("Cameras")]
    public Camera camera1;
    public Camera camera2;

    private Camera currentCamera;

    private bool cameraDirections = false;

    [Header("Camera Rotation")]
    public float cameraRotateSpeed = 50f;

    [Header("Shoot / Kill Bird")]
    public float killForce = 500f;
    public LayerMask birdLayer = ~0;

    private void Start()
    {
        currentCamera = Camera.main;

        if (currentCamera == null)
        {
            currentCamera = camera1;
        }

        if (birdControl == null)
        {
            GameObject controllerObject =
                GameObject.Find("_livingBirdsController");

            if (controllerObject != null)
            {
                birdControl =
                    controllerObject.GetComponent<lb_BirdController>();
            }
        }

        if (birdControl == null)
        {
            birdControl =
                FindFirstObjectByType<lb_BirdController>();
        }

        if (birdControl != null && currentCamera != null)
        {
            birdControl.ChangeCamera(currentCamera);
        }

        StartCoroutine(SpawnSomeBirds());
    }

    private void Update()
    {
        HandleCameraRotation();

        HandleShootBird();
    }

    private void HandleCameraRotation()
    {
        if (camera1 == null)
            return;


        if (Keyboard.current == null)
            return;


        float direction = 0f;

        if (Keyboard.current.dKey.isPressed ||
            Keyboard.current.rightArrowKey.isPressed)
        {
            direction = 1f;
        }

        if (Keyboard.current.aKey.isPressed ||
            Keyboard.current.leftArrowKey.isPressed)
        {
            direction = -1f;
        }


        if (direction != 0f)
        {
            cameraDirections = true;


            camera1.transform.Rotate(
                Vector3.up,
                direction * cameraRotateSpeed * Time.deltaTime,
                Space.World
            );
        }
        else
        {
            cameraDirections = false;
        }
    }

    private void HandleShootBird()
    {
        if (Mouse.current == null)
            return;


        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;


        Camera cam = currentCamera;


        if (cam == null)
        {
            cam = Camera.main;
        }


        if (cam == null)
            return;

        Vector2 mousePosition =
            Mouse.current.position.ReadValue();

        Ray ray =
            cam.ScreenPointToRay(mousePosition);


        RaycastHit[] hits =
            Physics.RaycastAll(
                ray,
                1000f,
                birdLayer,
                QueryTriggerInteraction.Ignore
            );


        if (hits == null || hits.Length == 0)
            return;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;


            if (!hit.collider.CompareTag("lb_bird"))
                continue;

            hit.collider.SendMessage(
                "KillBirdWithForce",
                ray.direction * killForce,
                SendMessageOptions.DontRequireReceiver
            );


            break;
        }
    }

    private IEnumerator SpawnSomeBirds()
    {
        // Chờ 2 giây
        yield return new WaitForSeconds(2f);


        if (birdControl == null)
        {
            Debug.LogWarning(
                "livingBirdsDemoScript: Không tìm thấy lb_BirdController."
            );

            yield break;
        }

        birdControl.SpawnAmount(
            birdControl.idealNumberOfBirds
        );
    }

    public void ChangeCamera()
    {
        if (camera1 == null || camera2 == null)
        {
            Debug.LogWarning(
                "livingBirdsDemoScript: Camera 1 hoặc Camera 2 chưa được gán."
            );

            return;
        }


        if (currentCamera == camera1)
        {
            currentCamera = camera2;
        }
        else
        {
            currentCamera = camera1;
        }

        camera1.gameObject.SetActive(
            currentCamera == camera1
        );

        camera2.gameObject.SetActive(
            currentCamera == camera2
        );

        if (currentCamera != null)
        {
            currentCamera.tag = "MainCamera";
        }

        if (birdControl != null)
        {
            birdControl.ChangeCamera(currentCamera);
        }
    }

    public void Pause()
    {
        if (birdControl == null)
            return;


        birdControl.Pause();
    }

    public void ScareAll()
    {
        if (birdControl == null)
            return;


        birdControl.AllFlee();
    }

    public void ReviveBirds()
    {
        if (birdControl == null)
            return;

        var birds =
            birdControl.GetBirds();


        if (birds == null)
            return;


        foreach (GameObject bird in birds)
        {
            if (bird == null)
                continue;


            bird.SendMessage(
                "Revive",
                SendMessageOptions.DontRequireReceiver
            );
        }
    }

    private void OnGUI()
    {
        GUI.Label(
            new Rect(20f, 20f, 300f, 30f),
            "Living Birds Demo"
        );

        if (GUI.Button(
                new Rect(20f, 60f, 150f, 35f),
                "Pause / Resume"))
        {
            Pause();
        }

        if (GUI.Button(
                new Rect(20f, 105f, 150f, 35f),
                "Scare All"))
        {
            ScareAll();
        }

        if (GUI.Button(
                new Rect(20f, 150f, 150f, 35f),
                "Change Camera"))
        {
            ChangeCamera();
        }

        if (GUI.Button(
                new Rect(20f, 195f, 150f, 35f),
                "Revive Birds"))
        {
            ReviveBirds();
        }

        int birdCount = 0;


        if (birdControl != null)
        {
            birdCount =
                birdControl.GetActiveBirdCount();
        }


        GUI.Label(
            new Rect(20f, 245f, 300f, 30f),
            "Birds: " + birdCount
        );

        GUI.Label(
            new Rect(20f, 280f, 500f, 30f),
            "A / D hoặc ← / → : Xoay camera"
        );


        GUI.Label(
            new Rect(20f, 310f, 500f, 30f),
            "Left Mouse : Bắn / Kill bird"
        );
    }
}