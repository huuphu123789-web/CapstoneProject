using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý vật lý dây tai nghe / điện thoại bằng ConfigurableJoint,
/// giúp dây rủ cong tự nhiên theo trọng lực và tai nghe rơi lơ lửng,
/// đung đưa mềm mại thay vì bị cứng đơ như khúc gỗ (FixedJoint) hay quay tít mù.
/// </summary>
public class PhoneCordPhysics : MonoBehaviour
{
    [Header("=== CÀI ĐẶT DÂY (WIRE) ===")]
    [Tooltip("Khối lượng mỗi đốt dây (nhẹ để dây uốn mềm)")]
    public float wireSegmentMass = 0.03f;

    [Tooltip("Lực cản không khí khi di chuyển")]
    public float wireLinearDamping = 1.0f;

    [Tooltip("Lực cản góc xoay (giúp dây không bị xoắn loạn)")]
    public float wireAngularDamping = 2.5f;

    [Tooltip("Độ đàn hồi của dây: 0 = rủ mềm hoàn toàn; 0.05 - 0.2 = dây cao su uốn lượn tự nhiên")]
    public float wireSpring = 0.05f;

    [Tooltip("Độ giảm chấn lò xo (dập tắt dao động lắc lư nhanh chóng)")]
    public float wireDamper = 0.6f;

    [Header("=== CÀI ĐẶT TAI NGHE (PHONE) ===")]
    [Tooltip("Khối lượng tai nghe (nặng để kéo chùng dây rủ xuống tự nhiên)")]
    public float phoneMass = 0.5f;

    [Tooltip("Lực cản di chuyển của tai nghe")]
    public float phoneLinearDamping = 1.0f;

    [Tooltip("Lực cản góc xoay của tai nghe (giúp tai nghe ổn định, không bị quay tít)")]
    public float phoneAngularDamping = 3.5f;

    void Awake()
    {
        SetupCordPhysics();
    }

    [ContextMenu("Setup Configurable Joints Now")]
    public void SetupCordPhysics()
    {
        // 1. Tìm các GameObject con: Radio, Phone và các đoạn Wire
        Transform radio = null;
        Transform phone = null;
        List<Transform> wireList = new List<Transform>();

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            string childName = child.name.ToLower();

            if (childName.Contains("radio") && !childName.Contains("leftover"))
            {
                radio = child;
            }
            else if (childName.Contains("phone"))
            {
                phone = child;
            }
            else if (childName.Contains("wire"))
            {
                wireList.Add(child);
            }
        }

        if (wireList.Count == 0 || phone == null)
        {
            Debug.LogWarning("[PhoneCordPhysics] Không tìm thấy đủ các đoạn Wire hoặc Phone để setup.");
            return;
        }

        // 2. Sắp xếp thứ tự các đoạn dây từ vị trí cắm (gần Radio) tới tai nghe (gần Phone)
        Vector3 radioPos = (radio != null) ? radio.position : wireList[0].position;
        wireList.Sort((a, b) =>
        {
            float distA = Vector3.Distance(a.position, radioPos);
            float distB = Vector3.Distance(b.position, radioPos);
            return distA.CompareTo(distB);
        });

        // 3. Đoạn dây số 0 (Mỏ neo cắm trên bàn):
        // Cố định hoàn toàn (Kinematic) để giữ cả chuỗi dây không bị rơi khỏi bàn
        Transform anchorSegment = wireList[0];
        Rigidbody anchorRb = anchorSegment.GetComponent<Rigidbody>();
        if (anchorRb == null) anchorRb = anchorSegment.gameObject.AddComponent<Rigidbody>();
        anchorRb.isKinematic = true;
        anchorRb.useGravity = false;

        // Xóa Joint cũ trên mỏ neo nếu có
        CleanOldJoints(anchorSegment.gameObject);

        // Đảm bảo collider là trigger để không cấn mặt bàn
        Collider anchorCol = anchorSegment.GetComponent<Collider>();
        if (anchorCol != null) anchorCol.isTrigger = true;

