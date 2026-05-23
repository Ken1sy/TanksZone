using UnityEditor;
using UnityEngine;
using System.IO;

public class SkinConfigAutomator
{
    [MenuItem("Assets/Собрать скины из структуры Colormap")]
    public static void GenerateConfigsFromFolders()
    {
        string basePath = "Assets/Resources/Colormap";

        // Проверяем, существует ли папка
        if (!AssetDatabase.IsValidFolder(basePath))
        {
            Debug.LogError($"Не найдена папка {basePath}! Убедись, что путь правильный.");
            return;
        }

        // Получаем все подпапки (zeus, forest, winter и т.д.)
        string[] skinFolders = AssetDatabase.GetSubFolders(basePath);
        int count = 0;

        foreach (string folderPath in skinFolders)
        {
            string folderName = Path.GetFileName(folderPath); // Например "zeus"

            // Ищем картинки по точным именам
            Texture2D img = AssetDatabase.LoadAssetAtPath<Texture2D>($"{folderPath}/image.jpg");
            if (img == null) img = AssetDatabase.LoadAssetAtPath<Texture2D>($"{folderPath}/image.png"); // На случай если где-то png

            Texture2D prev = AssetDatabase.LoadAssetAtPath<Texture2D>($"{folderPath}/preview.png");
            if (prev == null) prev = AssetDatabase.LoadAssetAtPath<Texture2D>($"{folderPath}/preview.jpg");

            if (img != null)
            {
                // Путь для сохранения конфига
                string configPath = $"{folderPath}/{folderName}_Config.asset";

                // Проверяем, нет ли уже такого конфига (чтобы не перезаписывать ручной тайлинг)
                TankSkinConfig config = AssetDatabase.LoadAssetAtPath<TankSkinConfig>(configPath);

                if (config == null)
                {
                    // Создаем новый
                    config = ScriptableObject.CreateInstance<TankSkinConfig>();
                    config.baseTiling = 1.5f; // Тайлинг по умолчанию
                    AssetDatabase.CreateAsset(config, configPath);
                }

                // Обновляем данные
                config.skinId = folderName;
                config.skinTexture = img;
                config.previewTexture = prev; // Даже если null, скрипт просто оставит поле пустым

                // Отмечаем, что файл изменен, чтобы Unity его сохранил
                EditorUtility.SetDirty(config);
                count++;
            }
            else
            {
                Debug.LogWarning($"В папке {folderName} не найден файл image.jpg или image.png");
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"<color=green>Успешно!</color> Обработано и обновлено конфигов: {count}");
    }

    [MenuItem("Assets/Установить тайлинг 10x10 для ВСЕХ конфигов")]
    public static void ForceUpdateAllTiling()
    {
        // AssetDatabase.FindAssets умеет искать файлы по их типу. 
        // t:TankSkinConfig найдет все твои конфиги в любой папке проекта.
        string[] guids = AssetDatabase.FindAssets("t:TankSkinConfig");
        int count = 0;

        foreach (string guid in guids)
        {
            // Получаем реальный путь к файлу по его GUID
            string path = AssetDatabase.GUIDToAssetPath(guid);

            // Загружаем конфиг
            TankSkinConfig config = AssetDatabase.LoadAssetAtPath<TankSkinConfig>(path);

            if (config != null)
            {
                // Жестко перезаписываем тайлинг
                config.baseTiling = 1.5f;

                // Говорим Юнити: "Я изменил этот файл, не забудь его сохранить"
                EditorUtility.SetDirty(config);
                count++;
            }
        }

        // Сохраняем все измененные файлы на жесткий диск
        AssetDatabase.SaveAssets();

        Debug.Log($"<color=cyan>Массовое обновление завершено!</color> Изменено тайлингов: {count}");
    }

    [MenuItem("Assets/Удалить ВСЕ конфиги скинов (Опасно!)")]
    public static void DeleteAllConfigs()
    {
        // Находим все файлы типа TankSkinConfig во всем проекте
        string[] guids = AssetDatabase.FindAssets("t:TankSkinConfig");

        // Защита от случайного нажатия: выводим всплывающее окно с подтверждением
        bool isSure = EditorUtility.DisplayDialog(
            "Удаление конфигов",
            $"Вы собираетесь безвозвратно удалить {guids.Length} конфигов скинов из проекта.\n\nВы уверены?",
            "Да, удалить всё",
            "Отмена"
        );

        if (!isSure)
        {
            Debug.Log("Удаление отменено.");
            return;
        }

        int count = 0;

        // Проходимся по всем найденным файлам и удаляем их
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AssetDatabase.DeleteAsset(path);
            count++;
        }

        // Обновляем базу данных редактора, чтобы файлы сразу исчезли из окна Project
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<color=red>Очистка завершена!</color> Удалено файлов конфигурации: {count}");
    }

