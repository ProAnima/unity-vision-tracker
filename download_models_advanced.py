"""
🚀 Расширенный скрипт загрузки моделей YOLO для Unity
Автор: Universal Tracker System
"""

import os
import sys
from pathlib import Path
import argparse

try:
    from ultralytics import YOLO
    print("✅ Ultralytics найден")
except ImportError:
    print("❌ Установите: pip install ultralytics")
    sys.exit(1)

# Все доступные модели
ALL_MODELS = {
    # YOLO26 Detection
    "yolo26n": {"full_name": "yolo26n.pt", "task": "detection", "size": "nano", "version": "26"},
    "yolo26s": {"full_name": "yolo26s.pt", "task": "detection", "size": "small", "version": "26"},
    "yolo26m": {"full_name": "yolo26m.pt", "task": "detection", "size": "medium", "version": "26"},
    "yolo26l": {"full_name": "yolo26l.pt", "task": "detection", "size": "large", "version": "26"},
    "yolo26x": {"full_name": "yolo26x.pt", "task": "detection", "size": "xlarge", "version": "26"},
    
    # YOLO26 Pose
    "yolo26n-pose": {"full_name": "yolo26n-pose.pt", "task": "pose", "size": "nano", "version": "26"},
    "yolo26s-pose": {"full_name": "yolo26s-pose.pt", "task": "pose", "size": "small", "version": "26"},
    "yolo26m-pose": {"full_name": "yolo26m-pose.pt", "task": "pose", "size": "medium", "version": "26"},
    
    # YOLO26 Segmentation
    "yolo26n-seg": {"full_name": "yolo26n-seg.pt", "task": "segmentation", "size": "nano", "version": "26"},
    "yolo26s-seg": {"full_name": "yolo26s-seg.pt", "task": "segmentation", "size": "small", "version": "26"},
    "yolo26m-seg": {"full_name": "yolo26m-seg.pt", "task": "segmentation", "size": "medium", "version": "26"},
    
    # YOLO11 Detection
    "yolo11n": {"full_name": "yolo11n.pt", "task": "detection", "size": "nano", "version": "11"},
    "yolo11s": {"full_name": "yolo11s.pt", "task": "detection", "size": "small", "version": "11"},
    "yolo11m": {"full_name": "yolo11m.pt", "task": "detection", "size": "medium", "version": "11"},
    
    # YOLO11 Pose
    "yolo11n-pose": {"full_name": "yolo11n-pose.pt", "task": "pose", "size": "nano", "version": "11"},
    "yolo11s-pose": {"full_name": "yolo11s-pose.pt", "task": "pose", "size": "small", "version": "11"},
    
    # YOLO12 Detection (если нужно)
    # "yolo12n": {"full_name": "yolo12n.pt", "task": "detection", "size": "nano", "version": "12"},
}

# Предустановленные наборы
PRESETS = {
    "minimal": ["yolo26n", "yolo26n-pose"],
    "recommended": ["yolo26n", "yolo26s", "yolo26n-pose", "yolo11n"],
    "full": ["yolo26n", "yolo26s", "yolo26m", "yolo26n-pose", "yolo26s-pose", 
             "yolo11n", "yolo11s", "yolo11n-pose"],
    "all-yolo26": [k for k in ALL_MODELS if ALL_MODELS[k]["version"] == "26"],
    "all-yolo11": [k for k in ALL_MODELS if ALL_MODELS[k]["version"] == "11"],
}

