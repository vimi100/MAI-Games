#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

public static class EchoSceneBuilder
{
    private const string ScenePath = "Assets/SonicShaper/Scenes/EchoMechanics_Test.unity";

    [MenuItem("Tools/Sonic Shaper/Create Echo Mechanics Test Scene")]
    public static void CreateEchoMechanicsScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "EchoMechanics_Test";

        CreateLighting();
        CreateRoomWeightPlate(out Transform room1Exit);
        CreateRoomBridge(out Transform room2Exit);
        CreateCorridorBetweenRooms(room1Exit.position, new Vector3(16f, 0f, 0f));
        CreateRoomTurret(room2Exit.position + new Vector3(20f, 0f, 0f), out Transform room3Entry, out Transform room3Exit);
        CreateCorridorBetweenRooms(room2Exit.position, room3Entry.position);
        CreateSimplePlayerSpawn(Vector3.zero + new Vector3(0f, 1.2f, -6f));

        EnsureFolder("Assets/SonicShaper");
        EnsureFolder("Assets/SonicShaper/Scenes");
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("EchoMechanics_Test scene created at: " + ScenePath);
    }

    private static void CreateLighting()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.6f, 0.6f, 0.65f);

        GameObject lightGo = new GameObject("Directional Light");
        Light light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    private static void CreateRoomWeightPlate(out Transform exit)
    {
        Vector3 origin = Vector3.zero;
        CreateFloor(origin, new Vector3(14f, 1f, 12f), "Room1_Floor");
        CreateWallsWithDoorGap(origin, new Vector3(14f, 4f, 12f), "Room1_Walls", WallSide.Right, 3.6f, 3.2f);

        GameObject plateGo = CreateTriggerPlate(origin + new Vector3(2f, 0.12f, 0f), new Vector3(3.6f, 0.24f, 3.6f), "Room1_HeavyPlate");
        PressurePlate plate = plateGo.AddComponent<PressurePlate>();
        plate.requiredMass = 2.0f;
        plate.acceptedPolarity = EchoPolarity.Any;
        plate.onlyEchoCopies = false;
        plate.triggerLayers = ~0;
        plate.plateRenderer = plateGo.GetComponent<Renderer>();
        plate.inactiveColor = new Color(0.95f, 0.2f, 0.2f);
        plate.activeColor = new Color(0.2f, 1f, 0.3f);
        if (plate.OnActivated == null) plate.OnActivated = new UnityEvent();
        if (plate.OnDeactivated == null) plate.OnDeactivated = new UnityEvent();

        GameObject statusLamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        statusLamp.name = "Room1_PlateIndicator";
        statusLamp.transform.position = origin + new Vector3(2f, 1.2f, -1.7f);
        statusLamp.transform.localScale = Vector3.one * 0.45f;
        Collider lampCollider = statusLamp.GetComponent<Collider>();
        if (lampCollider != null) Object.DestroyImmediate(lampCollider);
        SimpleStateIndicator indicator = statusLamp.AddComponent<SimpleStateIndicator>();
        indicator.indicatorRenderer = statusLamp.GetComponent<Renderer>();
        plate.OnActivated.AddListener(indicator.SetOn);
        plate.OnDeactivated.AddListener(indicator.SetOff);

        GameObject doorGo = CreateDoor(origin + new Vector3(6f, 1.5f, 0f), new Vector3(0.5f, 3f, 3f), "Room1_Door");
        PuzzleDoor door = doorGo.AddComponent<PuzzleDoor>();
        door.doorType = PuzzleDoor.DoorType.Slide;
        door.slideOffset = new Vector3(0f, 3.2f, 0f);
        door.openDuration = 0.75f;
        plate.OnActivated.AddListener(door.Open);
        plate.OnDeactivated.AddListener(door.Close);

        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = "Room1_HeavyBox";
        box.transform.position = origin + new Vector3(-2f, 0.7f, 0f);
        box.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
        Rigidbody rb = box.AddComponent<Rigidbody>();
        rb.mass = 1.2f;
        box.AddComponent<EchoCloneable>();
        box.AddComponent<EchoPolarityObject>();
        PaintInteractableCopyable(box);

        exit = doorGo.transform;
    }

    private static void CreateRoomBridge(out Transform exit)
    {
        Vector3 origin = new Vector3(20f, 0f, 0f);
        CreateFloor(origin + new Vector3(-4f, 0f, 0f), new Vector3(8f, 1f, 12f), "Room2_StartFloor");
        CreateFloor(origin + new Vector3(8f, 0f, 0f), new Vector3(8f, 1f, 12f), "Room2_EndFloor");
        CreateWallsWithDoorGap(origin + new Vector3(2f, 0f, 0f), new Vector3(20f, 4f, 12f), "Room2_Walls", WallSide.Left, 3.6f, 3.2f);

        GameObject bridgeObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bridgeObject.name = "Room2_BridgePlatform";
        bridgeObject.transform.position = origin + new Vector3(-1.5f, 0.55f, 0f);
        bridgeObject.transform.localScale = new Vector3(3f, 0.5f, 3f);
        Rigidbody bridgeRb = bridgeObject.AddComponent<Rigidbody>();
        bridgeRb.mass = 1f;
        bridgeRb.linearDamping = 4f;
        bridgeObject.AddComponent<EchoCloneable>();
        bridgeObject.AddComponent<EchoPolarityObject>();
        PaintInteractableCopyable(bridgeObject);

        GameObject invertedPath = GameObject.CreatePrimitive(PrimitiveType.Cube);
        invertedPath.name = "Room2_InvertedPathOnly";
        invertedPath.transform.position = origin + new Vector3(2.5f, 0.5f, 0f);
        invertedPath.transform.localScale = new Vector3(2.5f, 1f, 3f);
        invertedPath.AddComponent<EchoPolaritySurface>().acceptedPolarity = EchoPolarity.Inverted;
        PaintBright(invertedPath, new Color(0.55f, 0.2f, 1f));

        GameObject doorGo = CreateDoor(origin + new Vector3(13f, 1.5f, 0f), new Vector3(0.5f, 3f, 3f), "Room2_Door");
        PuzzleDoor door = doorGo.AddComponent<PuzzleDoor>();
        door.doorType = PuzzleDoor.DoorType.Slide;
        door.slideOffset = new Vector3(0f, 3.2f, 0f);
        door.openDuration = 0.75f;

        GameObject activator = CreateTriggerPlate(origin + new Vector3(11f, 0.15f, 0f), new Vector3(2f, 0.3f, 2f), "Room2_InvertedActivator");
        EchoPolarityActivator polarityActivator = activator.AddComponent<EchoPolarityActivator>();
        polarityActivator.acceptedPolarity = EchoPolarity.Inverted;
        polarityActivator.onlyEchoCopies = true;
        polarityActivator.triggerLayers = ~0;
        PaintBright(activator, new Color(0.65f, 0.3f, 1f));
        if (polarityActivator.OnActivated == null) polarityActivator.OnActivated = new UnityEvent();
        if (polarityActivator.OnDeactivated == null) polarityActivator.OnDeactivated = new UnityEvent();
        polarityActivator.OnActivated.AddListener(door.Open);
        polarityActivator.OnDeactivated.AddListener(door.Close);

        exit = doorGo.transform;
    }

    private static void CreateRoomTurret(Vector3 origin, out Transform entryPoint, out Transform exitPoint)
    {
        Vector3 roomSize = new Vector3(18f, 4f, 12f);
        float halfX = roomSize.x * 0.5f;
        CreateFloor(origin, new Vector3(roomSize.x, 1f, roomSize.z), "Room3_Floor");
        CreateWallsWithTwoDoorGaps(origin, roomSize, "Room3_Walls", 3.6f, 3.2f);

        GameObject lane = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lane.name = "Room3_DangerLane";
        lane.transform.position = origin + new Vector3(0f, 0.02f, 0f);
        lane.transform.localScale = new Vector3(14.5f, 0.04f, 2.2f);
        PaintBright(lane, new Color(0.6f, 0.1f, 0.1f));
        Collider laneCol = lane.GetComponent<Collider>();
        if (laneCol != null) Object.DestroyImmediate(laneCol);

        GameObject turretBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        turretBase.name = "Room3_TurretBase";
        turretBase.transform.position = origin + new Vector3(1.8f, 0.8f, 0f);
        turretBase.transform.localScale = new Vector3(1f, 0.8f, 1f);

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
        head.name = "TurretHead";
        head.transform.SetParent(turretBase.transform);
        head.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        head.transform.localScale = new Vector3(1.4f, 0.5f, 1.4f);

        GameObject muzzle = new GameObject("MuzzlePoint");
        muzzle.transform.SetParent(head.transform);
        muzzle.transform.localPosition = new Vector3(0f, 0f, 0.75f);

        EchoTurretTargeting turret = turretBase.AddComponent<EchoTurretTargeting>();
        turret.rotatingPart = head.transform;
        turret.muzzlePoint = muzzle.transform;
        turret.stateRenderer = head.GetComponent<Renderer>();
        turret.detectionRadius = 15f;
        turret.attackRange = 18f;
        turret.attackInterval = 0.32f;
        turret.aimDotThreshold = 0.93f;
        turret.rocketSpeed = 24f;
        turret.rocketDamage = 18f;
        turret.rocketLifetime = 3f;
        turret.rocketBlastRadius = 1.35f;
        turret.turnSpeed = 6f;
        turret.lockPitch = true;
        turret.audioSource = turretBase.AddComponent<AudioSource>();

        GameObject bait = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bait.name = "Room3_BaitBox";
        bait.transform.position = origin + new Vector3(-4.8f, 0.7f, 1.7f);
        bait.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
        Rigidbody baitRb = bait.AddComponent<Rigidbody>();
        baitRb.mass = 1.1f;
        bait.AddComponent<EchoCloneable>();
        bait.AddComponent<EchoPolarityObject>();
        PaintInteractableCopyable(bait);

        CreateCover(origin + new Vector3(-1.6f, 0.8f, -1.9f), "Room3_Cover_1");
        CreateCover(origin + new Vector3(2.8f, 0.8f, 1.9f), "Room3_Cover_2");
        CreateCover(origin + new Vector3(6.2f, 0.8f, -1.9f), "Room3_Cover_3");

        GameObject respawn = new GameObject("Room3_RespawnPoint");
        respawn.transform.position = origin + new Vector3(-halfX - 2.2f, 1.2f, 0f);

        GameObject zone = GameObject.CreatePrimitive(PrimitiveType.Cube);
        zone.name = "Room3_CombatZone";
        zone.transform.position = origin;
        zone.transform.localScale = new Vector3(roomSize.x - 0.8f, 3f, roomSize.z - 0.8f);
        Collider zoneCol = zone.GetComponent<Collider>();
        if (zoneCol != null) zoneCol.isTrigger = true;
        Renderer zoneRenderer = zone.GetComponent<Renderer>();
        if (zoneRenderer != null) zoneRenderer.enabled = false;

        TurretCombatZone combatZone = zone.AddComponent<TurretCombatZone>();
        combatZone.turret = turret;
        combatZone.roomRespawnPoint = respawn.transform;

        GameObject goal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        goal.name = "Room3_ExitGoal";
        goal.transform.position = origin + new Vector3(halfX - 0.9f, 0.75f, 0f);
        goal.transform.localScale = new Vector3(0.5f, 1.5f, 2.8f);
        Collider goalCol = goal.GetComponent<Collider>();
        if (goalCol != null) goalCol.isTrigger = true;
        Room3Goal room3Goal = goal.AddComponent<Room3Goal>();
        room3Goal.goalRenderer = goal.GetComponent<Renderer>();
        PaintBright(goal, new Color(0.2f, 0.6f, 1f));

        entryPoint = new GameObject("Room3_EntryPoint").transform;
        entryPoint.position = origin + new Vector3(-halfX - 0.3f, 0f, 0f);
        exitPoint = new GameObject("Room3_ExitPoint").transform;
        exitPoint.position = origin + new Vector3(halfX + 0.3f, 0f, 0f);
    }

    private static void CreateCorridorBetweenRooms(Vector3 room1DoorPos, Vector3 room2EntryPos)
    {
        Vector3 mid = (room1DoorPos + room2EntryPos) * 0.5f;
        float length = Mathf.Abs(room2EntryPos.x - room1DoorPos.x) + 1.5f;
        CreateFloor(mid, new Vector3(length, 1f, 4.5f), "Room1_Room2_CorridorFloor");

        CreateWall(null, mid + new Vector3(0f, 1.5f, 2.25f), new Vector3(length, 3f, 0.35f), "CorridorWall_Front");
        CreateWall(null, mid + new Vector3(0f, 1.5f, -2.25f), new Vector3(length, 3f, 0.35f), "CorridorWall_Back");

        CreateDirectionArrow(room1DoorPos + new Vector3(2.4f, 0.02f, 0f), Vector3.right, "ToRoom2_Arrow_1");
        CreateDirectionArrow(mid + new Vector3(0f, 0.02f, 0f), Vector3.right, "ToRoom2_Arrow_2");
        CreateDirectionArrow(room2EntryPos + new Vector3(-2.2f, 0.02f, 0f), Vector3.right, "ToRoom2_Arrow_3");
    }

    private static void CreateSimplePlayerSpawn(Vector3 position)
    {
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = position;
        player.tag = "Player";
        Collider collider = player.GetComponent<Collider>();
        if (collider != null) Object.DestroyImmediate(collider);

        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.35f;
        cc.center = new Vector3(0f, 0.9f, 0f);

        GameObject camGo = new GameObject("PlayerCamera");
        camGo.transform.SetParent(player.transform);
        camGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        Camera cam = camGo.AddComponent<Camera>();
        cam.nearClipPlane = 0.03f;
        camGo.AddComponent<AudioListener>();

        player.AddComponent<PlayerController>().cameraTransform = camGo.transform;
        player.AddComponent<PlayerDash>();
        PlayerRigidbodyPusher pusher = player.AddComponent<PlayerRigidbodyPusher>();
        pusher.pushPower = 0.9f;
        pusher.maxPushMass = 20f;
        player.AddComponent<PlayerHealth>();
        SoundEmitter emitter = player.AddComponent<SoundEmitter>();
        emitter.audioSource = player.AddComponent<AudioSource>();
        emitter.maxCopyDistance = 5f;
        emitter.copyLifetime = 8f;
        emitter.cooldown = 12f;
        emitter.maxSimultaneousCopies = 1;
    }

    private static GameObject CreateTriggerPlate(Vector3 position, Vector3 size, string name)
    {
        GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plate.name = name;
        plate.transform.position = position;
        plate.transform.localScale = size;
        Collider col = plate.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
        return plate;
    }

    private static GameObject CreateDoor(Vector3 position, Vector3 size, string name)
    {
        GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
        door.name = name;
        door.transform.position = position;
        door.transform.localScale = size;
        return door;
    }

    private static void CreateFloor(Vector3 center, Vector3 size, string name)
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = name;
        floor.transform.position = center + new Vector3(0f, -0.5f, 0f);
        floor.transform.localScale = size;
    }

    private static void CreateWalls(Vector3 center, Vector3 bounds, string rootName)
    {
        GameObject root = new GameObject(rootName);
        float halfX = bounds.x * 0.5f;
        float halfZ = bounds.z * 0.5f;
        float wallHeight = bounds.y;
        float wallThickness = 0.5f;

        CreateWall(root.transform, center + new Vector3(halfX, wallHeight * 0.5f, 0f), new Vector3(wallThickness, wallHeight, bounds.z), "RightWall");
        CreateWall(root.transform, center + new Vector3(-halfX, wallHeight * 0.5f, 0f), new Vector3(wallThickness, wallHeight, bounds.z), "LeftWall");
        CreateWall(root.transform, center + new Vector3(0f, wallHeight * 0.5f, halfZ), new Vector3(bounds.x, wallHeight, wallThickness), "FrontWall");
        CreateWall(root.transform, center + new Vector3(0f, wallHeight * 0.5f, -halfZ), new Vector3(bounds.x, wallHeight, wallThickness), "BackWall");
    }

    private enum WallSide { Left, Right, Front, Back }

    private static void CreateWallsWithDoorGap(Vector3 center, Vector3 bounds, string rootName, WallSide gapSide, float gapWidth, float gapHeight)
    {
        GameObject root = new GameObject(rootName);
        float halfX = bounds.x * 0.5f;
        float halfZ = bounds.z * 0.5f;
        float wallHeight = bounds.y;
        float wallThickness = 0.5f;

        if (gapSide == WallSide.Right)
            CreateWallWithGap(root.transform, center + new Vector3(halfX, 0f, 0f), wallThickness, wallHeight, bounds.z, gapWidth, gapHeight, true, "RightWall");
        else
            CreateWall(root.transform, center + new Vector3(halfX, wallHeight * 0.5f, 0f), new Vector3(wallThickness, wallHeight, bounds.z), "RightWall");

        if (gapSide == WallSide.Left)
            CreateWallWithGap(root.transform, center + new Vector3(-halfX, 0f, 0f), wallThickness, wallHeight, bounds.z, gapWidth, gapHeight, true, "LeftWall");
        else
            CreateWall(root.transform, center + new Vector3(-halfX, wallHeight * 0.5f, 0f), new Vector3(wallThickness, wallHeight, bounds.z), "LeftWall");

        if (gapSide == WallSide.Front)
            CreateWallWithGap(root.transform, center + new Vector3(0f, 0f, halfZ), wallThickness, wallHeight, bounds.x, gapWidth, gapHeight, false, "FrontWall");
        else
            CreateWall(root.transform, center + new Vector3(0f, wallHeight * 0.5f, halfZ), new Vector3(bounds.x, wallHeight, wallThickness), "FrontWall");

        if (gapSide == WallSide.Back)
            CreateWallWithGap(root.transform, center + new Vector3(0f, 0f, -halfZ), wallThickness, wallHeight, bounds.x, gapWidth, gapHeight, false, "BackWall");
        else
            CreateWall(root.transform, center + new Vector3(0f, wallHeight * 0.5f, -halfZ), new Vector3(bounds.x, wallHeight, wallThickness), "BackWall");
    }

    private static void CreateWallsWithTwoDoorGaps(Vector3 center, Vector3 bounds, string rootName, float gapWidth, float gapHeight)
    {
        GameObject root = new GameObject(rootName);
        float halfX = bounds.x * 0.5f;
        float halfZ = bounds.z * 0.5f;
        float wallHeight = bounds.y;
        float wallThickness = 0.5f;

        CreateWallWithGap(root.transform, center + new Vector3(halfX, 0f, 0f), wallThickness, wallHeight, bounds.z, gapWidth, gapHeight, true, "RightWall");
        CreateWallWithGap(root.transform, center + new Vector3(-halfX, 0f, 0f), wallThickness, wallHeight, bounds.z, gapWidth, gapHeight, true, "LeftWall");
        CreateWall(root.transform, center + new Vector3(0f, wallHeight * 0.5f, halfZ), new Vector3(bounds.x, wallHeight, wallThickness), "FrontWall");
        CreateWall(root.transform, center + new Vector3(0f, wallHeight * 0.5f, -halfZ), new Vector3(bounds.x, wallHeight, wallThickness), "BackWall");
    }

    private static void CreateWallWithGap(Transform parent, Vector3 wallCenterOnGround, float thickness, float height, float span, float gapWidth, float gapHeight, bool alongZ, string wallName)
    {
        GameObject root = new GameObject(wallName);
        if (parent != null) root.transform.SetParent(parent);

        float sideSpan = Mathf.Max(0.2f, (span - gapWidth) * 0.5f);
        float topHeight = Mathf.Max(0.2f, height - gapHeight);

        if (alongZ)
        {
            CreateWall(root.transform, wallCenterOnGround + new Vector3(0f, height * 0.5f, (gapWidth + sideSpan) * 0.5f), new Vector3(thickness, height, sideSpan), "SideA");
            CreateWall(root.transform, wallCenterOnGround + new Vector3(0f, height * 0.5f, -(gapWidth + sideSpan) * 0.5f), new Vector3(thickness, height, sideSpan), "SideB");
            CreateWall(root.transform, wallCenterOnGround + new Vector3(0f, gapHeight + topHeight * 0.5f, 0f), new Vector3(thickness, topHeight, gapWidth), "Top");
        }
        else
        {
            CreateWall(root.transform, wallCenterOnGround + new Vector3((gapWidth + sideSpan) * 0.5f, height * 0.5f, 0f), new Vector3(sideSpan, height, thickness), "SideA");
            CreateWall(root.transform, wallCenterOnGround + new Vector3(-(gapWidth + sideSpan) * 0.5f, height * 0.5f, 0f), new Vector3(sideSpan, height, thickness), "SideB");
            CreateWall(root.transform, wallCenterOnGround + new Vector3(0f, gapHeight + topHeight * 0.5f, 0f), new Vector3(gapWidth, topHeight, thickness), "Top");
        }
    }

    private static void CreateWall(Transform parent, Vector3 position, Vector3 scale, string name)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent);
        wall.transform.position = position;
        wall.transform.localScale = scale;
    }

    private static void CreateCover(Vector3 position, string name)
    {
        GameObject cover = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cover.name = name;
        cover.transform.position = position;
        cover.transform.localScale = new Vector3(1.3f, 1.6f, 1.3f);
        PaintBright(cover, new Color(0.3f, 0.35f, 0.4f));
    }

    private static void CreateDirectionArrow(Vector3 position, Vector3 direction, string name)
    {
        GameObject arrowRoot = new GameObject(name);
        arrowRoot.transform.position = position;
        if (direction.sqrMagnitude > 0.001f)
            arrowRoot.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

        GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stem.name = "Stem";
        stem.transform.SetParent(arrowRoot.transform);
        stem.transform.localPosition = new Vector3(-0.65f, 0.03f, 0f);
        stem.transform.localScale = new Vector3(1.3f, 0.06f, 0.35f);
        PaintBright(stem, new Color(1f, 0.85f, 0.15f));

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
        head.name = "Head";
        head.transform.SetParent(arrowRoot.transform);
        head.transform.localPosition = new Vector3(0.2f, 0.03f, 0f);
        head.transform.localScale = new Vector3(0.65f, 0.06f, 0.65f);
        head.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
        PaintBright(head, new Color(1f, 0.7f, 0.05f));

        Collider c1 = stem.GetComponent<Collider>();
        if (c1 != null) Object.DestroyImmediate(c1);
        Collider c2 = head.GetComponent<Collider>();
        if (c2 != null) Object.DestroyImmediate(c2);
    }

    private static void PaintBright(GameObject go, Color color)
    {
        Renderer r = go.GetComponent<Renderer>();
        if (r == null) return;
        r.material.color = color;
    }

    private static void PaintInteractableCopyable(GameObject go)
    {
        // Shared color for objects player can copy/grab.
        PaintBright(go, new Color(0.1f, 0.9f, 1f));
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = path.Substring(0, path.LastIndexOf('/'));
        string name = path.Substring(path.LastIndexOf('/') + 1);
        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
#endif