    [MenuItem("Assets/1. Гараж: Сделать все preview спрайтами")]
    public static void FixPreviewSprites()
    {
        string basePath = "Assets/Resources/Colormap";
        if (!AssetDatabase.IsValidFolder(basePath))
        {
            Debug.LogWarning("Папка " + basePath + " не найдена.");
            return;
        }

        string[] skinFolders = AssetDatabase.GetSubFolders(basePath);
        int count = 0;

        foreach (string folderPath in skinFolders)
        {
            string previewPng = $"{folderPath}/preview.png";
            string previewJpg = $"{folderPath}/preview.jpg";

            // Ищем, есть ли файл с таким форматом
            string targetPath = File.Exists(previewPng) ? previewPng : (File.Exists(previewJpg) ? previewJpg : null);

            if (targetPath != null)
            {
                // Получаем доступ к настройкам импорта файла
                TextureImporter importer = AssetImporter.GetAtPath(targetPath) as TextureImporter;
                if (importer != null)
                {
                    bool changed = false;

                    if (importer.textureType != TextureImporterType.Sprite)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        changed = true;
                    }

                    if (importer.spriteImportMode != SpriteImportMode.Single)
                    {
                        importer.spriteImportMode = SpriteImportMode.Single;
                        changed = true;
                    }

                    // Сохраняем только если были изменения
                    if (changed)
                    {
                        importer.SaveAndReimport();
                        count++;
                    }
                }
            }
        }

        Debug.Log($"<color=green>Обновлено превью (сделаны спрайтами): {count} шт.</color>");
    }

    [MenuItem("Assets/2. Гараж: Добавить все краски в базу (Items Database)")]
    public static void AddPaintsToGarageDatabase()
    {
        // Ищем менеджер гаража на ОТКРЫТОЙ сцене
        GarageItemsManager manager = Object.FindAnyObjectByType<GarageItemsManager>();
        if (manager == null)
        {
            Debug.LogError("На сцене не найден GarageItemsManager! Откройте сцену гаража перед запуском.");
            return;
        }

        string basePath = "Assets/Resources/Colormap";
        if (!AssetDatabase.IsValidFolder(basePath)) return;

        string[] skinFolders = AssetDatabase.GetSubFolders(basePath);
        int addedCount = 0;

        // Если список еще не создан, создаем его
        if (manager.itemsDatabase == null)
            manager.itemsDatabase = new System.Collections.Generic.List<GarageItemInfo>();

        foreach (string folderPath in skinFolders)
        {
            string folderName = Path.GetFileName(folderPath);
            string expectedId = "paint_" + folderName;

            // Проверяем, не добавляли ли мы эту краску ранее (чтобы не было дубликатов)
            bool exists = false;
            foreach (var item in manager.itemsDatabase)
            {
                if (item.itemID == expectedId)
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                GarageItemInfo newItem = new GarageItemInfo();
                newItem.itemID = expectedId;
                newItem.itemName = char.ToUpper(folderName[0]) + folderName.Substring(1); // Делаем первую букву названия заглавной
                newItem.category = ItemCategory.Paint;
                newItem.paintSkinId = folderName;
                newItem.price = 1000; // Цена по умолчанию
                newItem.isOwned = false;
                newItem.isEquipped = false;
                newItem.requiredRankIndex = 0; // Доступно с Новобранца

                // Загружаем наш спрайт превью (который мы настроили предыдущей кнопкой)
                Sprite previewSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{folderPath}/preview.png");
                if (previewSprite == null) previewSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{folderPath}/preview.jpg");

                newItem.itemIcon = previewSprite;

                manager.itemsDatabase.Add(newItem);
                addedCount++;
            }
        }

        if (addedCount > 0)
        {
            // Говорим Unity, что мы изменили компонент на сцене, чтобы появилась "звездочка" сохранения
            EditorUtility.SetDirty(manager);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            Debug.Log($"<color=green>Успешно добавлено {addedCount} новых красок в базу Гаража!</color>");
        }
        else
        {
            Debug.Log("Новых красок не найдено (все уже есть в базе).");
        }
    }
}