        // 4. Các đoạn dây tiếp theo: Thiết lập ConfigurableJoint nối tiếp nhau
        Rigidbody previousRb = anchorRb;
        for (int i = 1; i < wireList.Count; i++)
        {
            Transform segment = wireList[i];
            Rigidbody rb = segment.GetComponent<Rigidbody>();
            if (rb == null) rb = segment.gameObject.AddComponent<Rigidbody>();

            rb.isKinematic = false;
            rb.useGravity = true;
            rb.mass = wireSegmentMass;
            rb.linearDamping = wireLinearDamping;
            rb.angularDamping = wireAngularDamping;

            // Xóa FixedJoint cũ
            CleanOldJoints(segment.gameObject);

            // Thêm và cài đặt ConfigurableJoint
            ConfigurableJoint cj = segment.GetComponent<ConfigurableJoint>();
            if (cj == null) cj = segment.gameObject.AddComponent<ConfigurableJoint>();

            // Khớp nối nằm ở đầu đốt dây (local Y = 1 do capsule xoay -90 độ Z)
            ConfigureJoint(cj, previousRb, new Vector3(0f, 1f, 0f), wireSpring, wireDamper);

            // Chuyển collider thành trigger để các mắt xích uốn lượn tự do, không bị cấn nhau
            Collider col = segment.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            previousRb = rb;
        }

        // 5. Cài đặt cho Tai nghe (Phone) ở cuối dây
        Rigidbody phoneRb = phone.GetComponent<Rigidbody>();
        if (phoneRb == null) phoneRb = phone.gameObject.AddComponent<Rigidbody>();

        phoneRb.isKinematic = false;
        phoneRb.useGravity = true;
        phoneRb.mass = phoneMass;
        phoneRb.linearDamping = phoneLinearDamping;
        phoneRb.angularDamping = phoneAngularDamping;

        // Tắt ConstantForce nếu có (nguyên nhân gây quay tít mù)
        ConstantForce cf = phone.GetComponent<ConstantForce>();
        if (cf != null)
        {
            cf.enabled = false;
            cf.torque = Vector3.zero;
            cf.relativeForce = Vector3.zero;
        }

        // Xóa FixedJoint cũ trên Phone
        CleanOldJoints(phone.gameObject);

        // Nối Phone vào đoạn dây cuối cùng bằng ConfigurableJoint
        ConfigurableJoint phoneJoint = phone.GetComponent<ConfigurableJoint>();
        if (phoneJoint == null) phoneJoint = phone.gameObject.AddComponent<ConfigurableJoint>();

        // Phone xoay quanh tâm của nó
        ConfigureJoint(phoneJoint, previousRb, Vector3.zero, wireSpring, wireDamper);

        // Chỉnh SphereCollider vừa vặn thành Trigger để không cấn vào khung bàn
        SphereCollider sc = phone.GetComponent<SphereCollider>();
        if (sc != null)
        {
            sc.isTrigger = true;
            sc.radius = 0.35f;
            sc.center = Vector3.zero;
        }

        Debug.Log("[PhoneCordPhysics] Đã thiết lập thành công ConfigurableJoint: dây mềm rủ tự nhiên, tai nghe treo lơ lửng.");
    }

    private void ConfigureJoint(ConfigurableJoint cj, Rigidbody connectedBody, Vector3 anchor, float spring, float damper)
    {
        cj.connectedBody = connectedBody;
        cj.autoConfigureConnectedAnchor = false;
        cj.anchor = anchor;

        // Tính toán connectedAnchor chuẩn xác theo không gian thế giới
        Vector3 worldAnchor = cj.transform.TransformPoint(anchor);
        cj.connectedAnchor = connectedBody.transform.InverseTransformPoint(worldAnchor);

        // Khóa vị trí tuyệt đối (dây không bị kéo giãn hay đứt rời)
        cj.xMotion = ConfigurableJointMotion.Locked;
        cj.yMotion = ConfigurableJointMotion.Locked;
        cj.zMotion = ConfigurableJointMotion.Locked;

        // Mở tự do góc xoay 3D (cho phép dây gập và uốn lượn rủ xuống tự nhiên theo trọng lực)
        cj.angularXMotion = ConfigurableJointMotion.Free;
        cj.angularYMotion = ConfigurableJointMotion.Free;
        cj.angularZMotion = ConfigurableJointMotion.Free;

        // Tắt va chạm trực tiếp giữa 2 mắt xích liền kề
        cj.enableCollision = false;

        // Giảm chấn lò xo để dây uốn mềm mại tự nhiên như cao su, triệt tiêu dao động rung lắc
        cj.rotationDriveMode = RotationDriveMode.Slerp;
        JointDrive drive = new JointDrive
        {
            positionSpring = spring,
            positionDamper = damper,
            maximumForce = Mathf.Infinity
        };
        cj.slerpDrive = drive;
    }

    private void CleanOldJoints(GameObject go)
    {
        Joint[] joints = go.GetComponents<Joint>();
        foreach (var j in joints)
        {
            if (j is ConfigurableJoint) continue; // Giữ lại ConfigurableJoint
            j.connectedBody = null;
            j.breakForce = 0;
            if (Application.isPlaying)
                Destroy(j);
            else
                DestroyImmediate(j);
        }
    }
}
