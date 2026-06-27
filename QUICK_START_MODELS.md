# 🚀 Быстрый старт: Загрузка моделей YOLO

## ⚡ Самый быстрый способ

### 1. Установите Python пакеты
```bash
pip install ultralytics
```

### 2. Запустите скрипт
```bash
# Рекомендованный набор (4 модели, ~40MB)
python download_models_advanced.py --preset recommended
```

### 3. Готово! 🎉
Модели загружены в `Assets/Models/`

---

## 📋 Все команды

### Просмотр доступных моделей
```bash
python download_models_advanced.py --list
```

### Предустановленные наборы

#### 🎯 Minimal (начните с этого!)
```bash
python download_models_advanced.py --preset minimal
```
**Включает:**
- yolo26n.onnx (~6MB) - детекция
- yolo26n-pose.onnx (~8MB) - поза
- **Итого: ~15MB, 2 модели**

#### ⭐ Recommended (оптимально)
```bash
python download_models_advanced.py --preset recommended
```
**Включает:**
- yolo26n.onnx - быстрая детекция
- yolo26s.onnx - точная детекция
- yolo26n-pose.onnx - поза
- yolo11n.onnx - резервная
- **Итого: ~40MB, 4 модели**

#### 📦 Full (всё что нужно)
```bash
python download_models_advanced.py --preset full
```
**Включает:**
- YOLO26: n, s, m (detection)
- YOLO26: n, s (pose)
- YOLO11: n, s, n-pose
- **Итого: ~100MB, 8 моделей**

#### 🚀 All YOLO26 (только новейшие)
```bash
python download_models_advanced.py --preset all-yolo26
```
**Включает:** Все модели YOLO26 (~200MB, 11 моделей)

#### ✅ All YOLO11 (стабильные)
```bash
python download_models_advanced.py --preset all-yolo11
```
**Включает:** Все модели YOLO11 (~60MB, 5 моделей)

---

## 🎯 Выборочная загрузка

### Только конкретные модели
```bash
# Одна модель
python download_models_advanced.py --models yolo26n

# Несколько моделей
python download_models_advanced.py --models yolo26n yolo26n-pose yolo11n
```

### С кастомными настройками

#### Для слабых устройств (меньший размер входа)
```bash
python download_models_advanced.py --preset minimal --size 320
```

#### В другую папку
```bash
python download_models_advanced.py --preset recommended --output MyModels/
```

#### Для отладки (без упрощения графа)
```bash
python download_models_advanced.py --models yolo26n --no-simplify
```

#### Динамический размер входа
```bash
python download_models_advanced.py --models yolo26n --dynamic
```

---

## 📊 Таблица всех моделей

### YOLO26 (2026, рекомендуется) ⭐

| Команда | Модель | Размер | Задача | Скорость |
|---------|--------|--------|--------|----------|
| `yolo26n` | yolo26n.onnx | ~6MB | Detection | ⚡⚡⚡⚡ |
| `yolo26s` | yolo26s.onnx | ~22MB | Detection | ⚡⚡⚡ |
| `yolo26m` | yolo26m.onnx | ~52MB | Detection | ⚡⚡ |
| `yolo26l` | yolo26l.onnx | ~110MB | Detection | ⚡ |
| `yolo26x` | yolo26x.onnx | ~140MB | Detection | ⚡ |
| `yolo26n-pose` | yolo26n-pose.onnx | ~8MB | Pose | ⚡⚡⚡⚡ |
| `yolo26s-pose` | yolo26s-pose.onnx | ~24MB | Pose | ⚡⚡⚡ |
| `yolo26m-pose` | yolo26m-pose.onnx | ~54MB | Pose | ⚡⚡ |
| `yolo26n-seg` | yolo26n-seg.onnx | ~8MB | Segmentation | ⚡⚡⚡ |
| `yolo26s-seg` | yolo26s-seg.onnx | ~24MB | Segmentation | ⚡⚡ |
| `yolo26m-seg` | yolo26m-seg.onnx | ~54MB | Segmentation | ⚡ |

### YOLO11 (2024, стабильные) ✅

| Команда | Модель | Размер | Задача |
|---------|--------|--------|--------|
| `yolo11n` | yolo11n.onnx | ~6MB | Detection |
| `yolo11s` | yolo11s.onnx | ~21MB | Detection |
| `yolo11m` | yolo11m.onnx | ~51MB | Detection |
| `yolo11n-pose` | yolo11n-pose.onnx | ~7MB | Pose |
| `yolo11s-pose` | yolo11s-pose.onnx | ~23MB | Pose |

---

## 🆘 Решение проблем

### Python не найден
```bash
# Скачайте Python 3.8+
https://www.python.org/downloads/

# Проверьте установку
python --version
```

### pip не работает
```bash
# Попробуйте python -m pip
python -m pip install ultralytics
```

### Ultralytics не установлен
```bash
pip install ultralytics

# Или обновите
pip install --upgrade ultralytics
```

### Ошибка при экспорте
```bash
# Установите дополнительные зависимости
pip install onnx onnxsim onnxruntime
```

### Модели скачались не в ту папку
```bash
# Укажите явно
python download_models_advanced.py --preset minimal --output "D:/Projects/MyProject/Assets/Models"
```

### Медленная загрузка
```bash
# Начните с minimal набора
python download_models_advanced.py --preset minimal

# Или загружайте по одной
python download_models_advanced.py --models yolo26n
```

---

## ✅ Checklist

- [ ] Python 3.8+ установлен
- [ ] `pip install ultralytics` выполнен
- [ ] Выбран нужный preset/модели
- [ ] Скрипт запущен успешно
- [ ] Модели в `Assets/Models/`
- [ ] Unity импортировала .onnx файлы
- [ ] Создан ModelConfig
- [ ] Модель назначена

---

## 💡 Советы

### Для первого раза
```bash
# Начните с minimal
python download_models_advanced.py --preset minimal
```

### Для продакшна
```bash
# Используйте recommended
python download_models_advanced.py --preset recommended
```

### Для экспериментов
```bash
# Загрузите full или all-yolo26
python download_models_advanced.py --preset full
```

### Для mobile
```bash
# Меньший размер входа = быстрее
python download_models_advanced.py --preset minimal --size 320
```

---

## 📝 После загрузки

### В Unity:
1. Откройте Unity проект
2. Дождитесь импорта моделей (Assets/Models/)
3. Создайте GameObject
4. Добавьте `UniversalTrackerManager`
5. В Inspector:
   - Model Configs → Add
   - Назначьте .onnx модель
   - Input Type → WebCam/Camera
6. Play!

### Проверка:
```csharp
// Система автоматически определит:
// yolo26n.onnx → YOLO26DetectionModel (NMS-free!)
// yolo11n.onnx → YOLO11DetectionModel (NMS-based)
// yolo26n-pose.onnx → YOLO26PoseModel
```

---

## 🎉 Готово!

Модели загружены и готовы к использованию!

**Следующий шаг:** Читайте `Assets/Scripts/UniversalTracker/README.md` для полной документации системы.
