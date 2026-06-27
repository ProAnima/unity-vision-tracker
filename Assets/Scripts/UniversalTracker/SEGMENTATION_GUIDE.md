# 🎨 Как включить Segmentation (сегментацию)

## ✅ Модель уже загружена!
- **Файл**: `Assets/Models/yolo26n-seg.onnx`
- **Размер**: 10.7 MB
- **Задача**: Instance Segmentation (80 COCO классов)

---

## 📝 Пошаговая инструкция

### **1. Создать ModelConfig (ScriptableObject)**

В Unity:
1. **ПКМ в Project** → `Create` → `Universal Tracker` → `Model Config`
2. Назови: `YOLO26n-Seg Config`

### **2. Настроить конфиг**

В Inspector выбери созданный конфиг и заполни:

```
Model Name:          yolo26n-seg
Model Asset:         yolo26n-seg (перетащи .onnx файл)
Backend:             GPUPixel (или GPUCompute на DX11)
Input Size:          640
Confidence Threshold: 0.5
NMS Threshold:       0.45
Flip X:              false
Flip Y:              false
```

### **3. Привязать к UniversalTrackerManager**

На GameObject с `UniversalTrackerManager`:
1. Найди **Model Configs** (массив)
2. Увеличь **Size** на 1
3. Перетащи `YOLO26n-Seg Config` в новый слот

### **4. Включить визуализацию масок**

На компоненте `UIVisualizationReceiver` (или где подключён `SimpleGUIVisualizer`):

```
✅ Draw Bounding Boxes:  true
✅ Draw Masks:          true  ← ВКЛЮЧИ!
   Mask Alpha:          0.4
   Draw Keypoints:      false (для seg не нужно)
   Draw Connections:    false
```

### **5. Запустить!**

Нажми **Play** в Unity!

---

## 🎨 Что увидишь:

- **Полупрозрачные цветные заливки** внутри bbox (как на скрине)
- **Разные цвета** для разных классов (person=красный, car=зелёный и т.д.)
- **Label** с названием класса + confidence
- **Контур** вокруг маски

---

## 🔧 Переключение между моделями

### Detection → Segmentation:
1. В `UniversalTrackerManager` найди **Active Model Index**
2. Измени на индекс seg модели (например, `2` если pose=0, detection=1, seg=2)

### Или динамически в коде:
```csharp
trackerManager.SwitchModel(2); // Индекс seg модели
```

---

## 📊 Поддерживаемые классы (COCO)

80 классов:
- **Люди**: person
- **Транспорт**: car, bus, truck, motorcycle, bicycle
- **Животные**: dog, cat, bird, horse, sheep, cow
- **Еды**: pizza, donut, cake, banana, apple
- **Мебель**: chair, couch, bed, dining table
- **Электроника**: tv, laptop, mouse, keyboard, cell phone
- И много других...

---

## 🐛 Troubleshooting

### "Модель не работает"
- Проверь что **Model Asset** заполнен
- Убедись что **Backend = GPUPixel** (или DX11 для GPUCompute)
- Проверь логи: должно быть `✅ [YOLO26Seg] Найдено детекций: N`

### "Маски не видны"
- Включи **Draw Masks** в `UIVisualizationReceiver`
- Проверь **Mask Alpha** (0.4 = полупрозрачный)
- В логах должно быть: `🎨 [SimpleGUI] Рисуем N масок`

### "Слишком медленно"
- Используй **yolo26n-seg** (nano, самая быстрая)
- Включи **DX11** вместо DX12 (Project Settings → Player → Other → Graphics API)
- Backend = **GPUCompute** на DX11 (быстрее чем GPUPixel)

---

## 🚀 Дополнительно: Загрузить другие модели

```bash
# Более точная, но медленнее
python download_models_advanced.py --models yolo26s-seg

# Ещё точнее
python download_models_advanced.py --models yolo26m-seg

# Все seg модели сразу
python download_models_advanced.py --models yolo26n-seg yolo26s-seg yolo26m-seg
```

---

**Готово!** 🎉 Теперь у тебя работает **detection + pose + segmentation**!
