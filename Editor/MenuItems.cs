﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Cognitive3D
{
    public class MenuItems
    {
        [MenuItem("Cognitive3D/Select Cognitive3D Analytics Manager", priority = 0)]
        static void Cognitive3DManager()
        {
            var found = Object.FindObjectOfType<Cognitive3D_Manager>();
            if (found != null)
            {
                Selection.activeGameObject = found.gameObject;
                return;
            }
            else
            {
                EditorCore.SpawnManager(EditorCore.DisplayValue(DisplayKey.ManagerName));
            }
        }

        [MenuItem("Cognitive3D/Select Active Session View Canvas", priority = 3)]
        static void SelectSessionView()
        {
            var found = Object.FindObjectOfType<ActiveSession.ActiveSessionView>();
            if (found != null)
            {
                Selection.activeGameObject = found.gameObject;
                return;
            }

            string[] guids = UnityEditor.AssetDatabase.FindAssets("t: Prefab activesessionview");
            if (guids.Length > 0)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0])));
                Selection.activeGameObject = instance;
                var asv = instance.GetComponent<ActiveSession.ActiveSessionView>();
                ActiveSession.ActiveSessionViewEditor.SetCameraTarget(asv);
                Undo.RegisterCreatedObjectUndo(instance, "Added Active Session View");
            }
        }

        [MenuItem("Cognitive3D/Open Web Dashboard...", priority = 5)]
        static void Cognitive3DDashboard()
        {
            Application.OpenURL("https://" + Cognitive3D_Preferences.Instance.Dashboard);
        }

        [MenuItem("Cognitive3D/Check for Updates...", priority = 10)]
        static void CognitiveCheckUpdates()
        {
            EditorCore.ForceCheckUpdates();
        }

        [MenuItem("Cognitive3D/Scene Setup And Upload", priority = 55)]
        static void Cognitive3DSceneSetup()
        {
            //open window
            InitWizard.Init();
        }

        [MenuItem("Cognitive3D/360 Setup", priority = 56)]
        static void Cognitive3D360Setup()
        {
            //open window
            Setup360Window.Init();
        }

        [MenuItem("Cognitive3D/Debug Information", priority = 58)]
        static void CognitiveDebugWindow()
        {
            //open window
            DebugInformationWindow.Init();
        }

        [MenuItem("Cognitive3D/Manage Dynamic Objects", priority = 60)]
        static void Cognitive3DManageDynamicObjects()
        {
            //open window
            ManageDynamicObjects.Init();
        }

        [MenuItem("Cognitive3D/Preferences", priority = 65)]
        static void Cognitive3DOptions()
        {
            //select asset
            Selection.activeObject = EditorCore.GetPreferences();
        }

        [MenuItem("Cognitive3D/Fetch Media from Dashboard", priority = 110)]
        static void RefreshMediaSources()
        {
            EditorCore.RefreshMediaSources();
        }
    }
}