
using UnityEditor.EditorTools;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private CharacterController characterController;
    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Header("Thong So Di Chuyen")]
    public float moveSpeed;
    public float sprintSpeed;
    [Header("Trong luc va Nhay")]
    public float gravity = -19.62f; //* Trong luc  (Thuong gap doi thuc te de game cam giac nhah hon)
    public float jumpHeight = 3f; //*Do cao cu nhay
    [Header("Cài đặt Check Chạm Đất")]
    [Tooltip("Kéo đối tượng GroundCheck vào đây")]
    public Transform groundCheck;
    [Tooltip("Bán kính hình cầu quét chạm đất")]
    public float groundDistance = 0.4f;
    [Tooltip("Chọn Layer Ground ở đây")]
    public LayerMask groundMask;


    [Header("Cấu hình âm thanh bước chân")]
  
   
    [Tooltip("Danh sách tiếng bước chân (càng nhiều càng real)")]
    [SerializeField] private AudioClip[] footStepSounds;
    [SerializeField] private AudioSource audioSource;

    [Tooltip("Tốc độ bước đi (0.5 phát 1 lần)")]
    [SerializeField] private float stepRate = 0.5f;


    private Vector3 velocity;
    private bool isGrounded;
    private float footStepTimer;//* Bộ đếm thời gian bước chân


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //* Tu dong tim kiem thanh phan CharacterController
        characterController = GetComponent<CharacterController>();

        //*Nếu quên kéo AudioSource thì sẽ tự tìm và gắn vào 
       
        if(audioSource==null) audioSource = GetComponent<AudioSource>();
        if (animator == null) animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        // Dừng mọi xử lý khi game đang Pause
        if (PlayerHUDManager.instance != null && PlayerHUDManager.instance.isPaused)
            return;

        PlayerMove();
        PlayerJump();
    }

    
    public void PlayerMove()
    {
        //*Lay du lieu nhap vao tu ban phim
        float x = Input.GetAxis("Horizontal"); //* Truc ngang AD or Trai Phai
        float z = Input.GetAxis("Vertical"); //*Truc doc WS or Len Xuong
       
        //*Tinh toan huong di chuyen dua tren huog quay cua Player
        Vector3 move = transform.right * x + transform.forward * z;
        //*Ra lenh cho Character Controller  di chuyen nhan vat
        
        if(move.magnitude >0.1f)
        { 

            animator.SetBool("isWalk",true);
            characterController.Move(move * moveSpeed * Time.deltaTime);
        }
        else
        {
            animator.SetBool("isWalk",false);
            characterController.Move(move * moveSpeed * Time.deltaTime);
        }

        // Kiểm tra thể lực từ HUD (dùng instance, không FindObject mỗi frame)
        bool hasStamina = true;
        if (PlayerHUDManager.instance != null)
        {
            hasStamina = (PlayerHUDManager.instance.currentStamina > 0);
        }

        if(Input.GetKey(KeyCode.LeftShift) && hasStamina && move.magnitude > 0.1f)
        {
            characterController.Move(move * sprintSpeed * Time.deltaTime);
            animator.SetBool("isWalk",false);
            animator.SetBool("isRun",true);
        }
        else
        {
            characterController.Move(move * moveSpeed * Time.deltaTime);
            animator.SetBool("isRun",false);
        }
        //*Điều kiện player đang trên mặt đất và dang di chuyển (move.mangitude >0)
        if (isGrounded && move.magnitude > 0.1f)
        {
            footStepTimer -= Time.deltaTime; //*Đếm ngược thời gian
            if (footStepTimer <= 0)
            {
                PlayRandomFootStep();
                footStepTimer = stepRate; //*Reset lai bo dem
            }
        }
        else
        {
            footStepTimer = 0; //*Đứng yên thì reset  bộ đếm về 0
        }
    }

    public void PlayerJump()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (isGrounded && velocity.y < 0)
        {
            //*Reset van toc truc y khi cham dat
            velocity.y = -2f;
        }
        //*Xu li nhay
        // 3. Nhảy (Sử dụng biến isGrounded tự quét ở trên)
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        //*Ap dung trong luc roi tu do theo thoi gian
        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);

        // Tự động kiểm tra bề mặt dưới chân (hỗ trợ cả MeshCollider cầu thang)
        CheckFootstepSurface();
    }

    private void CheckFootstepSurface()
    {
        if (groundCheck != null)
        {
            if (Physics.Raycast(groundCheck.position + Vector3.up * 0.2f, Vector3.down, out RaycastHit hit, groundDistance + 0.5f))
            {
                StairFootstepZone stairZone = hit.collider.GetComponent<StairFootstepZone>();
                if (stairZone == null) stairZone = hit.collider.GetComponentInParent<StairFootstepZone>();

                if (stairZone != null && stairZone.stairFootstepSounds != null && stairZone.stairFootstepSounds.Length > 0)
                {
                    currentFootstepOverride = stairZone.stairFootstepSounds;
                    return;
                }
            }
        }
    }

    // Tự động nhận diện khi chân bước dẫm lên MeshCollider của cầu thang
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.normal.y > 0.3f) // Đang dẫm lên bề mặt phía dưới
        {
            StairFootstepZone stairZone = hit.collider.GetComponent<StairFootstepZone>();
            if (stairZone == null) stairZone = hit.collider.GetComponentInParent<StairFootstepZone>();

            if (stairZone != null && stairZone.stairFootstepSounds != null && stairZone.stairFootstepSounds.Length > 0)
            {
                currentFootstepOverride = stairZone.stairFootstepSounds;
            }
            else
            {
                currentFootstepOverride = null;
            }
        }
    }

    private AudioClip[] currentFootstepOverride;
    private float defaultStepRate;

    public void SetCustomFootsteps(AudioClip[] newSounds, float customStepRate = -1f)
    {
        currentFootstepOverride = newSounds;
        if (customStepRate > 0f)
        {
            if (defaultStepRate <= 0f) defaultStepRate = stepRate;
            stepRate = customStepRate;
        }
    }

    public void ResetFootsteps()
    {
        currentFootstepOverride = null;
        if (defaultStepRate > 0f)
        {
            stepRate = defaultStepRate;
        }
    }

    public void PlayRandomFootStep()
    {
        AudioClip[] activeClips = (currentFootstepOverride != null && currentFootstepOverride.Length > 0) ? currentFootstepOverride : footStepSounds;

        if (activeClips == null || activeClips.Length == 0) return;

        //*Lấy ngẫu nhiên 1 index trong mảng
        int randomIndex = Random.Range(0, activeClips.Length);
        AudioClip clip = activeClips[randomIndex];

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX(clip);
        }
        else if (audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
    }
}
