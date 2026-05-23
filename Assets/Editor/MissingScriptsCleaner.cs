using UnityEngine;
using UnityEditor;

public class MissingScriptsCleaner : Editor
{
    [MenuItem("Tools/Удалить отсутствующие скрипты (Remove Missing)")]
    public static void RemoveMissingScripts()
    {
        // Получаем то, что сейчас выделено мышкой
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("Пожалуйста, выделите объекты в Иерархии (например, корень вашего префаба).");
            return;
        }

        int totalRemoved = 0;

        foreach (GameObject go in selectedObjects)
        {
            // Получаем сам объект и всех его "детей", даже если они выключены (inactive)
            Transform[] allChildren = go.GetComponentsInChildren<Transform>(true);

            foreach (Transform t in allChildren)
            {
                // Регистрируем действие для возможности отмены (Ctrl+Z)
                Undo.RegisterCompleteObjectUndo(t.gameObject, "Remove Missing Scripts");

                // Встроенная функция Unity для удаления потерянных скриптов
                int removedCount = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);

                if (removedCount > 0)
                {
                    totalRemoved += removedCount;
                    // Сообщаем Unity, что префаб был изменен и его нужно сохранить
                    EditorUtility.SetDirty(t.gameObject);
                }
            }
        }

        Debug.Log($"Очистка завершена! Удалено битых скриптов: {totalRemoved}");
    }
}