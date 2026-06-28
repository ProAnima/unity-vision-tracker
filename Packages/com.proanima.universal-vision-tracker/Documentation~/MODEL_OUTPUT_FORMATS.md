# Форматы выхода YOLO моделей

## 📚 Официальная документация
- **Results API**: https://docs.ultralytics.com/reference/results/
- **Keypoints**: https://docs.ultralytics.com/reference/engine/results/
- **Tasks**: https://docs.ultralytics.com/tasks/

---

## 🎯 YOLO26 (End-to-End, NMS-Free)

### Detection (`yolo26n.pt`)
**Output**: `[batch, num_detections, 6]`

| Index | Значение | Тип | Диапазон |
|-------|----------|-----|----------|
| 0-3 | `[x1, y1, x2, y2]` | float32 | Пиксели (0-640) |
| 4 | `confidence` | float32 | 0.0-1.0 |
| 5 | `class_id` | int | 0-79 (COCO) |

**Особенности:**
- ✅ NMS уже применён (end-to-end)
- ✅ До 300 детекций (большинство с conf~0)
- ✅ Координаты в пикселях, нужна нормализация

---

### Pose (`yolo26n-pose.pt`)
**Output**: `[batch, num_detections, 56]`

| Index | Значение | Тип | Описание |
|-------|----------|-----|----------|
| 0-3 | `[x1, y1, x2, y2]` | float32 | BBox в пикселях |
| 4 | `person_confidence` | float32 | Уверенность детекции |
| 5-55 | `17 keypoints × 3` | float32 | **[conf, x, y]** для каждой точки |

**ВАЖНО:** Порядок keypoint данных: **`[confidence, x, y]`**, а не `[x, y, confidence]`!

**17 COCO Keypoints:**
```
0: Nose          5: L Shoulder    11: L Hip        
1: L Eye         6: R Shoulder    12: R Hip        
2: R Eye         7: L Elbow       13: L Knee       
3: L Ear         8: R Elbow       14: R Knee       
4: R Ear         9: L Wrist       15: L Ankle      
                10: R Wrist       16: R Ankle      
```

**Connections (скелет):**
```csharp
// Голова: 0-1, 0-2, 1-3, 2-4
// Руки: 5-6, 5-7, 7-9, 6-8, 8-10
// Торс: 5-11, 6-12, 11-12
// Ноги: 11-13, 13-15, 12-14, 14-16
```

---

### Segmentation (`yolo26n-seg.pt`)
**Output**: `[batch, num_detections, 6 + num_mask_coeffs]`

| Index | Значение | Описание |
|-------|----------|----------|
| 0-3 | `[x1, y1, x2, y2]` | BBox |
| 4 | `confidence` | Уверенность |
| 5 | `class_id` | Класс объекта |
| 6+ | `mask_coefficients` | 32 коэффициента для proto-маски |

**Дополнительно**: Proto masks tensor `[batch, 32, mask_h, mask_w]` (обычно 160×160)

**Восстановление маски:**
```
mask = sigmoid(coefficients @ proto_masks) > 0.5
mask = resize(mask, original_size)
mask = crop(mask, bbox)
```

---

## 🔧 YOLO11/12 (Anchor-Based)

### Detection (`yolo11n.pt`)
**Output**: `[batch, 84, 8400]`

| Channel | Значение | Описание |
|---------|----------|----------|
| 0-3 | `[cx, cy, w, h]` | Центр + размер (normalized) |
| 4-83 | `80 class scores` | Вероятности для каждого класса |

**Особенности:**
- ❌ Нужен NMS постобработка
- 8400 anchors (grid 80×80 + 40×40 + 20×20)
- **Channel-first layout**: `data[c * 8400 + anchor_idx]`

---

### Pose (`yolo11n-pose.pt`)
**Output**: `[batch, 56, 8400]`

| Channel | Значение |
|---------|----------|
| 0-3 | `[cx, cy, w, h]` |
| 4 | `person_confidence` |
| 5-55 | `17 keypoints × 3` = `[x, y, confidence]` |

**ВАЖНО:** YOLO11 использует `[x, y, confidence]`, в отличие от YOLO26!

---

## 🎨 Визуализация

### BBox рисование
```csharp
// Normalized (0-1) → Screen pixels
float x = box.rect.x * Screen.width;
float y = box.rect.y * Screen.height;
float w = box.rect.width * Screen.width;
float h = box.rect.height * Screen.height;
```

### Keypoints рисование
```csharp
// Точки
for (int i = 0; i < 17; i++) {
    if (kp.confidences[i] > 0.5f) {
        float px = kp.points[i].x * Screen.width;
        float py = kp.points[i].y * Screen.height;
        DrawCircle(px, py, radius);
    }
}

// Связи
foreach (var (idx1, idx2) in connections) {
    if (kp.confidences[idx1] > 0.5f && kp.confidences[idx2] > 0.5f) {
        DrawLine(kp.points[idx1], kp.points[idx2]);
    }
}
```

### Masks рисование
```csharp
// 1. Создать Texture2D с альфа-каналом
// 2. Для каждого пикселя маски:
if (mask[y, x] > 0.5f)
    texture.SetPixel(x, y, new Color(r, g, b, 0.5f)); // Полупрозрачный
// 3. Apply() и отрисовать поверх изображения
```

---

## 🐛 Частые ошибки

1. **Неправильный порядок keypoint данных**
   - YOLO26: `[conf, x, y]` ✅
   - YOLO11: `[x, y, conf]` ✅

2. **Неправильный layout тензора**
   - YOLO26: `[detection][feature]` — последовательно
   - YOLO11: `[feature][anchor]` — channel-first

3. **Забыли нормализовать координаты**
   - YOLO26 выдаёт пиксели → делим на `inputSize`
   - YOLO11 выдаёт normalized → не нужно

4. **Не применили NMS для YOLO11**
   - YOLO11 требует NMS + фильтрацию
   - YOLO26 уже с NMS

---

## 📝 Changelog

- **2026-01**: Добавлена документация по YOLO26 форматам
- **2026-01**: Исправлен порядок keypoint данных для YOLO26 Pose
