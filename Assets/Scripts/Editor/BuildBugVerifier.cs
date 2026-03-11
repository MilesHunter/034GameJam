#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

public static class BuildBugVerifier {
    [MenuItem("034GameJam/Verify Build Bug Fix")]
    public static void Run() {
        EnsureBallLayerExists();
        Test_AttachToExistingBall_NoSpawn();
        Test_SpawnAutoBall_WhenNoNearbyBall();

        Debug.Log("[BuildBugVerifier] PASS");
    }

    static void EnsureBallLayerExists() {
        int ballLayer = LayerMask.NameToLayer("Ball");
        if (ballLayer < 0)
            throw new InvalidOperationException("Layer 'Ball' does not exist.");
    }

    static void Test_AttachToExistingBall_NoSpawn() {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        (GameManager gm, Stick stick, Transform freeEnd) = CreateManagerAndStick();

        GameObject existingGo = CreateBall("ExistingBall", freeEnd.position);
        Ball existingBall = existingGo.GetComponent<Ball>();
        Vector3 existingPosBefore = existingGo.transform.position;
        int beforeBallCount = UnityEngine.Object.FindObjectsOfType<Ball>(true).Length;

        gm.AutoSpawnBallOnStickFreeEnd(stick);

        int afterBallCount = UnityEngine.Object.FindObjectsOfType<Ball>(true).Length;
        if (afterBallCount != beforeBallCount)
            throw new InvalidOperationException($"Expected no new ball, but ball count changed {beforeBallCount} -> {afterBallCount}.");

        if (GameObject.Find("AutoBall") != null)
            throw new InvalidOperationException("Expected no 'AutoBall' to be created, but found one.");

        bool attached = stick.endpointA == existingBall || stick.endpointB == existingBall;
        if (!attached)
            throw new InvalidOperationException("Expected stick to attach to existing ball, but it did not.");

        Vector3 existingPosAfter = existingGo.transform.position;
        if ((existingPosAfter - existingPosBefore).sqrMagnitude > 0.0000001f)
            throw new InvalidOperationException($"Expected existing ball position unchanged, but moved {existingPosBefore} -> {existingPosAfter}.");
    }

    static void Test_SpawnAutoBall_WhenNoNearbyBall() {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        (GameManager gm, Stick stick, Transform freeEnd) = CreateManagerAndStick();
        int beforeBallCount = UnityEngine.Object.FindObjectsOfType<Ball>(true).Length;

        gm.AutoSpawnBallOnStickFreeEnd(stick);

        int afterBallCount = UnityEngine.Object.FindObjectsOfType<Ball>(true).Length;
        if (afterBallCount != beforeBallCount + 1)
            throw new InvalidOperationException($"Expected one new ball, but ball count changed {beforeBallCount} -> {afterBallCount}.");

        GameObject autoGo = GameObject.Find("AutoBall");
        if (autoGo == null)
            throw new InvalidOperationException("Expected 'AutoBall' to be created, but it was not found.");

        if (!autoGo.TryGetComponent(out Ball autoBall))
            throw new InvalidOperationException("Expected 'AutoBall' to have a Ball component.");

        bool attached = stick.endpointA == autoBall || stick.endpointB == autoBall;
        if (!attached)
            throw new InvalidOperationException("Expected stick to attach to AutoBall, but it did not.");

        if (!autoBall.connectedSticks.Contains(stick))
            throw new InvalidOperationException("Expected AutoBall.connectedSticks to include stick, but it did not.");
    }

    static (GameManager gm, Stick stick, Transform freeEnd) CreateManagerAndStick() {
        GameObject gmGo = new GameObject("GameManager");
        GameManager gm = gmGo.AddComponent<GameManager>();
        GameManager.Instance = gm;

        GameObject stickGo = new GameObject("Stick");
        stickGo.transform.position = Vector3.zero;
        stickGo.transform.localScale = new Vector3(0.3f, 2f, 1f);

        stickGo.AddComponent<BoxCollider2D>();
        Rigidbody2D rb = stickGo.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        Stick stick = stickGo.AddComponent<Stick>();
        stick.rb = rb;

        GameObject endA = new GameObject("EndA");
        endA.transform.SetParent(stickGo.transform, false);
        endA.transform.localPosition = new Vector3(0f, 0.5f, 0f);

        GameObject endB = new GameObject("EndB");
        endB.transform.SetParent(stickGo.transform, false);
        endB.transform.localPosition = new Vector3(0f, -0.5f, 0f);

        stick.Initialize(endA.transform, endB.transform);
        Transform freeEnd = stick.FreeEnd;
        if (freeEnd == null)
            throw new InvalidOperationException("Expected stick.FreeEnd to be non-null.");

        return (gm, stick, freeEnd);
    }

    static GameObject CreateBall(string name, Vector3 position) {
        int ballLayer = LayerMask.NameToLayer("Ball");
        GameObject go = new GameObject(name);
        go.layer = ballLayer;
        go.transform.position = position;
        go.transform.localScale = Vector3.one * 0.8f;
        go.AddComponent<CircleCollider2D>();
        AllocatableBall ball = go.AddComponent<AllocatableBall>();
        ball.isFixed = false;
        return go;
    }
}
#endif
