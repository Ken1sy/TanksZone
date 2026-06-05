using UnityEngine;

public class LocalPhysicsSimulator : MonoBehaviour
{
    private PhysicsScene _physicsScene;

    private void Start()
    {
        // ѕолучаем физическую сцену именно той изолированной комнаты, 
        // в которой сейчас находитс€ этот объект
        _physicsScene = gameObject.scene.GetPhysicsScene();
    }

    private void FixedUpdate()
    {
        // ≈сли сцена валидна и это изолированна€ комната (не глобальна€ физика Unity)
        if (_physicsScene.IsValid() && _physicsScene != Physics.defaultPhysicsScene)
        {
            // ¬ручную "крутим ручку" физического движка дл€ этой комнаты!
            // Ёто заставит работать гравитацию, столкновени€ и RigidBody.
            _physicsScene.Simulate(Time.fixedDeltaTime);
        }
    }
}