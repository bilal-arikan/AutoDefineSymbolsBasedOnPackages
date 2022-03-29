using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager.UI;
using UnityEngine;

namespace Arikan.Editor
{
    public class AutoSymbolDefiner
    {
        static ListRequest listRequest;
        private static bool registeredToCompilationPipeline = false;
        private static bool isRunningDetectAllPackageSymbols => listRequest != null;


        // [UnityEditor.Callbacks.DidReloadScripts]
        private static void RegisterWithDidReloadScripts()
        {
            if (registeredToCompilationPipeline)
            {
                // Debug.Log("RegisterWithDidReloadScripts: Already registered to compilation pipeline");
                return;
            }
            void OnCompilationStarted(object obj)
            {
                // Debug.Log("OnCompilationStarted " + obj);
                DetectAllPackageSymbols();
            }
            // Debug.Log("RegisterWithDidReloadScripts");
            UnityEditor.Compilation.CompilationPipeline.compilationStarted += OnCompilationStarted;
            registeredToCompilationPipeline = true;
        }
        [UnityEditor.InitializeOnLoadMethod]
        private static void RegisterWithInitializeOnLoadMethod()
        {
            DetectAllPackageSymbols();
            Events.registeringPackages += OnRegisteringPackages;
        }

        [MenuItem("Tools/AutoSymbolDefiner/Detect All Defines")]
        public static void DetectAllPackageSymbols()
        {
            if (isRunningDetectAllPackageSymbols)
            {
                Debug.Log("DetectAllPackageSymbols: already running");
                return;
            }
            // Debug.Log("DetectAllPackageSymbols");
            // Events.registeringPackages += OnRegisteredPackages;
            listRequest = UnityEditor.PackageManager.Client.List(true, true);
            EditorApplication.update += AutoSymbolDefinerProgress;
        }

        static void AutoSymbolDefinerProgress()
        {
            // Debug.Log("AutoSymbolDefinerProgress");
            if (listRequest != null && listRequest.IsCompleted)
            {
                EditorApplication.update -= AutoSymbolDefinerProgress;
                if (listRequest.Status >= StatusCode.Failure)
                {
                    Debug.LogError(listRequest.Error.message);
                    listRequest = null;
                    return;
                }

                DefineAllSymbols(listRequest.Result);

                // Debug.Log("AutoSymbolDefinerProgress Completed");
                listRequest = null;
            }
        }

        private static void OnRegisteringPackages(PackageRegistrationEventArgs obj)
        {
            // Debug.Log("Registered packages changed : " + obj.added.Count + " added, " + obj.removed.Count + " removed");
            AddSymbol(obj.added.ToArray());
            RemoveSymbol(obj.removed.ToArray());
        }

        public static bool AddSymbol(params UnityEditor.PackageManager.PackageInfo[] packages)
        {
            var defaultSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            var symbols = defaultSymbols;
            foreach (var package in packages)
            {
                var symbol = package.name.Replace(".", "_").Replace("-", "_").ToUpper(new System.Globalization.CultureInfo("en-US", false));
                if (!symbols.Contains(symbol))
                {
                    symbols += ";" + symbol;
                }
            }
            if (symbols != defaultSymbols)
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, symbols);
                return true;
            }
            return false;
        }

        public static bool RemoveSymbol(params UnityEditor.PackageManager.PackageInfo[] packages)
        {
            var defaultSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            var symbols = defaultSymbols;
            foreach (var package in packages)
            {
                var symbol = package.name.Replace(".", "_").Replace("-", "_").ToUpper(new System.Globalization.CultureInfo("en-US", false));
                if (symbols.Contains(symbol))
                {
                    symbols = symbols.Replace(symbol, "");
                }
            }
            if (symbols != defaultSymbols)
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, symbols);
                return true;
            }
            return false;
        }

        public static void DefineAllSymbols(PackageCollection coll)
        {
            foreach (var pck in coll)
            {
                var direct = pck.source
                        != PackageSource.BuiltIn && pck.source
                        != PackageSource.Embedded;
                if (direct)
                {
                    // // Debug.Log("Package Adding: " + pck.name);
                    AddSymbol(pck);
                }
                else
                {
                    // // Debug.Log("Package Skipping: " + pck.name);
                }
            }
        }
    }

}

