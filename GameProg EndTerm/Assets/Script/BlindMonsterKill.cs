using UnityEngine;
using UnityEngine.SceneManagement;

public class MonsterCatchPlayer : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Safely find the root object
        Transform rootTransform = other.attachedRigidbody != null ? other.attachedRigidbody.transform : other.transform.root;

        if (rootTransform.CompareTag("Player") || other.CompareTag("Player"))
        {
            // NEW CHECK: Is the player hiding?
            // We know the ChestHidingSpot script turns off the PlayerNoiseEmitter when hiding.
            PlayerNoiseEmitter noiseScript = rootTransform.GetComponent<PlayerNoiseEmitter>();

            // If the script exists and is currently disabled, the player is safely in the chest.
            if (noiseScript != null && noiseScript.enabled == false)
            {
                // Ignore the collision and cancel the kill sequence!
                return;
            }

            // Otherwise, they are out in the open. Catch them!
            CatchPlayer();
        }
    }

    private void CatchPlayer()
    {
        Debug.Log("Player caught! Restarting scene...");
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
}