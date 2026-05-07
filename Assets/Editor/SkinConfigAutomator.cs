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
                    config.tiling = new Vector2(10f, 10f); // Тайлинг по умолчанию
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
                config.tiling = new Vector2(5f, 5f);

                // Говорим Юнити: "Я изменил этот файл, не забудь его сохранить"
                EditorUtility.SetDirty(config);
                count++;
            }
        }

        // Сохраняем все измененные файлы на жесткий диск
        AssetDatabase.SaveAssets();

        Debug.Log($"<color=cyan>Массовое обновление завершено!</color> Изменено тайлингов на 10x10: {count}");
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
}