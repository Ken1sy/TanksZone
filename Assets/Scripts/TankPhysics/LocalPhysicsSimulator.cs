using UnityEngine;

public class LocalPhysicsSimulator : MonoBehaviour
{
    private PhysicsScene _physicsScene;
    private void Start() { _physicsScene = gameObject.scene.GetPhysicsScene(); }
    private void FixedUpdate()
    {
        if (_physicsScene.IsValid() && _physicsScene != Physics.defaultPhysicsScene)
        { _physicsScene.Simulate(Time.fixedDeltaTime); }
    }
}