def parse_args():
    parser = argparse.ArgumentParser(
        description="🤖 Загрузка YOLO моделей для Unity InferenceEngine",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
📋 Примеры использования:

  # Рекомендованный набор (4 модели, ~40MB)
  python download_models_advanced.py --preset recommended

  # Минимальный набор (2 модели, ~15MB)
  python download_models_advanced.py --preset minimal

  # Полный набор (8 моделей, ~100MB)
  python download_models_advanced.py --preset full

  # Все модели YOLO26
  python download_models_advanced.py --preset all-yolo26

  # Конкретные модели
  python download_models_advanced.py --models yolo26n yolo26n-pose

  # С кастомным размером входа (для слабых устройств)
  python download_models_advanced.py --preset minimal --size 320

  # Список всех доступных моделей
  python download_models_advanced.py --list
        """
    )
    
    parser.add_argument("--preset", choices=PRESETS.keys(), 
                        help="Предустановленный набор моделей")
    parser.add_argument("--models", nargs="+", choices=ALL_MODELS.keys(),
                        help="Конкретные модели для загрузки")
    parser.add_argument("--output", default="Assets/Models",
                        help="Директория для сохранения (default: Assets/Models)")
    parser.add_argument("--size", type=int, default=640,
                        help="Размер входа модели (default: 640)")
    parser.add_argument("--opset", type=int, default=13,
                        help="ONNX opset версия (default: 13 для Unity)")
    parser.add_argument("--no-simplify", action="store_true",
                        help="Отключить упрощение ONNX графа")
    parser.add_argument("--dynamic", action="store_true",
                        help="Динамический размер входа")
    parser.add_argument("--list", action="store_true",
                        help="Показать все доступные модели и выйти")
    
    return parser.parse_args()

def list_models():
    """Показывает все доступные модели"""
    print("\n" + "="*60)
    print("📋 ДОСТУПНЫЕ МОДЕЛИ")
    print("="*60 + "\n")
    
    for version in ["26", "11", "12"]:
        models = {k: v for k, v in ALL_MODELS.items() if v["version"] == version}
        if models:
            print(f"🔸 YOLO{version}:")
            for key, info in models.items():
                print(f"   {key:20s} - {info['task']:15s} ({info['size']})")
            print()
    
    print("="*60)
    print("📦 ПРЕДУСТАНОВЛЕННЫЕ НАБОРЫ")
    print("="*60 + "\n")
    
    for preset, models in PRESETS.items():
        count = len(models)
        model_names = ', '.join(models[:3])
        if count > 3:
            model_names += f"... (+{count-3})"
        print(f"   {preset:15s} - {count:2d} моделей: {model_names}")
    
    print("\n" + "="*60)

def download_model(model_key, output_dir, args):
    """Загружает и экспортирует одну модель"""
    info = ALL_MODELS[model_key]
    model_name = info["full_name"]
    
    try:
        print(f"\n{'='*60}")
        print(f"🔄 {model_key}")
        print(f"   Задача: {info['task']}")
        print(f"   Размер: {info['size']}")
        print(f"   Версия: YOLO{info['version']}")
        print(f"{'='*60}")
        
        print(f"📥 Загрузка модели...")
        model = YOLO(model_name)
        
        output_name = model_name.replace('.pt', '.onnx')
        output_path = output_dir / output_name
        
        print(f"🔧 Экспорт в ONNX...")
        print(f"   Opset: {args.opset}")
        print(f"   Input size: {args.size}x{args.size}")
        print(f"   Simplify: {not args.no_simplify}")
        print(f"   Dynamic: {args.dynamic}")
        
        model.export(
            format='onnx',
            opset=args.opset,
            simplify=not args.no_simplify,
            dynamic=args.dynamic,
            imgsz=args.size
        )
        
        # Перемещаем файл
        exported_file = Path(model_name.replace('.pt', '.onnx'))
        if exported_file.exists():
            if output_path.exists():
                output_path.unlink()
            exported_file.rename(output_path)
            
            size_mb = output_path.stat().st_size / (1024 * 1024)
            print(f"✅ Сохранено: {output_path.name}")
            print(f"   Размер файла: {size_mb:.2f} MB")
            return True
        else:
            print(f"⚠️ Файл не найден после экспорта")
            return False
            
    except Exception as e:
        print(f"❌ Ошибка: {e}")
        return False

def create_readme(output_dir, downloaded_models):
    """Создает README с информацией о загруженных моделях"""
    readme_path = output_dir / "README.md"
    
    content = f"""# 🤖 Модели YOLO для Universal Tracker

Автоматически загружено: {len(downloaded_models)} моделей

## 📦 Загруженные модели

"""
    
    for model_key in downloaded_models:
        info = ALL_MODELS[model_key]
        content += f"- `{info['full_name'].replace('.pt', '.onnx')}` - YOLO{info['version']} {info['task']} ({info['size']})\n"
    
    content += """

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
"""
    
    readme_path.write_text(content, encoding='utf-8')
    print(f"\n📝 Создан README: {readme_path}")

def main():
    args = parse_args()
    
    # Показать список и выйти
    if args.list:
        list_models()
        return
    
    print("="*60)
    print("🤖 YOLO Models Downloader для Unity InferenceEngine")
    print("="*60)
    
    # Определяем какие модели загружать
    models_to_download = []
    
    if args.preset:
        models_to_download = PRESETS[args.preset]
        print(f"\n📦 Набор: {args.preset}")
        print(f"   Моделей: {len(models_to_download)}")
    elif args.models:
        models_to_download = args.models
        print(f"\n🎯 Выбрано моделей: {len(args.models)}")
    else:
        print("\n❌ Укажите --preset или --models")
        print("   Используйте --help для справки")
        print("   Или --list для списка моделей")
        return
    
    # Создаем директорию
    output_dir = Path(args.output)
    output_dir.mkdir(parents=True, exist_ok=True)
    print(f"📁 Директория: {output_dir.absolute()}\n")
    
    # Загружаем модели
    success = 0
    failed = 0
    downloaded = []
    
    for model_key in models_to_download:
        if download_model(model_key, output_dir, args):
            success += 1
            downloaded.append(model_key)
        else:
            failed += 1
    
    # Создаем README
    if downloaded:
        create_readme(output_dir, downloaded)
    
    # Итоги
    print("\n" + "="*60)
    print("📊 ИТОГИ")
    print("="*60)
    print(f"✅ Успешно: {success}/{len(models_to_download)}")
    if failed > 0:
        print(f"❌ Ошибок: {failed}/{len(models_to_download)}")
    print(f"📁 Сохранено в: {output_dir.absolute()}")
    print("="*60)
    
    if success > 0:
        print("\n✨ ГОТОВО!")
        print("📝 Следующие шаги:")
        print("   1. Откройте Unity проект")
        print("   2. Модели в Assets/Models/")
        print("   3. Создайте ModelConfig")
        print("   4. Назначьте .onnx модель")
        print("   5. Запустите UniversalTrackerManager")
        print("\n🎉 Удачи!")

if __name__ == "__main__":
    main()
