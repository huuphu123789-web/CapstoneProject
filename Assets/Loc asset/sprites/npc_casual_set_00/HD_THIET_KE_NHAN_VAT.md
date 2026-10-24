# HƯỚNG DẪN SỬ DỤNG: NPC CHARACTER DESIGNER (TRÌNH THIẾT KẾ NHÂN VẬT MODULAR)

Hệ thống thiết kế nhân vật tự động và trực quan dành riêng cho gói tài nguyên **npc_casual_set_00** trong dự án Capstone.

---

## 🚀 Cách Mở Công Cụ Trong Unity Editor

Bạn có thể mở cửa sổ thiết kế nhân vật theo các cách sau:
1. **Trên thanh Menu trên cùng**: Chọn `Tools > NPC Character Designer 👤`
2. **Hoặc**: Chọn `Window > NPC Character Designer`
3. **Trong Project Window**: Chuột phải vào thư mục `npc_casual_set_00` hoặc bất kỳ asset nào thuộc `Loc asset` -> Chọn `Open in NPC Character Designer`.

---

## 🎨 Các Tính Năng Chính

### 1. 3D Preview Trực Quan (Tương tác bằng chuột)
- **Xoay 360°**: Giữ chuột trái (LMB) và kéo để xoay quanh nhân vật.
- **Zoom Phóng to / Thu nhỏ**: Cuộn chuột giữa (Scroll Wheel) để soi chi tiết khuôn mặt hoặc ngắm toàn thân.
- **Di chuyển góc nhìn (Pan)**: Giữ chuột phải (RMB) hoặc chuột giữa và kéo lên/xuống.
- **Nút Focus Nhanh**:
  - `Focus Mặt`: Tự động căn góc cận cảnh khuôn mặt.
  - `Focus Toàn Thân`: Căn góc nhìn toàn bộ trang phục.

### 2. Các Tab Tùy Chỉnh

#### 👤 Tab 1: Danh Tính & Giới Tính
- **Tên Nhân Vật / CCCD**: Nhập tên (ví dụ: *Trịnh Hoàng Long*, *Nguyễn Văn A*).
- **Giới Tính**: Chuyển đổi linh hoạt giữa **Nam (Male)** và **Nữ (Female)**.
- **Kiểu Áo Cơ Bản**:
  - `Áo Phông (T-Shirt)`
  - `Sơ Mi Mở Cúc (Open Shirt)`
- **Tông Màu Da & Khuôn Mặt**: 5 biến thể màu da (Sáng, Tự nhiên, Ngăm nắng, Trưởng thành, Da ngăm đậm).

#### 💇 Tab 2: Tóc & Râu
- **Kiểu Tóc**:
  - Không tóc (Trọc đầu)
  - 8 Kiểu tóc nam/nữ đa dạng (A-01, A-02, A-03, C-01, C-02, C-03, D-01, E-01).
- **Bảng 12 Màu Tóc**: Chọn trực quan bằng 12 ô màu (Đen tuyền, Nâu đậm, Hạt dẻ, Vàng kim, Bạch kim, Đỏ hung, Bạc, Xám, Xanh dương, Hồng đậm, Xanh ngọc,...).
- **Kiểu Râu (Dành cho Nam)**:
  - Cạo sạch
  - Râu quai nón (Full Beard A-01)
  - Râu cằm (Goatee B-01)
  - Râu mép cổ điển (Mustache C-01)
  - Râu lún phún (Stubble D-01)

#### 👕 Tab 3: Trang Phục
- **Họa Tiết & Màu Áo**: Tùy biến mẫu vải áo phông / áo sơ mi (Xanh navy, Đỏ đô, Trắng, Caro đỏ, Xám, Denim,...).
- **Màu Quần**: Quần Jean xanh, Jean đen than, Kaki be, Nâu.
- **Kiểu Giày**: Sneaker thể thao đỏ/trắng, Sneaker đen/trắng, Giày xanh, Giày da nâu.

#### 🏋️ Tab 4: Vóc Dáng (Body Blendshapes)
- **Cơ Bắp (Athletic)**: 0% -> 100% (cơ bắp nở nang).
- **Đầy Đặn (Overweight)**: 0% -> 100% (thân hình mập mạp).
- **Gầy Gò (Underweight)**: 0% -> 100% (thân hình thanh mảnh).
- *Quần áo và phụ kiện tự động co giãn theo vóc dáng, không bị xuyên thấu mesh (no clipping).*

#### 🎮 Tab 5: Thiết Lập Game
- Tự động tích hợp sẵn các thành phần cho Game Kiểm Duyệt (Inspection):
  - `Animator` với bộ điều khiển `Npc-Normal.controller` (có sẵn animation đi bộ, đứng yên).
  - `NavMeshAgent` (tốc độ 2.5m/s chuẩn).
  - `CapsuleCollider` & `Rigidbody` (Kinematic).
  - `AudioSource` 3D phát tiếng bước chân.
  - Script `NPCNormalBehavior` (tự động đi tới vạch kiểm tra `InspectionZone`).

---

## ⚡ Các Thao Tác Nhanh (Action Buttons)

1. **🎲 Tạo Ngẫu Nhiên (Randomize)**:
   - 1 Click tạo ngay một công dân hoàn chỉnh với phối màu trang phục và kiểu tóc hài hòa. Rất tiện để sinh ra hàng loạt NPC cho game.
2. **📸 Chụp Ảnh Thẻ CCCD**:
   - Tự động dựng camera chụp góc chân dung chuẩn CCCD (nền xanh, góc thẳng mặt) và xuất file PNG trực tiếp vào:
     `Assets/JumpScare/Loc asset/sprites/CCCD/CCQN/<TênNhânVật>.png`
   - File ảnh sẵn sàng để gắn vào thẻ CCCD 3D trong game!
3. **💾 Lưu Thành Prefab (Save Prefab)**:
   - Xuất nhân vật đã thiết kế thành một file Prefab hoàn chỉnh đặt tại `Assets/JumpScare/Loc asset/Prefabs/CCCD/` (hoặc thư mục tự chọn).
4. **🚀 Đặt Vào Scene (Spawn in Scene)**:
   - Thả trực tiếp nhân vật vào Scene hiện tại ngay trước tầm nhìn camera để test gameplay ngay lập tức.
