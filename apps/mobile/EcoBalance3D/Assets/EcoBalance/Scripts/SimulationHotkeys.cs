using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class SimulationHotkeys : MonoBehaviour
{
    private const string SimulationScene = "HimalayaSimulation";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneListener()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != SimulationScene)
            return;

        if (FindAnyObjectByType<SimulationHotkeys>() != null)
            return;

        GameObject controller = new GameObject("[SimulationHotkeys]");
        SceneManager.MoveGameObjectToScene(controller, scene);
        controller.AddComponent<SimulationHotkeys>();
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            RunBreedingDemo();

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
            RunHuntingDemo();

        if (Keyboard.current.digit3Key.wasPressedThisFrame)
            RunEatingDemo();

        if (Keyboard.current.digit4Key.wasPressedThisFrame)
            RunDrinkingDemo();

        if (Keyboard.current.digit5Key.wasPressedThisFrame && ScreenFlow.GuidedMode)
            RunGuidedEndingDemo();
    }


    private void RunBreedingDemo()
    {
        AnimalBrain mother = null;
        AnimalBrain partner = null;

        float closestDistance = float.MaxValue;

        // Find the closest pair of adult Blue Sheep.
        foreach (AnimalBrain a in AnimalBrain.All)
        {
            if (a == null || a.IsCaught || a.IsYoung)
                continue;

            if (AnimalBrain.SpeciesLabel(a.species_data) != "blue sheep")
                continue;

            foreach (AnimalBrain b in AnimalBrain.All)
            {
                if (b == null || b == a || b.IsCaught || b.IsYoung)
                    continue;

                if (b.species_data != a.species_data)
                    continue;

                float distance = Vector3.Distance(
                    a.transform.position,
                    b.transform.position
                );

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    mother = a;
                    partner = b;
                }
            }
        }

        if (mother == null || partner == null)
        {
            Debug.LogWarning("[Hotkey] No suitable adult Blue Sheep pair found.");
            return;
        }

        // Focus the camera on the breeding demonstration.
        TerrainCameraController cameraController =
            FindAnyObjectByType<TerrainCameraController>();

        if (cameraController != null)
            cameraController.Follow(mother.transform, 18f);

        // Trigger the actual breeding and birth.
        if (mother.ForcePresentationMating(partner))
        {
            Debug.Log("[Hotkey] Breeding demonstration started!");
        }
        else
        {
            Debug.LogWarning(
                "[Hotkey] Breeding could not start. Check the Events mode and population cap."
            );
        }
    }


    private void RunHuntingDemo()
    {
        AnimalBrain leopard = null;
        AnimalBrain sheep = null;

        float closestDistance = float.MaxValue;

        // Find the closest living Snow Leopard and Blue Sheep.
        foreach (AnimalBrain predator in AnimalBrain.All)
        {
            if (predator == null || predator.IsCaught || predator.IsYoung)
                continue;

            if (!predator.IsPredatorSpecies)
                continue;

            if (AnimalBrain.SpeciesLabel(predator.species_data) != "snow leopard")
                continue;

            foreach (AnimalBrain prey in AnimalBrain.All)
            {
                if (prey == null || prey.IsCaught || prey.IsYoung)
                    continue;

                if (prey.IsPredatorSpecies)
                    continue;

                if (AnimalBrain.SpeciesLabel(prey.species_data) != "blue sheep")
                    continue;

                float distance = Vector3.Distance(
                    predator.transform.position,
                    prey.transform.position
                );

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    leopard = predator;
                    sheep = prey;
                }
            }
        }

        if (leopard == null || sheep == null)
        {
            Debug.LogWarning(
                "[Hotkey] Could not find a living Snow Leopard and Blue Sheep."
            );
            return;
        }
        // Move the selected sheep close enough for a visible demonstration.
        Vector3 direction = (sheep.transform.position - leopard.transform.position);
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
            direction = leopard.transform.forward;

        direction.Normalize();

        Vector3 desiredPosition =
            leopard.transform.position + direction * 10f;

        if (NavMesh.SamplePosition(
            desiredPosition,
            out NavMeshHit hit,
            15f,
            NavMesh.AllAreas))
        {
            NavMeshAgent sheepAgent =
                sheep.GetComponent<NavMeshAgent>();

            if (sheepAgent != null && sheepAgent.enabled)
            {
                sheepAgent.Warp(hit.position);
            }
            else
            {
                sheep.transform.position = hit.position;
            }
        }
        else
        {
            Debug.LogWarning("[Hotkey] Could not find a nearby NavMesh position for the sheep.");
            return;
        }

        // Focus the camera on the hunting leopard.
        TerrainCameraController cameraController =
            FindAnyObjectByType<TerrainCameraController>();

        if (cameraController != null)
            cameraController.Follow(leopard.transform, 20f);

        if (leopard.ForcePresentationHunt(sheep))
        {
            Debug.Log("[Hotkey] Hunting demonstration started!");
        }
        else
        {
            Debug.LogWarning("[Hotkey] Hunting demonstration could not start.");
        }
    }

    private void RunEatingDemo()
    {
        AnimalBrain sheep = null;

        float closestDistance = float.MaxValue;

        foreach (AnimalBrain animal in AnimalBrain.All)
        {
            if (animal == null || animal.IsCaught || animal.IsYoung)
                continue;

            if (animal.IsPredatorSpecies)
                continue;

            if (AnimalBrain.SpeciesLabel(animal.species_data) != "blue sheep")
                continue;

            // Prefer the sheep closest to an edible grass patch.
            float nearestGrassDistance = float.MaxValue;

            foreach (ResourcePatch patch in ResourcePatch.All)
            {
                if (patch == null || patch.IsWater || !patch.IsEdible)
                    continue;

                float distance = Vector3.Distance(
                    animal.transform.position,
                    patch.transform.position
                );

                nearestGrassDistance = Mathf.Min(
                    nearestGrassDistance,
                    distance
                );
            }

            if (nearestGrassDistance < closestDistance)
            {
                closestDistance = nearestGrassDistance;
                sheep = animal;
            }
        }

        if (sheep == null || closestDistance == float.MaxValue)
        {
            Debug.LogWarning("[Hotkey] No Blue Sheep or edible grass patch found.");
            return;
        }

        TerrainCameraController cameraController =
            FindAnyObjectByType<TerrainCameraController>();

        if (cameraController != null)
            cameraController.Follow(sheep.transform, 15f);

        if (sheep.ForcePresentationEating())
        {
            Debug.Log("[Hotkey] Eating demonstration started!");
        }
        else
        {
            Debug.LogWarning("[Hotkey] Eating demonstration could not start.");
        }
    }

    private void RunDrinkingDemo()
    {
        AnimalBrain selectedAnimal = null;
        float closestDistance = float.MaxValue;

        foreach (AnimalBrain animal in AnimalBrain.All)
        {
            if (animal == null || animal.IsCaught || animal.IsYoung)
                continue;

            if (!animal.HasThirst)
                continue;

            // Find the nearest available water patch for this animal.
            float nearestWaterDistance = float.MaxValue;

            foreach (ResourcePatch patch in ResourcePatch.All)
            {
                if (patch == null || !patch.IsWater || !patch.IsEdible)
                    continue;

                float distance = Vector3.Distance(
                    animal.transform.position,
                    patch.transform.position
                );

                nearestWaterDistance = Mathf.Min(
                    nearestWaterDistance,
                    distance
                );
            }

            if (nearestWaterDistance < closestDistance)
            {
                closestDistance = nearestWaterDistance;
                selectedAnimal = animal;
            }
        }

        if (selectedAnimal == null || closestDistance == float.MaxValue)
        {
            Debug.LogWarning("[Hotkey] No animal with an available water patch found.");
            return;
        }

        TerrainCameraController cameraController =
            FindAnyObjectByType<TerrainCameraController>();

        if (cameraController != null)
            cameraController.Follow(selectedAnimal.transform, 15f);

        if (selectedAnimal.ForcePresentationDrinking())
        {
            Debug.Log("[Hotkey] Drinking demonstration started!");
        }
        else
        {
            Debug.LogWarning("[Hotkey] Drinking demonstration could not start.");
        }
    }

    private void RunGuidedEndingDemo()
    {
        if (!ScreenFlow.GuidedMode)
            return;

        GuidedChallenge challenge =
            FindAnyObjectByType<GuidedChallenge>();

        if (challenge == null)
        {
            Debug.LogWarning("[Hotkey] GuidedChallenge not found.");
            return;
        }

        Debug.Log("[Hotkey] Triggering Guided Exploration ending!");

        challenge.FinishForPresentation();
    }
}
