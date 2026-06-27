# 🤖 Модели YOLO для Universal Tracker

Автоматически загружено: 1 моделей

## 📦 Загруженные модели

- `yolo26n-seg.onnx` - YOLO26 segmentation (nano)


## 🎯 Как использовать в Unity

1. Модели уже импортированы в Unity (Assets/Models/)
2. Создайте `ModelConfig` в Inspector
3. Назначьте .onnx файл в `Model Asset`
4. Система автоматически определит версию и тип модели

## ⚙️ Параметры экспорта

- **Format**: ONNX
- **Opset**: 13 (совместимо с Unity InferenceEngine)
- **Input size**: 640x640 (по умолчанию)
- **Simplified**: Yes
- **Dynamic**: No (фиксированный размер для стабильности)

## 📊 Рекомендации

### Для Mobile/Edge:
- `yolo26n.onnx` - детекция
- `yolo26n-pose.onnx` - поза

### Для Desktop:
- `yolo26s.onnx` или `yolo26m.onnx` - детекция
- `yolo26s-pose.onnx` - поза

### Резервная (стабильная):
- `yolo11n.onnx` - проверенная детекция

## 🔄 Обновление моделей

Запустите скрипт снова:
```bash
python download_models_advanced.py --preset recommended
```

---
📅 Создано: автоматически
🤖 Universal Tracker System
